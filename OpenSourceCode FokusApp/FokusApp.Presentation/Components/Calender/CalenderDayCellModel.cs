namespace FokusApp.Components.Calender;

public sealed class CalenderDayCellModel
{
    public DateTime Date { get; init; }

    public bool IsInCurrentMonth { get; init; }

    public bool IsToday { get; init; }

    public bool IsSelected { get; init; }

    public IReadOnlyList<CalenderDotModel> Dots { get; init; } = [];

    public bool HasMoreTasks { get; init; }
}
