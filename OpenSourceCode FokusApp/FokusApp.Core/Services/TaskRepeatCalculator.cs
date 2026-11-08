using FokusApp.Core.Enums;

namespace FokusApp.Core.Services;

public static class TaskRepeatCalculator
{
    public const int MinimumInterval = 1;
    public const int MaximumInterval = 99;

    public static TaskWeekdays ToWeekdays(DayOfWeek dayOfWeek)
        => (TaskWeekdays)(1 << ((int)dayOfWeek + 6) % 7);

    public static int NormalizeInterval(int interval)
        => Math.Clamp(interval, MinimumInterval, MaximumInterval);

    public static TaskWeekdays? ToSingleWeekday(TaskWeekdays weekdays)
    {
        var selected = weekdays & TaskWeekdays.All;
        if (selected == TaskWeekdays.None)
        {
            return null;
        }

        return (TaskWeekdays)((int)selected & -(int)selected);
    }

    public static bool IsValidWeekdayOrdinal(int ordinal) => ordinal is -1 or 1 or 2 or 3 or 4;

    public static IReadOnlyList<DateTime> GetOccurrences(
        DateTime start,
        TaskRepeatUnit unit,
        int interval,
        TaskWeekdays weekdays,
        DateTime from,
        DateTime to,
        int? weekdayOrdinal = null,
        int? month = null,
        TaskWeekdays? weekStart = null,
        DateTime? until = null)
    {
        if (unit == TaskRepeatUnit.None || !Enum.IsDefined(unit))
        {
            return [];
        }

        var firstDay = start.Date;
        var normalizedInterval = NormalizeInterval(interval);
        var selectedWeekdays = weekdays == TaskWeekdays.None ? ToWeekdays(firstDay.DayOfWeek) : weekdays;
        var lastDay = until.HasValue && until.Value.Date < to.Date ? until.Value.Date : to.Date;
        var occurrences = new List<DateTime>();

        for (var day = from.Date > firstDay ? from.Date : firstDay; day <= lastDay; day = day.AddDays(1))
        {
            if (IsOccurrence(firstDay, day, unit, normalizedInterval, selectedWeekdays, weekdayOrdinal, month, weekStart))
            {
                occurrences.Add(day);
            }
        }

        return occurrences;
    }

    private static bool IsOccurrence(
        DateTime firstDay,
        DateTime day,
        TaskRepeatUnit unit,
        int interval,
        TaskWeekdays weekdays,
        int? weekdayOrdinal,
        int? month,
        TaskWeekdays? weekStart)
    {
        switch (unit)
        {
            case TaskRepeatUnit.Day:
                return (day - firstDay).Days % interval == 0;
            case TaskRepeatUnit.Week:
                var weekStartOffset = ToMondayBasedOffset(weekStart);
                var weeksBetween = (GetWeekStart(day, weekStartOffset) - GetWeekStart(firstDay, weekStartOffset)).Days / 7;
                return weeksBetween % interval == 0 && weekdays.HasFlag(ToWeekdays(day.DayOfWeek));
            case TaskRepeatUnit.Month:
                var monthsBetween = (day.Year - firstDay.Year) * 12 + day.Month - firstDay.Month;
                return monthsBetween % interval == 0 &&
                    day.Day == Math.Min(firstDay.Day, DateTime.DaysInMonth(day.Year, day.Month));
            case TaskRepeatUnit.Year:
                var yearsBetween = day.Year - firstDay.Year;
                return yearsBetween % interval == 0 &&
                    day.Month == firstDay.Month &&
                    day.Day == Math.Min(firstDay.Day, DateTime.DaysInMonth(day.Year, firstDay.Month));
            case TaskRepeatUnit.MonthWeekday:
                var monthWeekdayMonthsBetween = (day.Year - firstDay.Year) * 12 + day.Month - firstDay.Month;
                return monthWeekdayMonthsBetween % interval == 0 &&
                    weekdays.HasFlag(ToWeekdays(day.DayOfWeek)) &&
                    MatchesWeekdayOrdinal(day, weekdayOrdinal);
            case TaskRepeatUnit.YearWeekday:
                var yearWeekdayYearsBetween = day.Year - firstDay.Year;
                return yearWeekdayYearsBetween % interval == 0 &&
                    day.Month == month &&
                    weekdays.HasFlag(ToWeekdays(day.DayOfWeek)) &&
                    MatchesWeekdayOrdinal(day, weekdayOrdinal);
            default:
                return false;
        }
    }

    private static bool MatchesWeekdayOrdinal(DateTime day, int? ordinal)
    {
        if (ordinal == -1)
        {
            return day.AddDays(7).Month != day.Month;
        }

        return GetWeekdayOrdinalInMonth(day) == ordinal;
    }

    private static int GetWeekdayOrdinalInMonth(DateTime day) => (day.Day - 1) / 7 + 1;

    private static DateTime GetWeekStart(DateTime day, int weekStartOffset)
    {
        var dayOffset = ((int)day.DayOfWeek + 6) % 7;
        return day.AddDays(-(((dayOffset - weekStartOffset) % 7 + 7) % 7));
    }

    private static int ToMondayBasedOffset(TaskWeekdays? weekStart) =>
        weekStart is { } value and not TaskWeekdays.None ? (int)Math.Log2((double)(int)value) : 0;
}
