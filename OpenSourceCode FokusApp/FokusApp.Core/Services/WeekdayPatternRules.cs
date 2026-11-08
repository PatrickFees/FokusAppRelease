using FokusApp.Core.Enums;
using FokusApp.Core.Models;

namespace FokusApp.Core.Services;

public static class WeekdayPatternRules
{

    public const int MinimumWeeks = 2;

    public const int MinimumPlannedTasks = 4;

    public const double MostlyDutiesMaximumForYouShare = 0.2;

    public const int MinimumPlannedForYou = 3;

    public const double RarelyDoneMaximumShare = 1.0 / 3.0;

    public static IReadOnlyList<WeekdayInsight> Detect(IReadOnlyList<WeekdayBalance> weekdays, int weeksWithData)
    {
        if (weeksWithData < MinimumWeeks)
        {
            return [];
        }

        var insights = new List<WeekdayInsight>();

        var rarelyDone = weekdays
            .Where(IsForYouPlannedButRarelyDone)
            .OrderByDescending(day => day.PlannedForYou - day.DoneForYou)
            .ThenBy(day => OrderFromMonday(day.Day))
            .FirstOrDefault();
        if (rarelyDone is not null)
        {
            insights.Add(new WeekdayInsight(rarelyDone.Day, WeekdayInsightKind.ForYouPlannedButRarelyDone));
        }

        var mostlyDuties = weekdays
            .Where(IsMostlyDuties)
            .OrderBy(day => (double)day.PlannedForYou / (day.PlannedForYou + day.PlannedToDo))
            .ThenByDescending(day => day.PlannedToDo)
            .ThenBy(day => OrderFromMonday(day.Day))
            .FirstOrDefault();
        if (mostlyDuties is not null)
        {
            insights.Add(new WeekdayInsight(mostlyDuties.Day, WeekdayInsightKind.MostlyDuties));
        }

        return insights;
    }

    private static bool IsMostlyDuties(WeekdayBalance day)
    {
        var planned = day.PlannedForYou + day.PlannedToDo;
        return planned >= MinimumPlannedTasks &&
               (double)day.PlannedForYou / planned <= MostlyDutiesMaximumForYouShare;
    }

    private static bool IsForYouPlannedButRarelyDone(WeekdayBalance day) =>
        day.PlannedForYou >= MinimumPlannedForYou &&
        (double)day.DoneForYou / day.PlannedForYou <= RarelyDoneMaximumShare;

    private static int OrderFromMonday(DayOfWeek day) => ((int)day + 6) % 7;
}
