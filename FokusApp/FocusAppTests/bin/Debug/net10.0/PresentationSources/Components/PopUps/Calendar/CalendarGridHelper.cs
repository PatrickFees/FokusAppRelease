namespace FokusApp.Components.PopUps.Calendar;

/// <summary>
/// Shared month-grid arithmetic (Monday-first, six weeks) used by the calendar popup and the calendar page.
/// </summary>
public static class CalendarGridHelper
{
    public const int WeeksInGrid = 6;
    public const int DaysInWeek = 7;
    public const int DaysInGrid = WeeksInGrid * DaysInWeek;

    public static DateTime GetFirstDayOfMonth(DateTime date)
    {
        return new DateTime(date.Year, date.Month, 1);
    }

    public static DateTime GetFirstVisibleCalendarDay(DateTime monthStart)
    {
        var mondayBasedOffset = ((int)monthStart.DayOfWeek + 6) % 7;
        return monthStart.AddDays(-mondayBasedOffset);
    }
}
