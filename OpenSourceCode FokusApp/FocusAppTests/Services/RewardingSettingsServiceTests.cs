using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.Services;

public class RewardingSettingsServiceTests
{
    private readonly ISettingRepository _settingRepository = Substitute.For<ISettingRepository>();
    private readonly IRingtoneCatalogService _ringtoneCatalogService = Substitute.For<IRingtoneCatalogService>();
    private readonly SettingStore _store;
    private readonly RewardingSettingsService _service;
    private SettingEntity? _updatedSetting;

    public RewardingSettingsServiceTests()
    {
        SetStoredSettings();
        _settingRepository.UpdateAsync(Arg.Do<SettingEntity>(setting => _updatedSetting = setting));
        _store = new SettingStore(_settingRepository, _ringtoneCatalogService);
        _service = new RewardingSettingsService(_store);
    }

    [Fact]
    public void Rewarding_BeforeInitialization_ReturnsDefaults()
    {
        Assert.Equal(AppRewardingSettings.Default, _service.Rewarding);
    }

    [Fact]
    public void Defaults_FreshInstall_AreAllOnAndGoalIsOneToOne()
    {
        var defaults = AppRewardingSettings.Default;

        Assert.True(defaults.MicroFeedbackEnabled);
        Assert.True(defaults.ReviewsEnabled);
        Assert.True(defaults.CompanionEnabled);
        Assert.Equal(BalanceGoal.OneToOne, defaults.BalanceGoal);
    }

    [Fact]
    public async Task Rewarding_StoredValues_AreReadAfterInitialization()
    {
        var setting = CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0);
        setting.MicroFeedbackEnabled = false;
        setting.ReviewsEnabled = false;
        setting.CompanionEnabled = false;
        setting.BalanceGoal = (int)BalanceGoal.ThreeToTwo;
        SetStoredSettings(setting);

        await _store.InitializeAsync();

        Assert.Equal(new AppRewardingSettings(false, false, false, BalanceGoal.ThreeToTwo), _service.Rewarding);
    }

    [Theory]
    [InlineData(1, BalanceGoal.Custom, 1, 2)]
    [InlineData(2, BalanceGoal.Custom, 1, 3)]
    [InlineData(3, BalanceGoal.OneToOne, 1, 1)]
    public async Task Rewarding_StoredRemovedBalanceGoal_BecomesTheNearestRemainingOne(int stored, BalanceGoal goal, int forYou, int toDo)
    {
        var setting = CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0);
        setting.BalanceGoal = stored;
        SetStoredSettings(setting);

        await _store.InitializeAsync();

        Assert.Equal(goal, _service.Rewarding.BalanceGoal);
        Assert.Equal(forYou, _service.Rewarding.CustomForYou);
        Assert.Equal(toDo, _service.Rewarding.CustomToDo);
    }

    [Fact]
    public async Task Rewarding_StoredUnknownBalanceGoal_FallsBackToOneToOne()
    {
        var setting = CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0);
        setting.BalanceGoal = 99;
        SetStoredSettings(setting);

        await _store.InitializeAsync();

        Assert.Equal(BalanceGoal.OneToOne, _service.Rewarding.BalanceGoal);
    }

    [Fact]
    public async Task SetRewardingAsync_Change_PersistsAllFieldsAndKeepsOtherSettings()
    {
        var ringtone = CreateRingtone("Raw/Eins.mp3");
        var setting = CreateSetting(ringtone, language: 0);
        setting.Theme = 2;
        SetStoredSettings(setting);
        await _store.InitializeAsync();
        var changed = new AppRewardingSettings(false, true, false, BalanceGoal.Custom, 2, 5);

        await _service.SetRewardingAsync(changed);

        Assert.NotNull(_updatedSetting);
        Assert.False(_updatedSetting.MicroFeedbackEnabled);
        Assert.True(_updatedSetting.ReviewsEnabled);
        Assert.False(_updatedSetting.CompanionEnabled);
        Assert.Equal((int)BalanceGoal.Custom, _updatedSetting.BalanceGoal);
        Assert.Equal(2, _updatedSetting.CustomForYou);
        Assert.Equal(5, _updatedSetting.CustomToDo);
        Assert.Equal(2, _updatedSetting.Theme);
        Assert.Equal(setting.RingtoneId, _updatedSetting.RingtoneId);
        Assert.Equal(changed, _service.Rewarding);
    }

    [Fact]
    public async Task SetRewardingAsync_Change_RaisesRewardingChangedOnce()
    {
        SetStoredSettings(CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0));
        await _store.InitializeAsync();
        var raised = 0;
        _service.RewardingChanged += (_, _) => raised++;

        await _service.SetRewardingAsync(AppRewardingSettings.Default with { MicroFeedbackEnabled = false });

        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task SetRewardingAsync_SameValues_DoesNotWriteOrRaise()
    {
        SetStoredSettings(CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0));
        await _store.InitializeAsync();
        var raised = 0;
        _service.RewardingChanged += (_, _) => raised++;

        await _service.SetRewardingAsync(AppRewardingSettings.Default);

        await _settingRepository.DidNotReceive().UpdateAsync(Arg.Any<SettingEntity>());
        Assert.Equal(0, raised);
    }

    [Fact]
    public async Task SetRewardingAsync_UnknownBalanceGoal_IsStoredAsOneToOne()
    {
        SetStoredSettings(CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0));
        await _store.InitializeAsync();

        await _service.SetRewardingAsync(AppRewardingSettings.Default with { CompanionEnabled = false, BalanceGoal = (BalanceGoal)42 });

        Assert.Equal((int)BalanceGoal.OneToOne, _updatedSetting!.BalanceGoal);
    }

    [Fact]
    public async Task SetRewardingAsync_NoStoredSetting_DoesNothing()
    {
        await _store.InitializeAsync();

        await _service.SetRewardingAsync(AppRewardingSettings.Default with { CompanionEnabled = false });

        await _settingRepository.DidNotReceive().UpdateAsync(Arg.Any<SettingEntity>());
        Assert.Equal(AppRewardingSettings.Default, _service.Rewarding);
    }

    [Fact]
    public async Task SetRewardingAsync_RepositoryFails_KeepsOldValuesAndDoesNotRaise()
    {
        SetStoredSettings(CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0));
        await _store.InitializeAsync();
        _settingRepository.UpdateAsync(Arg.Any<SettingEntity>()).Returns(Task.FromException(new InvalidOperationException("kaputt")));
        var raised = 0;
        _service.RewardingChanged += (_, _) => raised++;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SetRewardingAsync(AppRewardingSettings.Default with { ReviewsEnabled = false }));

        Assert.Equal(AppRewardingSettings.Default, _service.Rewarding);
        Assert.Equal(0, raised);
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
