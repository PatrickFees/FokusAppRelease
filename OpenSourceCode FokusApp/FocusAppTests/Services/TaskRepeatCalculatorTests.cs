using FokusApp.Core.Enums;
using FokusApp.Core.Services;

namespace FocusAppTests.Services;

public class TaskRepeatCalculatorTests
{

    private static readonly DateTime Start = new(2026, 3, 2);

    [Fact]
    public void GetOccurrences_NoRepeat_ReturnsNothing()
    {
        var occurrences = TaskRepeatCalculator.GetOccurrences(
            Start, TaskRepeatUnit.None, 1, TaskWeekdays.None, Start, Start.AddDays(30));

        Assert.Empty(occurrences);
    }

    [Fact]
    public void GetOccurrences_EveryDay_ReturnsEveryDayIncludingStart()
    {
        var occurrences = TaskRepeatCalculator.GetOccurrences(
            Start, TaskRepeatUnit.Day, 1, TaskWeekdays.None, Start, Start.AddDays(3));

        Assert.Equal([Start, Start.AddDays(1), Start.AddDays(2), Start.AddDays(3)], occurrences);
    }

    [Fact]
    public void GetOccurrences_EveryThirdDay_SkipsTwoDaysEachTime()
    {
        var occurrences = TaskRepeatCalculator.GetOccurrences(
            Start, TaskRepeatUnit.Day, 3, TaskWeekdays.None, Start, Start.AddDays(9));

        Assert.Equal([Start, Start.AddDays(3), Start.AddDays(6), Start.AddDays(9)], occurrences);
    }

    [Fact]
    public void GetOccurrences_RangeStartsLater_KeepsTheRhythmOfTheStart()
    {
        var occurrences = TaskRepeatCalculator.GetOccurrences(
            Start, TaskRepeatUnit.Day, 3, TaskWeekdays.None, Start.AddDays(4), Start.AddDays(9));

        Assert.Equal([Start.AddDays(6), Start.AddDays(9)], occurrences);
    }

    [Fact]
    public void GetOccurrences_RangeEndsBeforeStart_ReturnsNothing()
    {
        var occurrences = TaskRepeatCalculator.GetOccurrences(
            Start, TaskRepeatUnit.Day, 1, TaskWeekdays.None, Start.AddDays(-10), Start.AddDays(-1));

        Assert.Empty(occurrences);
    }

    [Fact]
    public void GetOccurrences_WeeklyOnSelectedWeekdays_ReturnsOnlyThoseDays()
    {
        var weekdays = TaskWeekdays.Monday | TaskWeekdays.Wednesday;

        var occurrences = TaskRepeatCalculator.GetOccurrences(
            Start, TaskRepeatUnit.Week, 1, weekdays, Start, Start.AddDays(9));

        Assert.Equal([Start, Start.AddDays(2), Start.AddDays(7), Start.AddDays(9)], occurrences);
    }

    [Fact]
    public void GetOccurrences_WeeklyWithoutWeekdays_UsesWeekdayOfStart()
    {
        var occurrences = TaskRepeatCalculator.GetOccurrences(
            Start, TaskRepeatUnit.Week, 1, TaskWeekdays.None, Start, Start.AddDays(14));

        Assert.Equal([Start, Start.AddDays(7), Start.AddDays(14)], occurrences);
    }

    [Fact]
    public void GetOccurrences_EverySecondWeek_SkipsEveryOtherWeek()
    {
        var weekdays = TaskWeekdays.Monday | TaskWeekdays.Friday;

        var occurrences = TaskRepeatCalculator.GetOccurrences(
            Start, TaskRepeatUnit.Week, 2, weekdays, Start, Start.AddDays(20));

        Assert.Equal([Start, Start.AddDays(4), Start.AddDays(14), Start.AddDays(18)], occurrences);
    }

    [Fact]
    public void GetOccurrences_EveryOtherWeekDefaultWeekStart_GroupsSundayWithThePrecedingMonday()
    {
        var sundayStart = Start.AddDays(-1);
        var weekdays = TaskWeekdays.Sunday | TaskWeekdays.Monday;

        var occurrences = TaskRepeatCalculator.GetOccurrences(
            sundayStart, TaskRepeatUnit.Week, 2, weekdays, sundayStart, sundayStart.AddDays(15));

        Assert.Equal(
            [sundayStart, sundayStart.AddDays(8), sundayStart.AddDays(14)],
            occurrences);
    }

    [Fact]
    public void GetOccurrences_EveryOtherWeekWithSundayWeekStart_GroupsSundayWithTheFollowingMonday()
    {
        var sundayStart = Start.AddDays(-1);
        var weekdays = TaskWeekdays.Sunday | TaskWeekdays.Monday;

        var occurrences = TaskRepeatCalculator.GetOccurrences(
            sundayStart, TaskRepeatUnit.Week, 2, weekdays, sundayStart, sundayStart.AddDays(15), weekStart: TaskWeekdays.Sunday);

        Assert.Equal(
            [sundayStart, sundayStart.AddDays(1), sundayStart.AddDays(14), sundayStart.AddDays(15)],
            occurrences);
    }

    [Fact]
    public void GetOccurrences_WeeklyStartingMidWeek_DoesNotReturnEarlierWeekdaysOfFirstWeek()
    {
        var wednesdayStart = Start.AddDays(2);
        var weekdays = TaskWeekdays.Monday | TaskWeekdays.Wednesday;

        var occurrences = TaskRepeatCalculator.GetOccurrences(
            wednesdayStart, TaskRepeatUnit.Week, 1, weekdays, wednesdayStart, wednesdayStart.AddDays(7));

        Assert.Equal([wednesdayStart, wednesdayStart.AddDays(5), wednesdayStart.AddDays(7)], occurrences);
    }

    [Fact]
    public void GetOccurrences_Monthly_ReturnsSameDayOfMonth()
    {
        var start = new DateTime(2026, 1, 15);

        var occurrences = TaskRepeatCalculator.GetOccurrences(
            start, TaskRepeatUnit.Month, 1, TaskWeekdays.None, start, new DateTime(2026, 4, 30));

        Assert.Equal(
            [start, new DateTime(2026, 2, 15), new DateTime(2026, 3, 15), new DateTime(2026, 4, 15)],
            occurrences);
    }

    [Fact]
    public void GetOccurrences_MonthlyOnThe31st_FallsBackToLastDayOfShortMonths()
    {
        var start = new DateTime(2026, 1, 31);

        var occurrences = TaskRepeatCalculator.GetOccurrences(
            start, TaskRepeatUnit.Month, 1, TaskWeekdays.None, start, new DateTime(2026, 4, 30));

        Assert.Equal(
            [start, new DateTime(2026, 2, 28), new DateTime(2026, 3, 31), new DateTime(2026, 4, 30)],
            occurrences);
    }

    [Fact]
    public void GetOccurrences_EveryThirdMonth_SkipsTwoMonths()
    {
        var start = new DateTime(2026, 1, 10);

        var occurrences = TaskRepeatCalculator.GetOccurrences(
            start, TaskRepeatUnit.Month, 3, TaskWeekdays.None, start, new DateTime(2026, 12, 31));

        Assert.Equal(
            [start, new DateTime(2026, 4, 10), new DateTime(2026, 7, 10), new DateTime(2026, 10, 10)],
            occurrences);
    }

    [Fact]
    public void GetOccurrences_Yearly_ReturnsSameDayEachYear()
    {
        var start = new DateTime(2024, 5, 20);

        var occurrences = TaskRepeatCalculator.GetOccurrences(
            start, TaskRepeatUnit.Year, 1, TaskWeekdays.None, start, new DateTime(2026, 12, 31));

        Assert.Equal([start, new DateTime(2025, 5, 20), new DateTime(2026, 5, 20)], occurrences);
    }

    [Fact]
    public void GetOccurrences_YearlyOnLeapDay_FallsBackToFebruary28InOtherYears()
    {
        var start = new DateTime(2024, 2, 29);

        var occurrences = TaskRepeatCalculator.GetOccurrences(
            start, TaskRepeatUnit.Year, 1, TaskWeekdays.None, start, new DateTime(2026, 12, 31));

        Assert.Equal([start, new DateTime(2025, 2, 28), new DateTime(2026, 2, 28)], occurrences);
    }

    [Fact]
    public void GetOccurrences_MonthWeekdayFirstMonday_ReturnsFirstMondayOfEachMonth()
    {
        var start = new DateTime(2026, 3, 2);

        var occurrences = TaskRepeatCalculator.GetOccurrences(
            start, TaskRepeatUnit.MonthWeekday, 1, TaskWeekdays.Monday, start, new DateTime(2026, 5, 31), weekdayOrdinal: 1);

        Assert.Equal([start, new DateTime(2026, 4, 6), new DateTime(2026, 5, 4)], occurrences);
    }

    [Fact]
    public void GetOccurrences_MonthWeekdayLastFriday_ReturnsLastFridayOfEachMonth()
    {
        var start = new DateTime(2026, 3, 27);

        var occurrences = TaskRepeatCalculator.GetOccurrences(
            start, TaskRepeatUnit.MonthWeekday, 1, TaskWeekdays.Friday, start, new DateTime(2026, 5, 31), weekdayOrdinal: -1);

        Assert.Equal([start, new DateTime(2026, 4, 24), new DateTime(2026, 5, 29)], occurrences);
    }

    [Fact]
    public void GetOccurrences_MonthWeekdayEveryOtherMonth_SkipsAMonth()
    {
        var start = new DateTime(2026, 1, 6);

        var occurrences = TaskRepeatCalculator.GetOccurrences(
            start, TaskRepeatUnit.MonthWeekday, 2, TaskWeekdays.Tuesday, start, new DateTime(2026, 5, 31), weekdayOrdinal: 1);

        Assert.Equal([start, new DateTime(2026, 3, 3), new DateTime(2026, 5, 5)], occurrences);
    }

    [Fact]
    public void GetOccurrences_YearWeekdayLastFridayOfNovember_ReturnsOneDayPerYear()
    {
        var start = new DateTime(2025, 11, 28);

        var occurrences = TaskRepeatCalculator.GetOccurrences(
            start, TaskRepeatUnit.YearWeekday, 1, TaskWeekdays.Friday, start, new DateTime(2027, 12, 31),
            weekdayOrdinal: -1, month: 11);

        Assert.Equal([start, new DateTime(2026, 11, 27), new DateTime(2027, 11, 26)], occurrences);
    }

    [Fact]
    public void GetOccurrences_UntilBeforeRangeEnd_StopsAtUntil()
    {
        var occurrences = TaskRepeatCalculator.GetOccurrences(
            Start, TaskRepeatUnit.Day, 1, TaskWeekdays.None, Start, Start.AddDays(9), until: Start.AddDays(3));

        Assert.Equal([Start, Start.AddDays(1), Start.AddDays(2), Start.AddDays(3)], occurrences);
    }

    [Fact]
    public void GetOccurrences_UntilAfterRangeEnd_IsIgnored()
    {
        var occurrences = TaskRepeatCalculator.GetOccurrences(
            Start, TaskRepeatUnit.Day, 1, TaskWeekdays.None, Start, Start.AddDays(3), until: Start.AddDays(30));

        Assert.Equal([Start, Start.AddDays(1), Start.AddDays(2), Start.AddDays(3)], occurrences);
    }

    [Fact]
    public void GetOccurrences_UntilBeforeFrom_ReturnsNothing()
    {
        var occurrences = TaskRepeatCalculator.GetOccurrences(
            Start, TaskRepeatUnit.Day, 1, TaskWeekdays.None, Start.AddDays(5), Start.AddDays(9), until: Start.AddDays(2));

        Assert.Empty(occurrences);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-4, 1)]
    [InlineData(5, 5)]
    [InlineData(500, 99)]
    public void NormalizeInterval_ClampsToAllowedRange(int interval, int expected)
    {
        var normalized = TaskRepeatCalculator.NormalizeInterval(interval);

        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, TaskWeekdays.Monday)]
    [InlineData(DayOfWeek.Wednesday, TaskWeekdays.Wednesday)]
    [InlineData(DayOfWeek.Sunday, TaskWeekdays.Sunday)]
    public void ToWeekdays_MapsDayOfWeekToFlag(DayOfWeek dayOfWeek, TaskWeekdays expected)
    {
        var weekdays = TaskRepeatCalculator.ToWeekdays(dayOfWeek);

        Assert.Equal(expected, weekdays);
    }
}
