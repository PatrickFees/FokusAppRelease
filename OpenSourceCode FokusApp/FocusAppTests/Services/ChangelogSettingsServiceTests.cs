using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Enums;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.Services;

public class ChangelogSettingsServiceTests
{
    private readonly ISettingRepository _settingRepository = Substitute.For<ISettingRepository>();
    private readonly IRingtoneCatalogService _ringtoneCatalogService = Substitute.For<IRingtoneCatalogService>();
    private readonly SettingStore _store;
    private readonly ChangelogSettingsService _service;
    private SettingEntity? _updatedSetting;

    public ChangelogSettingsServiceTests()
    {
        SetStoredSettings();
        _settingRepository.UpdateAsync(Arg.Do<SettingEntity>(setting => _updatedSetting = setting));
        _store = new SettingStore(_settingRepository, _ringtoneCatalogService);
        _service = new ChangelogSettingsService(_store);
    }

    [Fact]
    public void LastSeenChangelogVersion_NothingLoaded_IsNull()
    {
        Assert.Null(_service.LastSeenChangelogVersion);
    }

    [Fact]
    public async Task SetLastSeenChangelogVersionAsync_Version_IsPersistedAndExposed()
    {
        SetStoredSettings(CreateSetting());
        await _store.InitializeAsync();

        await _service.SetLastSeenChangelogVersionAsync("1.5.0");

        Assert.Equal("1.5.0", _updatedSetting!.LastSeenChangelogVersion);
        Assert.Equal("1.5.0", _service.LastSeenChangelogVersion);
    }

    [Fact]
    public async Task SetLastSeenChangelogVersionAsync_SameVersion_DoesNotWriteAgain()
    {
        var setting = CreateSetting();
        setting.LastSeenChangelogVersion = "1.5.0";
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        await _service.SetLastSeenChangelogVersionAsync("1.5.0");

        Assert.Null(_updatedSetting);
    }

    [Fact]
    public async Task SetLastSeenChangelogVersionAsync_NewerVersion_ReplacesTheStoredOne()
    {
        var setting = CreateSetting();
        setting.LastSeenChangelogVersion = "1.4.0";
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        await _service.SetLastSeenChangelogVersionAsync("1.5.0");

        Assert.Equal("1.5.0", _updatedSetting!.LastSeenChangelogVersion);
        Assert.Equal("1.5.0", _service.LastSeenChangelogVersion);
    }

    [Fact]
    public async Task SetLastSeenChangelogVersionAsync_NoStoredSetting_DoesNothing()
    {
        await _service.SetLastSeenChangelogVersionAsync("1.5.0");

        Assert.Null(_service.LastSeenChangelogVersion);
    }

    private static SettingEntity CreateSetting()
    {
        var ringtone = new RingtonesEntity { Id = Guid.NewGuid(), RingtoneFilePath = "Raw/Eins.mp3" };
        return new SettingEntity
        {
            Id = Guid.NewGuid(),
            Theme = (int)AppColorTheme.Blue,
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
