using FokusApp.Core.Enums;

namespace FokusApp.Components.Calender;

public sealed class CalenderDotModel
{
    public TaskPriority Priority { get; init; }

    public bool IsDone { get; init; }
}

public sealed class CalenderDayCellModel
{
    public DateTime Date { get; init; }

    public bool IsInCurrentMonth { get; init; }

    public bool IsToday { get; init; }

    public bool IsSelected { get; init; }

    public IReadOnlyList<CalenderDotModel> Dots { get; init; } = [];

    public bool HasMoreTasks { get; init; }
}

public sealed class CalenderWeekModel
{
    public int WeekNumber { get; init; }

    public IReadOnlyList<CalenderDayCellModel> Days { get; init; } = [];
}
