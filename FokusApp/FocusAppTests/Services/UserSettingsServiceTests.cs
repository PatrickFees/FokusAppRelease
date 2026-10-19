using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Enums;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.Services;

public class UserSettingsServiceTests
{
    private readonly ISettingRepository _settingRepository = Substitute.For<ISettingRepository>();
    private readonly IRingtoneCatalogService _ringtoneCatalogService = Substitute.For<IRingtoneCatalogService>();
    private readonly UserSettingsService _service;
    private SettingEntity? _updatedSetting;

    public UserSettingsServiceTests()
    {
        SetStoredSettings();
        _settingRepository.UpdateAsync(Arg.Do<SettingEntity>(setting => _updatedSetting = setting));
        _service = new UserSettingsService(_settingRepository, _ringtoneCatalogService);
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
        await _service.InitializeAsync();

        Assert.Equal(expected, _service.CurrentLanguage);
    }

    [Fact]
    public async Task InitializeAsync_NoStoredSettings_LeavesNoSelectedRingtone()
    {
        await _service.InitializeAsync();

        var ringtone = await _service.GetSelectedRingtoneAsync();
        Assert.Null(ringtone);
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_LoadsOnlyOnce()
    {
        await _service.InitializeAsync();
        await _service.InitializeAsync();

        await _settingRepository.Received(1).GetAllAsync();
    }

    [Fact]
    public async Task ReloadAsync_ChangedRepository_ReplacesCachedSetting()
    {
        var ringtone = CreateRingtone("Raw/Eins.mp3");
        var germanSetting = CreateSetting(ringtone, (int)AppLanguage.German);
        SetStoredSettings(germanSetting);
        await _service.InitializeAsync();
        var englishSetting = CreateSetting(ringtone, (int)AppLanguage.English);
        SetStoredSettings(englishSetting);

        await _service.ReloadAsync();

        Assert.Equal(AppLanguage.English, _service.CurrentLanguage);
    }

    [Fact]
    public async Task ReloadAsync_WithoutPriorInitialization_LoadsAndPreventsSecondInitialLoad()
    {
        await _service.ReloadAsync();
        await _service.InitializeAsync();

        await _settingRepository.Received(1).GetAllAsync();
    }

    [Fact]
    public async Task GetSelectedRingtoneAsync_RingtoneInCatalog_ReturnsCatalogRingtone()
    {
        var catalogRingtone = CreateRingtone("Raw/Katalog.mp3");
        var storedRingtone = new RingtonesEntity { Id = catalogRingtone.Id, RingtoneFilePath = "Raw/Gespeichert.mp3" };
        var setting = CreateSetting(storedRingtone, (int)AppLanguage.German);
        _ringtoneCatalogService.FindById(catalogRingtone.Id).Returns(catalogRingtone);
        SetStoredSettings(setting);
        await _service.InitializeAsync();

        var result = await _service.GetSelectedRingtoneAsync();

        Assert.NotNull(result);
        Assert.Equal("Raw/Katalog.mp3", result.RingtoneFilePath);
    }

    [Fact]
    public async Task GetSelectedRingtoneAsync_RingtoneNotInCatalog_FallsBackToStoredRingtone()
    {
        var storedRingtone = CreateRingtone("Raw/Gespeichert.mp3");
        var setting = CreateSetting(storedRingtone, (int)AppLanguage.German);
        SetStoredSettings(setting);
        await _service.InitializeAsync();

        var result = await _service.GetSelectedRingtoneAsync();

        Assert.NotNull(result);
        Assert.Equal(storedRingtone.Id, result.Id);
        Assert.Equal("Raw/Gespeichert.mp3", result.RingtoneFilePath);
    }

    [Fact]
    public async Task GetSelectedRingtoneAsync_RingtoneMissingEverywhere_ReturnsPlaceholderWithIdOnly()
    {
        var ringtoneId = Guid.NewGuid();
        var setting = new SettingEntity { Id = Guid.NewGuid(), RingtoneId = ringtoneId, Ringtone = null! };
        SetStoredSettings(setting);
        await _service.InitializeAsync();

        var result = await _service.GetSelectedRingtoneAsync();

        Assert.NotNull(result);
        Assert.Equal(ringtoneId, result.Id);
        Assert.Null(result.RingtoneFilePath);
    }

    [Fact]
    public async Task GetSelectedRingtoneAsync_ModifyingResult_DoesNotChangeCache()
    {
        var ringtone = CreateRingtone("Raw/Eins.mp3");
        var setting = CreateSetting(ringtone, (int)AppLanguage.German);
        SetStoredSettings(setting);
        await _service.InitializeAsync();

        var firstResult = await _service.GetSelectedRingtoneAsync();
        firstResult!.RingtoneFilePath = "Raw/Geändert.mp3";

        var secondResult = await _service.GetSelectedRingtoneAsync();
        Assert.Equal("Raw/Eins.mp3", secondResult!.RingtoneFilePath);
    }

    [Fact]
    public async Task SetSelectedRingtoneAsync_KnownRingtone_UpdatesRepositoryAndCache()
    {
        var oldRingtone = CreateRingtone("Raw/Alt.mp3");
        var newRingtone = CreateRingtone("Raw/Neu.mp3");
        var setting = CreateSetting(oldRingtone, (int)AppLanguage.German);
        setting.Theme = 2;
        _ringtoneCatalogService.FindById(newRingtone.Id).Returns(newRingtone);
        SetStoredSettings(setting);
        await _service.InitializeAsync();

        await _service.SetSelectedRingtoneAsync(newRingtone.Id);

        Assert.NotNull(_updatedSetting);
        Assert.Equal(setting.Id, _updatedSetting.Id);
        Assert.Equal(2, _updatedSetting.Theme);
        Assert.Equal((int)AppLanguage.German, _updatedSetting.Language);
        Assert.Equal(newRingtone.Id, _updatedSetting.RingtoneId);
        var selected = await _service.GetSelectedRingtoneAsync();
        Assert.Equal(newRingtone.Id, selected?.Id);
        Assert.Equal("Raw/Neu.mp3", selected?.RingtoneFilePath);
    }

    [Fact]
    public async Task SetSelectedRingtoneAsync_UnknownRingtone_DoesNotUpdateAnything()
    {
        var ringtone = CreateRingtone("Raw/Eins.mp3");
        var setting = CreateSetting(ringtone, (int)AppLanguage.German);
        var unknownId = Guid.NewGuid();
        SetStoredSettings(setting);
        await _service.InitializeAsync();

        await _service.SetSelectedRingtoneAsync(unknownId);

        await _settingRepository.DidNotReceive().UpdateAsync(Arg.Any<SettingEntity>());
        var selected = await _service.GetSelectedRingtoneAsync();
        Assert.Equal(ringtone.Id, selected?.Id);
    }

    [Fact]
    public async Task SetSelectedRingtoneAsync_NoSettingLoaded_DoesNotUpdateAnything()
    {
        var ringtone = CreateRingtone("Raw/Eins.mp3");
        _ringtoneCatalogService.FindById(ringtone.Id).Returns(ringtone);
        await _service.InitializeAsync();

        await _service.SetSelectedRingtoneAsync(ringtone.Id);

        await _settingRepository.DidNotReceive().UpdateAsync(Arg.Any<SettingEntity>());
    }

    [Fact]
    public async Task SetLanguageAsync_DifferentLanguage_UpdatesRepositoryAndCache()
    {
        var ringtone = CreateRingtone("Raw/Eins.mp3");
        var setting = CreateSetting(ringtone, (int)AppLanguage.German);
        setting.Theme = 2;
        SetStoredSettings(setting);
        await _service.InitializeAsync();

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
        await _service.InitializeAsync();

        await _service.SetLanguageAsync(AppLanguage.German);

        await _settingRepository.DidNotReceive().UpdateAsync(Arg.Any<SettingEntity>());
    }

    [Fact]
    public async Task SetLanguageAsync_NoSettingLoaded_DoesNotUpdateRepository()
    {
        await _service.InitializeAsync();

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
        await _service.InitializeAsync();

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
        await _service.InitializeAsync();

        await _service.SetLanguageAsync(AppLanguage.English);

        var selected = await _service.GetSelectedRingtoneAsync();
        Assert.Equal(ringtone.Id, selected?.Id);
        Assert.Equal("Raw/Eins.mp3", selected?.RingtoneFilePath);
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
            Theme = 0,
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
