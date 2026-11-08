using FokusApp.Core.Enums;
using System.Text;

namespace FokusApp.Core.Services;

public static class TaskRepeatRuleBuilder
{

    public static string? TryBuild(
        TaskRepeatUnit unit,
        int interval,
        TaskWeekdays weekdays,
        int? weekdayOrdinal,
        int? month,
        TaskWeekdays? weekStart,
        DateTime? repeatUntil)
    {
        if (unit == TaskRepeatUnit.None)
        {
            return null;
        }

        var parts = new List<string> { $"FREQ={ToFreq(unit)}" };

        if (interval != 1)
        {
            parts.Add($"INTERVAL={interval}");
        }

        switch (unit)
        {
            case TaskRepeatUnit.Week:
                if (weekdays != TaskWeekdays.None)
                {
                    parts.Add($"BYDAY={ToWeekdayCsv(weekdays)}");
                }

                if (weekStart is { } wkst)
                {
                    parts.Add($"WKST={ToWeekdayCode(wkst)}");
                }

                break;

            case TaskRepeatUnit.MonthWeekday:
                parts.Add($"BYDAY={weekdayOrdinal}{ToWeekdayCode(weekdays)}");
                break;

            case TaskRepeatUnit.YearWeekday:
                parts.Add($"BYDAY={weekdayOrdinal}{ToWeekdayCode(weekdays)}");
                if (month is { } byMonth)
                {
                    parts.Add($"BYMONTH={byMonth}");
                }

                break;
        }

        if (repeatUntil is { } until)
        {
            parts.Add($"UNTIL={until:yyyyMMdd}");
        }

        return string.Join(';', parts);
    }

    private static string ToFreq(TaskRepeatUnit unit) => unit switch
    {
        TaskRepeatUnit.Day => "DAILY",
        TaskRepeatUnit.Week => "WEEKLY",
        TaskRepeatUnit.Month or TaskRepeatUnit.MonthWeekday => "MONTHLY",
        TaskRepeatUnit.Year or TaskRepeatUnit.YearWeekday => "YEARLY",
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Not a repeating unit.")
    };

    private static string ToWeekdayCsv(TaskWeekdays weekdays)
    {
        var builder = new StringBuilder();
        foreach (var flag in OrderedWeekdays)
        {
            if (weekdays.HasFlag(flag))
            {
                if (builder.Length > 0)
                {
                    builder.Append(',');
                }

                builder.Append(ToWeekdayCode(flag));
            }
        }

        return builder.ToString();
    }

    private static readonly TaskWeekdays[] OrderedWeekdays =
    [
        TaskWeekdays.Monday, TaskWeekdays.Tuesday, TaskWeekdays.Wednesday, TaskWeekdays.Thursday,
        TaskWeekdays.Friday, TaskWeekdays.Saturday, TaskWeekdays.Sunday
    ];

    private static string ToWeekdayCode(TaskWeekdays weekday) => weekday switch
    {
        TaskWeekdays.Monday => "MO",
        TaskWeekdays.Tuesday => "TU",
        TaskWeekdays.Wednesday => "WE",
        TaskWeekdays.Thursday => "TH",
        TaskWeekdays.Friday => "FR",
        TaskWeekdays.Saturday => "SA",
        TaskWeekdays.Sunday => "SU",
        _ => throw new ArgumentOutOfRangeException(nameof(weekday), weekday, "Not a single weekday.")
    };
}
