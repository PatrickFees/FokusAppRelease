using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class CompanionMoodService : ICompanionMoodService
{
    private readonly IBalanceForYouToDoService _balanceService;
    private readonly TimeProvider _timeProvider;

    public CompanionMoodService(IBalanceForYouToDoService balanceService, TimeProvider timeProvider)
    {
        _balanceService = balanceService;
        _timeProvider = timeProvider;
    }

    public async Task<CompanionMood> GetRecentMoodAsync()
    {
        var today = _timeProvider.GetLocalNow().Date;
        var windowStart = today.AddDays(1 - CompanionMoodRules.DaysLookedAt);

        var currentWeek = await _balanceService.GetWeekAsync(today).ConfigureAwait(false);
        var days = currentWeek.Days.AsEnumerable();
        if (windowStart < currentWeek.WeekStart)
        {
            var previousWeek = await _balanceService.GetWeekAsync(currentWeek.WeekStart.AddDays(-1)).ConfigureAwait(false);
            days = previousWeek.Days.Concat(currentWeek.Days);
        }

        return CompanionMoodRules.Evaluate(days.Where(day => day.Date >= windowStart && day.Date <= today));
    }
}
