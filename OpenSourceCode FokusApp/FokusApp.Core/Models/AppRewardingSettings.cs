using FokusApp.Core.Enums;

namespace FokusApp.Core.Models;

public sealed record AppRewardingSettings(
    bool MicroFeedbackEnabled,
    bool ReviewsEnabled,
    bool CompanionEnabled,
    BalanceGoal BalanceGoal,
    int CustomForYou = AppRewardingSettings.DefaultCustomPart,
    int CustomToDo = AppRewardingSettings.DefaultCustomPart)
{

    public const int DefaultCustomPart = 1;
    public const int MaximumCustomPart = 20;

    public static int ClampCustomPart(int part) => Math.Clamp(part, DefaultCustomPart, MaximumCustomPart);

    private const int LegacyOneToTwo = 1;
    private const int LegacyOneToThree = 2;
    private const int LegacyMoreForYou = 3;

    public static (BalanceGoal Goal, int ForYou, int ToDo) ReadStoredGoal(int goal, int forYou, int toDo)
    {
        return goal switch
        {
            LegacyOneToTwo => (BalanceGoal.Custom, 1, 2),
            LegacyOneToThree => (BalanceGoal.Custom, 1, 3),
            LegacyMoreForYou => (BalanceGoal.OneToOne, DefaultCustomPart, DefaultCustomPart),
            _ when Enum.IsDefined((BalanceGoal)goal) => ((BalanceGoal)goal, ClampCustomPart(forYou), ClampCustomPart(toDo)),
            _ => (BalanceGoal.OneToOne, DefaultCustomPart, DefaultCustomPart)
        };
    }

    public static bool IsKnownStoredGoal(int goal) =>
        goal is >= LegacyOneToTwo and <= LegacyMoreForYou || Enum.IsDefined((BalanceGoal)goal);

    public static AppRewardingSettings Default { get; } = new(
        MicroFeedbackEnabled: true,
        ReviewsEnabled: true,
        CompanionEnabled: true,
        BalanceGoal: BalanceGoal.OneToOne);
}
