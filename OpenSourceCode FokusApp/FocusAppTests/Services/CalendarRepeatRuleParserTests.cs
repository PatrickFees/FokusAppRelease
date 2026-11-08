using FokusApp.Core.Enums;
using FokusApp.Core.Services;

namespace FocusAppTests.Services;

public class CalendarRepeatRuleParserTests
{

    private static readonly DateTime Anchor = new(2026, 3, 2);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_NoRule_ReturnsFalse(string? rrule)
    {
        var success = CalendarRepeatRuleParser.TryParse(
            rrule, Anchor, out _, out _, out _, out _, out _, out _, out _);

        Assert.False(success);
    }

    [Fact]
    public void TryParse_Daily_MapsToDay()
    {
        var success = CalendarRepeatRuleParser.TryParse(
            "FREQ=DAILY", Anchor, out var unit, out var interval, out var weekdays, out var ordinal, out var month, out _, out _);

        Assert.True(success);
        Assert.Equal(TaskRepeatUnit.Day, unit);
        Assert.Equal(1, interval);
        Assert.Equal(TaskWeekdays.None, weekdays);
        Assert.Null(ordinal);
        Assert.Null(month);
    }

    [Fact]
    public void TryParse_DailyWithInterval_NormalizesInterval()
    {
        var success = CalendarRepeatRuleParser.TryParse(
            "FREQ=DAILY;INTERVAL=3", Anchor, out var unit, out var interval, out _, out _, out _, out _, out _);

        Assert.True(success);
        Assert.Equal(TaskRepeatUnit.Day, unit);
        Assert.Equal(3, interval);
    }

    [Fact]
    public void TryParse_WeeklyWithByDay_MapsWeekdayFlags()
    {
        var success = CalendarRepeatRuleParser.TryParse(
            "FREQ=WEEKLY;BYDAY=MO,WE", Anchor, out var unit, out _, out var weekdays, out _, out _, out _, out _);

        Assert.True(success);
        Assert.Equal(TaskRepeatUnit.Week, unit);
        Assert.Equal(TaskWeekdays.Monday | TaskWeekdays.Wednesday, weekdays);
    }

    [Fact]
    public void TryParse_WeeklyWithoutByDay_UsesAnchorWeekday()
    {
        var success = CalendarRepeatRuleParser.TryParse(
            "FREQ=WEEKLY", Anchor, out var unit, out _, out var weekdays, out _, out _, out _, out _);

        Assert.True(success);
        Assert.Equal(TaskRepeatUnit.Week, unit);
        Assert.Equal(TaskWeekdays.Monday, weekdays);
    }

    [Fact]
    public void TryParse_WeeklyWithWkst_MapsWeekStart()
    {
        var success = CalendarRepeatRuleParser.TryParse(
            "FREQ=WEEKLY;INTERVAL=2;WKST=SU", Anchor, out _, out _, out _, out _, out _, out var weekStart, out _);

        Assert.True(success);
        Assert.Equal(TaskWeekdays.Sunday, weekStart);
    }

    [Fact]
    public void TryParse_WeeklyWithoutWkst_WeekStartIsNull()
    {
        var success = CalendarRepeatRuleParser.TryParse(
            "FREQ=WEEKLY;INTERVAL=2", Anchor, out _, out _, out _, out _, out _, out var weekStart, out _);

        Assert.True(success);
        Assert.Null(weekStart);
    }

    [Fact]
    public void TryParse_WeeklyWithInvalidWkst_WeekStartIsNull()
    {
        var success = CalendarRepeatRuleParser.TryParse(
            "FREQ=WEEKLY;WKST=XX", Anchor, out _, out _, out _, out _, out _, out var weekStart, out _);

        Assert.True(success);
        Assert.Null(weekStart);
    }

    [Fact]
    public void TryParse_MonthlyWithWkst_WeekStartIsIgnored()
    {
        var success = CalendarRepeatRuleParser.TryParse(
            "FREQ=MONTHLY;WKST=SU", Anchor, out var unit, out _, out _, out _, out _, out var weekStart, out _);

        Assert.True(success);
        Assert.Equal(TaskRepeatUnit.Month, unit);
        Assert.Null(weekStart);
    }

    [Fact]
    public void TryParse_MonthlyWithoutByDay_MapsToMonth()
    {
        var success = CalendarRepeatRuleParser.TryParse(
            "FREQ=MONTHLY", Anchor, out var unit, out _, out _, out var ordinal, out var month, out _, out _);

        Assert.True(success);
        Assert.Equal(TaskRepeatUnit.Month, unit);
        Assert.Null(ordinal);
        Assert.Null(month);
    }

    [Fact]
    public void TryParse_MonthlyWithOrdinalByDay_MapsToMonthWeekday()
    {
        var success = CalendarRepeatRuleParser.TryParse(
            "FREQ=MONTHLY;BYDAY=1MO", Anchor, out var unit, out _, out var weekdays, out var ordinal, out var month, out _, out _);

        Assert.True(success);
        Assert.Equal(TaskRepeatUnit.MonthWeekday, unit);
        Assert.Equal(TaskWeekdays.Monday, weekdays);
        Assert.Equal(1, ordinal);
        Assert.Null(month);
    }

    [Fact]
    public void TryParse_MonthlyWithLastByDay_MapsOrdinalMinusOne()
    {
        var success = CalendarRepeatRuleParser.TryParse(
            "FREQ=MONTHLY;BYDAY=-1FR", Anchor, out var unit, out _, out var weekdays, out var ordinal, out _, out _, out _);

        Assert.True(success);
        Assert.Equal(TaskRepeatUnit.MonthWeekday, unit);
        Assert.Equal(TaskWeekdays.Friday, weekdays);
        Assert.Equal(-1, ordinal);
    }

    [Fact]
    public void TryParse_YearlyWithoutByDay_MapsToYear()
    {
        var success = CalendarRepeatRuleParser.TryParse(
            "FREQ=YEARLY", Anchor, out var unit, out _, out _, out var ordinal, out var month, out _, out _);

        Assert.True(success);
        Assert.Equal(TaskRepeatUnit.Year, unit);
        Assert.Null(ordinal);
        Assert.Null(month);
    }

    [Fact]
    public void TryParse_YearlyWithOrdinalByDayAndMonth_MapsToYearWeekday()
    {
        var success = CalendarRepeatRuleParser.TryParse(
            "FREQ=YEARLY;BYDAY=-1FR;BYMONTH=11",
            Anchor,
            out var unit,
            out _,
            out var weekdays,
            out var ordinal,
            out var month,
            out _,
            out _);

        Assert.True(success);
        Assert.Equal(TaskRepeatUnit.YearWeekday, unit);
        Assert.Equal(TaskWeekdays.Friday, weekdays);
        Assert.Equal(-1, ordinal);
        Assert.Equal(11, month);
    }

    [Fact]
    public void TryParse_YearlyWithOrdinalByDayWithoutByMonth_FallsBackToAnchorMonth()
    {
        var success = CalendarRepeatRuleParser.TryParse(
            "FREQ=YEARLY;BYDAY=1MO", Anchor, out _, out _, out _, out _, out var month, out _, out _);

        Assert.True(success);
        Assert.Equal(Anchor.Month, month);
    }

    [Theory]
    [InlineData("FREQ=MONTHLY;BYDAY=1MO,3FR")]
    [InlineData("FREQ=MONTHLY;BYMONTHDAY=15")]
    [InlineData("FREQ=YEARLY;BYSETPOS=-1;BYDAY=FR")]
    [InlineData("FREQ=WEEKLY;BYDAY=1MO")]
    [InlineData("FREQ=SECONDLY")]
    public void TryParse_UnsupportedRule_ReturnsFalse(string rrule)
    {
        var success = CalendarRepeatRuleParser.TryParse(
            rrule, Anchor, out _, out _, out _, out _, out _, out _, out _);

        Assert.False(success);
    }

    [Fact]
    public void TryParse_WithUntil_MapsRepeatUntilToThatDay()
    {
        var success = CalendarRepeatRuleParser.TryParse(
            "FREQ=DAILY;UNTIL=20260410T000000Z", Anchor, out _, out _, out _, out _, out _, out _, out var repeatUntil);

        Assert.True(success);
        Assert.Equal(new DateTime(2026, 4, 10), repeatUntil);
    }

    [Fact]
    public void TryParse_WithCount_DerivesRepeatUntilFromTheNthOccurrence()
    {
        var success = CalendarRepeatRuleParser.TryParse(
            "FREQ=DAILY;COUNT=5", Anchor, out _, out _, out _, out _, out _, out _, out var repeatUntil);

        Assert.True(success);
        Assert.Equal(Anchor.AddDays(4), repeatUntil);
    }

    [Fact]
    public void TryParse_WithUntilAndCount_UntilTakesPrecedence()
    {
        var success = CalendarRepeatRuleParser.TryParse(
            "FREQ=DAILY;COUNT=5;UNTIL=20260410T000000Z", Anchor, out _, out _, out _, out _, out _, out _, out var repeatUntil);

        Assert.True(success);
        Assert.Equal(new DateTime(2026, 4, 10), repeatUntil);
    }

    [Fact]
    public void TryParse_WithoutUntilOrCount_RepeatUntilIsNull()
    {
        var success = CalendarRepeatRuleParser.TryParse(
            "FREQ=DAILY", Anchor, out _, out _, out _, out _, out _, out _, out var repeatUntil);

        Assert.True(success);
        Assert.Null(repeatUntil);
    }

    [Fact]
    public void TryParse_CountBeyondSearchHorizon_RepeatUntilIsNull()
    {
        var success = CalendarRepeatRuleParser.TryParse(
            "FREQ=YEARLY;COUNT=200", Anchor, out _, out _, out _, out _, out _, out _, out var repeatUntil);

        Assert.True(success);
        Assert.Null(repeatUntil);
    }
}
