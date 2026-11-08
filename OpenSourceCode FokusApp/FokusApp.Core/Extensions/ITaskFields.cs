using FokusApp.Core.Enums;
using FokusApp.Core.Models;

namespace FokusApp.Core.Extensions;

internal interface ITaskFields
{
    bool IsDone { get; set; }
    string? Note { get; set; }
    bool IsTemplate { get; set; }
    bool IsWorkTask { get; set; }
    bool IsPositive { get; set; }
    DateTime CreationDate { get; set; }
    DateTime? TaskDate { get; set; }
    DateTime? CompletedAt { get; set; }
    TaskPriority Priority { get; set; }
    int BreakMinutes { get; set; }
    int TimerMinutes { get; set; }
    TimeSpan? TaskTime { get; set; }
    int? ReminderMinutesBefore { get; set; }
    TaskRepeatUnit RepeatUnit { get; set; }
    int RepeatInterval { get; set; }
    TaskWeekdays RepeatWeekdays { get; set; }
    Guid? SeriesId { get; set; }
    DateTime? RepeatGeneratedUntil { get; set; }
    int? RepeatWeekdayOrdinal { get; set; }
    int? RepeatMonth { get; set; }
    TaskWeekdays? RepeatWeekStart { get; set; }

    DateTime? RepeatEndDate { get; set; }
    string? SourceCalendarEventId { get; set; }
}
