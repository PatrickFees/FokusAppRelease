using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Core.Services;

namespace FocusAppTests.Services;

public class WeekdayPatternRulesTests
{
    [Fact]
    public void Detect_TooFewWeeks_FindsNothingEvenWithClearNumbers()
    {
        var weekdays = Week(Day(DayOfWeek.Wednesday, plannedForYou: 0, plannedToDo: 10));

        var insights = WeekdayPatternRules.Detect(weekdays, weeksWithData: 1);

        Assert.Empty(insights);
    }

    [Fact]
    public void Detect_NothingPlanned_FindsNothing()
    {
        var insights = WeekdayPatternRules.Detect(Week(), weeksWithData: 4);

        Assert.Empty(insights);
    }

    [Fact]
    public void Detect_AlmostOnlyDuties_FindsMostlyDuties()
    {
        var weekdays = Week(Day(DayOfWeek.Wednesday, plannedForYou: 1, plannedToDo: 9));

        var insights = WeekdayPatternRules.Detect(weekdays, weeksWithData: 3);

        var insight = Assert.Single(insights);
        Assert.Equal(DayOfWeek.Wednesday, insight.Day);
        Assert.Equal(WeekdayInsightKind.MostlyDuties, insight.Kind);
    }

    [Fact]
    public void Detect_FewTasksOnly_IsNoise()
    {
        var weekdays = Week(Day(DayOfWeek.Wednesday, plannedForYou: 0, plannedToDo: 3));

        var insights = WeekdayPatternRules.Detect(weekdays, weeksWithData: 4);

        Assert.Empty(insights);
    }

    [Fact]
    public void Detect_BalancedPlanning_FindsNoMostlyDuties()
    {
        var weekdays = Week(Day(DayOfWeek.Wednesday, plannedForYou: 4, plannedToDo: 6, doneForYou: 3));

        var insights = WeekdayPatternRules.Detect(weekdays, weeksWithData: 4);

        Assert.Empty(insights);
    }

    [Fact]
    public void Detect_ForYouPlannedButSeldomDone_FindsRarelyDone()
    {
        var weekdays = Week(Day(DayOfWeek.Tuesday, plannedForYou: 6, plannedToDo: 6, doneForYou: 2));

        var insights = WeekdayPatternRules.Detect(weekdays, weeksWithData: 4);

        var insight = Assert.Single(insights);
        Assert.Equal(DayOfWeek.Tuesday, insight.Day);
        Assert.Equal(WeekdayInsightKind.ForYouPlannedButRarelyDone, insight.Kind);
    }

    [Fact]
    public void Detect_ForYouOftenDone_FindsNothing()
    {
        var weekdays = Week(Day(DayOfWeek.Tuesday, plannedForYou: 6, plannedToDo: 6, doneForYou: 4));

        var insights = WeekdayPatternRules.Detect(weekdays, weeksWithData: 4);

        Assert.Empty(insights);
    }

    [Fact]
    public void Detect_OnlyTwoForYouPlanned_IsNoise()
    {
        var weekdays = Week(Day(DayOfWeek.Tuesday, plannedForYou: 2, plannedToDo: 2, doneForYou: 0));

        var insights = WeekdayPatternRules.Detect(weekdays, weeksWithData: 4);

        Assert.Empty(insights);
    }

    [Fact]
    public void Detect_BothKinds_ListsRarelyDoneFirst()
    {
        var weekdays = Week(
            Day(DayOfWeek.Wednesday, plannedForYou: 0, plannedToDo: 8),
            Day(DayOfWeek.Tuesday, plannedForYou: 5, plannedToDo: 5, doneForYou: 0));

        var insights = WeekdayPatternRules.Detect(weekdays, weeksWithData: 4);

        Assert.Equal(2, insights.Count);
        Assert.Equal(WeekdayInsightKind.ForYouPlannedButRarelyDone, insights[0].Kind);
        Assert.Equal(WeekdayInsightKind.MostlyDuties, insights[1].Kind);
    }

    [Fact]
    public void Detect_SeveralWeekdaysWithTheSamePattern_ReportsOnlyTheClearestOne()
    {
        var weekdays = Week(
            Day(DayOfWeek.Monday, plannedForYou: 1, plannedToDo: 9),
            Day(DayOfWeek.Thursday, plannedForYou: 0, plannedToDo: 5),
            Day(DayOfWeek.Friday, plannedForYou: 0, plannedToDo: 9));

        var insights = WeekdayPatternRules.Detect(weekdays, weeksWithData: 4);

        var insight = Assert.Single(insights);
        Assert.Equal(DayOfWeek.Friday, insight.Day);
    }

    private static WeekdayBalance Day(
        DayOfWeek day,
        int plannedForYou,
        int plannedToDo,
        int doneForYou = 0,
        int doneToDo = 0) => new(day, plannedForYou, plannedToDo, doneForYou, doneToDo);

    private static List<WeekdayBalance> Week(params WeekdayBalance[] days) => Enumerable.Range(1, 7)
        .Select(index => (DayOfWeek)(index % 7))
        .Select(day => days.FirstOrDefault(item => item.Day == day) ?? new WeekdayBalance(day, 0, 0, 0, 0))
        .ToList();
}
