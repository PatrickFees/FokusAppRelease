using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Enums;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.Services;

public class TimerAlertModeSettingsServiceTests
{
    private readonly ISettingRepository _settingRepository = Substitute.For<ISettingRepository>();
    private readonly IRingtoneCatalogService _ringtoneCatalogService = Substitute.For<IRingtoneCatalogService>();
    private readonly SettingStore _store;
    private readonly TimerAlertModeSettingsService _service;
    private SettingEntity? _updatedSetting;

    public TimerAlertModeSettingsServiceTests()
    {
        SetStoredSettings();
        _settingRepository.UpdateAsync(Arg.Do<SettingEntity>(setting => _updatedSetting = setting));
        _store = new SettingStore(_settingRepository, _ringtoneCatalogService);
        _service = new TimerAlertModeSettingsService(_store);
    }

    [Fact]
    public void TimerAlertMode_NothingLoaded_IsSound()
    {
        Assert.Equal(TimerAlertMode.Sound, _service.TimerAlertMode);
    }

    [Theory]
    [InlineData(0, TimerAlertMode.None)]
    [InlineData(1, TimerAlertMode.Sound)]
    [InlineData(2, TimerAlertMode.Vibration)]
    [InlineData(3, TimerAlertMode.Sound | TimerAlertMode.Vibration)]
    [InlineData(7, TimerAlertMode.Sound | TimerAlertMode.Vibration)]
    [InlineData(8, TimerAlertMode.AdaptToRingerMode)]
    [InlineData(9, TimerAlertMode.Sound | TimerAlertMode.AdaptToRingerMode)]
    [InlineData(15, TimerAlertMode.Sound | TimerAlertMode.Vibration | TimerAlertMode.AdaptToRingerMode)]
    [InlineData(99, TimerAlertMode.Sound | TimerAlertMode.Vibration)]
    public async Task TimerAlertMode_Stored_IsExposedAndUnknownBitsAreMasked(int stored, TimerAlertMode expected)
    {
        var setting = CreateSetting(CreateRingtone("Raw/Eins.mp3"));
        setting.TimerAlertMode = stored;
        SetStoredSettings(setting);

        await _store.InitializeAsync();

        Assert.Equal(expected, _service.TimerAlertMode);
    }

    [Fact]
    public async Task InitializeAsync_StoredSettings_RaisesTimerAlertModeChanged()
    {
        SetStoredSettings(CreateSetting(CreateRingtone("Raw/Eins.mp3")));
        var raised = 0;
        _service.TimerAlertModeChanged += (_, _) => raised++;

        await _store.InitializeAsync();

        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task SetTimerAlertModeAsync_CombinedMode_IsPersistedAndRaisesChanged()
    {
        SetStoredSettings(CreateSetting(CreateRingtone("Raw/Eins.mp3")));
        await _store.InitializeAsync();
        var raised = 0;
        _service.TimerAlertModeChanged += (_, _) => raised++;

        await _service.SetTimerAlertModeAsync(TimerAlertMode.Vibration | TimerAlertMode.AdaptToRingerMode);

        Assert.Equal((int)(TimerAlertMode.Vibration | TimerAlertMode.AdaptToRingerMode), _updatedSetting!.TimerAlertMode);
        Assert.Equal(TimerAlertMode.Vibration | TimerAlertMode.AdaptToRingerMode, _service.TimerAlertMode);
        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task SetTimerAlertModeAsync_SoundWithAdaptToRingerMode_IsPersistedAndRaisesChanged()
    {
        SetStoredSettings(CreateSetting(CreateRingtone("Raw/Eins.mp3")));
        await _store.InitializeAsync();
        var raised = 0;
        _service.TimerAlertModeChanged += (_, _) => raised++;

        await _service.SetTimerAlertModeAsync(TimerAlertMode.Sound | TimerAlertMode.AdaptToRingerMode);

        Assert.Equal((int)(TimerAlertMode.Sound | TimerAlertMode.AdaptToRingerMode), _updatedSetting!.TimerAlertMode);
        Assert.Equal(TimerAlertMode.Sound | TimerAlertMode.AdaptToRingerMode, _service.TimerAlertMode);
        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task SetTimerAlertModeAsync_UnknownBits_AreMaskedBeforePersisting()
    {
        SetStoredSettings(CreateSetting(CreateRingtone("Raw/Eins.mp3")));
        await _store.InitializeAsync();

        await _service.SetTimerAlertModeAsync((TimerAlertMode)99);

        Assert.Equal((int)(TimerAlertMode.Sound | TimerAlertMode.Vibration), _updatedSetting!.TimerAlertMode);
    }

    [Fact]
    public async Task SetTimerAlertModeAsync_SameMode_StoresNothing()
    {
        var setting = CreateSetting(CreateRingtone("Raw/Eins.mp3"));
        setting.TimerAlertMode = (int)TimerAlertMode.Sound;
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        await _service.SetTimerAlertModeAsync(TimerAlertMode.Sound);

        await _settingRepository.DidNotReceive().UpdateAsync(Arg.Any<SettingEntity>());
    }

    [Fact]
    public async Task SetTimerAlertModeAsync_None_IsAllowedAndPersisted()
    {
        SetStoredSettings(CreateSetting(CreateRingtone("Raw/Eins.mp3")));
        await _store.InitializeAsync();

        await _service.SetTimerAlertModeAsync(TimerAlertMode.None);

        Assert.Equal(0, _updatedSetting!.TimerAlertMode);
        Assert.Equal(TimerAlertMode.None, _service.TimerAlertMode);
    }

    private static RingtonesEntity CreateRingtone(string filePath)
    {
        return new RingtonesEntity
        {
            Id = Guid.NewGuid(),
            RingtoneFilePath = filePath
        };
    }

    private static SettingEntity CreateSetting(RingtonesEntity ringtone)
    {
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
