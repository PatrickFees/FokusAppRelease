using FokusApp.Core.Enums;
using FokusApp.Core.Models;

namespace FokusApp.Core.Services;

public static class BalanceForYouToDoRules
{
    public static bool IsBalanceDay(int doneForYou, int doneToDo) => doneForYou >= 1 && doneToDo >= 1;

    public static bool MeetsGoal(
        int doneForYou,
        int doneToDo,
        BalanceGoal goal,
        int customForYou = AppRewardingSettings.DefaultCustomPart,
        int customToDo = AppRewardingSettings.DefaultCustomPart)
    {
        if (!IsBalanceDay(doneForYou, doneToDo))
        {
            return false;
        }

        var (partForYou, partToDo) = GetParts(goal, customForYou, customToDo);
        return doneForYou * partToDo >= doneToDo * partForYou;
    }

    public static bool ReachesRatio(
        int forYou,
        int toDo,
        BalanceGoal goal,
        int customForYou = AppRewardingSettings.DefaultCustomPart,
        int customToDo = AppRewardingSettings.DefaultCustomPart)
    {
        var (partForYou, partToDo) = GetParts(goal, customForYou, customToDo);
        return forYou >= 1 && forYou * partToDo >= toDo * partForYou;
    }

    public static bool ExceedsRatio(
        int forYou,
        int toDo,
        BalanceGoal goal,
        int customForYou = AppRewardingSettings.DefaultCustomPart,
        int customToDo = AppRewardingSettings.DefaultCustomPart)
    {
        var (partForYou, partToDo) = GetParts(goal, customForYou, customToDo);
        return forYou >= 1 && forYou * partToDo > toDo * partForYou;
    }

    public static GoalProgress GetProgress(
        int forYou,
        int toDo,
        BalanceGoal goal,
        int customForYou = AppRewardingSettings.DefaultCustomPart,
        int customToDo = AppRewardingSettings.DefaultCustomPart)
    {
        if (ExceedsRatio(forYou, toDo, goal, customForYou, customToDo))
        {
            return GoalProgress.Exceeded;
        }

        return ReachesRatio(forYou, toDo, goal, customForYou, customToDo) ? GoalProgress.Reached : GoalProgress.Open;
    }

    public static (int ForYou, int ToDo) GetParts(BalanceGoal goal, int customForYou, int customToDo)
    {
        return goal switch
        {
            BalanceGoal.OneToOne => (1, 1),
            BalanceGoal.TwoToThree => (2, 3),
            BalanceGoal.ThreeToTwo => (3, 2),
            BalanceGoal.Custom => (AppRewardingSettings.ClampCustomPart(customForYou), AppRewardingSettings.ClampCustomPart(customToDo)),
            _ => (1, 1)
        };
    }

    public static DateTime GetWeekStart(DateTime day)
    {
        var daysSinceMonday = ((int)day.DayOfWeek + 6) % 7;
        return day.Date.AddDays(-daysSinceMonday);
    }
}
