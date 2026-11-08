using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Enums;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.Services;

public class UpdateSettingsServiceTests
{
    private readonly ISettingRepository _settingRepository = Substitute.For<ISettingRepository>();
    private readonly IRingtoneCatalogService _ringtoneCatalogService = Substitute.For<IRingtoneCatalogService>();
    private readonly SettingStore _store;
    private readonly UpdateSettingsService _service;
    private SettingEntity? _updatedSetting;

    public UpdateSettingsServiceTests()
    {
        SetStoredSettings();
        _settingRepository.UpdateAsync(Arg.Do<SettingEntity>(setting => _updatedSetting = setting));
        _store = new SettingStore(_settingRepository, _ringtoneCatalogService);
        _service = new UpdateSettingsService(_store);
    }

    [Fact]
    public void DismissedUpdateVersion_NothingLoaded_IsNull()
    {
        Assert.Null(_service.DismissedUpdateVersion);
    }

    [Fact]
    public async Task SetDismissedUpdateVersionAsync_Version_IsPersistedAndExposed()
    {
        SetStoredSettings(CreateSetting());
        await _store.InitializeAsync();

        await _service.SetDismissedUpdateVersionAsync("1.4.0");

        Assert.Equal("1.4.0", _updatedSetting!.DismissedUpdateVersion);
        Assert.Equal("1.4.0", _service.DismissedUpdateVersion);
    }

    [Fact]
    public async Task SetDismissedUpdateVersionAsync_SameVersion_DoesNotWriteAgain()
    {
        var setting = CreateSetting();
        setting.DismissedUpdateVersion = "1.4.0";
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        await _service.SetDismissedUpdateVersionAsync("1.4.0");

        Assert.Null(_updatedSetting);
    }

    [Fact]
    public async Task SetDismissedUpdateVersionAsync_NewerVersion_ReplacesTheStoredOne()
    {
        var setting = CreateSetting();
        setting.DismissedUpdateVersion = "1.4.0";
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        await _service.SetDismissedUpdateVersionAsync("1.5.0");

        Assert.Equal("1.5.0", _updatedSetting!.DismissedUpdateVersion);
        Assert.Equal("1.5.0", _service.DismissedUpdateVersion);
    }

    [Fact]
    public async Task SetDismissedUpdateVersionAsync_NoStoredSetting_DoesNothing()
    {
        await _service.SetDismissedUpdateVersionAsync("1.4.0");

        Assert.Null(_service.DismissedUpdateVersion);
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
