using FokusApp.Core.Enums;
using FokusApp.Core.Services;

namespace FocusAppTests.Services;

public class TaskRepeatRuleBuilderTests
{

    private static readonly DateTime Anchor = new(2026, 3, 2);

    [Fact]
    public void TryBuild_None_ReturnsNull()
    {
        var rrule = TaskRepeatRuleBuilder.TryBuild(
            TaskRepeatUnit.None, 1, TaskWeekdays.None, null, null, null, null);

        Assert.Null(rrule);
    }

    [Fact]
    public void TryBuild_Daily_ReturnsFreqOnly()
    {
        var rrule = TaskRepeatRuleBuilder.TryBuild(
            TaskRepeatUnit.Day, 1, TaskWeekdays.None, null, null, null, null);

        Assert.Equal("FREQ=DAILY", rrule);
    }

    [Fact]
    public void TryBuild_DailyWithInterval_IncludesInterval()
    {
        var rrule = TaskRepeatRuleBuilder.TryBuild(
            TaskRepeatUnit.Day, 3, TaskWeekdays.None, null, null, null, null);

        Assert.Equal("FREQ=DAILY;INTERVAL=3", rrule);
    }

    [Fact]
    public void TryBuild_Weekly_IncludesByDayInMondayFirstOrder()
    {
        var rrule = TaskRepeatRuleBuilder.TryBuild(
            TaskRepeatUnit.Week, 1, TaskWeekdays.Wednesday | TaskWeekdays.Monday, null, null, null, null);

        Assert.Equal("FREQ=WEEKLY;BYDAY=MO,WE", rrule);
    }

    [Fact]
    public void TryBuild_WeeklyWithWeekStart_IncludesWkst()
    {
        var rrule = TaskRepeatRuleBuilder.TryBuild(
            TaskRepeatUnit.Week, 2, TaskWeekdays.Monday, null, null, TaskWeekdays.Sunday, null);

        Assert.Equal("FREQ=WEEKLY;INTERVAL=2;BYDAY=MO;WKST=SU", rrule);
    }

    [Fact]
    public void TryBuild_WeeklyWithoutWeekStart_OmitsWkst()
    {
        var rrule = TaskRepeatRuleBuilder.TryBuild(
            TaskRepeatUnit.Week, 2, TaskWeekdays.Monday, null, null, null, null);

        Assert.DoesNotContain("WKST", rrule);
    }

    [Fact]
    public void TryBuild_Monthly_ReturnsFreqOnly()
    {
        var rrule = TaskRepeatRuleBuilder.TryBuild(
            TaskRepeatUnit.Month, 1, TaskWeekdays.None, null, null, null, null);

        Assert.Equal("FREQ=MONTHLY", rrule);
    }

    [Fact]
    public void TryBuild_MonthWeekday_IncludesOrdinalByDay()
    {
        var rrule = TaskRepeatRuleBuilder.TryBuild(
            TaskRepeatUnit.MonthWeekday, 1, TaskWeekdays.Monday, 1, null, null, null);

        Assert.Equal("FREQ=MONTHLY;BYDAY=1MO", rrule);
    }

    [Fact]
    public void TryBuild_MonthWeekdayLast_UsesMinusOne()
    {
        var rrule = TaskRepeatRuleBuilder.TryBuild(
            TaskRepeatUnit.MonthWeekday, 1, TaskWeekdays.Friday, -1, null, null, null);

        Assert.Equal("FREQ=MONTHLY;BYDAY=-1FR", rrule);
    }

    [Fact]
    public void TryBuild_Yearly_ReturnsFreqOnly()
    {
        var rrule = TaskRepeatRuleBuilder.TryBuild(
            TaskRepeatUnit.Year, 1, TaskWeekdays.None, null, null, null, null);

        Assert.Equal("FREQ=YEARLY", rrule);
    }

    [Fact]
    public void TryBuild_YearWeekday_IncludesOrdinalByDayAndMonth()
    {
        var rrule = TaskRepeatRuleBuilder.TryBuild(
            TaskRepeatUnit.YearWeekday, 1, TaskWeekdays.Friday, -1, 11, null, null);

        Assert.Equal("FREQ=YEARLY;BYDAY=-1FR;BYMONTH=11", rrule);
    }

    [Fact]
    public void TryBuild_WithRepeatUntil_IncludesUntil()
    {
        var rrule = TaskRepeatRuleBuilder.TryBuild(
            TaskRepeatUnit.Day, 1, TaskWeekdays.None, null, null, null, new DateTime(2026, 4, 10));

        Assert.Equal("FREQ=DAILY;UNTIL=20260410", rrule);
    }

    [Theory]
    [MemberData(nameof(RoundTripCases))]
    public void TryBuild_ThenTryParse_RoundTrips(
        TaskRepeatUnit unit, int interval, TaskWeekdays weekdays, int? ordinal, int? month, TaskWeekdays? weekStart, DateTime? until)
    {
        var rrule = TaskRepeatRuleBuilder.TryBuild(unit, interval, weekdays, ordinal, month, weekStart, until);

        var success = CalendarRepeatRuleParser.TryParse(
            rrule, Anchor,
            out var parsedUnit, out var parsedInterval, out var parsedWeekdays,
            out var parsedOrdinal, out var parsedMonth, out var parsedWeekStart, out var parsedUntil);

        Assert.True(success);
        Assert.Equal(unit, parsedUnit);
        Assert.Equal(interval, parsedInterval);
        Assert.Equal(weekdays, parsedWeekdays);
        Assert.Equal(ordinal, parsedOrdinal);
        Assert.Equal(month, parsedMonth);
        Assert.Equal(weekStart, parsedWeekStart);
        Assert.Equal(until, parsedUntil);
    }

    public static IEnumerable<object?[]> RoundTripCases()
    {
        yield return [TaskRepeatUnit.Day, 1, TaskWeekdays.None, null, null, null, null];
        yield return [TaskRepeatUnit.Day, 5, TaskWeekdays.None, null, null, null, new DateTime(2026, 6, 1)];
        yield return [TaskRepeatUnit.Week, 1, TaskWeekdays.Monday | TaskWeekdays.Friday, null, null, null, null];
        yield return [TaskRepeatUnit.Week, 2, TaskWeekdays.Tuesday, null, null, TaskWeekdays.Sunday, null];
        yield return [TaskRepeatUnit.Month, 1, TaskWeekdays.None, null, null, null, null];
        yield return [TaskRepeatUnit.Month, 3, TaskWeekdays.None, null, null, null, null];
        yield return [TaskRepeatUnit.MonthWeekday, 1, TaskWeekdays.Monday, 1, null, null, null];
        yield return [TaskRepeatUnit.MonthWeekday, 1, TaskWeekdays.Friday, -1, null, null, null];
        yield return [TaskRepeatUnit.Year, 1, TaskWeekdays.None, null, null, null, null];
        yield return [TaskRepeatUnit.YearWeekday, 1, TaskWeekdays.Friday, -1, 11, null, null];
        yield return [TaskRepeatUnit.YearWeekday, 1, TaskWeekdays.Monday, 1, 6, null, new DateTime(2030, 1, 1)];
    }
}
