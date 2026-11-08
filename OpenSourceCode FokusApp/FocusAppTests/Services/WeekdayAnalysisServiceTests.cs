using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.Services;

public class WeekdayAnalysisServiceTests
{
    private static readonly DateTime Wednesday = new(2026, 3, 18);
    private static readonly DateTime CurrentWeekStart = new(2026, 3, 16);

    private readonly IBalanceForYouToDoService _balance = Substitute.For<IBalanceForYouToDoService>();
    private readonly WeekdayAnalysisService _service;

    public WeekdayAnalysisServiceTests()
    {
        _balance.GetWeekdayPlannedVersusDoneAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>()).Returns(EmptyWeek());
        _service = new WeekdayAnalysisService(_balance);
    }

    [Fact]
    public async Task AnalyzeAsync_Reference_LooksAtTheFourWholeWeeksBeforeTheCurrentWeek()
    {
        await _service.AnalyzeAsync(Wednesday);

        foreach (var weeksBack in new[] { 1, 2, 3, 4 })
        {
            var start = CurrentWeekStart.AddDays(-7 * weeksBack);
            await _balance.Received(1).GetWeekdayPlannedVersusDoneAsync(start, start);
        }

        await _balance.Received(4).GetWeekdayPlannedVersusDoneAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task AnalyzeAsync_NoData_HasNotEnoughDataAndNoInsights()
    {
        var analysis = await _service.AnalyzeAsync(Wednesday);

        Assert.Equal(0, analysis.WeeksWithData);
        Assert.False(analysis.HasEnoughData);
        Assert.Empty(analysis.Insights);
        Assert.Equal(7, analysis.Weekdays.Count);
        Assert.Equal(DayOfWeek.Monday, analysis.Weekdays[0].Day);
        Assert.Equal(DayOfWeek.Sunday, analysis.Weekdays[6].Day);
    }

    [Fact]
    public async Task AnalyzeAsync_OneWeekWithData_HasNotEnoughData()
    {
        var start = CurrentWeekStart.AddDays(-7);
        _balance.GetWeekdayPlannedVersusDoneAsync(start, start).Returns(WeekWithWednesday(0, 6));

        var analysis = await _service.AnalyzeAsync(Wednesday);

        Assert.Equal(1, analysis.WeeksWithData);
        Assert.False(analysis.HasEnoughData);
        Assert.Empty(analysis.Insights);
    }

    [Fact]
    public async Task AnalyzeAsync_SeveralWeeks_SumsTheWeekdaysAndFindsTheClearPattern()
    {
        foreach (var weeksBack in new[] { 1, 2, 3 })
        {
            var start = CurrentWeekStart.AddDays(-7 * weeksBack);
            _balance.GetWeekdayPlannedVersusDoneAsync(start, start).Returns(WeekWithWednesday(0, 2));
        }

        var analysis = await _service.AnalyzeAsync(Wednesday);

        var wednesday = analysis.Weekdays[2];
        Assert.Equal(DayOfWeek.Wednesday, wednesday.Day);
        Assert.Equal(6, wednesday.PlannedToDo);
        Assert.Equal(3, analysis.WeeksWithData);
        Assert.True(analysis.HasEnoughData);
        var insight = Assert.Single(analysis.Insights);
        Assert.Equal(WeekdayInsightKind.MostlyDuties, insight.Kind);
        Assert.Equal(DayOfWeek.Wednesday, insight.Day);
    }

    private static IReadOnlyList<WeekdayBalance> EmptyWeek() => Enumerable.Range(1, 7)
        .Select(index => new WeekdayBalance((DayOfWeek)(index % 7), 0, 0, 0, 0))
        .ToList();

    private static IReadOnlyList<WeekdayBalance> WeekWithWednesday(int plannedForYou, int plannedToDo) =>
        EmptyWeek()
            .Select(day => day.Day == DayOfWeek.Wednesday
                ? new WeekdayBalance(DayOfWeek.Wednesday, plannedForYou, plannedToDo, 0, 0)
                : day)
            .ToList();
}
