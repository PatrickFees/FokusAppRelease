using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Implementations;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;
using Microsoft.EntityFrameworkCore;

namespace FocusAppTests.DataBase;

public class SettingRepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly SettingRepository _repository;
    private readonly RingtonesRepository _ringtonesRepository;

    public SettingRepositoryTests()
    {
        _database.MigrateApp();
        _repository = new SettingRepository(_database.AppFactory);
        _ringtonesRepository = new RingtonesRepository(_database.AppFactory);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task AnySettingExistsAsync_EmptyDatabase_ReturnsFalse()
    {
        var result = await _repository.AnySettingExistsAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task AnySettingExistsAsync_AfterAdd_ReturnsTrue()
    {
        var ringtone = await AddRingtoneAsync();
        var setting = TestEntities.CreateSetting(ringtone.Id);
        await _repository.AddAsync(setting);

        var result = await _repository.AnySettingExistsAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task AddAsync_SettingWithNavigationProperty_PersistsWithoutDuplicatingRingtone()
    {
        var ringtone = await AddRingtoneAsync();
        var setting = TestEntities.CreateSetting(ringtone.Id, theme: 2, language: 1);
        setting.Ringtone = ringtone;

        await _repository.AddAsync(setting);

        var settings = await _repository.GetAllAsync();
        var ringtones = await _ringtonesRepository.GetAllAsync();
        var stored = Assert.Single(settings);
        Assert.Equal(setting.Id, stored.Id);
        Assert.Equal(2, stored.Theme);
        Assert.Equal(1, stored.Language);
        Assert.Single(ringtones);
    }

    [Fact]
    public async Task GetAllAsync_StoredSetting_IncludesRingtone()
    {
        var ringtone = await AddRingtoneAsync("Raw/Eigener.mp3");
        var setting = TestEntities.CreateSetting(ringtone.Id);
        await _repository.AddAsync(setting);

        var settings = await _repository.GetAllAsync();

        var stored = Assert.Single(settings);
        Assert.NotNull(stored.Ringtone);
        Assert.Equal("Raw/Eigener.mp3", stored.Ringtone.RingtoneFilePath);
    }

    [Fact]
    public async Task AddAsync_UnknownRingtone_ThrowsDbUpdateException()
    {
        var unknownRingtoneId = Guid.NewGuid();
        var setting = TestEntities.CreateSetting(unknownRingtoneId);

        await Assert.ThrowsAsync<DbUpdateException>(() => _repository.AddAsync(setting));
    }

    [Fact]
    public async Task AddRangeAsync_MultipleSettings_PersistsAll()
    {
        var ringtone = await AddRingtoneAsync();
        var first = TestEntities.CreateSetting(ringtone.Id);
        var second = TestEntities.CreateSetting(ringtone.Id);
        var settings = new[] { first, second };

        await _repository.AddRangeAsync(settings);

        var stored = await _repository.GetAllAsync();
        Assert.Equal(2, stored.Count);
    }

    [Fact]
    public async Task UpdateAsync_ExistingSetting_UpdatesThemeLanguageAndRingtone()
    {
        var firstRingtone = await AddRingtoneAsync("Raw/Eins.mp3");
        var secondRingtone = await AddRingtoneAsync("Raw/Zwei.mp3");
        var setting = TestEntities.CreateSetting(firstRingtone.Id, theme: 0, language: 0);
        await _repository.AddAsync(setting);
        var changed = TestEntities.CreateSetting(secondRingtone.Id, theme: 2, language: 1);
        changed.Id = setting.Id;

        await _repository.UpdateAsync(changed);

        var settings = await _repository.GetAllAsync();
        var stored = Assert.Single(settings);
        Assert.Equal(2, stored.Theme);
        Assert.Equal(1, stored.Language);
        Assert.Equal(secondRingtone.Id, stored.RingtoneId);
        Assert.Equal("Raw/Zwei.mp3", stored.Ringtone.RingtoneFilePath);
    }
    [Fact]
    public async Task UpdateAsync_ColorMode_IsStored()
    {
        var ringtone = await AddRingtoneAsync();
        var setting = TestEntities.CreateSetting(ringtone.Id, colorMode: (int)AppColorMode.Light);
        await _repository.AddAsync(setting);
        var changed = TestEntities.CreateSetting(ringtone.Id, theme: (int)AppColorTheme.Pink, colorMode: (int)AppColorMode.Dark);
        changed.Id = setting.Id;

        await _repository.UpdateAsync(changed);

        var settings = await _repository.GetAllAsync();
        var stored = Assert.Single(settings);
        Assert.Equal((int)AppColorMode.Dark, stored.ColorMode);
        Assert.Equal((int)AppColorTheme.Pink, stored.Theme);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_ThrowsInvalidOperationException()
    {
        var ringtone = await AddRingtoneAsync();
        var unknown = TestEntities.CreateSetting(ringtone.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.UpdateAsync(unknown));

        Assert.Contains(unknown.Id.ToString(), exception.Message);
    }

    [Fact]
    public async Task DeleteAllAsync_ExistingSettings_RemovesSettingsButKeepsRingtones()
    {
        var ringtone = await AddRingtoneAsync();
        var setting = TestEntities.CreateSetting(ringtone.Id);
        await _repository.AddAsync(setting);

        await _repository.DeleteAllAsync();

        var settings = await _repository.GetAllAsync();
        var ringtones = await _ringtonesRepository.GetAllAsync();
        Assert.Empty(settings);
        Assert.Single(ringtones);
    }

    [Fact]
    public async Task UserSettings_ChangedRewarding_SurviveAppRestart()
    {
        var ringtone = await AddRingtoneAsync();
        await _repository.AddAsync(TestEntities.CreateSetting(ringtone.Id));
        var catalog = Substitute.For<IRingtoneCatalogService>();
        var firstRunStore = new SettingStore(_repository, catalog);
        await firstRunStore.InitializeAsync();
        var firstRun = new RewardingSettingsService(firstRunStore);
        var chosen = new AppRewardingSettings(false, false, true, BalanceGoal.Custom, 2, 5);

        await firstRun.SetRewardingAsync(chosen);
        var secondRunStore = new SettingStore(new SettingRepository(_database.AppFactory), catalog);
        await secondRunStore.InitializeAsync();
        var secondRun = new RewardingSettingsService(secondRunStore);

        Assert.Equal(chosen, secondRun.Rewarding);
    }

    [Fact]
    public async Task UpdateAsync_CompanionHiddenDay_IsPersistedAndCanBeCleared()
    {
        var ringtone = await AddRingtoneAsync();
        var setting = TestEntities.CreateSetting(ringtone.Id);
        await _repository.AddAsync(setting);
        setting.CompanionHiddenDay = new DateTime(2026, 3, 15);
        await _repository.UpdateAsync(setting);
        Assert.Equal(new DateTime(2026, 3, 15), Assert.Single(await _repository.GetAllAsync()).CompanionHiddenDay);

        setting.CompanionHiddenDay = null;
        await _repository.UpdateAsync(setting);

        Assert.Null(Assert.Single(await _repository.GetAllAsync()).CompanionHiddenDay);
    }

    [Fact]
    public async Task UpdateAsync_TimerAlertMode_IsPersisted()
    {
        var ringtone = await AddRingtoneAsync();
        var setting = TestEntities.CreateSetting(ringtone.Id);
        await _repository.AddAsync(setting);
        setting.TimerAlertMode = (int)(TimerAlertMode.Vibration | TimerAlertMode.AdaptToRingerMode);

        await _repository.UpdateAsync(setting);

        Assert.Equal(
            (int)(TimerAlertMode.Vibration | TimerAlertMode.AdaptToRingerMode),
            Assert.Single(await _repository.GetAllAsync()).TimerAlertMode);
    }

    [Fact]
    public async Task AddAsync_NewSetting_HasTimerAlertModeSoundDefault()
    {
        var ringtone = await AddRingtoneAsync();
        await _repository.AddAsync(TestEntities.CreateSetting(ringtone.Id));

        var stored = Assert.Single(await _repository.GetAllAsync());

        Assert.Equal((int)TimerAlertMode.Sound, stored.TimerAlertMode);
    }

    [Fact]
    public async Task UpdateAsync_OnboardingState_IsPersisted()
    {
        var ringtone = await AddRingtoneAsync();
        var setting = TestEntities.CreateSetting(ringtone.Id);
        await _repository.AddAsync(setting);
        setting.OnboardingState = 1;

        await _repository.UpdateAsync(setting);

        Assert.Equal(1, Assert.Single(await _repository.GetAllAsync()).OnboardingState);
    }

    [Fact]
    public async Task UpdateAsync_LastSeenChangelogVersion_IsPersisted()
    {
        var ringtone = await AddRingtoneAsync();
        var setting = TestEntities.CreateSetting(ringtone.Id);
        await _repository.AddAsync(setting);
        setting.LastSeenChangelogVersion = "1.5.0";

        await _repository.UpdateAsync(setting);

        Assert.Equal("1.5.0", Assert.Single(await _repository.GetAllAsync()).LastSeenChangelogVersion);
    }

    [Fact]
    public async Task UpdateAsync_DismissedUpdateVersion_IsPersisted()
    {
        var ringtone = await AddRingtoneAsync();
        var setting = TestEntities.CreateSetting(ringtone.Id);
        await _repository.AddAsync(setting);
        setting.DismissedUpdateVersion = "1.5.0";

        await _repository.UpdateAsync(setting);

        Assert.Equal("1.5.0", Assert.Single(await _repository.GetAllAsync()).DismissedUpdateVersion);
    }

    [Fact]
    public async Task LastSeenChangelogVersion_ChangedViaSettingStore_SurvivesAppRestart()
    {
        var ringtone = await AddRingtoneAsync();
        await _repository.AddAsync(TestEntities.CreateSetting(ringtone.Id));
        var catalog = Substitute.For<IRingtoneCatalogService>();
        var firstRunStore = new SettingStore(_repository, catalog);
        await firstRunStore.InitializeAsync();
        var firstRun = new ChangelogSettingsService(firstRunStore);

        await firstRun.SetLastSeenChangelogVersionAsync("1.5.0");
        var secondRunStore = new SettingStore(new SettingRepository(_database.AppFactory), catalog);
        await secondRunStore.InitializeAsync();
        var secondRun = new ChangelogSettingsService(secondRunStore);

        Assert.Equal("1.5.0", secondRun.LastSeenChangelogVersion);
    }

    [Fact]
    public async Task AddAsync_NewSetting_HasRewardingDefaults()
    {
        var ringtone = await AddRingtoneAsync();
        await _repository.AddAsync(TestEntities.CreateSetting(ringtone.Id));

        var stored = Assert.Single(await _repository.GetAllAsync());

        Assert.True(stored.MicroFeedbackEnabled);
        Assert.True(stored.ReviewsEnabled);
        Assert.True(stored.CompanionEnabled);
        Assert.Equal(0, stored.BalanceGoal);
    }

    private async Task<RingtonesEntity> AddRingtoneAsync(string filePath = "Raw/Klingelton.mp3")
    {
        var ringtone = TestEntities.CreateRingtone(filePath);
        await _ringtonesRepository.AddAsync(ringtone);

        return ringtone;
    }
}
