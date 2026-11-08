namespace FokusApp.Components.Calender;

public sealed class CalenderWeekModel
{
    public int WeekNumber { get; init; }

    public IReadOnlyList<CalenderDayCellModel> Days { get; init; } = [];
}
