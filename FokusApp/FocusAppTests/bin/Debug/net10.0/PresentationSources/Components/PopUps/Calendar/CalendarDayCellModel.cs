namespace FokusApp.Components.PopUps.Calendar;

public sealed class CalendarDayCellModel
{
    public DateTime Date { get; init; }

    public bool IsInCurrentMonth { get; init; }

    public bool IsToday { get; init; }

    public bool IsSelected { get; init; }
}
