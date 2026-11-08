using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.Services;

public class CalendarAutoSyncSettingsServiceTests
{
    private readonly ISettingRepository _settingRepository = Substitute.For<ISettingRepository>();
    private readonly IRingtoneCatalogService _ringtoneCatalogService = Substitute.For<IRingtoneCatalogService>();
    private readonly ICalendarAutoSyncCalendarRepository _calendarRepository = Substitute.For<ICalendarAutoSyncCalendarRepository>();
    private readonly ICalendarAutoSyncBackgroundScheduler _backgroundScheduler = Substitute.For<ICalendarAutoSyncBackgroundScheduler>();
    private readonly SettingStore _store;
    private readonly CalendarAutoSyncSettingsService _service;
    private SettingEntity? _updatedSetting;

    public CalendarAutoSyncSettingsServiceTests()
    {
        SetStoredSettings(CreateSetting());
        _settingRepository.UpdateAsync(Arg.Do<SettingEntity>(setting => _updatedSetting = setting));
        _store = new SettingStore(_settingRepository, _ringtoneCatalogService);
        _service = new CalendarAutoSyncSettingsService(_store, _calendarRepository, _backgroundScheduler);
    }

    [Fact]
    public void Enabled_NothingLoaded_IsFalse()
    {
        Assert.False(_service.Enabled);
    }

    [Fact]
    public async Task Enabled_StoredAsTrue_IsExposed()
    {
        var setting = CreateSetting();
        setting.CalendarAutoSyncEnabled = true;
        SetStoredSettings(setting);

        await _store.InitializeAsync();

        Assert.True(_service.Enabled);
    }

    [Fact]
    public async Task LastRunAt_Stored_IsExposed()
    {
        var runAt = new DateTime(2026, 3, 15, 9, 0, 0);
        var setting = CreateSetting();
        setting.CalendarAutoSyncLastRunAt = runAt;
        SetStoredSettings(setting);

        await _store.InitializeAsync();

        Assert.Equal(runAt, _service.LastRunAt);
    }

    [Fact]
    public async Task SetEnabledAsync_True_PersistsAndEnablesBackgroundJob()
    {
        await _store.InitializeAsync();

        await _service.SetEnabledAsync(true);

        Assert.True(_updatedSetting!.CalendarAutoSyncEnabled);
        Assert.True(_service.Enabled);
        _backgroundScheduler.Received(1).Enable();
        _backgroundScheduler.DidNotReceive().Disable();
    }

    [Fact]
    public async Task SetEnabledAsync_False_PersistsAndDisablesBackgroundJob()
    {
        var setting = CreateSetting();
        setting.CalendarAutoSyncEnabled = true;
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        await _service.SetEnabledAsync(false);

        Assert.False(_updatedSetting!.CalendarAutoSyncEnabled);
        _backgroundScheduler.Received(1).Disable();
        _backgroundScheduler.DidNotReceive().Enable();
    }

    [Fact]
    public async Task SetEnabledAsync_SameValue_StoresNothingButStillTogglesScheduler()
    {
        await _store.InitializeAsync();

        await _service.SetEnabledAsync(false);

        await _settingRepository.DidNotReceive().UpdateAsync(Arg.Any<SettingEntity>());
        _backgroundScheduler.Received(1).Disable();
    }

    [Fact]
    public async Task GetSelectedCalendarIdsAsync_DelegatesToRepository()
    {
        _calendarRepository.GetAllAsync().Returns([
            new CalendarAutoSyncCalendarEntity { Id = Guid.NewGuid(), CalendarId = "calendar-1" }
        ]);

        var calendarIds = await _service.GetSelectedCalendarIdsAsync();

        Assert.Equal(["calendar-1"], calendarIds);
    }

    [Fact]
    public async Task SetSelectedCalendarIdsAsync_DelegatesToRepository()
    {
        await _service.SetSelectedCalendarIdsAsync(["calendar-1", "calendar-2"]);

        var expectedCalendarIds = new List<string> { "calendar-1", "calendar-2" };
        await _calendarRepository.Received(1).ReplaceAllAsync(Arg.Is<IReadOnlyList<string>>(
            ids => ids.SequenceEqual(expectedCalendarIds)));
    }

    [Fact]
    public async Task RecordRunAsync_PersistsTimestamp()
    {
        await _store.InitializeAsync();
        var runAt = new DateTime(2026, 3, 15, 10, 30, 0);

        await _service.RecordRunAsync(runAt);

        Assert.Equal(runAt, _updatedSetting!.CalendarAutoSyncLastRunAt);
    }

    private static RingtonesEntity CreateRingtone(string filePath)
    {
        return new RingtonesEntity
        {
            Id = Guid.NewGuid(),
            RingtoneFilePath = filePath
        };
    }

    private SettingEntity CreateSetting()
    {
        var ringtone = CreateRingtone("Raw/Eins.mp3");
        return new SettingEntity
        {
            Id = Guid.NewGuid(),
            RingtoneId = ringtone.Id,
            Ringtone = ringtone
        };
    }

    private void SetStoredSettings(params SettingEntity[] settings)
    {
        var storedSettings = settings.ToList();
        _settingRepository.GetAllAsync().Returns(storedSettings);
    }
}
