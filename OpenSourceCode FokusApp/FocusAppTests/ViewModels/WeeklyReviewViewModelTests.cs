using FokusApp.Components.WeeklyReviewComponent;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using static FocusAppTests.TestSupport.WaitForAsyncHelper;

namespace FocusAppTests.ViewModels;

public class WeeklyReviewViewModelTests
{
    private static readonly DateTime Sunday = new(2026, 3, 15);
    private static readonly DateTime WeekStart = new(2026, 3, 9);

    private readonly IBalanceForYouToDoService _balance = Substitute.For<IBalanceForYouToDoService>();
    private readonly IRewardingSettingsService _rewardingSettings = TestSettings.Create();
    private readonly IAppStartupStateService _startupState = Substitute.For<IAppStartupStateService>();
    private readonly IDayChangeService _dayChange = Substitute.For<IDayChangeService>();
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly ITaskCacheService _taskCache = Substitute.For<ITaskCacheService>();
    private readonly FakeTimeProvider _clock = TestTasks.CreateClock();
    private readonly WeeklyReviewViewModel _viewModel;

    public WeeklyReviewViewModelTests()
    {
        _balance.GetWeekAsync(Arg.Any<DateTime>()).Returns(call => EmptyWeek(BalanceForYouToDoRules.GetWeekStart(call.Arg<DateTime>())));
        _viewModel = new WeeklyReviewViewModel(
            _balance,
            _rewardingSettings,
            _startupState,
            _dayChange,
            _errorCapture,
            _taskCache,
            _clock);
    }

    [Fact]
    public void Constructor_BeforeInitialization_IsNotLoadedAndShowsNeutralValues()
    {
        Assert.False(_viewModel.IsLoaded);
        Assert.Empty(_viewModel.Days);
        Assert.Equal(0, _viewModel.CreatedForYou);
        Assert.Equal(0, _viewModel.CreatedToDo);
        Assert.Equal(0, _viewModel.DoneForYou);
        Assert.Equal(0, _viewModel.DoneToDo);
        Assert.Null(_viewModel.CreatedProgress);
        Assert.Null(_viewModel.DoneProgress);
        Assert.Null(_viewModel.MostForYouDays);
        Assert.Null(_viewModel.MostToDoDays);
    }

    [Fact]
    public async Task InitializeAsync_AppDataLoading_WaitsForStartupAndLoadsTheCurrentWeek()
    {
        await _viewModel.InitializeAsync();

        await _startupState.Received(1).WaitForReadyAsync(Arg.Any<CancellationToken>());
        await _balance.Received(1).GetWeekAsync(WeekStart);
        Assert.True(_viewModel.IsLoaded);
        Assert.True(_viewModel.IsCurrentWeek);
        Assert.Equal(WeekStart, _viewModel.WeekStart);
        Assert.Equal(Sunday, _viewModel.WeekEnd);
    }

    [Fact]
    public async Task InitializeAsync_EmptyWeek_IsFriendlyAndShowsNoProgressAndNoPeaks()
    {
        await _viewModel.InitializeAsync();

        Assert.True(_viewModel.IsEmpty);
        Assert.Null(_viewModel.CreatedProgress);
        Assert.Null(_viewModel.DoneProgress);
        Assert.Null(_viewModel.MostForYouDays);
        Assert.Null(_viewModel.MostToDoDays);
        Assert.Equal(7, _viewModel.Days.Count);
        Assert.All(_viewModel.Days, day => Assert.Equal(0, day.ForYouHeightPercent + day.ToDoHeightPercent));
    }

    [Fact]
    public async Task InitializeAsync_WeekWithTasks_ExposesCreatedAndDoneOfTheWeek()
    {
        SetWeek(
            WeekStart,
            Day(0, doneForYou: 2, doneToDo: 1, plannedForYou: 2, plannedToDo: 2),
            Day(1, doneForYou: 1, doneToDo: 2, plannedForYou: 1, plannedToDo: 3),
            Day(2, doneForYou: 0, doneToDo: 0, plannedForYou: 4, plannedToDo: 0));

        await _viewModel.InitializeAsync();

        Assert.Equal(7, _viewModel.CreatedForYou);
        Assert.Equal(5, _viewModel.CreatedToDo);
        Assert.Equal(3, _viewModel.DoneForYou);
        Assert.Equal(3, _viewModel.DoneToDo);
        Assert.False(_viewModel.IsEmpty);
    }

    [Fact]
    public async Task IsEmpty_OnlyCreatedTasks_IsFalse()
    {
        SetWeek(WeekStart, Day(0, plannedToDo: 1));

        await _viewModel.InitializeAsync();

        Assert.False(_viewModel.IsEmpty);
    }

    [Theory]
    [InlineData(BalanceGoal.OneToOne, 1, 1)]
    [InlineData(BalanceGoal.TwoToThree, 2, 3)]
    [InlineData(BalanceGoal.ThreeToTwo, 3, 2)]
    public async Task Goal_PresetFromTheSettings_IsExposedAsRatio(BalanceGoal goal, int forYou, int toDo)
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { BalanceGoal = goal });

        await _viewModel.InitializeAsync();

        Assert.Equal(forYou, _viewModel.GoalForYou);
        Assert.Equal(toDo, _viewModel.GoalToDo);
    }

    [Fact]
    public async Task Goal_CustomRatioFromTheSettings_IsExposed()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { BalanceGoal = BalanceGoal.Custom, CustomForYou = 1, CustomToDo = 4 });

        await _viewModel.InitializeAsync();

        Assert.Equal(1, _viewModel.GoalForYou);
        Assert.Equal(4, _viewModel.GoalToDo);
    }

    [Fact]
    public async Task Progress_CreatedAndDoneDifferently_AreRatedSeparately()
    {
        SetWeek(WeekStart, Day(0, doneForYou: 1, doneToDo: 2, plannedForYou: 3, plannedToDo: 2));

        await _viewModel.InitializeAsync();

        Assert.Equal(GoalProgress.Exceeded, _viewModel.CreatedProgress);
        Assert.Equal(GoalProgress.Open, _viewModel.DoneProgress);
    }

    [Fact]
    public async Task Progress_ExactlyTheRatio_IsReached()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { BalanceGoal = BalanceGoal.TwoToThree });
        SetWeek(WeekStart, Day(0, doneForYou: 2, doneToDo: 3, plannedForYou: 2, plannedToDo: 3));

        await _viewModel.InitializeAsync();

        Assert.Equal(GoalProgress.Reached, _viewModel.CreatedProgress);
        Assert.Equal(GoalProgress.Reached, _viewModel.DoneProgress);
    }

    [Fact]
    public async Task Progress_ForYouWithoutAnyToDo_IsFine()
    {
        SetWeek(WeekStart, Day(0, doneForYou: 1, plannedForYou: 1));

        await _viewModel.InitializeAsync();

        Assert.Equal(GoalProgress.Exceeded, _viewModel.DoneProgress);
    }

    [Fact]
    public async Task Progress_OnlyToDos_IsOpen()
    {
        SetWeek(WeekStart, Day(0, doneToDo: 2, plannedToDo: 2));

        await _viewModel.InitializeAsync();

        Assert.Equal(GoalProgress.Open, _viewModel.DoneProgress);
        Assert.Equal(GoalProgress.Open, _viewModel.CreatedProgress);
    }

    [Fact]
    public async Task Progress_NothingCreatedButSomethingDone_RatesOnlyWhatExists()
    {
        SetWeek(WeekStart, Day(0, doneForYou: 1, doneToDo: 1));

        await _viewModel.InitializeAsync();

        Assert.Null(_viewModel.CreatedProgress);
        Assert.Equal(GoalProgress.Reached, _viewModel.DoneProgress);
    }

    [Fact]
    public async Task MostForYouDays_OneBusiestDay_IsThatDay()
    {
        SetWeek(WeekStart, Day(0, doneForYou: 1), Day(2, doneForYou: 3), Day(4, doneForYou: 2));

        await _viewModel.InitializeAsync();

        var peak = Assert.IsType<WeeklyReviewPeak>(_viewModel.MostForYouDays);
        Assert.Equal(3, peak.Count);
        Assert.Equal([WeekStart.AddDays(2)], peak.Days);
    }

    [Fact]
    public async Task MostToDoDays_TiedDays_ListsAllInDateOrder()
    {
        SetWeek(WeekStart, Day(1, doneToDo: 4), Day(3, doneToDo: 4), Day(5, doneToDo: 1));

        await _viewModel.InitializeAsync();

        var peak = Assert.IsType<WeeklyReviewPeak>(_viewModel.MostToDoDays);
        Assert.Equal(4, peak.Count);
        Assert.Equal([WeekStart.AddDays(1), WeekStart.AddDays(3)], peak.Days);
    }

    [Fact]
    public async Task Peaks_OnlyOneKindDone_ShowNoPeakForTheOther()
    {
        SetWeek(WeekStart, Day(0, doneForYou: 2));

        await _viewModel.InitializeAsync();

        Assert.NotNull(_viewModel.MostForYouDays);
        Assert.Null(_viewModel.MostToDoDays);
    }

    [Fact]
    public async Task Peaks_OnlyCreatedTasks_ShowNoPeak()
    {
        SetWeek(WeekStart, Day(0, plannedForYou: 3, plannedToDo: 3));

        await _viewModel.InitializeAsync();

        Assert.Null(_viewModel.MostForYouDays);
        Assert.Null(_viewModel.MostToDoDays);
    }

    [Fact]
    public async Task Days_Bars_AreScaledToTheBusiestDay()
    {
        SetWeek(WeekStart, Day(0, doneForYou: 2, doneToDo: 2), Day(1, doneForYou: 1));

        await _viewModel.InitializeAsync();

        var monday = _viewModel.Days[0];
        var tuesday = _viewModel.Days[1];
        Assert.Equal(50, monday.ForYouHeightPercent, 6);
        Assert.Equal(50, monday.ToDoHeightPercent, 6);
        Assert.Equal(25, tuesday.ForYouHeightPercent, 6);
        Assert.Equal(0, tuesday.ToDoHeightPercent, 6);
        Assert.Equal(WeekStart, monday.Date);
    }

    [Fact]
    public async Task ShowPreviousWeekAsync_LoadsTheWeekBefore()
    {
        await _viewModel.InitializeAsync();

        await _viewModel.ShowPreviousWeekAsync();

        await _balance.Received(1).GetWeekAsync(new DateTime(2026, 3, 2));
        Assert.Equal(new DateTime(2026, 3, 2), _viewModel.WeekStart);
        Assert.False(_viewModel.IsCurrentWeek);
        Assert.True(_viewModel.CanShowNextWeek);
    }

    [Fact]
    public async Task ShowPreviousWeekAsync_SeveralTimes_GoesBackWeekByWeek()
    {
        await _viewModel.InitializeAsync();

        await _viewModel.ShowPreviousWeekAsync();
        await _viewModel.ShowPreviousWeekAsync();

        Assert.Equal(new DateTime(2026, 2, 23), _viewModel.WeekStart);
    }

    [Fact]
    public async Task ShowNextWeekAsync_InCurrentWeek_StaysThere()
    {
        await _viewModel.InitializeAsync();

        await _viewModel.ShowNextWeekAsync();

        Assert.False(_viewModel.CanShowNextWeek);
        Assert.Equal(WeekStart, _viewModel.WeekStart);
        await _balance.Received(1).GetWeekAsync(Arg.Any<DateTime>());
    }

    [Fact]
    public async Task ShowNextWeekAsync_InPastWeek_GoesForwardAgain()
    {
        await _viewModel.InitializeAsync();
        await _viewModel.ShowPreviousWeekAsync();

        await _viewModel.ShowNextWeekAsync();

        Assert.True(_viewModel.IsCurrentWeek);
        Assert.Equal(WeekStart, _viewModel.WeekStart);
    }

    [Fact]
    public async Task ShowCurrentWeekAsync_FromFarBack_JumpsToTheCurrentWeek()
    {
        await _viewModel.InitializeAsync();
        await _viewModel.ShowPreviousWeekAsync();
        await _viewModel.ShowPreviousWeekAsync();

        await _viewModel.ShowCurrentWeekAsync();

        Assert.True(_viewModel.IsCurrentWeek);
        Assert.Equal(WeekStart, _viewModel.WeekStart);
    }

    [Fact]
    public void IsReviewEnabled_ReviewSwitchedOff_IsFalse()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { ReviewsEnabled = false });

        Assert.False(_viewModel.IsReviewEnabled);
    }

    [Fact]
    public async Task InitializeAsync_LoadingFails_CapturesErrorAndStaysNotLoaded()
    {
        var failure = new InvalidOperationException("Datenbank gesperrt");
        _balance.GetWeekAsync(Arg.Any<DateTime>()).ThrowsAsync(failure);

        await _viewModel.InitializeAsync();

        Assert.False(_viewModel.IsLoaded);
        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null && options.Source == "WeeklyReviewViewModel.LoadAsync" && options.IsUiFailure));
    }

    [Fact]
    public async Task InitializeAsync_Loaded_NotifiesPropertyChanged()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        await _viewModel.InitializeAsync();

        Assert.Contains(nameof(IWeeklyReviewViewModel.IsLoaded), changed);
        Assert.Contains(nameof(IWeeklyReviewViewModel.Days), changed);
        Assert.Contains(nameof(IWeeklyReviewViewModel.DoneProgress), changed);
        Assert.Contains(nameof(IWeeklyReviewViewModel.MostForYouDays), changed);
    }

    [Fact]
    public async Task RewardingChanged_WantedRatioChanged_ReadsTheWeekAndRatesItAgain()
    {
        SetWeek(WeekStart, Day(0, doneForYou: 2, doneToDo: 3));
        await _viewModel.InitializeAsync();
        Assert.Equal(GoalProgress.Open, _viewModel.DoneProgress);

        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { BalanceGoal = BalanceGoal.TwoToThree });
        _rewardingSettings.RewardingChanged += Raise.EventWith(EventArgs.Empty);

        await WaitForAsync(() => _viewModel.DoneProgress == GoalProgress.Reached);
        Assert.Equal(GoalProgress.Reached, _viewModel.DoneProgress);
        Assert.Equal(2, _viewModel.GoalForYou);
    }

    [Fact]
    public async Task DayChanged_ToNewWeekWhileShowingCurrentWeek_ShowsTheNewWeek()
    {
        await _viewModel.InitializeAsync();
        _clock.SetUtcNow(new DateTimeOffset(2026, 3, 16, 0, 5, 0, TimeSpan.Zero));

        _dayChange.DayChanged += Raise.EventWith(new DayChangedEventArgs(Sunday, Sunday.AddDays(1)));

        await WaitForAsync(() => _viewModel.WeekStart == new DateTime(2026, 3, 16));
        Assert.Equal(new DateTime(2026, 3, 16), _viewModel.WeekStart);
        Assert.True(_viewModel.IsCurrentWeek);
    }

    [Fact]
    public async Task DayChanged_ToNewWeekWhileShowingPastWeek_KeepsTheShownWeek()
    {
        await _viewModel.InitializeAsync();
        await _viewModel.ShowPreviousWeekAsync();
        _clock.SetUtcNow(new DateTimeOffset(2026, 3, 16, 0, 5, 0, TimeSpan.Zero));

        _dayChange.DayChanged += Raise.EventWith(new DayChangedEventArgs(Sunday, Sunday.AddDays(1)));

        await WaitForAsync(() => _balance.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "GetWeekAsync") >= 3);
        Assert.Equal(new DateTime(2026, 3, 2), _viewModel.WeekStart);
        Assert.False(_viewModel.IsCurrentWeek);
    }

    [Fact]
    public async Task DayChanged_ToMiddleOfWeekWhileShowingPastWeek_KeepsTheShownWeek()
    {
        await _viewModel.InitializeAsync();
        await _viewModel.ShowPreviousWeekAsync();
        var shownWeek = _viewModel.WeekStart;
        var tuesday = new DateTime(2026, 3, 17);

        _dayChange.DayChanged += Raise.EventWith(new DayChangedEventArgs(new DateTime(2026, 3, 16), tuesday));
        await Task.Delay(50);

        Assert.Equal(shownWeek, _viewModel.WeekStart);
        Assert.False(_viewModel.IsCurrentWeek);
    }

    [Fact]
    public async Task Dispose_AfterDispose_IgnoresSettingAndDayChanges()
    {
        await _viewModel.InitializeAsync();
        _viewModel.Dispose();

        _rewardingSettings.RewardingChanged += Raise.EventWith(EventArgs.Empty);
        _dayChange.DayChanged += Raise.EventWith(new DayChangedEventArgs(Sunday, Sunday.AddDays(1)));
        await Task.Delay(50);

        await _balance.Received(1).GetWeekAsync(Arg.Any<DateTime>());
    }

    [Fact]
    public async Task TasksChanged_TaskAddedOrDone_ReadsTheWeekAgain()
    {
        await _viewModel.InitializeAsync();
        SetWeek(WeekStart, Day(0, doneForYou: 2, doneToDo: 1, plannedForYou: 2, plannedToDo: 1));

        _taskCache.DoneTasksChanged += Raise.Event();

        await WaitForAsync(() => _viewModel.DoneForYou == 2);
        Assert.Equal(2, _viewModel.DoneForYou);
        Assert.Equal(1, _viewModel.DoneToDo);
        Assert.Equal(2, _viewModel.CreatedForYou);
    }

    [Fact]
    public async Task TasksChanged_NormalTasksAndTemplates_ReadTheWeekAgain()
    {
        await _viewModel.InitializeAsync();

        _taskCache.NormalTasksChanged += Raise.Event();
        _taskCache.TemplatesChanged += Raise.Event();

        await WaitForAsync(() => _balance.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "GetWeekAsync") >= 3);
        await _balance.Received(3).GetWeekAsync(WeekStart);
    }

    [Fact]
    public async Task TasksChanged_AfterDispose_DoesNotReadAgain()
    {
        await _viewModel.InitializeAsync();
        _viewModel.Dispose();

        _taskCache.NormalTasksChanged += Raise.Event();
        _taskCache.DoneTasksChanged += Raise.Event();
        _taskCache.TemplatesChanged += Raise.Event();
        await Task.Delay(50);

        await _balance.Received(1).GetWeekAsync(WeekStart);
    }

    [Fact]
    public async Task TasksChanged_OlderReadFinishesLast_KeepsTheNewerResult()
    {
        await _viewModel.InitializeAsync();
        var olderRead = new TaskCompletionSource<WeekBalance>();
        _balance.GetWeekAsync(Arg.Any<DateTime>()).Returns(olderRead.Task, Task.FromResult(BuildWeek(WeekStart, Day(0, doneToDo: 4))));

        _taskCache.NormalTasksChanged += Raise.Event();
        _taskCache.DoneTasksChanged += Raise.Event();
        await WaitForAsync(() => _viewModel.DoneToDo == 4);
        olderRead.SetResult(BuildWeek(WeekStart, Day(0, doneForYou: 1)));
        await Task.Delay(50);

        Assert.Equal(4, _viewModel.DoneToDo);
        Assert.Equal(0, _viewModel.DoneForYou);
    }

    private void SetWeek(DateTime weekStart, params WeekDayCounts[] days) =>
        _balance.GetWeekAsync(Arg.Any<DateTime>()).Returns(BuildWeek(weekStart, days));

    private static WeekDayCounts Day(int offset, int doneForYou = 0, int doneToDo = 0, int plannedForYou = 0, int plannedToDo = 0) =>
        new(offset, doneForYou, doneToDo, plannedForYou, plannedToDo);

    private static WeekBalance EmptyWeek(DateTime weekStart) => BuildWeek(weekStart);

    private static WeekBalance BuildWeek(DateTime weekStart, params WeekDayCounts[] counts)
    {
        var days = Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var entry = counts.FirstOrDefault(item => item.Offset == offset) ?? new WeekDayCounts(offset, 0, 0, 0, 0);
                return new DayBalance(
                    weekStart.AddDays(offset),
                    entry.DoneForYou,
                    entry.DoneToDo,
                    entry.PlannedForYou,
                    entry.PlannedToDo,
                    0,
                    entry.DoneForYou >= 1 && entry.DoneToDo >= 1,
                    false);
            })
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

    private sealed record WeekDayCounts(int Offset, int DoneForYou, int DoneToDo, int PlannedForYou, int PlannedToDo);
}
