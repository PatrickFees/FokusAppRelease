using FokusApp.Core.Enums;

namespace FokusApp.Components.PopUps.TaskSchedule;

public sealed class TaskScheduleModel
{

    public TimeSpan? Time { get; init; }

    public int? ReminderMinutesBefore { get; init; }

    public TaskRepeatUnit RepeatUnit { get; init; }

    public int RepeatInterval { get; init; } = 1;

    public TaskWeekdays RepeatWeekdays { get; init; }

    public int? RepeatWeekdayOrdinal { get; init; }

    public int? RepeatMonth { get; init; }

    public TaskWeekdays? RepeatWeekStart { get; init; }

    public DateTime? RepeatEndDate { get; init; }
}
