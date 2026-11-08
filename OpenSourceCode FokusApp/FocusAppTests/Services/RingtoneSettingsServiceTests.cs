using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Enums;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.Services;

public class RingtoneSettingsServiceTests
{
    private readonly ISettingRepository _settingRepository = Substitute.For<ISettingRepository>();
    private readonly IRingtoneCatalogService _ringtoneCatalogService = Substitute.For<IRingtoneCatalogService>();
    private readonly SettingStore _store;
    private readonly RingtoneSettingsService _service;
    private SettingEntity? _updatedSetting;

    public RingtoneSettingsServiceTests()
    {
        SetStoredSettings();
        _settingRepository.UpdateAsync(Arg.Do<SettingEntity>(setting => _updatedSetting = setting));
        _store = new SettingStore(_settingRepository, _ringtoneCatalogService);
        _service = new RingtoneSettingsService(_store, _ringtoneCatalogService);
    }

    [Fact]
    public async Task InitializeAsync_NoStoredSettings_LeavesNoSelectedRingtone()
    {
        await _store.InitializeAsync();

        var ringtone = await _service.GetSelectedRingtoneAsync();
        Assert.Null(ringtone);
    }

    [Fact]
    public async Task GetSelectedRingtoneAsync_RingtoneInCatalog_ReturnsCatalogRingtone()
    {
        var catalogRingtone = CreateRingtone("Raw/Katalog.mp3");
        var storedRingtone = new RingtonesEntity { Id = catalogRingtone.Id, RingtoneFilePath = "Raw/Gespeichert.mp3" };
        var setting = CreateSetting(storedRingtone, (int)AppLanguage.German);
        _ringtoneCatalogService.FindById(catalogRingtone.Id).Returns(catalogRingtone);
        SetStoredSettings(setting);
        await _store.InitializeAsync();

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
        await _store.InitializeAsync();

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
        await _store.InitializeAsync();

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
        await _store.InitializeAsync();

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
        await _store.InitializeAsync();

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
        await _store.InitializeAsync();

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
        await _store.InitializeAsync();

        await _service.SetSelectedRingtoneAsync(ringtone.Id);

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
