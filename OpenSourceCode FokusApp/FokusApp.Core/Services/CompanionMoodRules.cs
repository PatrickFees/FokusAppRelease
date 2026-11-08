using FokusApp.Core.Models;

namespace FokusApp.Core.Services;

public static class CompanionMoodRules
{

    public const int DaysLookedAt = 7;

    public const int BudBalanceDays = 1;

    public const int BloomBalanceDays = 3;

    public static CompanionMood Evaluate(IEnumerable<DayBalance> days)
    {
        ArgumentNullException.ThrowIfNull(days);

        var list = days.ToList();
        var balanceDays = list.Count(day => day.IsBalanceDay);

        if (balanceDays >= BloomBalanceDays)
        {
            return CompanionMood.Blooming;
        }

        if (balanceDays >= BudBalanceDays)
        {
            return CompanionMood.Budding;
        }

        return list.Any(day => day.DoneTotal > 0) ? CompanionMood.Awake : CompanionMood.Resting;
    }
}
