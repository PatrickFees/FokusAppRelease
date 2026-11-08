using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Enums;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.Services;

public class OnboardingSettingsServiceTests
{
    private readonly ISettingRepository _settingRepository = Substitute.For<ISettingRepository>();
    private readonly IRingtoneCatalogService _ringtoneCatalogService = Substitute.For<IRingtoneCatalogService>();
    private readonly SettingStore _store;
    private readonly OnboardingSettingsService _service;
    private SettingEntity? _updatedSetting;

    public OnboardingSettingsServiceTests()
    {
        SetStoredSettings();
        _settingRepository.UpdateAsync(Arg.Do<SettingEntity>(setting => _updatedSetting = setting));
        _store = new SettingStore(_settingRepository, _ringtoneCatalogService);
        _service = new OnboardingSettingsService(_store);
    }

    [Theory]
    [InlineData(0, OnboardingState.NotSeen)]
    [InlineData(1, OnboardingState.Completed)]
    [InlineData(2, OnboardingState.InvitePending)]
    [InlineData(42, OnboardingState.Completed)]
    public async Task OnboardingState_Stored_IsExposedAndUnknownCountsAsCompleted(int stored, OnboardingState expected)
    {
        var setting = CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0);
        setting.OnboardingState = stored;
        SetStoredSettings(setting);

        await _store.InitializeAsync();

        Assert.Equal(expected, _service.OnboardingState);
    }

    [Fact]
    public void OnboardingState_NothingLoaded_IsCompletedSoNothingPopsUp()
    {
        Assert.Equal(OnboardingState.Completed, _service.OnboardingState);
    }

    [Fact]
    public async Task SetOnboardingStateAsync_State_IsPersistedAndRaisesNoRewardingChange()
    {
        var setting = CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0);
        setting.OnboardingState = 0;
        SetStoredSettings(setting);
        await _store.InitializeAsync();
        var rewardingService = new RewardingSettingsService(_store);
        var raised = 0;
        rewardingService.RewardingChanged += (_, _) => raised++;

        await _service.SetOnboardingStateAsync(OnboardingState.Completed);

        Assert.Equal(1, _updatedSetting!.OnboardingState);
        Assert.Equal(OnboardingState.Completed, _service.OnboardingState);
        Assert.Equal(0, raised);
    }

    [Fact]
    public async Task SetOnboardingStateAsync_SameState_DoesNotWriteAgain()
    {
        var setting = CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0);
        setting.OnboardingState = 1;
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        await _service.SetOnboardingStateAsync(OnboardingState.Completed);

        Assert.Null(_updatedSetting);
    }

    [Fact]
    public async Task SetOnboardingStateAsync_NoStoredSetting_DoesNothing()
    {
        await _service.SetOnboardingStateAsync(OnboardingState.Completed);

        Assert.Null(_updatedSetting);
    }

    [Fact]
    public async Task SetCompanionHiddenDayAsync_Day_IsStoredAsDateAndRaisesNoRewardingChange()
    {
        SetStoredSettings(CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0));
        await _store.InitializeAsync();
        var rewardingService = new RewardingSettingsService(_store);
        var raised = 0;
        rewardingService.RewardingChanged += (_, _) => raised++;

        await _service.SetCompanionHiddenDayAsync(new DateTime(2026, 3, 15, 14, 0, 0));

        Assert.Equal(new DateTime(2026, 3, 15), _updatedSetting!.CompanionHiddenDay);
        Assert.Equal(new DateTime(2026, 3, 15), _service.CompanionHiddenDay);
        Assert.Equal(0, raised);
    }

    [Fact]
    public async Task SetCompanionHiddenDayAsync_Null_ShowsTheCompanionAgain()
    {
        var setting = CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0);
        setting.CompanionHiddenDay = new DateTime(2026, 3, 15);
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        await _service.SetCompanionHiddenDayAsync(null);

        Assert.Null(_updatedSetting!.CompanionHiddenDay);
        Assert.Null(_service.CompanionHiddenDay);
    }

    [Fact]
    public async Task SetCompanionHiddenDayAsync_SameDay_DoesNotWriteAgain()
    {
        var setting = CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0);
        setting.CompanionHiddenDay = new DateTime(2026, 3, 15);
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        await _service.SetCompanionHiddenDayAsync(new DateTime(2026, 3, 15));

        Assert.Null(_updatedSetting);
    }

    [Fact]
    public async Task SetCompanionHiddenDayAsync_NoStoredSetting_DoesNothing()
    {
        await _service.SetCompanionHiddenDayAsync(new DateTime(2026, 3, 15));

        Assert.Null(_service.CompanionHiddenDay);
    }

    [Fact]
    public async Task SetCompanionHiddenDayAsync_OtherDayThanStored_ReplacesTheStoredDay()
    {
        var setting = CreateSetting(CreateRingtone("Raw/Eins.mp3"), language: 0);
        setting.CompanionHiddenDay = new DateTime(2026, 3, 15);
        SetStoredSettings(setting);
        await _store.InitializeAsync();

        await _service.SetCompanionHiddenDayAsync(new DateTime(2026, 3, 16, 8, 0, 0));

        Assert.Equal(new DateTime(2026, 3, 16), _updatedSetting!.CompanionHiddenDay);
        Assert.Equal(new DateTime(2026, 3, 16), _service.CompanionHiddenDay);
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
