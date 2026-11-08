using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class WeekdayAnalysisService : IWeekdayAnalysisService
{

    public const int WeeksLookedAt = 4;

    private const int DaysInWeek = 7;

    private readonly IBalanceForYouToDoService _balanceService;

    public WeekdayAnalysisService(IBalanceForYouToDoService balanceService)
    {
        _balanceService = balanceService;
    }

    public async Task<WeekdayAnalysis> AnalyzeAsync(DateTime referenceDay)
    {
        var currentWeekStart = BalanceForYouToDoRules.GetWeekStart(referenceDay);
        var totals = new int[DaysInWeek, 4];
        var weeksWithData = 0;

        for (var weeksBack = WeeksLookedAt; weeksBack >= 1; weeksBack--)
        {
            var weekStart = currentWeekStart.AddDays(-weeksBack * DaysInWeek);
            var week = await _balanceService
                .GetWeekdayPlannedVersusDoneAsync(weekStart, weekStart)
                .ConfigureAwait(false);

            if (week.Any(day => day.PlannedForYou + day.PlannedToDo + day.DoneForYou + day.DoneToDo > 0))
            {
                weeksWithData++;
            }

            for (var index = 0; index < DaysInWeek && index < week.Count; index++)
            {
                totals[index, 0] += week[index].PlannedForYou;
                totals[index, 1] += week[index].PlannedToDo;
                totals[index, 2] += week[index].DoneForYou;
                totals[index, 3] += week[index].DoneToDo;
            }
        }

        var weekdays = Enumerable.Range(0, DaysInWeek)
            .Select(index => new WeekdayBalance(
                (DayOfWeek)(((int)DayOfWeek.Monday + index) % DaysInWeek),
                totals[index, 0],
                totals[index, 1],
                totals[index, 2],
                totals[index, 3]))
            .ToList();

        var hasEnoughData = weeksWithData >= WeekdayPatternRules.MinimumWeeks;
        var insights = WeekdayPatternRules.Detect(weekdays, weeksWithData);

        return new WeekdayAnalysis(weekdays, weeksWithData, hasEnoughData, insights);
    }
}
