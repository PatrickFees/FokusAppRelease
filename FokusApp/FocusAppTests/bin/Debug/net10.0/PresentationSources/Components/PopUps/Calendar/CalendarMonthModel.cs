
namespace FokusApp.Components.PopUps.Calendar;

public sealed class CalendarMonthModel
{
    public DateTime MonthStart { get; init; }

    public IReadOnlyList<CalendarDayCellModel> DayCells { get; init; } = [];
}
