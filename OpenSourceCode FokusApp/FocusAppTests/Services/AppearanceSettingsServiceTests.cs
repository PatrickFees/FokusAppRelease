using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.Services;

public class AppearanceSettingsServiceTests
{
    private readonly ISettingRepository _settingRepository = Substitute.For<ISettingRepository>();
    private readonly IRingtoneCatalogService _ringtoneCatalogService = Substitute.For<IRingtoneCatalogService>();
    private readonly SettingStore _store;
    private readonly AppearanceSettingsService _service;
    private SettingEntity? _updatedSetting;

    public AppearanceSettingsServiceTests()
    {
        SetStoredSettings();
        _settingRepository.UpdateAsync(Arg.Do<SettingEntity>(setting => _updatedSetting = setting));
        _store = new SettingStore(_settingRepository, _ringtoneCatalogService);
        _service = new AppearanceSettingsService(_store);
    }

    [Fact]
    public void CurrentLanguage_BeforeInitialization_IsEnglish()
    {
        Assert.Equal(AppLanguage.English, _service.CurrentLanguage);
    }

    [Theory]
    [InlineData(0, AppLanguage.German)]
    [InlineData(1, AppLanguage.English)]
    [InlineData(99, AppLanguage.English)]
    [InlineData(-1, AppLanguage.English)]
    public async Task CurrentLanguage_StoredLanguageValue_IsNormalizedToKnownLanguage(int storedLanguage, AppLanguage expected)
    {
        var ringtone = CreateRingtone("Raw/Eins.mp3");
        var setting = CreateSetting(ringtone, storedLanguage);
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        Assert.Equal(expected, _service.CurrentLanguage);
    }

    [Fact]
    public async Task SetLanguageAsync_DifferentLanguage_UpdatesRepositoryAndCache()
    {
        var ringtone = CreateRingtone("Raw/Eins.mp3");
        var setting = CreateSetting(ringtone, (int)AppLanguage.German);
        setting.Theme = 2;
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        await _service.SetLanguageAsync(AppLanguage.English);

        Assert.NotNull(_updatedSetting);
        Assert.Equal(setting.Id, _updatedSetting.Id);
        Assert.Equal(2, _updatedSetting.Theme);
        Assert.Equal((int)AppLanguage.English, _updatedSetting.Language);
        Assert.Equal(ringtone.Id, _updatedSetting.RingtoneId);
        Assert.Equal(AppLanguage.English, _service.CurrentLanguage);
    }

    [Fact]
    public async Task SetLanguageAsync_SameLanguage_DoesNotUpdateRepository()
    {
        var ringtone = CreateRingtone("Raw/Eins.mp3");
        var setting = CreateSetting(ringtone, (int)AppLanguage.German);
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        await _service.SetLanguageAsync(AppLanguage.German);

        await _settingRepository.DidNotReceive().UpdateAsync(Arg.Any<SettingEntity>());
    }

    [Fact]
    public async Task SetLanguageAsync_NoSettingLoaded_DoesNotUpdateRepository()
    {
        await _store.InitializeAsync();

        await _service.SetLanguageAsync(AppLanguage.German);

        await _settingRepository.DidNotReceive().UpdateAsync(Arg.Any<SettingEntity>());
    }

    [Fact]
    public async Task SetLanguageAsync_UnknownLanguageValue_IsStoredAsEnglish()
    {
        var ringtone = CreateRingtone("Raw/Eins.mp3");
        var setting = CreateSetting(ringtone, (int)AppLanguage.German);
        var unknownLanguage = (AppLanguage)99;
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        await _service.SetLanguageAsync(unknownLanguage);

        Assert.Equal((int)AppLanguage.English, _updatedSetting?.Language);
        Assert.Equal(AppLanguage.English, _service.CurrentLanguage);
    }

    [Fact]
    public async Task SetLanguageAsync_AfterChange_KeepsSelectedRingtone()
    {
        var ringtone = CreateRingtone("Raw/Eins.mp3");
        var setting = CreateSetting(ringtone, (int)AppLanguage.German);
        SetStoredSettings(setting);
        await _store.InitializeAsync();
        var ringtoneService = new RingtoneSettingsService(_store, _ringtoneCatalogService);

        await _service.SetLanguageAsync(AppLanguage.English);

        var selected = await ringtoneService.GetSelectedRingtoneAsync();
        Assert.Equal(ringtone.Id, selected?.Id);
        Assert.Equal("Raw/Eins.mp3", selected?.RingtoneFilePath);
    }

    [Fact]
    public async Task SetLanguageAsync_AfterRewardingChanged_KeepsRewarding()
    {
        SetStoredSettings(CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0));
        await _store.InitializeAsync();
        var rewardingService = new RewardingSettingsService(_store);
        await rewardingService.SetRewardingAsync(AppRewardingSettings.Default with { CompanionEnabled = false });

        await _service.SetLanguageAsync(AppLanguage.English);

        Assert.False(_updatedSetting!.CompanionEnabled);
        Assert.False(rewardingService.Rewarding.CompanionEnabled);
    }

    [Theory]
    [InlineData(0, AppColorTheme.Blue)]
    [InlineData(4, AppColorTheme.Blue)]
    [InlineData(1, AppColorTheme.Blue)]
    [InlineData(2, AppColorTheme.Blue)]
    [InlineData(5, AppColorTheme.Red)]
    [InlineData(6, AppColorTheme.Green)]
    [InlineData(7, AppColorTheme.Yellow)]
    [InlineData(99, AppColorTheme.Blue)]
    [InlineData(-1, AppColorTheme.Blue)]
    public async Task CurrentTheme_StoredThemeValue_IsNormalizedToKnownTheme(int storedTheme, AppColorTheme expected)
    {
        var setting = CreateSetting(CreateRingtone("Raw/Eins.mp3"), (int)AppLanguage.German);
        setting.Theme = storedTheme;
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        Assert.Equal(expected, _service.CurrentTheme);
    }

    [Fact]
    public async Task SetThemeAsync_NewTheme_PersistsThemeAndKeepsOtherSettings()
    {
        var ringtone = CreateRingtone("Raw/Eins.mp3");
        var setting = CreateSetting(ringtone, (int)AppLanguage.English);
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        await _service.SetThemeAsync(AppColorTheme.Pink);

        Assert.Equal(AppColorTheme.Pink, _service.CurrentTheme);
        Assert.Equal((int)AppColorTheme.Pink, _updatedSetting!.Theme);
        Assert.Equal((int)AppLanguage.English, _updatedSetting.Language);
        Assert.Equal(ringtone.Id, _updatedSetting.RingtoneId);
    }

    [Fact]
    public async Task SetThemeAsync_SameTheme_DoesNotWrite()
    {
        var setting = CreateSetting(CreateRingtone("Raw/Eins.mp3"), (int)AppLanguage.German);
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        await _service.SetThemeAsync(AppColorTheme.Blue);

        await _settingRepository.DidNotReceive().UpdateAsync(Arg.Any<SettingEntity>());
    }

    [Fact]
    public void CurrentTheme_BeforeInitialization_IsWhite()
    {
        Assert.Equal(AppColorTheme.Blue, _service.CurrentTheme);
    }

    [Theory]
    [InlineData(0, AppColorMode.Light)]
    [InlineData(1, AppColorMode.Dark)]
    [InlineData(2, AppColorMode.Light)]
    [InlineData(-1, AppColorMode.Light)]
    public async Task CurrentColorMode_StoredModeValue_IsNormalizedToKnownMode(int storedMode, AppColorMode expected)
    {
        var setting = CreateSetting(CreateRingtone("Raw/Eins.mp3"), (int)AppLanguage.German);
        setting.ColorMode = storedMode;
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        Assert.Equal(expected, _service.CurrentColorMode);
    }

    [Fact]
    public void CurrentColorMode_BeforeInitialization_IsLight()
    {
        Assert.Equal(AppColorMode.Light, _service.CurrentColorMode);
    }

    [Fact]
    public async Task SetColorModeAsync_NewMode_PersistsModeAndKeepsThemeAndOtherSettings()
    {
        var setting = CreateSetting(CreateRingtone("Raw/Eins.mp3"), (int)AppLanguage.English);
        setting.Theme = (int)AppColorTheme.Pink;
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        await _service.SetColorModeAsync(AppColorMode.Dark);

        Assert.Equal(AppColorMode.Dark, _service.CurrentColorMode);
        Assert.Equal((int)AppColorMode.Dark, _updatedSetting!.ColorMode);
        Assert.Equal((int)AppColorTheme.Pink, _updatedSetting.Theme);
        Assert.Equal((int)AppLanguage.English, _updatedSetting.Language);
        Assert.Equal(AppColorTheme.Pink, _service.CurrentTheme);
    }

    [Fact]
    public async Task SetThemeAsync_NewTheme_KeepsTheColorMode()
    {
        var setting = CreateSetting(CreateRingtone("Raw/Eins.mp3"), (int)AppLanguage.German);
        setting.ColorMode = (int)AppColorMode.Dark;
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        await _service.SetThemeAsync(AppColorTheme.Yellow);

        Assert.Equal((int)AppColorMode.Dark, _updatedSetting!.ColorMode);
        Assert.Equal(AppColorMode.Dark, _service.CurrentColorMode);
    }

    [Fact]
    public async Task SetColorModeAsync_SameMode_DoesNotWrite()
    {
        var setting = CreateSetting(CreateRingtone("Raw/Eins.mp3"), (int)AppLanguage.German);
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        await _service.SetColorModeAsync(AppColorMode.Light);

        await _settingRepository.DidNotReceive().UpdateAsync(Arg.Any<SettingEntity>());
    }

    [Fact]
    public async Task SetColorModeAsync_NoSettingLoaded_DoesNotWrite()
    {
        await _store.InitializeAsync();

        await _service.SetColorModeAsync(AppColorMode.Dark);

        await _settingRepository.DidNotReceive().UpdateAsync(Arg.Any<SettingEntity>());
    }

    [Fact]
    public async Task SetThemeAsync_NoSettingLoaded_DoesNotWrite()
    {
        await _store.InitializeAsync();

        await _service.SetThemeAsync(AppColorTheme.Green);

        await _settingRepository.DidNotReceive().UpdateAsync(Arg.Any<SettingEntity>());
    }

    private static RingtonesEntity CreateRingtone(string filePath)
    {
        return new RingtonesEntity
        {
            Id = Guid.NewGuid(),
            RingtoneFilePath = filePath
        };
    }

    private static SettingEntity CreateSetting(RingtonesEntity ringtone, int language)
    {
        return new SettingEntity
        {
            Id = Guid.NewGuid(),
            Theme = (int)AppColorTheme.Blue,
            Language = language,
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
