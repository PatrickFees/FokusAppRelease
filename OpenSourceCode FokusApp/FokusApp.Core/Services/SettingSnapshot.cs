using FokusApp.Core.DataBase.Entities;

namespace FokusApp.Core.Services;

internal static class SettingSnapshot
{
    public static SettingEntity Copy(SettingEntity setting) => new()
    {
        Id = setting.Id,
        Theme = setting.Theme,
        ColorMode = setting.ColorMode,
        Language = setting.Language,
        RingtoneId = setting.RingtoneId,
        Ringtone = setting.Ringtone,
        MicroFeedbackEnabled = setting.MicroFeedbackEnabled,
        ReviewsEnabled = setting.ReviewsEnabled,
        CompanionEnabled = setting.CompanionEnabled,
        BalanceGoal = setting.BalanceGoal,
        CustomForYou = setting.CustomForYou,
        CustomToDo = setting.CustomToDo,
        OnboardingState = setting.OnboardingState,
        DefaultScope = setting.DefaultScope,
        CompanionHiddenDay = setting.CompanionHiddenDay,
        DismissedUpdateVersion = setting.DismissedUpdateVersion,
        LastSeenChangelogVersion = setting.LastSeenChangelogVersion,
        CalendarAutoSyncEnabled = setting.CalendarAutoSyncEnabled,
        CalendarAutoSyncLastRunAt = setting.CalendarAutoSyncLastRunAt,
        TimerAlertMode = setting.TimerAlertMode
    };

    public static RingtonesEntity CopyRingtone(RingtonesEntity ringtone) => new()
    {
        Id = ringtone.Id,
        RingtoneFilePath = ringtone.RingtoneFilePath
    };
}
