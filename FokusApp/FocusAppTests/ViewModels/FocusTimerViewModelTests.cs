using FokusApp.Components.FocusTimerComponent;
using FokusApp.Components.SharedUI.FilterComponent;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FocusAppTests.ViewModels;

public class FocusTimerViewModelTests
{
    private readonly ITaskCacheService _taskCache = Substitute.For<ITaskCacheService>();
    private readonly IActiveTaskService _activeTaskService = Substitute.For<IActiveTaskService>();
    private readonly IScopeService _scopeService = Substitute.For<IScopeService>();
    private readonly IAppStartupStateService _startupState = Substitute.For<IAppStartupStateService>();
    private readonly ITimerSessionController _timer = Substitute.For<ITimerSessionController>();
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly FakeTimeProvider _clock = TestTasks.CreateClock();
    private readonly IDayChangeService _dayChange = Substitute.For<IDayChangeService>();
    private readonly FocusTimerViewModel _viewModel;

    public FocusTimerViewModelTests()
    {
        IReadOnlyList<TaskDto> empty = [];
        _taskCache.GetNormalTasks(Arg.Any<DateTime?>()).Returns(empty);
        _taskCache.GetRegularTasks(Arg.Any<DateTime?>()).Returns(empty);
        _taskCache.GetDoneTasks(Arg.Any<DateTime?>()).Returns(empty);
        _viewModel = new FocusTimerViewModel(
            _taskCache,
            _activeTaskService,
            _scopeService,
            _startupState,
            _timer,
            _errorCapture,
            _clock,
            _dayChange);
    }

    [Fact]
    public void Constructor_NoInteraction_StartsOnTodayWithoutTask()
    {
        Assert.Equal(TestTasks.Today, _viewModel.SelectedTaskDate);
        Assert.Equal(TestTasks.Today, _viewModel.TaskFilter.SelectedDate);
        Assert.Null(_viewModel.SelectedTaskId);
        Assert.Equal(TestTasks.Today, _viewModel.SelectedTask.TaskDate);
    }

    [Fact]
    public async Task ApplyFilterChangesAsync_PayloadWithoutDay_SwitchesToTodayOfClock()
    {
        await LoadDayAsync(TestTasks.Day);
        var payload = new FilterModel { SelectedDate = null };

        await _viewModel.ApplyFilterChangesAsync(payload);

        Assert.Equal(TestTasks.Today, _viewModel.SelectedTaskDate);
        _taskCache.Received().GetNormalTasks(TestTasks.Today);
    }

    [Fact]
    public async Task ResetTimer_AfterMidnight_CreatesEmptyTaskForNewToday()
    {
        await LoadDayAsync(TestTasks.Day);
        _clock.Advance(TimeSpan.FromDays(1));

        await _viewModel.ResetTimer();

        Assert.Equal(new DateTime(2026, 3, 16), _viewModel.SelectedTask.TaskDate);
    }

    [Fact]
    public async Task InitializeAsync_StartupNotReady_WaitsAndLoadsAvailableTasks()
    {
        var task = TestTasks.Create();
        SetNormalTasks(task);

        await _viewModel.InitializeAsync();

        await _startupState.Received(1).WaitForReadyAsync(Arg.Any<CancellationToken>());
        Assert.True(_viewModel.HasAvailableTasks);
    }

    [Fact]
    public async Task InitializeAsync_NoTasks_HasNoAvailableTasks()
    {
        await _viewModel.InitializeAsync();

        Assert.False(_viewModel.HasAvailableTasks);
        Assert.Null(_viewModel.SelectedTaskId);
    }

    [Fact]
    public async Task InitializeAsync_ActiveTaskOnOtherDay_SelectsTaskAndItsDay()
    {
        var task = TestTasks.Create("Aktiv", timerMinutes: 25);
        SetNormalTasks(task);
        _activeTaskService.ActiveTask.Returns(task);

        await _viewModel.InitializeAsync();

        Assert.Equal(task.Id, _viewModel.SelectedTaskId);
        Assert.Equal(TestTasks.Day, _viewModel.SelectedTaskDate);
        Assert.Equal(TestTasks.Day, _viewModel.TaskFilter.SelectedDate);
        Assert.Equal(25, _viewModel.InputMinutes);
        Assert.Equal(1500, _viewModel.InitialDurationInSeconds);
        Assert.Contains(task, _viewModel.NormalTasks);
    }

    [Fact]
    public async Task SelectTaskAsync_KnownTask_BecomesCurrentAndActiveTask()
    {
        var task = TestTasks.Create("Sport", priority: TaskPriority.Hoch, timerMinutes: 40);
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);

        await _viewModel.SelectTaskAsync(task.Id);

        Assert.Equal(task.Id, _viewModel.SelectedTaskId);
        Assert.Equal("Sport", _viewModel.SelectedTaskNote);
        Assert.Equal(TaskPriority.Hoch, _viewModel.SelectedTaskPriority);
        Assert.Equal(40, _viewModel.InputMinutes);
        Assert.Equal(2400, _viewModel.InitialDurationInSeconds);
        _activeTaskService.Received(1).SetActiveTask(task.Id);
        await _timer.Received(1).StopAsync();
    }

    [Fact]
    public async Task SelectTaskAsync_SameTaskTwice_StopsTimerOnlyOnce()
    {
        var task = TestTasks.Create();
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);

        await _viewModel.SelectTaskAsync(task.Id);
        await _viewModel.SelectTaskAsync(task.Id);

        await _timer.Received(1).StopAsync();
    }

    [Fact]
    public async Task SelectTaskAsync_UnknownId_FallsBackToNoTask()
    {
        await LoadDayAsync(TestTasks.Day);
        var unknownId = Guid.NewGuid();

        await _viewModel.SelectTaskAsync(unknownId);

        Assert.Null(_viewModel.SelectedTaskId);
    }

    [Fact]
    public async Task SelectTaskAsync_Null_SelectsTimerWithoutTask()
    {
        var task = TestTasks.Create();
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);

        await _viewModel.SelectTaskAsync(null);

        Assert.Null(_viewModel.SelectedTaskId);
        Assert.Equal(string.Empty, _viewModel.SelectedTaskNote);
    }

    [Fact]
    public async Task SelectTaskAsync_RegularTemplate_CanBeUsedLikeAnyTask()
    {
        var template = TestTasks.Create("Vorlage", isRegular: true);
        SetRegularTasks(template);
        await LoadDayAsync(TestTasks.Day);

        await _viewModel.SelectTaskAsync(template.Id);

        Assert.Equal(template.Id, _viewModel.SelectedTaskId);
        Assert.Contains(template, _viewModel.RegularTasks);
    }

    [Fact]
    public async Task PrepareTaskPicker_SelectedDay_AppliesDayToFilter()
    {
        var todayTask = TestTasks.Create("Heute");
        var otherDayTask = TestTasks.Create("Anderer Tag", taskDate: TestTasks.Day.AddDays(1));
        SetNormalTasks(todayTask, otherDayTask);
        await LoadDayAsync(TestTasks.Day);

        _viewModel.PrepareTaskPicker();

        Assert.Equal(TestTasks.Day, _viewModel.TaskFilter.SelectedDate);
        var visible = Assert.Single(_viewModel.NormalTasks);
        Assert.Equal(todayTask.Id, visible.Id);
    }

    [Fact]
    public async Task ApplyFilterChangesAsync_DayChanged_ReloadsTasksAndClearsSelection()
    {
        var task = TestTasks.Create();
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        ClearCalls();
        var otherDay = TestTasks.Day.AddDays(1);
        var payload = new FilterModel { SelectedDate = otherDay };

        await _viewModel.ApplyFilterChangesAsync(payload);

        Assert.Equal(otherDay, _viewModel.SelectedTaskDate);
        Assert.Null(_viewModel.SelectedTaskId);
        _taskCache.Received().GetNormalTasks(otherDay);
        _taskCache.Received().GetRegularTasks(otherDay);
        _activeTaskService.DidNotReceive().SetActiveTask(task.Id);
        await _timer.Received(1).StopAsync();
    }

    [Fact]
    public async Task ApplyFilterChangesAsync_OnlySearchChanged_FiltersWithoutTouchingTimerOrSelection()
    {
        var sport = TestTasks.Create("Sport");
        var reading = TestTasks.Create("Lesen");
        SetNormalTasks(sport, reading);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(reading.Id);
        ClearCalls();
        var payload = new FilterModel
        {
            SelectedDate = TestTasks.Day,
            SearchText = "Sport"
        };

        await _viewModel.ApplyFilterChangesAsync(payload);

        var visible = Assert.Single(_viewModel.NormalTasks);
        Assert.Equal(sport.Id, visible.Id);
        Assert.Equal(reading.Id, _viewModel.SelectedTaskId);
        Assert.Equal(TestTasks.Day, _viewModel.SelectedTaskDate);
        await _timer.DidNotReceive().StopAsync();
        _taskCache.DidNotReceive().GetNormalTasks(Arg.Any<DateTime?>());
    }

    [Fact]
    public async Task ApplyFilterChangesAsync_DayFilter_KeepsRegularTemplatesVisible()
    {
        var template = TestTasks.Create("Vorlage", isRegular: true);
        SetRegularTasks(template);
        await LoadDayAsync(TestTasks.Day);
        var otherDay = TestTasks.Day.AddDays(3);
        var payload = new FilterModel { SelectedDate = otherDay };

        await _viewModel.ApplyFilterChangesAsync(payload);

        Assert.Contains(template, _viewModel.RegularTasks);
    }

    [Fact]
    public async Task ApplyFilterChangesAsync_SameDayWithOtherTime_KeepsSelectionTimerAndActiveTask()
    {
        var task = TestTasks.Create();
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        ClearCalls();
        var sameDayLater = TestTasks.Day.AddHours(15);
        var payload = new FilterModel { SelectedDate = sameDayLater };

        await _viewModel.ApplyFilterChangesAsync(payload);

        Assert.Equal(task.Id, _viewModel.SelectedTaskId);
        Assert.Equal(TestTasks.Day, _viewModel.SelectedTaskDate);
        await _timer.DidNotReceive().StopAsync();
        _activeTaskService.DidNotReceive().SetActiveTask(Arg.Any<Guid?>());
        _taskCache.DidNotReceive().GetNormalTasks(Arg.Any<DateTime?>());
    }

    [Fact]
    public async Task ApplyFilterChangesAsync_DayChangedWithoutActiveTask_ReloadsTasksAndStaysWithoutTask()
    {
        var task = TestTasks.Create();
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        var otherDay = TestTasks.Day.AddDays(1);
        var payload = new FilterModel { SelectedDate = otherDay };

        await _viewModel.ApplyFilterChangesAsync(payload);

        Assert.Equal(otherDay, _viewModel.SelectedTaskDate);
        Assert.Null(_viewModel.SelectedTaskId);
        _taskCache.Received().GetNormalTasks(otherDay);
    }

    [Fact]
    public async Task ApplyFilterChangesAsync_DayChanged_ActiveTaskOfOtherDay_IsReset()
    {
        var task = TestTasks.Create("Aktiv", taskDate: TestTasks.Day.AddDays(5));
        await LoadDayAsync(TestTasks.Day);
        SetCentralActiveTask(task);
        var otherDay = TestTasks.Day.AddDays(1);
        var payload = new FilterModel { SelectedDate = otherDay };

        await _viewModel.ApplyFilterChangesAsync(payload);

        _activeTaskService.Received().SetActiveTask(Arg.Is<Guid?>(id => id != task.Id));
    }

    [Fact]
    public async Task ApplyFilterChangesAsync_DayChanged_ActiveTaskOfNewDay_IsNotReset()
    {
        var task = TestTasks.Create("Aktiv", taskDate: TestTasks.Day.AddDays(1));
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        SetCentralActiveTask(task);
        var newDay = TestTasks.Day.AddDays(1);
        var payload = new FilterModel { SelectedDate = newDay };

        await _viewModel.ApplyFilterChangesAsync(payload);

        _activeTaskService.DidNotReceive().SetActiveTask(Arg.Is<Guid?>(id => id != task.Id));
        Assert.Equal(task.Id, _viewModel.SelectedTaskId);
    }

    [Fact]
    public async Task ApplyFilterChangesAsync_DayChanged_ActiveRegularTemplate_IsNotReset()
    {
        var template = TestTasks.Create("Vorlage", isRegular: true);
        await LoadDayAsync(TestTasks.Day);
        SetCentralActiveTask(template);
        var otherDay = TestTasks.Day.AddDays(1);
        var payload = new FilterModel { SelectedDate = otherDay };

        await _viewModel.ApplyFilterChangesAsync(payload);

        _activeTaskService.DidNotReceive().SetActiveTask(Arg.Is<Guid?>(id => id != template.Id));
    }

    [Fact]
    public async Task ApplyFilterChangesAsync_DayChanged_SelectedActiveRegularTemplate_StaysSelectedWithRunningTimer()
    {
        var template = TestTasks.Create("Vorlage", isRegular: true);
        SetRegularTasks(template);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(template.Id);
        SetCentralActiveTask(template);
        ClearCalls();
        var otherDay = TestTasks.Day.AddDays(1);
        var payload = new FilterModel { SelectedDate = otherDay };

        await _viewModel.ApplyFilterChangesAsync(payload);

        Assert.Equal(template.Id, _viewModel.SelectedTaskId);
        await _timer.DidNotReceive().StopAsync();
        _activeTaskService.DidNotReceive().SetActiveTask(Arg.Any<Guid?>());
    }

    [Fact]
    public async Task ApplyFilterChangesAsync_DayChanged_NoActiveTask_DoesNotTouchActiveTask()
    {
        await LoadDayAsync(TestTasks.Day);
        var otherDay = TestTasks.Day.AddDays(1);
        var payload = new FilterModel { SelectedDate = otherDay };

        await _viewModel.ApplyFilterChangesAsync(payload);

        _activeTaskService.DidNotReceive().SetActiveTask(Arg.Any<Guid?>());
    }

    [Fact]
    public async Task ActiveTaskChanged_NoActiveTask_KeepsSelectedDay()
    {
        await LoadDayAsync(TestTasks.Day);
        var noTask = TestTasks.Create();
        noTask.Id = null;
        noTask.TaskDate = TestTasks.Today;
        _activeTaskService.ActiveTask.Returns(noTask);

        _activeTaskService.ActiveTaskChanged += Raise.Event();

        Assert.Equal(TestTasks.Day, _viewModel.SelectedTaskDate);
        Assert.Equal(TestTasks.Day, _viewModel.TaskFilter.SelectedDate);
    }

    [Fact]
    public async Task NormalTasksChanged_SelectedTaskChangedInCache_UpdatesNotePriorityAndBreakTime()
    {
        var task = TestTasks.Create("Alt", priority: TaskPriority.Keine, breakMinutes: 7);
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        var changedTask = CreateChangedCopy(task, "Neu", TaskPriority.Hoch, 25, 12);
        SetNormalTasks(changedTask);
        var changed = TrackChanges();

        _taskCache.NormalTasksChanged += Raise.Event();

        Assert.Equal("Neu", _viewModel.SelectedTaskNote);
        Assert.Equal(TaskPriority.Hoch, _viewModel.SelectedTaskPriority);
        Assert.Equal(task.Id, _viewModel.SelectedTaskId);
        Assert.Contains(nameof(FocusTimerViewModel.SelectedTaskNote), changed);
        Assert.Contains(nameof(FocusTimerViewModel.SelectedTaskPriority), changed);
    }

    [Fact]
    public async Task NormalTasksChanged_SelectedTaskBreakTimeChanged_NextBreakUsesNewBreakTime()
    {
        var task = TestTasks.Create("Aufgabe", timerMinutes: 25, breakMinutes: 7);
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        var changedTask = CreateChangedCopy(task, "Aufgabe", TaskPriority.Keine, 25, 12);
        SetNormalTasks(changedTask);
        _taskCache.NormalTasksChanged += Raise.Event();
        TimerStartRequest request = default;
        _ = _timer.SwitchTimerToStartPauseOrResumeAsync(Arg.Do<TimerStartRequest>(captured => request = captured));

        await _viewModel.SwitchTimerToStartOrStop();

        Assert.Equal(720, request.BreakDurationSeconds);
    }

    [Fact]
    public async Task NormalTasksChanged_SelectedTaskChangedInCache_KeepsSessionTime()
    {
        var task = TestTasks.Create("Alt", timerMinutes: 25);
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        _viewModel.SetMinutes(40);
        var initialDuration = _viewModel.InitialDurationInSeconds;
        var changedTask = CreateChangedCopy(task, "Neu", TaskPriority.Mittel, 30, 7);
        SetNormalTasks(changedTask);

        _taskCache.NormalTasksChanged += Raise.Event();

        Assert.Equal(40, _viewModel.InputMinutes);
        Assert.Equal(initialDuration, _viewModel.InitialDurationInSeconds);
        Assert.Equal("Neu", _viewModel.SelectedTaskNote);
    }

    [Fact]
    public async Task RegularTasksChanged_SelectedTemplateChangedInCache_UpdatesNote()
    {
        var template = TestTasks.Create("Alt", isRegular: true);
        SetRegularTasks(template);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(template.Id);
        var changedTemplate = CreateChangedCopy(template, "Neu", TaskPriority.Keine, 25, 7);
        SetRegularTasks(changedTemplate);

        _taskCache.RegularTasksChanged += Raise.Event();

        Assert.Equal("Neu", _viewModel.SelectedTaskNote);
        Assert.Equal(template.Id, _viewModel.SelectedTaskId);
    }

    [Fact]
    public async Task NormalTasksChanged_SelectedTaskUnchanged_RaisesNoSelectedTaskNotifications()
    {
        var task = TestTasks.Create("Gleich");
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        var changed = TrackChanges();

        _taskCache.NormalTasksChanged += Raise.Event();

        Assert.DoesNotContain(nameof(FocusTimerViewModel.SelectedTaskNote), changed);
        Assert.DoesNotContain(nameof(FocusTimerViewModel.InputMinutes), changed);
    }

    [Fact]
    public async Task NormalTasksChanged_SelectedTaskNotInDayList_KeepsSelectedTask()
    {
        var task = TestTasks.Create("Vom Vortag", taskDate: TestTasks.Today);
        SetNormalTasks(task);
        await _viewModel.InitializeAsync();
        await _viewModel.SelectTaskAsync(task.Id);
        _timer.IsTimerActive.Returns(true);
        RaiseDayChanged(TestTasks.Today, TestTasks.Today.AddDays(1));
        SetNormalTasks();

        _taskCache.NormalTasksChanged += Raise.Event();

        Assert.Equal(task.Id, _viewModel.SelectedTaskId);
        Assert.Equal("Vom Vortag", _viewModel.SelectedTaskNote);
    }

    [Fact]
    public async Task NormalTasksChanged_NoTaskSelected_DoesNotSelectAnything()
    {
        var task = TestTasks.Create();
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);

        _taskCache.NormalTasksChanged += Raise.Event();

        Assert.Null(_viewModel.SelectedTaskId);
    }

    [Fact]
    public async Task NormalTasksChanged_NewTaskInCache_RefreshesListButKeepsSelectionAndTimer()
    {
        var first = TestTasks.Create("Erste");
        var second = TestTasks.Create("Zweite");
        SetNormalTasks(first);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(first.Id);
        ClearCalls();
        SetNormalTasks(first, second);

        _taskCache.NormalTasksChanged += Raise.Event();

        Assert.Equal(2, _viewModel.NormalTasks.Count);
        Assert.Equal(first.Id, _viewModel.SelectedTaskId);
        await _timer.DidNotReceive().StopAsync();
    }

    [Fact]
    public async Task RegularTasksChanged_NewTemplate_BecomesAvailable()
    {
        await LoadDayAsync(TestTasks.Day);
        Assert.False(_viewModel.HasAvailableTasks);
        var template = TestTasks.Create("Vorlage", isRegular: true);
        SetRegularTasks(template);

        _taskCache.RegularTasksChanged += Raise.Event();

        Assert.True(_viewModel.HasAvailableTasks);
        Assert.Contains(template, _viewModel.RegularTasks);
    }

    [Fact]
    public async Task ScopeChanged_ReloadsTasksFromCache()
    {
        await LoadDayAsync(TestTasks.Day);
        var task = TestTasks.Create("Privat", isWork: false);
        SetNormalTasks(task);

        _scopeService.ScopeChanged += Raise.Event();

        Assert.Contains(task, _viewModel.NormalTasks);
    }

    [Fact]
    public async Task ActiveTaskChanged_TaskOnOtherDay_StopsSessionAndTakesOverTaskAndDay()
    {
        var task = TestTasks.Create("Extern gesetzt", taskDate: TestTasks.Day.AddDays(2));
        SetNormalTasks(task);
        await _viewModel.InitializeAsync();
        ClearCalls();
        _activeTaskService.ActiveTask.Returns(task);

        _activeTaskService.ActiveTaskChanged += Raise.Event();

        Assert.Equal(task.Id, _viewModel.SelectedTaskId);
        Assert.Equal(task.TaskDate, _viewModel.SelectedTaskDate);
        _taskCache.Received().GetNormalTasks(task.TaskDate);
        await _timer.Received(1).StopAsync();
    }

    [Fact]
    public async Task ActiveTaskChanged_TaskAlreadySelected_DoesNotStopSession()
    {
        var task = TestTasks.Create();
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        ClearCalls();
        _activeTaskService.ActiveTask.Returns(task);

        _activeTaskService.ActiveTaskChanged += Raise.Event();

        Assert.Equal(task.Id, _viewModel.SelectedTaskId);
        await _timer.DidNotReceive().StopAsync();
    }

    [Fact]
    public async Task SwitchTimerToStartOrStop_IdleWithTask_SetsActiveTaskAndStartsFocusSession()
    {
        var task = TestTasks.Create("  Sport  ", timerMinutes: 25, breakMinutes: 7);
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        ClearCalls();
        TimerStartRequest request = default;
        _ = _timer.SwitchTimerToStartPauseOrResumeAsync(Arg.Do<TimerStartRequest>(captured => request = captured));

        await _viewModel.SwitchTimerToStartOrStop();

        _activeTaskService.Received(1).SetActiveTask(task.Id);
        Assert.Equal(1500, request.DurationSeconds);
        Assert.Equal(TimerSessionMode.Focus, request.SessionMode);
        Assert.Equal("Sport", request.DisplayTaskName);
        Assert.Equal(420, request.BreakDurationSeconds);
    }

    [Fact]
    public async Task SwitchTimerToStartOrStop_NoTaskSelected_UsesFallbackNameAndDefaultBreak()
    {
        await LoadDayAsync(TestTasks.Day);
        TimerStartRequest request = default;
        _ = _timer.SwitchTimerToStartPauseOrResumeAsync(Arg.Do<TimerStartRequest>(captured => request = captured));

        await _viewModel.SwitchTimerToStartOrStop();

        Assert.Equal(TimerSessionMode.Focus, request.SessionMode);
        Assert.Equal(TimerNotificationTextComposer.FocusFallbackName, request.DisplayTaskName);
        Assert.Equal(300, request.BreakDurationSeconds);
    }

    [Fact]
    public async Task SwitchTimerToStartOrStop_BreakMode_StartsBreakSessionWithoutTaskName()
    {
        var task = TestTasks.Create();
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        ClearCalls();
        _timer.IsBreakMode.Returns(true);
        TimerStartRequest request = default;
        _ = _timer.SwitchTimerToStartPauseOrResumeAsync(Arg.Do<TimerStartRequest>(captured => request = captured));

        await _viewModel.SwitchTimerToStartOrStop();

        Assert.Equal(TimerSessionMode.Break, request.SessionMode);
        Assert.Equal(string.Empty, request.DisplayTaskName);
        Assert.Equal(request.DurationSeconds, request.BreakDurationSeconds);
        _activeTaskService.DidNotReceive().SetActiveTask(Arg.Any<Guid?>());
    }

    [Fact]
    public async Task SwitchTimerToStartOrStop_AlreadyRunning_DoesNotChangeActiveTask()
    {
        await LoadDayAsync(TestTasks.Day);
        _timer.IsRunning.Returns(true);

        await _viewModel.SwitchTimerToStartOrStop();

        _activeTaskService.DidNotReceive().SetActiveTask(Arg.Any<Guid?>());
        await _timer.Received(1).SwitchTimerToStartPauseOrResumeAsync(Arg.Any<TimerStartRequest>());
    }

    [Fact]
    public async Task ResetTimer_SelectedTask_StopsSessionAndKeepsTask()
    {
        var task = TestTasks.Create(timerMinutes: 25);
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        ClearCalls();

        await _viewModel.ResetTimer();

        await _timer.Received(1).StopAsync();
        Assert.Equal(task.Id, _viewModel.SelectedTaskId);
        Assert.Equal(1500, _viewModel.InitialDurationInSeconds);
        _activeTaskService.Received(1).SetActiveTask(task.Id);
    }

    [Fact]
    public async Task TimerCompleted_FocusWithTask_FinishesTaskAndStartsBreakWithTaskBreakTime()
    {
        var task = TestTasks.Create("Sport", timerMinutes: 25, breakMinutes: 7);
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        var completion = TimerSessionCompletionResult.FocusCompleted();
        _timer.Complete().Returns(completion);

        RaiseTimerChanged(TimerStateChangeReason.Completed, TimeSpan.Zero);

        await _taskCache.Received(1).FinishTaskAsync(task);
        Assert.Null(_viewModel.SelectedTaskId);
        Assert.Equal(7, _viewModel.InputMinutes);
        Assert.Equal(420, _viewModel.InitialDurationInSeconds);
    }

    [Fact]
    public async Task TimerCompleted_FocusWithRegularTemplate_FinishesTemplateTask()
    {
        var template = TestTasks.Create("Vorlage", isRegular: true, breakMinutes: 10);
        SetRegularTasks(template);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(template.Id);
        var completion = TimerSessionCompletionResult.FocusCompleted();
        _timer.Complete().Returns(completion);

        RaiseTimerChanged(TimerStateChangeReason.Completed, TimeSpan.Zero);

        await _taskCache.Received(1).FinishTaskAsync(template);
        Assert.Equal(10, _viewModel.InputMinutes);
    }

    [Fact]
    public async Task TimerCompleted_FocusWithoutTask_StartsDefaultBreakWithoutFinishingAnything()
    {
        await LoadDayAsync(TestTasks.Day);
        var completion = TimerSessionCompletionResult.FocusCompleted();
        _timer.Complete().Returns(completion);

        RaiseTimerChanged(TimerStateChangeReason.Completed, TimeSpan.Zero);

        await _taskCache.DidNotReceive().FinishTaskAsync(Arg.Any<TaskDto>());
        Assert.Equal(5, _viewModel.InputMinutes);
        Assert.Equal(300, _viewModel.InitialDurationInSeconds);
    }

    [Fact]
    public async Task TimerCompleted_BreakCompleted_ResetsTimeAndDoesNotFinishTask()
    {
        await LoadDayAsync(TestTasks.Day);
        var completion = TimerSessionCompletionResult.BreakCompleted();
        _timer.Complete().Returns(completion);

        RaiseTimerChanged(TimerStateChangeReason.Completed, TimeSpan.Zero);

        await _taskCache.DidNotReceive().FinishTaskAsync(Arg.Any<TaskDto>());
        Assert.Equal(0, _viewModel.InputMinutes);
        Assert.Equal(0, _viewModel.InitialDurationInSeconds);
    }

    [Fact]
    public async Task TimerCompleted_FinishingTaskFails_CapturesUiErrorInsteadOfThrowing()
    {
        var task = TestTasks.Create();
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        var completion = TimerSessionCompletionResult.FocusCompleted();
        _timer.Complete().Returns(completion);
        var failure = new InvalidOperationException("Speichern fehlgeschlagen");
        _taskCache.FinishTaskAsync(Arg.Any<TaskDto>()).ThrowsAsync(failure);

        RaiseTimerChanged(TimerStateChangeReason.Completed, TimeSpan.Zero);

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "FocusTimerViewModel.HandleTimerCompletedAsync" &&
                options.IsUiFailure &&
                !options.IsFatal));
    }

    [Fact]
    public async Task TimerStarted_RemainingTime_UpdatesDurationAndMinutes()
    {
        await LoadDayAsync(TestTasks.Day);
        var remaining = TimeSpan.FromMinutes(10);

        RaiseTimerChanged(TimerStateChangeReason.Started, remaining);

        Assert.Equal(600, _viewModel.InitialDurationInSeconds);
        Assert.Equal(10, _viewModel.InputMinutes);
    }

    [Fact]
    public async Task TimerCleared_SelectedTask_ClearsSelectionAndSession()
    {
        var task = TestTasks.Create();
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);

        RaiseTimerChanged(TimerStateChangeReason.Cleared, TimeSpan.Zero);

        Assert.Null(_viewModel.SelectedTaskId);
        Assert.Equal(0, _viewModel.InitialDurationInSeconds);
        _timer.Received(1).ClearSession();
    }

    [Fact]
    public async Task TimerTick_RaisesCountdownNotifications()
    {
        await LoadDayAsync(TestTasks.Day);
        var changed = TrackChanges();
        var remaining = TimeSpan.FromSeconds(30);

        RaiseTimerChanged(TimerStateChangeReason.Tick, remaining);

        Assert.Contains(nameof(FocusTimerViewModel.FormattedTime), changed);
        Assert.Contains(nameof(FocusTimerViewModel.CountdownProgress), changed);
    }

    [Theory]
    [InlineData(TimerStateChangeReason.Paused)]
    [InlineData(TimerStateChangeReason.Resumed)]
    public async Task TimerPausedOrResumed_RaisesStateNotifications(TimerStateChangeReason reason)
    {
        await LoadDayAsync(TestTasks.Day);
        var changed = TrackChanges();

        RaiseTimerChanged(reason, TimeSpan.FromMinutes(1));

        Assert.Contains(nameof(FocusTimerViewModel.IsPaused), changed);
        Assert.Contains(nameof(FocusTimerViewModel.IsRunning), changed);
    }

    [Fact]
    public async Task SetMinutes_SelectedTask_ChangesOnlySessionTimeNotTheTask()
    {
        var task = TestTasks.Create(timerMinutes: 25);
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);

        _viewModel.SetMinutes(40);

        Assert.Equal(40, _viewModel.InputMinutes);
        Assert.Equal(25, task.TimerMinutes);
        Assert.Equal(25, _viewModel.SelectedTask.TimerMinutes);
    }

    [Fact]
    public async Task SwitchTimerToStartOrStop_ChangedSessionTime_StartsWithSessionTime()
    {
        var task = TestTasks.Create(timerMinutes: 25);
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        _viewModel.SetMinutes(40);
        TimerStartRequest request = default;
        _ = _timer.SwitchTimerToStartPauseOrResumeAsync(Arg.Do<TimerStartRequest>(captured => request = captured));

        await _viewModel.SwitchTimerToStartOrStop();

        Assert.Equal(2400, request.DurationSeconds);
        Assert.Equal(25, task.TimerMinutes);
    }

    [Fact]
    public async Task TimerStarted_SelectedTask_DoesNotChangeStoredFocusTime()
    {
        var task = TestTasks.Create(timerMinutes: 25);
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        var remaining = TimeSpan.FromMinutes(40);

        RaiseTimerChanged(TimerStateChangeReason.Started, remaining);

        Assert.Equal(25, task.TimerMinutes);
        Assert.Equal(40, _viewModel.InputMinutes);
    }

    [Fact]
    public async Task NormalTasksChanged_ChangedSessionTime_KeepsSessionTimeAndStoredTimeInList()
    {
        var task = TestTasks.Create(timerMinutes: 25);
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        _viewModel.SetMinutes(40);
        var storedTask = TestTasks.Create(timerMinutes: 25);
        storedTask.Id = task.Id;
        SetNormalTasks(storedTask);

        _taskCache.NormalTasksChanged += Raise.Event();

        var listed = Assert.Single(_viewModel.NormalTasks);
        Assert.Equal(25, listed.TimerMinutes);
        Assert.Equal(40, _viewModel.InputMinutes);
    }

    [Fact]
    public async Task ActiveTaskChanged_SameTaskWithChangedSessionTime_KeepsSessionTime()
    {
        var task = TestTasks.Create(timerMinutes: 25);
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        _viewModel.SetMinutes(40);
        SetCentralActiveTask(task);

        _activeTaskService.ActiveTaskChanged += Raise.Event();

        Assert.Equal(40, _viewModel.InputMinutes);
        Assert.Equal(25, task.TimerMinutes);
    }

    [Fact]
    public async Task TimerCompleted_AfterChangedSessionTime_StartsBreakWithStoredBreakTimeAndKeepsTask()
    {
        var task = TestTasks.Create(timerMinutes: 25, breakMinutes: 7);
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        _viewModel.SetMinutes(40);
        var completion = TimerSessionCompletionResult.FocusCompleted();
        _timer.Complete().Returns(completion);

        RaiseTimerChanged(TimerStateChangeReason.Completed, TimeSpan.Zero);

        Assert.Equal(7, _viewModel.InputMinutes);
        Assert.Equal(25, task.TimerMinutes);
    }

    [Fact]
    public async Task SelectTaskAsync_AfterSwitchingTaskAndBack_UsesStoredFocusTime()
    {
        var task = TestTasks.Create("Erste", timerMinutes: 25);
        var other = TestTasks.Create("Zweite", timerMinutes: 10);
        SetNormalTasks(task, other);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        _viewModel.SetMinutes(40);

        await _viewModel.SelectTaskAsync(other.Id);
        await _viewModel.SelectTaskAsync(task.Id);

        Assert.Equal(25, _viewModel.InputMinutes);
    }

    [Fact]
    public async Task ResetTimer_ChangedSessionTime_RestoresStoredFocusTime()
    {
        var task = TestTasks.Create(timerMinutes: 25);
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        _viewModel.SetMinutes(40);

        await _viewModel.ResetTimer();

        Assert.Equal(25, _viewModel.InputMinutes);
    }

    [Theory]
    [InlineData(150, 120)]
    [InlineData(30, 30)]
    [InlineData(-4, 0)]
    public async Task SetMinutes_IdleTimer_LimitsToValidRange(int minutes, int expected)
    {
        await LoadDayAsync(TestTasks.Day);

        _viewModel.SetMinutes(minutes);

        Assert.Equal(expected, _viewModel.InputMinutes);
        Assert.Equal(expected * 60, _viewModel.InitialDurationInSeconds);
    }

    [Fact]
    public async Task SetMinutes_TimerActive_IgnoresChange()
    {
        await LoadDayAsync(TestTasks.Day);
        var before = _viewModel.InputMinutes;
        _timer.IsTimerActive.Returns(true);

        _viewModel.SetMinutes(50);

        Assert.Equal(before, _viewModel.InputMinutes);
    }

    [Fact]
    public async Task SelectedMinuteRatio_SixtyMinutes_IsHalfOfMaximum()
    {
        await LoadDayAsync(TestTasks.Day);

        _viewModel.SetMinutes(60);

        Assert.Equal(0.5, _viewModel.SelectedMinuteRatio);
    }

    [Fact]
    public async Task CanToggleTimer_Running_IsTrue()
    {
        await LoadDayAsync(TestTasks.Day);
        _viewModel.SetMinutes(0);
        _timer.IsRunning.Returns(true);

        Assert.True(_viewModel.CanToggleTimer);
    }

    [Theory]
    [InlineData(30, true)]
    [InlineData(0, false)]
    public async Task CanToggleTimer_Paused_NeedsRemainingTime(int remainingSeconds, bool expected)
    {
        await LoadDayAsync(TestTasks.Day);
        _timer.IsPaused.Returns(true);
        var remaining = TimeSpan.FromSeconds(remainingSeconds);
        _timer.RemainingTime.Returns(remaining);

        Assert.Equal(expected, _viewModel.CanToggleTimer);
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(0, false)]
    public async Task CanToggleTimer_Idle_NeedsConfiguredMinutes(int minutes, bool expected)
    {
        await LoadDayAsync(TestTasks.Day);

        _viewModel.SetMinutes(minutes);

        Assert.Equal(expected, _viewModel.CanToggleTimer);
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task CanResetTimer_RunningAndBreakState_DecidesAvailability(
        bool isRunning,
        bool isBreakMode,
        bool expected)
    {
        await LoadDayAsync(TestTasks.Day);
        _timer.IsRunning.Returns(isRunning);
        _timer.IsBreakMode.Returns(isBreakMode);

        Assert.Equal(expected, _viewModel.CanResetTimer);
    }

    [Fact]
    public async Task FormattedTime_CountdownVisible_ShowsRemainingTime()
    {
        await LoadDayAsync(TestTasks.Day);
        _timer.ShouldShowCountdown.Returns(true);
        var remaining = new TimeSpan(0, 1, 5);
        _timer.RemainingTime.Returns(remaining);

        Assert.Equal("01:05", _viewModel.FormattedTime);
    }

    [Fact]
    public async Task FormattedTime_CountdownHidden_ShowsConfiguredMinutes()
    {
        await LoadDayAsync(TestTasks.Day);
        _timer.ShouldShowCountdown.Returns(false);

        _viewModel.SetMinutes(25);

        Assert.Equal("25:00", _viewModel.FormattedTime);
    }

    [Fact]
    public async Task CountdownProgress_QuarterOfDurationLeft_IsQuarter()
    {
        var task = TestTasks.Create(timerMinutes: 25);
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        var remaining = TimeSpan.FromSeconds(375);
        _timer.RemainingTime.Returns(remaining);

        Assert.Equal(0.25, _viewModel.CountdownProgress);
    }

    [Fact]
    public async Task CountdownProgress_NoDuration_IsZero()
    {
        await LoadDayAsync(TestTasks.Day);
        _viewModel.SetMinutes(0);
        var remaining = TimeSpan.FromSeconds(30);
        _timer.RemainingTime.Returns(remaining);

        Assert.Equal(0, _viewModel.CountdownProgress);
    }

    [Fact]
    public async Task Dispose_AfterwardsEvents_AreIgnored()
    {
        await LoadDayAsync(TestTasks.Day);
        _viewModel.Dispose();
        _taskCache.ClearReceivedCalls();

        _scopeService.ScopeChanged += Raise.Event();
        _taskCache.NormalTasksChanged += Raise.Event();

        _taskCache.DidNotReceive().GetNormalTasks(Arg.Any<DateTime?>());
    }

    [Fact]
    public async Task DayChanged_TodayWasSelectedAndTimerIdle_FollowsLikeManualDayChange()
    {
        var task = TestTasks.Create(taskDate: TestTasks.Today);
        SetNormalTasks(task);
        await _viewModel.InitializeAsync();
        await _viewModel.SelectTaskAsync(task.Id);
        ClearCalls();
        var tomorrow = TestTasks.Today.AddDays(1);

        RaiseDayChanged(TestTasks.Today, tomorrow);

        Assert.Equal(tomorrow, _viewModel.SelectedTaskDate);
        Assert.Equal(tomorrow, _viewModel.TaskFilter.SelectedDate);
        Assert.Null(_viewModel.SelectedTaskId);
        _taskCache.Received().GetNormalTasks(tomorrow);
    }

    [Fact]
    public async Task DayChanged_TodayWasSelectedAndTimerActive_KeepsTaskAndTimerButLoadsNewDay()
    {
        var task = TestTasks.Create(taskDate: TestTasks.Today);
        SetNormalTasks(task);
        await _viewModel.InitializeAsync();
        await _viewModel.SelectTaskAsync(task.Id);
        ClearCalls();
        _timer.IsTimerActive.Returns(true);
        var tomorrow = TestTasks.Today.AddDays(1);

        RaiseDayChanged(TestTasks.Today, tomorrow);

        Assert.Equal(tomorrow, _viewModel.SelectedTaskDate);
        Assert.Equal(tomorrow, _viewModel.TaskFilter.SelectedDate);
        Assert.Equal(task.Id, _viewModel.SelectedTaskId);
        await _timer.DidNotReceive().StopAsync();
        _activeTaskService.DidNotReceive().SetActiveTask(Arg.Any<Guid?>());
        _taskCache.Received().GetNormalTasks(tomorrow);
    }

    [Fact]
    public async Task DayChanged_TimerActive_FocusEndingOnNewDayStillFinishesTaskOfPreviousDay()
    {
        var task = TestTasks.Create(taskDate: TestTasks.Today, breakMinutes: 7);
        SetNormalTasks(task);
        await _viewModel.InitializeAsync();
        await _viewModel.SelectTaskAsync(task.Id);
        _timer.IsTimerActive.Returns(true);
        SetNormalTasks();
        _taskCache.FindTaskById(task.Id!.Value).Returns(task);
        RaiseDayChanged(TestTasks.Today, TestTasks.Today.AddDays(1));
        var completion = TimerSessionCompletionResult.FocusCompleted();
        _timer.Complete().Returns(completion);

        RaiseTimerChanged(TimerStateChangeReason.Completed, TimeSpan.Zero);

        await _taskCache.Received(1).FinishTaskAsync(task);
        Assert.Equal(7, _viewModel.InputMinutes);
    }

    [Fact]
    public async Task DayChanged_OtherDayWasSelected_StaysOnSelectedDay()
    {
        var task = TestTasks.Create();
        SetNormalTasks(task);
        await LoadDayAsync(TestTasks.Day);
        await _viewModel.SelectTaskAsync(task.Id);
        ClearCalls();

        RaiseDayChanged(TestTasks.Today, TestTasks.Today.AddDays(1));

        Assert.Equal(TestTasks.Day, _viewModel.SelectedTaskDate);
        Assert.Equal(task.Id, _viewModel.SelectedTaskId);
        await _timer.DidNotReceive().StopAsync();
        _taskCache.DidNotReceive().GetNormalTasks(Arg.Any<DateTime?>());
    }

    [Fact]
    public async Task DayChanged_AfterDispose_IsIgnored()
    {
        await _viewModel.InitializeAsync();
        _viewModel.Dispose();

        RaiseDayChanged(TestTasks.Today, TestTasks.Today.AddDays(1));

        Assert.Equal(TestTasks.Today, _viewModel.SelectedTaskDate);
    }

    private void SetNormalTasks(params TaskDto[] tasks)
    {
        IReadOnlyList<TaskDto> list = tasks;
        _taskCache.GetNormalTasks(Arg.Any<DateTime?>()).Returns(list);
    }

    private void SetRegularTasks(params TaskDto[] tasks)
    {
        IReadOnlyList<TaskDto> list = tasks;
        _taskCache.GetRegularTasks(Arg.Any<DateTime?>()).Returns(list);
    }

    private static TaskDto CreateChangedCopy(
        TaskDto original,
        string note,
        TaskPriority priority,
        int timerMinutes,
        int breakMinutes)
    {
        var copy = TestTasks.Create(
            note,
            isRegular: original.IsRegularTask,
            taskDate: original.TaskDate,
            priority: priority,
            timerMinutes: timerMinutes,
            breakMinutes: breakMinutes);
        copy.Id = original.Id;
        return copy;
    }

    private void SetCentralActiveTask(TaskDto task)
    {
        _activeTaskService.ActiveTask.Returns(task);
        _activeTaskService.ActiveTaskId.Returns(task.Id);
    }

    private async Task LoadDayAsync(DateTime day)
    {
        await _viewModel.InitializeAsync();
        var payload = new FilterModel { SelectedDate = day };
        await _viewModel.ApplyFilterChangesAsync(payload);
        ClearCalls();
    }

    private void ClearCalls()
    {
        _taskCache.ClearReceivedCalls();
        _activeTaskService.ClearReceivedCalls();
        _timer.ClearReceivedCalls();
        _errorCapture.ClearReceivedCalls();
    }

    private void RaiseDayChanged(DateTime previousDay, DateTime currentDay)
    {
        var args = new DayChangedEventArgs(previousDay, currentDay);
        _dayChange.DayChanged += Raise.EventWith(args);
    }

    private void RaiseTimerChanged(TimerStateChangeReason reason, TimeSpan remaining)
    {
        var args = new TimerChangedEventArgs(remaining, false, reason);
        _timer.TimerChanged += Raise.EventWith(args);
    }

    private List<string?> TrackChanges()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        return changed;
    }
}
