using FocusAppTests.ViewModels;
using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FocusAppTests.Services;

public class BalanceForYouToDoServiceTests
{

    private static readonly DateTime Monday = new(2026, 3, 9);
    private static readonly DateTime Wednesday = new(2026, 3, 11);
    private static readonly DateTime Sunday = new(2026, 3, 15);

    private readonly ITaskRepository _repository = Substitute.For<ITaskRepository>();
    private readonly IRewardingSettingsService _rewardingSettings = TestSettings.Create();
    private readonly FakeTimeProvider _clock = TestTasks.CreateClock();
    private readonly BalanceForYouToDoService _service;

    public BalanceForYouToDoServiceTests()
    {
        SetTasks();
        _service = new BalanceForYouToDoService(_repository, _rewardingSettings, _clock);
    }

    [Fact]
    public async Task GetDayAsync_EmptyDay_HasNoTasksAndIsNoBalanceDay()
    {
        var day = await _service.GetDayAsync(Wednesday);

        Assert.Equal(Wednesday, day.Date);
        Assert.Equal(0, day.DoneForYou);
        Assert.Equal(0, day.DoneToDo);
        Assert.Equal(0, day.PlannedForYou);
        Assert.Equal(0, day.PlannedToDo);
        Assert.Equal(0, day.FocusMinutes);
        Assert.False(day.IsBalanceDay);
        Assert.False(day.MeetsGoal);
    }

    [Fact]
    public async Task GetDayAsync_OnlyForYou_IsNoBalanceDay()
    {
        SetTasks(Done(Wednesday, isPositive: true), Done(Wednesday, isPositive: true));

        var day = await _service.GetDayAsync(Wednesday);

        Assert.Equal(2, day.DoneForYou);
        Assert.Equal(0, day.DoneToDo);
        Assert.False(day.IsBalanceDay);
    }

    [Fact]
    public async Task GetDayAsync_OnlyToDo_IsNoBalanceDay()
    {
        SetTasks(Done(Wednesday, isPositive: false), Done(Wednesday, isPositive: false), Done(Wednesday, isPositive: false));

        var day = await _service.GetDayAsync(Wednesday);

        Assert.Equal(0, day.DoneForYou);
        Assert.Equal(3, day.DoneToDo);
        Assert.False(day.IsBalanceDay);
    }

    [Fact]
    public async Task GetDayAsync_OneOfEach_IsBalanceDay()
    {
        SetTasks(Done(Wednesday, isPositive: true), Done(Wednesday, isPositive: false));

        var day = await _service.GetDayAsync(Wednesday);

        Assert.True(day.IsBalanceDay);
        Assert.True(day.MeetsGoal);
        Assert.Equal(2, day.DoneTotal);
    }

    [Fact]
    public async Task GetDayAsync_WorkAndPrivateTasks_AreCountedTogether()
    {
        SetTasks(
            Done(Wednesday, isPositive: true, isWork: true),
            Done(Wednesday, isPositive: true, isWork: false),
            Done(Wednesday, isPositive: false, isWork: true),
            Done(Wednesday, isPositive: false, isWork: false));

        var day = await _service.GetDayAsync(Wednesday);

        Assert.Equal(2, day.DoneForYou);
        Assert.Equal(2, day.DoneToDo);
    }

    [Fact]
    public async Task GetDayAsync_Templates_DoNotCount()
    {
        SetTasks(
            Open(Wednesday, isPositive: true, isTemplate: true),
            Open(Wednesday, isPositive: false, isTemplate: true),
            Done(Wednesday, isPositive: true));

        var day = await _service.GetDayAsync(Wednesday);

        Assert.Equal(1, day.DoneForYou);
        Assert.Equal(0, day.DoneToDo);
        Assert.Equal(1, day.PlannedForYou);
        Assert.Equal(0, day.PlannedToDo);
    }

    [Fact]
    public async Task GetDayAsync_DoneEntryOfATemplate_CountsAsConcreteDoneTask()
    {
        var historyEntry = Done(Wednesday, isPositive: true);
        historyEntry.IsTemplate = false;
        SetTasks(historyEntry, Open(Wednesday, isPositive: true, isTemplate: true));

        var day = await _service.GetDayAsync(Wednesday);

        Assert.Equal(1, day.DoneForYou);
    }

    [Fact]
    public async Task GetDayAsync_PlannedTasks_IncludeOpenAndDoneOnesOfThatDay()
    {
        SetTasks(
            Open(Wednesday, isPositive: true),
            Open(Wednesday, isPositive: false),
            Open(Wednesday, isPositive: false),
            Done(Wednesday, isPositive: true));

        var day = await _service.GetDayAsync(Wednesday);

        Assert.Equal(2, day.PlannedForYou);
        Assert.Equal(2, day.PlannedToDo);
        Assert.Equal(1, day.DoneForYou);
        Assert.Equal(0, day.DoneToDo);
    }

    [Fact]
    public async Task GetDayAsync_TaskPlannedOneDayAndDoneTheNext_CountsPlannedAndDoneOnDifferentDays()
    {
        SetTasks(Done(Wednesday, isPositive: true, completedAt: Wednesday.AddDays(1).AddHours(9)));

        var planned = await _service.GetDayAsync(Wednesday);
        var done = await _service.GetDayAsync(Wednesday.AddDays(1));

        Assert.Equal(1, planned.PlannedForYou);
        Assert.Equal(0, planned.DoneForYou);
        Assert.Equal(0, done.PlannedForYou);
        Assert.Equal(1, done.DoneForYou);
    }

    [Fact]
    public async Task GetDayAsync_LegacyDoneTaskWithoutCompletedAt_CountsOnItsTaskDate()
    {
        SetTasks(Done(Wednesday, isPositive: true, legacy: true));

        var onTaskDate = await _service.GetDayAsync(Wednesday);
        var otherDay = await _service.GetDayAsync(Wednesday.AddDays(1));

        Assert.Equal(1, onTaskDate.DoneForYou);
        Assert.Equal(0, otherDay.DoneForYou);
    }

    [Fact]
    public async Task GetDayAsync_CompletedAroundMidnight_BelongsToTheDayOfTheTimestamp()
    {
        SetTasks(
            Done(Wednesday, isPositive: true, completedAt: Wednesday.AddHours(23).AddMinutes(59).AddSeconds(59)),
            Done(Wednesday, isPositive: false, completedAt: Wednesday.AddDays(1)));

        var wednesday = await _service.GetDayAsync(Wednesday);
        var thursday = await _service.GetDayAsync(Wednesday.AddDays(1));

        Assert.Equal((1, 0), (wednesday.DoneForYou, wednesday.DoneToDo));
        Assert.Equal((0, 1), (thursday.DoneForYou, thursday.DoneToDo));
    }

    [Theory]
    [InlineData(2026, 3, 29, 2, 30)]
    [InlineData(2026, 10, 25, 2, 30)]
    public async Task GetDayAsync_CompletedDuringDaylightSavingChange_CountsOnThatCalendarDay(
        int year, int month, int day, int hour, int minute)
    {
        var date = new DateTime(year, month, day);
        SetTasks(Done(date, isPositive: true, completedAt: new DateTime(year, month, day, hour, minute, 0)));

        var balance = await _service.GetDayAsync(date);

        Assert.Equal(1, balance.DoneForYou);
    }

    [Fact]
    public async Task GetDayAsync_FocusMinutes_SumsTheFocusTimeOfTheDoneTasks()
    {
        SetTasks(
            Done(Wednesday, isPositive: true, timerMinutes: 25),
            Done(Wednesday, isPositive: false, timerMinutes: 40),
            Open(Wednesday, isPositive: false, timerMinutes: 90),
            Done(Wednesday.AddDays(1), isPositive: false, timerMinutes: 15));

        var day = await _service.GetDayAsync(Wednesday);

        Assert.Equal(65, day.FocusMinutes);
    }

    [Theory]
    [InlineData(BalanceGoal.OneToOne, false)]
    [InlineData(BalanceGoal.TwoToThree, false)]
    [InlineData(BalanceGoal.ThreeToTwo, false)]
    public async Task GetDayAsync_OneForYouTwoToDos_MeetsGoalDependingOnTheWantedRatio(BalanceGoal goal, bool expected)
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { BalanceGoal = goal });
        SetTasks(Done(Wednesday, isPositive: true), Done(Wednesday, isPositive: false), Done(Wednesday, isPositive: false));

        var day = await _service.GetDayAsync(Wednesday);

        Assert.True(day.IsBalanceDay);
        Assert.Equal(expected, day.MeetsGoal);
    }

    [Fact]
    public async Task GetTodayAsync_UsesTheLocalDayOfTheTimeProvider()
    {
        SetTasks(Done(TestTasks.Today, isPositive: true), Done(TestTasks.Today.AddDays(1), isPositive: false));

        var today = await _service.GetTodayAsync();
        _clock.Advance(TimeSpan.FromHours(15));
        var afterMidnight = await _service.GetTodayAsync();

        Assert.Equal(TestTasks.Today, today.Date);
        Assert.Equal(1, today.DoneForYou);
        Assert.Equal(TestTasks.Today.AddDays(1), afterMidnight.Date);
        Assert.Equal(1, afterMidnight.DoneToDo);
    }

    [Fact]
    public async Task GetTodayTaskCountAsync_WorkScope_CountsOnlyWorkTasks()
    {
        SetTasks(
            Done(TestTasks.Today, isPositive: true, isWork: true),
            Open(TestTasks.Today, isPositive: false, isWork: true),
            Done(TestTasks.Today, isPositive: true, isWork: false),
            Done(TestTasks.Today, isPositive: true, isWork: false),
            Open(TestTasks.Today, isPositive: false, isWork: false));

        var today = await _service.GetTodayTaskCountAsync(isWorkScope: true);

        Assert.Equal(TestTasks.Today, today.Date);
        Assert.Equal(1, today.ForYou);
        Assert.Equal(1, today.ToDo);
        Assert.Equal(2, today.Total);
    }

    [Fact]
    public async Task GetTodayTaskCountAsync_PrivateScope_CountsOnlyPrivateTasks()
    {
        SetTasks(
            Done(TestTasks.Today, isPositive: true, isWork: true),
            Done(TestTasks.Today, isPositive: true, isWork: false),
            Open(TestTasks.Today, isPositive: true, isWork: false),
            Open(TestTasks.Today, isPositive: false, isWork: false));

        var today = await _service.GetTodayTaskCountAsync(isWorkScope: false);

        Assert.Equal(2, today.ForYou);
        Assert.Equal(1, today.ToDo);
    }

    [Fact]
    public async Task GetTodayTaskCountAsync_DoneAndOpenTasks_SaysHowManyOfThemAreDone()
    {
        SetTasks(
            Done(TestTasks.Today, isPositive: true),
            Open(TestTasks.Today, isPositive: true),
            Open(TestTasks.Today, isPositive: true),
            Done(TestTasks.Today, isPositive: false),
            Done(TestTasks.Today, isPositive: false),
            Open(TestTasks.Today, isPositive: false));

        var today = await _service.GetTodayTaskCountAsync(isWorkScope: true);

        Assert.Equal(3, today.ForYou);
        Assert.Equal(1, today.DoneForYou);
        Assert.Equal(3, today.ToDo);
        Assert.Equal(2, today.DoneToDo);
    }

    [Fact]
    public async Task GetTodayTaskCountAsync_DoneTaskPlannedForTodayToo_IsCountedOnce()
    {
        SetTasks(Done(TestTasks.Today, isPositive: true));

        var today = await _service.GetTodayTaskCountAsync(isWorkScope: true);

        Assert.Equal(1, today.ForYou);
        Assert.Equal(0, today.ToDo);
    }

    [Fact]
    public async Task GetTodayTaskCountAsync_DoneTodayFromAnotherDay_IsCounted()
    {
        SetTasks(Done(TestTasks.Today.AddDays(-3), isPositive: false, completedAt: TestTasks.Today.AddHours(9)));

        var today = await _service.GetTodayTaskCountAsync(isWorkScope: true);

        Assert.Equal(0, today.ForYou);
        Assert.Equal(1, today.ToDo);
    }

    [Fact]
    public async Task GetTodayTaskCountAsync_OpenTaskOfAnotherDayAndTemplate_AreNotCounted()
    {
        SetTasks(
            Open(TestTasks.Today.AddDays(1), isPositive: true),
            Open(TestTasks.Today.AddDays(-1), isPositive: false),
            Open(TestTasks.Today, isPositive: true, isTemplate: true));

        var today = await _service.GetTodayTaskCountAsync(isWorkScope: true);

        Assert.Equal(0, today.Total);
    }

    [Fact]
    public async Task GetTodayTaskCountAsync_ScopeWithoutTasks_IsEmpty()
    {
        SetTasks(Done(TestTasks.Today, isPositive: true, isWork: true));

        var today = await _service.GetTodayTaskCountAsync(isWorkScope: false);

        Assert.Equal(0, today.Total);
    }

    [Fact]
    public async Task GetDayAsync_RepositoryFails_PassesTheErrorOn()
    {
        _repository.GetAllAsync().ThrowsAsync(new InvalidOperationException("Datenbank gesperrt"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetDayAsync(Wednesday));
    }

    [Fact]
    public async Task GetWeekAsync_AnyDayOfTheWeek_ReturnsSevenDaysFromMondayToSunday()
    {
        var week = await _service.GetWeekAsync(Wednesday.AddHours(13));

        Assert.Equal(Monday, week.WeekStart);
        Assert.Equal(7, week.Days.Count);
        Assert.Equal(Monday, week.Days[0].Date);
        Assert.Equal(Sunday, week.Days[6].Date);
        Assert.Equal(DayOfWeek.Monday, week.Days[0].Date.DayOfWeek);
    }

    [Fact]
    public async Task GetWeekAsync_WeekBoundary_SundayBelongsToThisWeekMondayToTheNext()
    {
        SetTasks(Done(Sunday, isPositive: true), Done(Sunday.AddDays(1), isPositive: false));

        var thisWeek = await _service.GetWeekAsync(Sunday);
        var nextWeek = await _service.GetWeekAsync(Sunday.AddDays(1));

        Assert.Equal((1, 0), (thisWeek.DoneForYou, thisWeek.DoneToDo));
        Assert.Equal((0, 1), (nextWeek.DoneForYou, nextWeek.DoneToDo));
        Assert.Equal(Sunday.AddDays(1), nextWeek.WeekStart);
    }

    [Fact]
    public async Task GetWeekAsync_EmptyWeek_HasNoBalanceDaysNoBestDayAndNoShare()
    {
        var week = await _service.GetWeekAsync(Wednesday);

        Assert.Equal(0, week.BalanceDays);
        Assert.Equal(0, week.GoalDays);
        Assert.Null(week.BestDay);
        Assert.Null(week.ForYouShare);
        Assert.Equal(0, week.FocusMinutes);
    }

    [Fact]
    public async Task GetWeekAsync_MixedWeek_SumsBalanceDaysTotalsAndFocusTime()
    {
        SetTasks(
            Done(Monday, isPositive: true, timerMinutes: 20),
            Done(Monday, isPositive: false, timerMinutes: 30),
            Done(Wednesday, isPositive: false, timerMinutes: 10),
            Done(Wednesday, isPositive: false, timerMinutes: 10),
            Done(Sunday, isPositive: true, timerMinutes: 5),
            Done(Sunday, isPositive: false, timerMinutes: 0));

        var week = await _service.GetWeekAsync(Wednesday);

        Assert.Equal(2, week.BalanceDays);
        Assert.Equal(2, week.GoalDays);
        Assert.Equal(2, week.DoneForYou);
        Assert.Equal(4, week.DoneToDo);
        Assert.Equal(6, week.DoneTotal);
        Assert.Equal(75, week.FocusMinutes);
        Assert.Equal(2.0 / 6.0, week.ForYouShare!.Value, 6);
    }

    [Fact]
    public async Task GetWeekAsync_CustomOneToTwo_CountsMoreDaysAsGoalDaysThanOneToOne()
    {
        SetTasks(
            Done(Monday, isPositive: true), Done(Monday, isPositive: false), Done(Monday, isPositive: false),
            Done(Wednesday, isPositive: true), Done(Wednesday, isPositive: false));

        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { BalanceGoal = BalanceGoal.OneToOne });
        var strict = await _service.GetWeekAsync(Monday);
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { BalanceGoal = BalanceGoal.Custom, CustomForYou = 1, CustomToDo = 2 });
        var relaxed = await _service.GetWeekAsync(Monday);

        Assert.Equal(2, strict.BalanceDays);
        Assert.Equal(1, strict.GoalDays);
        Assert.Equal(2, relaxed.BalanceDays);
        Assert.Equal(2, relaxed.GoalDays);
    }

    [Fact]
    public async Task GetWeekAsync_BestDay_BalanceDayBeatsLargerUnbalancedDay()
    {
        SetTasks(
            Done(Monday, isPositive: false), Done(Monday, isPositive: false), Done(Monday, isPositive: false),
            Done(Wednesday, isPositive: true), Done(Wednesday, isPositive: false));

        var week = await _service.GetWeekAsync(Monday);

        Assert.Equal(Wednesday, week.BestDay);
    }

    [Fact]
    public async Task GetWeekAsync_BestDay_AmongBalanceDaysTheOneWithMoreDoneTasksWins()
    {
        SetTasks(
            Done(Monday, isPositive: true), Done(Monday, isPositive: false),
            Done(Wednesday, isPositive: true), Done(Wednesday, isPositive: false), Done(Wednesday, isPositive: false));

        var week = await _service.GetWeekAsync(Monday);

        Assert.Equal(Wednesday, week.BestDay);
    }

    [Fact]
    public async Task GetWeekAsync_BestDay_TieGoesToTheEarlierDay()
    {
        SetTasks(
            Done(Wednesday, isPositive: true), Done(Wednesday, isPositive: false),
            Done(Monday, isPositive: true), Done(Monday, isPositive: false));

        var week = await _service.GetWeekAsync(Monday);

        Assert.Equal(Monday, week.BestDay);
    }

    [Fact]
    public async Task GetWeekAsync_BestDay_WithoutBalanceDayTakesTheDayWithMostDoneTasks()
    {
        SetTasks(Done(Monday, isPositive: true), Done(Wednesday, isPositive: false), Done(Wednesday, isPositive: false));

        var week = await _service.GetWeekAsync(Monday);

        Assert.Equal(Wednesday, week.BestDay);
        Assert.Equal(0, week.BalanceDays);
    }

    [Fact]
    public async Task GetCurrentWeekAsync_UsesTheWeekOfTheTimeProvider()
    {
        SetTasks(Done(Sunday, isPositive: true), Done(Sunday.AddDays(1), isPositive: true));

        var week = await _service.GetCurrentWeekAsync();
        _clock.Advance(TimeSpan.FromHours(15));
        var nextWeek = await _service.GetCurrentWeekAsync();

        Assert.Equal(Monday, week.WeekStart);
        Assert.Equal(1, week.DoneForYou);
        Assert.Equal(Sunday.AddDays(1), nextWeek.WeekStart);
        Assert.Equal(1, nextWeek.DoneForYou);
    }

    [Fact]
    public async Task GetWeekdayPlannedVersusDoneAsync_Always_ReturnsSevenWeekdaysStartingOnMonday()
    {
        var result = await _service.GetWeekdayPlannedVersusDoneAsync(Monday, Sunday);

        Assert.Equal(
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday],
            result.Select(entry => entry.Day).ToArray());
        Assert.All(result, entry => Assert.Equal(0, entry.PlannedForYou + entry.PlannedToDo + entry.DoneForYou + entry.DoneToDo));
    }

    [Fact]
    public async Task GetWeekdayPlannedVersusDoneAsync_TwoWeeks_SumsPerWeekday()
    {
        var lastWednesday = Wednesday.AddDays(-7);
        SetTasks(
            Open(Wednesday, isPositive: true),
            Open(lastWednesday, isPositive: true),
            Done(Wednesday, isPositive: false),
            Done(lastWednesday, isPositive: false),
            Done(lastWednesday, isPositive: false),
            Open(Monday, isPositive: false));

        var result = await _service.GetWeekdayPlannedVersusDoneAsync(lastWednesday, Sunday);

        var wednesday = result.Single(entry => entry.Day == DayOfWeek.Wednesday);
        Assert.Equal(2, wednesday.PlannedForYou);
        Assert.Equal(3, wednesday.PlannedToDo);
        Assert.Equal(0, wednesday.DoneForYou);
        Assert.Equal(3, wednesday.DoneToDo);
        var monday = result.Single(entry => entry.Day == DayOfWeek.Monday);
        Assert.Equal(1, monday.PlannedToDo);
    }

    [Fact]
    public async Task GetWeekdayPlannedVersusDoneAsync_RangeStartsMidWeek_IncludesTheWholeWeeks()
    {
        SetTasks(Open(Monday, isPositive: true), Open(Sunday, isPositive: false));

        var result = await _service.GetWeekdayPlannedVersusDoneAsync(Wednesday, Wednesday);

        Assert.Equal(1, result.Single(entry => entry.Day == DayOfWeek.Monday).PlannedForYou);
        Assert.Equal(1, result.Single(entry => entry.Day == DayOfWeek.Sunday).PlannedToDo);
    }

    [Fact]
    public async Task GetWeekdayPlannedVersusDoneAsync_ForYouPlannedButNotDone_ShowsTheGap()
    {
        SetTasks(Open(Wednesday, isPositive: true), Open(Wednesday, isPositive: true), Done(Wednesday, isPositive: false));

        var result = await _service.GetWeekdayPlannedVersusDoneAsync(Monday, Sunday);

        var wednesday = result.Single(entry => entry.Day == DayOfWeek.Wednesday);
        Assert.Equal(2, wednesday.PlannedForYou);
        Assert.Equal(0, wednesday.DoneForYou);
        Assert.Equal(1, wednesday.DoneToDo);
    }

    [Fact]
    public void WeekBalance_ForYouShare_IsTheShareOfForYouAmongDoneTasks()
    {
        var week = new WeekBalance(Monday, [], 0, 0, 3, 1, 0, null);

        Assert.Equal(0.75, week.ForYouShare);
    }

    [Fact]
    public async Task GetDayAsync_OpenTaskWithoutTaskDate_IsNotPlannedOnAnyDay()
    {
        var undated = Open(Wednesday, isPositive: true);
        undated.TaskDate = null;
        SetTasks(undated);

        var day = await _service.GetDayAsync(Wednesday);

        Assert.Equal(0, day.PlannedForYou);
        Assert.Equal(0, day.PlannedToDo);
    }

    private void SetTasks(params TaskEntity[] tasks)
    {
        _repository.GetAllAsync().Returns(tasks.ToList());
    }

    private static TaskEntity Open(
        DateTime taskDate,
        bool isPositive,
        bool isTemplate = false,
        bool isWork = true,
        int timerMinutes = 25)
    {
        return new TaskEntity
        {
            Id = Guid.NewGuid(),
            IsDone = false,
            IsPositive = isPositive,
            IsTemplate = isTemplate,
            IsWorkTask = isWork,
            TaskDate = isTemplate ? null : taskDate,
            CreationDate = taskDate.AddDays(-2),
            TimerMinutes = timerMinutes
        };
    }

    private static TaskEntity Done(
        DateTime taskDate,
        bool isPositive,
        bool isWork = true,
        DateTime? completedAt = null,
        bool legacy = false,
        int timerMinutes = 25)
    {
        return new TaskEntity
        {
            Id = Guid.NewGuid(),
            IsDone = true,
            IsPositive = isPositive,
            IsWorkTask = isWork,
            TaskDate = taskDate,
            CreationDate = taskDate.AddDays(-2),
            TimerMinutes = timerMinutes,
            CompletedAt = legacy ? null : completedAt ?? taskDate.AddHours(11)
        };
    }
}
