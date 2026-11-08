using System.ComponentModel.DataAnnotations;

namespace FokusApp.Core.DataBase.Entities;

public class SettingEntity
{
    [Key]
    public Guid Id { get; set; }
    public int Theme { get; set; }

    public int ColorMode { get; set; }

    public int Language { get; set; }
    public Guid RingtoneId { get; set; }
    public RingtonesEntity Ringtone { get; set; } = null!;

    public bool MicroFeedbackEnabled { get; set; } = true;
    public bool ReviewsEnabled { get; set; } = true;
    public bool CompanionEnabled { get; set; } = true;
    public int BalanceGoal { get; set; }
    public int CustomForYou { get; set; } = 1;
    public int CustomToDo { get; set; } = 1;

    public int OnboardingState { get; set; }

    public int DefaultScope { get; set; }

    public DateTime? CompanionHiddenDay { get; set; }

    public string? DismissedUpdateVersion { get; set; }

    public string? LastSeenChangelogVersion { get; set; }

    public bool CalendarAutoSyncEnabled { get; set; }

    public DateTime? CalendarAutoSyncLastRunAt { get; set; }

    public int TimerAlertMode { get; set; } = 1;
}
