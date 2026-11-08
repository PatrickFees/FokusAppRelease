using FokusApp.Core.Enums;
using FokusApp.Core.Extensions;

namespace FokusApp.Core.Models;

public sealed class TaskDto : ITaskFields
{
    public Guid? Id { get; set; }
    public bool IsDone { get; set; }
    public string? Note { get; set; }
    public bool IsTemplate { get; set; }
    public bool IsWorkTask { get; set; } = true;
    public bool IsPositive { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? TaskDate { get; set; }

    public DateTime? CompletedAt { get; set; }
    public TaskPriority Priority { get; set; }
    public int BreakMinutes { get; set; } = 5;
    public int TimerMinutes { get; set; } = 5;

    public TimeSpan? TaskTime { get; set; }

    public int? ReminderMinutesBefore { get; set; }

    public TaskRepeatUnit RepeatUnit { get; set; }

    public int RepeatInterval { get; set; } = 1;

    public TaskWeekdays RepeatWeekdays { get; set; }

    public Guid? SeriesId { get; set; }

    public DateTime? RepeatGeneratedUntil { get; set; }

    public int? RepeatWeekdayOrdinal { get; set; }

    public int? RepeatMonth { get; set; }

    public TaskWeekdays? RepeatWeekStart { get; set; }

    public DateTime? RepeatEndDate { get; set; }

    public string? SourceCalendarEventId { get; set; }
}
