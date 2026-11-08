using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FokusApp.Core.DataBase.Repositories.Implementations;

public sealed class SettingRepository : ISettingRepository
{
    private readonly IDbContextFactory<FokusAppDbContext> _contextFactory;

    public SettingRepository(IDbContextFactory<FokusAppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<bool> AnySettingExistsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.Settings.AsNoTracking().AnyAsync().ConfigureAwait(false);
    }

    public async Task<List<SettingEntity>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.Settings
            .AsNoTracking()
            .Include(setting => setting.Ringtone)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task AddAsync(SettingEntity setting)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await context.Settings.AddAsync(CreatePersistenceCopy(setting)).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task AddRangeAsync(IEnumerable<SettingEntity> settings)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await context.Settings
            .AddRangeAsync(settings.Select(CreatePersistenceCopy))
            .ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(SettingEntity setting)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var existingSetting = await context.Settings
            .FirstOrDefaultAsync(item => item.Id == setting.Id)
            .ConfigureAwait(false);

        if (existingSetting is null)
        {

            throw new InvalidOperationException($"Setting with id '{setting.Id}' does not exist.");
        }

        existingSetting.Theme = setting.Theme;
        existingSetting.ColorMode = setting.ColorMode;
        existingSetting.Language = setting.Language;
        existingSetting.RingtoneId = setting.RingtoneId;
        existingSetting.MicroFeedbackEnabled = setting.MicroFeedbackEnabled;
        existingSetting.ReviewsEnabled = setting.ReviewsEnabled;
        existingSetting.CompanionEnabled = setting.CompanionEnabled;
        existingSetting.BalanceGoal = setting.BalanceGoal;
        existingSetting.CustomForYou = setting.CustomForYou;
        existingSetting.CustomToDo = setting.CustomToDo;
        existingSetting.OnboardingState = setting.OnboardingState;
        existingSetting.DefaultScope = setting.DefaultScope;
        existingSetting.CompanionHiddenDay = setting.CompanionHiddenDay;
        existingSetting.CalendarAutoSyncEnabled = setting.CalendarAutoSyncEnabled;
        existingSetting.CalendarAutoSyncLastRunAt = setting.CalendarAutoSyncLastRunAt;
        existingSetting.TimerAlertMode = setting.TimerAlertMode;
        existingSetting.DismissedUpdateVersion = setting.DismissedUpdateVersion;
        existingSetting.LastSeenChangelogVersion = setting.LastSeenChangelogVersion;
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await context.Settings.ExecuteDeleteAsync().ConfigureAwait(false);
    }

    private static SettingEntity CreatePersistenceCopy(SettingEntity setting) => new()
    {
        Id = setting.Id,
        Theme = setting.Theme,
        ColorMode = setting.ColorMode,
        Language = setting.Language,
        RingtoneId = setting.RingtoneId,
        MicroFeedbackEnabled = setting.MicroFeedbackEnabled,
        ReviewsEnabled = setting.ReviewsEnabled,
        CompanionEnabled = setting.CompanionEnabled,
        BalanceGoal = setting.BalanceGoal,
        CustomForYou = setting.CustomForYou,
        CustomToDo = setting.CustomToDo,
        OnboardingState = setting.OnboardingState,
        DefaultScope = setting.DefaultScope,
        CompanionHiddenDay = setting.CompanionHiddenDay,
        CalendarAutoSyncEnabled = setting.CalendarAutoSyncEnabled,
        CalendarAutoSyncLastRunAt = setting.CalendarAutoSyncLastRunAt,
        TimerAlertMode = setting.TimerAlertMode,
        Ringtone = null!
    };
}
