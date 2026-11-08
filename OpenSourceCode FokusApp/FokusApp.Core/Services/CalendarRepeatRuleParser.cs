using FokusApp.Core.Enums;
using System.Globalization;

namespace FokusApp.Core.Services;

public static class CalendarRepeatRuleParser
{

    public static bool TryParse(
        string? rrule,
        DateTime seriesAnchorDate,
        out TaskRepeatUnit unit,
        out int interval,
        out TaskWeekdays weekdays,
        out int? weekdayOrdinal,
        out int? month,
        out TaskWeekdays? weekStart,
        out DateTime? repeatUntil)
    {
        unit = TaskRepeatUnit.None;
        interval = 1;
        weekdays = TaskWeekdays.None;
        weekdayOrdinal = null;
        month = null;
        weekStart = null;
        repeatUntil = null;

        if (string.IsNullOrWhiteSpace(rrule))
        {
            return false;
        }

        var parts = ParseParts(rrule);
        if (!parts.TryGetValue("FREQ", out var freq) ||
            parts.ContainsKey("BYSETPOS") || parts.ContainsKey("BYYEARDAY") ||
            parts.ContainsKey("BYMONTHDAY") || parts.ContainsKey("BYWEEKNO"))
        {
            return false;
        }

        if (parts.TryGetValue("INTERVAL", out var intervalText) &&
            int.TryParse(intervalText, out var parsedInterval) && parsedInterval > 0)
        {
            interval = TaskRepeatCalculator.NormalizeInterval(parsedInterval);
        }

        var byDay = parts.TryGetValue("BYDAY", out var byDayText)
            ? byDayText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];

        var matched = false;
        switch (freq.Trim().ToUpperInvariant())
        {
            case "DAILY":
                if (byDay.Length == 0)
                {
                    unit = TaskRepeatUnit.Day;
                    matched = true;
                }

                break;

            case "WEEKLY":
                if (!byDay.Any(HasOrdinalPrefix))
                {
                    unit = TaskRepeatUnit.Week;
                    weekdays = byDay.Length == 0
                        ? TaskRepeatCalculator.ToWeekdays(seriesAnchorDate.DayOfWeek)
                        : ToWeekdayFlags(byDay);
                    weekStart = parts.TryGetValue("WKST", out var wkstText) && TryParseWeekdayCode(wkstText, out var wkst)
                        ? wkst
                        : null;
                    matched = weekdays != TaskWeekdays.None;
                }

                break;

            case "MONTHLY":
                if (byDay.Length == 0)
                {
                    unit = TaskRepeatUnit.Month;
                    matched = true;
                }
                else if (byDay.Length == 1 && TryParseOrdinalWeekday(byDay[0], out var monthWeekday, out var monthOrdinal))
                {
                    unit = TaskRepeatUnit.MonthWeekday;
                    weekdays = monthWeekday;
                    weekdayOrdinal = monthOrdinal;
                    matched = true;
                }

                break;

            case "YEARLY":
                if (byDay.Length == 0)
                {
                    unit = TaskRepeatUnit.Year;
                    matched = true;
                }
                else if (byDay.Length == 1 && TryParseOrdinalWeekday(byDay[0], out var yearWeekday, out var yearOrdinal))
                {
                    unit = TaskRepeatUnit.YearWeekday;
                    weekdays = yearWeekday;
                    weekdayOrdinal = yearOrdinal;
                    month = parts.TryGetValue("BYMONTH", out var byMonthText) &&
                        int.TryParse(byMonthText.Split(',')[0], out var parsedMonth) && parsedMonth is >= 1 and <= 12
                        ? parsedMonth
                        : seriesAnchorDate.Month;
                    matched = true;
                }

                break;
        }

        if (!matched)
        {
            return false;
        }

        repeatUntil = ParseRepeatUntil(parts, seriesAnchorDate, unit, interval, weekdays, weekdayOrdinal, month, weekStart);
        return true;
    }

    private static DateTime? ParseRepeatUntil(
        IReadOnlyDictionary<string, string> parts,
        DateTime seriesAnchorDate,
        TaskRepeatUnit unit,
        int interval,
        TaskWeekdays weekdays,
        int? weekdayOrdinal,
        int? month,
        TaskWeekdays? weekStart)
    {
        if (parts.TryGetValue("UNTIL", out var untilText) && untilText.Length >= 8 &&
            DateTime.TryParseExact(
                untilText[..8], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var until))
        {
            return until.Date;
        }

        if (parts.TryGetValue("COUNT", out var countText) &&
            int.TryParse(countText, out var count) && count > 0)
        {
            var occurrences = TaskRepeatCalculator.GetOccurrences(
                seriesAnchorDate,
                unit,
                interval,
                weekdays,
                seriesAnchorDate,
                seriesAnchorDate.AddYears(AppConstants.RepeatSearchYears),
                weekdayOrdinal,
                month,
                weekStart);
            return count <= occurrences.Count ? occurrences[count - 1] : null;
        }

        return null;
    }

    private static bool HasOrdinalPrefix(string byDayValue) =>
        byDayValue.Length > 2 && (char.IsDigit(byDayValue[0]) || byDayValue[0] == '-');

    private static bool TryParseOrdinalWeekday(string value, out TaskWeekdays weekday, out int ordinal)
    {
        weekday = TaskWeekdays.None;
        ordinal = 0;

        if (value.Length < 3 || !TryParseWeekdayCode(value[^2..], out weekday))
        {
            weekday = TaskWeekdays.None;
            return false;
        }

        var ordinalText = value[..^2];
        return int.TryParse(ordinalText, out ordinal) && TaskRepeatCalculator.IsValidWeekdayOrdinal(ordinal);
    }

    private static TaskWeekdays ToWeekdayFlags(IEnumerable<string> byDay)
    {
        var result = TaskWeekdays.None;
        foreach (var value in byDay)
        {
            if (TryParseWeekdayCode(value, out var flag))
            {
                result |= flag;
            }
        }

        return result;
    }

    private static bool TryParseWeekdayCode(string code, out TaskWeekdays weekday)
    {
        weekday = code.Trim().ToUpperInvariant() switch
        {
            "MO" => TaskWeekdays.Monday,
            "TU" => TaskWeekdays.Tuesday,
            "WE" => TaskWeekdays.Wednesday,
            "TH" => TaskWeekdays.Thursday,
            "FR" => TaskWeekdays.Friday,
            "SA" => TaskWeekdays.Saturday,
            "SU" => TaskWeekdays.Sunday,
            _ => TaskWeekdays.None
        };

        return weekday != TaskWeekdays.None;
    }

    private static Dictionary<string, string> ParseParts(string rrule)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in rrule.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var keyValue = pair.Split('=', 2);
            if (keyValue.Length == 2)
            {
                result[keyValue[0].Trim().ToUpperInvariant()] = keyValue[1].Trim();
            }
        }

        return result;
    }
}
