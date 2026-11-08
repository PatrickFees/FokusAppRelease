using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Enums;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.Services;

public class DefaultScopeSettingsServiceTests
{
    private readonly ISettingRepository _settingRepository = Substitute.For<ISettingRepository>();
    private readonly IRingtoneCatalogService _ringtoneCatalogService = Substitute.For<IRingtoneCatalogService>();
    private readonly SettingStore _store;
    private readonly DefaultScopeSettingsService _service;
    private SettingEntity? _updatedSetting;

    public DefaultScopeSettingsServiceTests()
    {
        SetStoredSettings();
        _settingRepository.UpdateAsync(Arg.Do<SettingEntity>(setting => _updatedSetting = setting));
        _store = new SettingStore(_settingRepository, _ringtoneCatalogService);
        _service = new DefaultScopeSettingsService(_store);
    }

    [Theory]
    [InlineData(0, DefaultScope.Both)]
    [InlineData(1, DefaultScope.Work)]
    [InlineData(2, DefaultScope.Private)]
    [InlineData(42, DefaultScope.Both)]
    public async Task DefaultScope_Stored_IsExposedAndUnknownCountsAsBoth(int stored, DefaultScope expected)
    {
        var setting = CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0);
        setting.DefaultScope = stored;
        SetStoredSettings(setting);

        await _store.InitializeAsync();

        Assert.Equal(expected, _service.DefaultScope);
    }

    [Fact]
    public void DefaultScope_NothingLoaded_IsBoth()
    {
        Assert.Equal(DefaultScope.Both, _service.DefaultScope);
    }

    [Fact]
    public async Task InitializeAsync_StoredSettings_RaisesDefaultScopeChanged()
    {
        SetStoredSettings(CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0));
        var raised = 0;
        _service.DefaultScopeChanged += (_, _) => raised++;

        await _store.InitializeAsync();

        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task SetDefaultScopeAsync_Scope_IsPersistedAndRaisesOnlyDefaultScopeChanged()
    {
        SetStoredSettings(CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0));
        await _store.InitializeAsync();
        var rewardingService = new RewardingSettingsService(_store);
        var scopeRaised = 0;
        var rewardingRaised = 0;
        _service.DefaultScopeChanged += (_, _) => scopeRaised++;
        rewardingService.RewardingChanged += (_, _) => rewardingRaised++;

        await _service.SetDefaultScopeAsync(DefaultScope.Private);

        Assert.Equal(2, _updatedSetting!.DefaultScope);
        Assert.Equal(DefaultScope.Private, _service.DefaultScope);
        Assert.Equal(1, scopeRaised);
        Assert.Equal(0, rewardingRaised);
    }

    [Fact]
    public async Task SetDefaultScopeAsync_SameScope_StoresNothing()
    {
        SetStoredSettings(CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0));
        await _store.InitializeAsync();

        await _service.SetDefaultScopeAsync(DefaultScope.Both);

        await _settingRepository.DidNotReceive().UpdateAsync(Arg.Any<SettingEntity>());
    }

    [Fact]
    public async Task SetDefaultScopeAsync_UnknownScope_StoresNothing()
    {
        SetStoredSettings(CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0));
        await _store.InitializeAsync();

        await _service.SetDefaultScopeAsync((DefaultScope)42);

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
