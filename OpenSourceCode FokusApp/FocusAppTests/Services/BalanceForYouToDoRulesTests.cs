using FokusApp.Core.Enums;
using FokusApp.Core.Services;

namespace FocusAppTests.Services;

public class BalanceForYouToDoRulesTests
{
    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(1, 0, false)]
    [InlineData(0, 3, false)]
    [InlineData(1, 1, true)]
    [InlineData(2, 5, true)]
    [InlineData(7, 1, true)]
    public void IsBalanceDay_Counts_NeedsAtLeastOneOfEach(int forYou, int toDo, bool expected)
    {
        Assert.Equal(expected, BalanceForYouToDoRules.IsBalanceDay(forYou, toDo));
    }

    [Theory]
    [InlineData(BalanceGoal.OneToOne, 1, 1, true)]
    [InlineData(BalanceGoal.OneToOne, 3, 2, true)]
    [InlineData(BalanceGoal.OneToOne, 1, 2, false)]
    public void MeetsGoal_BalanceDay_FollowsTheWantedRatio(BalanceGoal goal, int forYou, int toDo, bool expected)
    {
        Assert.Equal(expected, BalanceForYouToDoRules.MeetsGoal(forYou, toDo, goal));
    }

    [Theory]
    [InlineData(BalanceGoal.TwoToThree, 2, 3, true)]
    [InlineData(BalanceGoal.TwoToThree, 4, 6, true)]
    [InlineData(BalanceGoal.TwoToThree, 1, 2, false)]
    [InlineData(BalanceGoal.ThreeToTwo, 3, 2, true)]
    [InlineData(BalanceGoal.ThreeToTwo, 1, 1, false)]
    [InlineData(BalanceGoal.ThreeToTwo, 1, 2, false)]
    public void MeetsGoal_PresetRatio_FollowsForYouToToDo(BalanceGoal goal, int forYou, int toDo, bool expected)
    {
        Assert.Equal(expected, BalanceForYouToDoRules.MeetsGoal(forYou, toDo, goal));
    }

    [Theory]
    [InlineData(1, 4, 1, 4, true)]
    [InlineData(1, 4, 1, 5, false)]
    [InlineData(2, 5, 4, 10, true)]
    [InlineData(2, 5, 3, 10, false)]
    [InlineData(5, 1, 5, 1, true)]
    [InlineData(5, 1, 4, 1, false)]
    public void MeetsGoal_CustomRatio_FollowsTheOwnValues(int customForYou, int customToDo, int forYou, int toDo, bool expected)
    {
        Assert.Equal(expected, BalanceForYouToDoRules.MeetsGoal(forYou, toDo, BalanceGoal.Custom, customForYou, customToDo));
    }

    [Fact]
    public void MeetsGoal_CustomRatioOutOfRange_IsClampedToTheAllowedRange()
    {
        Assert.True(BalanceForYouToDoRules.MeetsGoal(1, 1, BalanceGoal.Custom, 0, -3));
        Assert.True(BalanceForYouToDoRules.MeetsGoal(1, 20, BalanceGoal.Custom, 1, 500));
        Assert.False(BalanceForYouToDoRules.MeetsGoal(1, 21, BalanceGoal.Custom, 1, 500));
    }

    [Theory]
    [InlineData(BalanceGoal.OneToOne)]
    [InlineData(BalanceGoal.TwoToThree)]
    [InlineData(BalanceGoal.ThreeToTwo)]
    [InlineData(BalanceGoal.Custom)]
    public void MeetsGoal_NoBalanceDay_IsNeverReachedWhateverTheGoal(BalanceGoal goal)
    {
        Assert.False(BalanceForYouToDoRules.MeetsGoal(0, 0, goal));
        Assert.False(BalanceForYouToDoRules.MeetsGoal(5, 0, goal));
        Assert.False(BalanceForYouToDoRules.MeetsGoal(0, 5, goal));
    }

    [Fact]
    public void MeetsGoal_UnknownGoal_IsTreatedLikeOneToOne()
    {
        Assert.True(BalanceForYouToDoRules.MeetsGoal(1, 1, (BalanceGoal)99));
        Assert.False(BalanceForYouToDoRules.MeetsGoal(1, 2, (BalanceGoal)99));
    }

    [Theory]
    [InlineData(BalanceGoal.OneToOne, 1, 1, false)]
    [InlineData(BalanceGoal.OneToOne, 3, 2, true)]
    [InlineData(BalanceGoal.OneToOne, 1, 2, false)]
    [InlineData(BalanceGoal.TwoToThree, 2, 3, false)]
    [InlineData(BalanceGoal.TwoToThree, 3, 3, true)]
    [InlineData(BalanceGoal.TwoToThree, 1, 2, false)]
    [InlineData(BalanceGoal.ThreeToTwo, 3, 2, false)]
    [InlineData(BalanceGoal.ThreeToTwo, 4, 2, true)]
    [InlineData(BalanceGoal.ThreeToTwo, 2, 2, false)]
    public void ExceedsRatio_PresetRatio_NeedsMoreForYouThanTheRatioAsks(BalanceGoal goal, int forYou, int toDo, bool expected)
    {
        Assert.Equal(expected, BalanceForYouToDoRules.ExceedsRatio(forYou, toDo, goal));
    }

    [Theory]
    [InlineData(1, 4, 1, 4, false)]
    [InlineData(1, 4, 2, 4, true)]
    [InlineData(2, 5, 4, 10, false)]
    [InlineData(2, 5, 5, 10, true)]
    [InlineData(2, 5, 3, 10, false)]
    public void ExceedsRatio_CustomRatio_FollowsTheOwnValues(int customForYou, int customToDo, int forYou, int toDo, bool expected)
    {
        Assert.Equal(expected, BalanceForYouToDoRules.ExceedsRatio(forYou, toDo, BalanceGoal.Custom, customForYou, customToDo));
    }

    [Theory]
    [InlineData(BalanceGoal.OneToOne)]
    [InlineData(BalanceGoal.TwoToThree)]
    [InlineData(BalanceGoal.ThreeToTwo)]
    [InlineData(BalanceGoal.Custom)]
    public void ExceedsRatio_WithoutForYou_IsNeverExceededWhateverTheGoal(BalanceGoal goal)
    {
        Assert.False(BalanceForYouToDoRules.ExceedsRatio(0, 0, goal));
        Assert.False(BalanceForYouToDoRules.ExceedsRatio(0, 5, goal));
    }

    [Theory]
    [InlineData(BalanceGoal.OneToOne)]
    [InlineData(BalanceGoal.TwoToThree)]
    [InlineData(BalanceGoal.ThreeToTwo)]
    [InlineData(BalanceGoal.Custom)]
    public void ExceedsRatio_ForYouWithoutAnyToDo_IsExceeded(BalanceGoal goal)
    {
        Assert.True(BalanceForYouToDoRules.ExceedsRatio(1, 0, goal));
    }

    [Theory]
    [InlineData(BalanceGoal.OneToOne, 1, 1, true)]
    [InlineData(BalanceGoal.OneToOne, 1, 2, false)]
    [InlineData(BalanceGoal.TwoToThree, 2, 3, true)]
    [InlineData(BalanceGoal.TwoToThree, 1, 3, false)]
    [InlineData(BalanceGoal.ThreeToTwo, 3, 2, true)]
    [InlineData(BalanceGoal.ThreeToTwo, 2, 2, false)]
    [InlineData(BalanceGoal.OneToOne, 1, 0, true)]
    [InlineData(BalanceGoal.ThreeToTwo, 1, 0, true)]
    public void ReachesRatio_PresetRatio_NeedsNoToDo(BalanceGoal goal, int forYou, int toDo, bool expected)
    {
        Assert.Equal(expected, BalanceForYouToDoRules.ReachesRatio(forYou, toDo, goal));
    }

    [Theory]
    [InlineData(BalanceGoal.OneToOne)]
    [InlineData(BalanceGoal.TwoToThree)]
    [InlineData(BalanceGoal.ThreeToTwo)]
    [InlineData(BalanceGoal.Custom)]
    public void ReachesRatio_WithoutForYou_IsNeverReachedWhateverTheGoal(BalanceGoal goal)
    {
        Assert.False(BalanceForYouToDoRules.ReachesRatio(0, 0, goal));
        Assert.False(BalanceForYouToDoRules.ReachesRatio(0, 5, goal));
    }

    [Theory]
    [InlineData(BalanceGoal.TwoToThree, 0, 0, GoalProgress.Open)]
    [InlineData(BalanceGoal.TwoToThree, 1, 3, GoalProgress.Open)]
    [InlineData(BalanceGoal.TwoToThree, 2, 3, GoalProgress.Reached)]
    [InlineData(BalanceGoal.TwoToThree, 3, 3, GoalProgress.Exceeded)]
    [InlineData(BalanceGoal.OneToOne, 2, 0, GoalProgress.Exceeded)]
    public void GetProgress_Counts_SaysOpenReachedOrExceeded(BalanceGoal goal, int forYou, int toDo, GoalProgress expected)
    {
        Assert.Equal(expected, BalanceForYouToDoRules.GetProgress(forYou, toDo, goal));
    }

    [Theory]
    [InlineData(BalanceGoal.OneToOne, 1, 1)]
    [InlineData(BalanceGoal.TwoToThree, 2, 3)]
    [InlineData(BalanceGoal.ThreeToTwo, 3, 2)]
    [InlineData(BalanceGoal.Custom, 4, 7)]
    public void GetParts_Goal_ReturnsForYouFirst(BalanceGoal goal, int forYou, int toDo)
    {
        Assert.Equal((forYou, toDo), BalanceForYouToDoRules.GetParts(goal, 4, 7));
    }

    [Fact]
    public void GetParts_CustomOutOfRange_IsClamped()
    {
        Assert.Equal((1, 20), BalanceForYouToDoRules.GetParts(BalanceGoal.Custom, 0, 99));
    }

    [Theory]
    [InlineData(1, 4, 1, 4, true)]
    [InlineData(1, 4, 1, 5, false)]
    [InlineData(5, 1, 4, 1, false)]
    public void ReachesRatio_CustomRatio_FollowsTheOwnValues(int customForYou, int customToDo, int forYou, int toDo, bool expected)
    {
        Assert.Equal(expected, BalanceForYouToDoRules.ReachesRatio(forYou, toDo, BalanceGoal.Custom, customForYou, customToDo));
    }

    [Theory]
    [InlineData(2026, 3, 9, 2026, 3, 9)]
    [InlineData(2026, 3, 10, 2026, 3, 9)]
    [InlineData(2026, 3, 13, 2026, 3, 9)]
    [InlineData(2026, 3, 14, 2026, 3, 9)]
    [InlineData(2026, 3, 15, 2026, 3, 9)]
    [InlineData(2026, 3, 16, 2026, 3, 16)]
    [InlineData(2027, 1, 1, 2026, 12, 28)]
    public void GetWeekStart_AnyDay_IsTheMondayOfThatWeek(int year, int month, int day, int weekYear, int weekMonth, int weekDay)
    {
        var weekStart = BalanceForYouToDoRules.GetWeekStart(new DateTime(year, month, day, 17, 45, 0));

        Assert.Equal(new DateTime(weekYear, weekMonth, weekDay), weekStart);
    }
}
