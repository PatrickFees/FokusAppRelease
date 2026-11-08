using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FocusAppTests.Services;

public class CompanionMoodTests
{

    private static readonly DateTime Sunday = new(2026, 3, 15);
    private static readonly DateTime WeekStart = new(2026, 3, 9);

    [Fact]
    public void Evaluate_NoDays_RestsFriendly()
    {
        Assert.Equal(CompanionMood.Resting, CompanionMoodRules.Evaluate([]));
    }

    [Fact]
    public void Evaluate_NothingCompleted_Rests()
    {
        var days = Days((0, 0), (0, 0), (0, 0));

        Assert.Equal(CompanionMood.Resting, CompanionMoodRules.Evaluate(days));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 4)]
    public void Evaluate_OnlyOneKindCompleted_IsAwakeButHasNoBud(int forYou, int toDo)
    {
        var days = Days((forYou, toDo), (0, 0));

        Assert.Equal(CompanionMood.Awake, CompanionMoodRules.Evaluate(days));
    }

    [Fact]
    public void Evaluate_ManyTasksOfOneKindOnSeveralDays_NeverGrowsABud()
    {
        var days = Days((5, 0), (0, 5), (5, 0), (0, 5));

        Assert.Equal(CompanionMood.Awake, CompanionMoodRules.Evaluate(days));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Evaluate_OneOrTwoBalanceDays_Buds(int balanceDays)
    {
        var days = Days(Enumerable.Repeat((1, 1), balanceDays).ToArray());

        Assert.Equal(CompanionMood.Budding, CompanionMoodRules.Evaluate(days));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(7)]
    public void Evaluate_ThreeOrMoreBalanceDays_Blooms(int balanceDays)
    {
        var days = Days(Enumerable.Repeat((1, 1), balanceDays).ToArray());

        Assert.Equal(CompanionMood.Blooming, CompanionMoodRules.Evaluate(days));
    }

    [Fact]
    public void Evaluate_BalanceDaysDoNotNeedToBeInARow_AMissedDayCostsNothing()
    {
        var days = Days((1, 1), (0, 0), (1, 1), (0, 0), (1, 1));

        Assert.Equal(CompanionMood.Blooming, CompanionMoodRules.Evaluate(days));
    }

    [Fact]
    public void Evaluate_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CompanionMoodRules.Evaluate(null!));
    }

    [Fact]
    public void Thresholds_Always_AreTheDocumentedOnes()
    {
        Assert.Equal(7, CompanionMoodRules.DaysLookedAt);
        Assert.Equal(1, CompanionMoodRules.BudBalanceDays);
        Assert.Equal(3, CompanionMoodRules.BloomBalanceDays);
    }

    [Fact]
    public async Task GetRecentMoodAsync_LastSevenDaysInTheCurrentWeek_UseOnlyThisWeek()
    {
        var balance = Substitute.For<IBalanceForYouToDoService>();
        balance.GetWeekAsync(Arg.Any<DateTime>()).Returns(Week(WeekStart, [(0, (1, 1)), (1, (1, 1)), (2, (1, 1))]));
        var service = Create(balance, Sunday);

        var mood = await service.GetRecentMoodAsync();

        Assert.Equal(CompanionMood.Blooming, mood);
        await balance.Received(1).GetWeekAsync(Arg.Any<DateTime>());
    }

    [Fact]
    public async Task GetRecentMoodAsync_EarlyInTheWeek_ReachesBackIntoTheWeekBefore()
    {
        var balance = Substitute.For<IBalanceForYouToDoService>();
        var previousStart = WeekStart.AddDays(-7);
        var wednesday = WeekStart.AddDays(9);
        balance.GetWeekAsync(Arg.Is<DateTime>(day => day >= WeekStart.AddDays(7))).Returns(Week(WeekStart.AddDays(7), []));
        balance.GetWeekAsync(Arg.Is<DateTime>(day => day < WeekStart.AddDays(7))).Returns(
            Week(WeekStart, [(4, (1, 1)), (5, (1, 1)), (6, (2, 1))]));
        var service = Create(balance, wednesday);

        var mood = await service.GetRecentMoodAsync();

        Assert.Equal(CompanionMood.Blooming, mood);
        Assert.NotEqual(previousStart, WeekStart);
    }

    [Fact]
    public async Task GetRecentMoodAsync_BalanceDayOlderThanSevenDays_NoLongerCounts()
    {
        var balance = Substitute.For<IBalanceForYouToDoService>();
        var nextWeekStart = WeekStart.AddDays(7);
        balance.GetWeekAsync(Arg.Is<DateTime>(day => day >= nextWeekStart)).Returns(Week(nextWeekStart, []));
        balance.GetWeekAsync(Arg.Is<DateTime>(day => day < nextWeekStart)).Returns(Week(WeekStart, [(0, (1, 1))]));
        var service = Create(balance, nextWeekStart.AddDays(3));

        var mood = await service.GetRecentMoodAsync();

        Assert.Equal(CompanionMood.Resting, mood);
    }

    [Fact]
    public async Task GetRecentMoodAsync_EveryDayOfTheWindowCounts_FadesSlowlyInsteadOfDroppingAtMonday()
    {
        var balance = Substitute.For<IBalanceForYouToDoService>();
        var nextWeekStart = WeekStart.AddDays(7);
        balance.GetWeekAsync(Arg.Is<DateTime>(day => day >= nextWeekStart)).Returns(Week(nextWeekStart, []));
        balance.GetWeekAsync(Arg.Is<DateTime>(day => day < nextWeekStart)).Returns(
            Week(WeekStart, [(3, (1, 1)), (4, (1, 1)), (5, (1, 1)), (6, (1, 1))]));
        var service = Create(balance, nextWeekStart);

        var mood = await service.GetRecentMoodAsync();

        Assert.Equal(CompanionMood.Blooming, mood);
    }

    private static CompanionMoodService Create(IBalanceForYouToDoService balance, DateTime today) =>
        new(balance, new FakeTimeProvider(new DateTimeOffset(today.AddHours(10), TimeSpan.Zero)));

    private static List<DayBalance> Days(params (int ForYou, int ToDo)[] done) =>
        done.Select((entry, index) => Day(WeekStart.AddDays(index), entry)).ToList();

    private static DayBalance Day(DateTime date, (int ForYou, int ToDo) done) =>
        new(date, done.ForYou, done.ToDo, 0, 0, 0, done.ForYou >= 1 && done.ToDo >= 1, false);

    private static WeekBalance Week(DateTime weekStart, (int Offset, (int ForYou, int ToDo) Done)[] entries)
    {
        var days = Enumerable.Range(0, 7)
            .Select(offset => Day(weekStart.AddDays(offset), entries.FirstOrDefault(entry => entry.Offset == offset).Done))
            .ToList();

        return new WeekBalance(
            weekStart,
            days,
            days.Count(day => day.IsBalanceDay),
            0,
            days.Sum(day => day.DoneForYou),
            days.Sum(day => day.DoneToDo),
            0,
            null);
    }
}
