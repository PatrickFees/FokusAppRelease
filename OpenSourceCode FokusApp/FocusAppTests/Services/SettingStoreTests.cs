using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Enums;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.Services;

public class SettingStoreTests
{
    private readonly ISettingRepository _settingRepository = Substitute.For<ISettingRepository>();
    private readonly IRingtoneCatalogService _ringtoneCatalogService = Substitute.For<IRingtoneCatalogService>();
    private readonly SettingStore _store;

    public SettingStoreTests()
    {
        SetStoredSettings();
        _store = new SettingStore(_settingRepository, _ringtoneCatalogService);
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_LoadsOnlyOnce()
    {
        await _store.InitializeAsync();
        await _store.InitializeAsync();

        await _settingRepository.Received(1).GetAllAsync();
    }

    [Fact]
    public async Task ReloadAsync_WithoutPriorInitialization_LoadsAndPreventsSecondInitialLoad()
    {
        await _store.ReloadAsync();
        await _store.InitializeAsync();

        await _settingRepository.Received(1).GetAllAsync();
    }

    [Fact]
    public async Task InitializeAsync_ConcurrentCalls_LoadsOnlyOnce()
    {
        var gate = new TaskCompletionSource<List<SettingEntity>>();
        _settingRepository.GetAllAsync().Returns(gate.Task);

        var first = _store.InitializeAsync();
        var second = _store.InitializeAsync();
        gate.SetResult([]);
        await Task.WhenAll(first, second);

        await _settingRepository.Received(1).GetAllAsync();
    }

    [Fact]
    public async Task ReloadAsync_ChangedRepository_ReplacesCachedSetting()
    {
        var ringtone = CreateRingtone("Raw/Eins.mp3");
        var germanSetting = CreateSetting(ringtone, (int)AppLanguage.German);
        SetStoredSettings(germanSetting);
        await _store.InitializeAsync();
        var englishSetting = CreateSetting(ringtone, (int)AppLanguage.English);
        SetStoredSettings(englishSetting);

        await _store.ReloadAsync();

        Assert.Equal((int)AppLanguage.English, _store.Read(setting => setting!.Language));
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
