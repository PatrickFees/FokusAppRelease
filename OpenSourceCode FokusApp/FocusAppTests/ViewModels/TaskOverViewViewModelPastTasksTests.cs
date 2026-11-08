using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Components.SharedUI.FilterComponent;
using FokusApp.Components.TaskOverViewComponent;
using FokusApp.Core.Enums;
using FokusApp.Core.Extensions;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FocusAppTests.ViewModels;

public class TaskOverViewViewModelPastTasksTests
{
    private readonly ITaskCacheService _taskCache = Substitute.For<ITaskCacheService>();
    private readonly IActiveTaskService _activeTaskService = Substitute.For<IActiveTaskService>();
    private readonly IScopeService _scopeService = Substitute.For<IScopeService>();
    private readonly IAppStartupStateService _startupState = Substitute.For<IAppStartupStateService>();
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly IUiTextProvider _textProvider = TestTexts.Create();
    private readonly ITaskPriorityFormatter _priorityFormatter = Substitute.For<ITaskPriorityFormatter>();
    private readonly ITaskSeriesService _taskSeriesService = Substitute.For<ITaskSeriesService>();
    private readonly FakeTimeProvider _clock = TestTasks.CreateClock();
    private readonly IDayChangeService _dayChange = Substitute.For<IDayChangeService>();
    private readonly TaskOverViewViewModel _viewModel;
    private List<TaskDto> _store = [];

    public TaskOverViewViewModelPastTasksTests()
    {
        IReadOnlyList<TaskDto> empty = [];
        _taskCache.GetTemplates(Arg.Any<DateTime?>()).Returns(empty);
        _taskCache.GetDoneTasks(Arg.Any<DateTime?>()).Returns(empty);
        _scopeService.IsWorkScope.Returns(true);
        var taskEditor = new TaskEditorCoordinator(_scopeService, _taskCache, _textProvider, _priorityFormatter, _taskSeriesService);
        _viewModel = new TaskOverViewViewModel(
            _taskCache,
            _activeTaskService,
            _scopeService,
            _startupState,
            taskEditor,
            _taskSeriesService,
            _errorCapture,
            _clock,
            _dayChange);
    }

    [Theory]
    [InlineData(PastTaskTarget.Today, 2026, 3, 15)]
    [InlineData(PastTaskTarget.Tomorrow, 2026, 3, 16)]
    [InlineData(PastTaskTarget.Weekend, 2026, 3, 21)]
    public async Task AssignPastTaskAsync_Target_SetsExpectedDate(PastTaskTarget target, int year, int month, int day)
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Today.AddDays(-1), priority: TaskPriority.Hoch);
        UseInMemoryCache(past);
        await _viewModel.InitializeAsync();

        await _viewModel.AssignPastTaskAsync(past.Id!.Value, target);

        var moved = Assert.Single(_store);
        Assert.Equal(new DateTime(year, month, day), moved.TaskDate);
        Assert.Equal(TaskPriority.Hoch, moved.Priority);
        Assert.Empty(_viewModel.OverdueTasks);
        Assert.Single(_viewModel.ActiveTasks);
    }

    [Fact]
    public async Task AssignPastTaskAsync_ActiveTask_DoesNotUpdate()
    {
        var past = TestTasks.Create("Läuft", taskDate: TestTasks.Today.AddDays(-1));
        UseInMemoryCache(past);
        _activeTaskService.IsTaskActive(past.Id!.Value).Returns(true);
        await _viewModel.InitializeAsync();

        await _viewModel.AssignPastTaskAsync(past.Id!.Value, PastTaskTarget.Today);

        await _taskCache.DidNotReceive().MoveTaskToDateAsync(Arg.Any<TaskDto>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task AssignPastTaskAsync_TaskNotInThePast_DoesNotUpdate()
    {
        var today = TestTasks.Create("Heute", taskDate: TestTasks.Today);
        UseInMemoryCache(today);
        await _viewModel.InitializeAsync();

        await _viewModel.AssignPastTaskAsync(today.Id!.Value, PastTaskTarget.Tomorrow);

        await _taskCache.DidNotReceive().MoveTaskToDateAsync(Arg.Any<TaskDto>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task AssignPastTaskAsync_UpdateFails_CapturesError()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Today.AddDays(-1));
        UseInMemoryCache(past);
        var failure = new InvalidOperationException("kaputt");
        _taskCache.MoveTaskToDateAsync(Arg.Any<TaskDto>(), Arg.Any<DateTime>()).Throws(failure);
        await _viewModel.InitializeAsync();

        await _viewModel.AssignPastTaskAsync(past.Id!.Value, PastTaskTarget.Today);

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "TaskOverViewViewModel.AssignPastTaskAsync"));
    }

    [Fact]
    public async Task DiscardPastTaskAsync_PastTask_DeletesAndOffersUndo()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Today.AddDays(-1));
        UseInMemoryCache(past, TestTasks.Create("Weitere", taskDate: TestTasks.Today.AddDays(-2)));
        await _viewModel.InitializeAsync();

        await _viewModel.DiscardPastTaskAsync(past.Id!.Value);

        await _taskCache.Received(1).DeleteTaskAsync(past.Id!.Value);
        Assert.Equal(new UndoNotice(UndoNoticeKind.TaskDiscarded, 1), _viewModel.CurrentUndo);
    }

    [Fact]
    public async Task DiscardPastTaskAsync_SeriesTask_DeletesOnlyThatTaskThroughTheSeriesService()
    {
        var past = CreateSeriesStart(TestTasks.Today.AddDays(-1));
        UseInMemoryCache(past);
        await _viewModel.InitializeAsync();

        await _viewModel.DiscardPastTaskAsync(past.Id!.Value);

        await _taskSeriesService.Received(1).DeleteTaskAsync(past.Id!.Value, TaskSeriesScope.ThisTaskOnly, null);
        await _taskCache.DidNotReceive().DeleteTaskAsync(Arg.Any<Guid>());
        Assert.Equal(new UndoNotice(UndoNoticeKind.TaskDiscarded, 1), _viewModel.CurrentUndo);
    }

    [Fact]
    public async Task UndoAsync_DiscardedSeriesStartHandedItsRuleOn_RestoresItAsFollowingTask()
    {
        var past = CreateSeriesStart(TestTasks.Today.AddDays(-1));
        var newStartId = Guid.NewGuid();
        UseInMemoryCache(past);
        _taskSeriesService.DeleteTaskAsync(past.Id!.Value, TaskSeriesScope.ThisTaskOnly, null).Returns(newStartId);
        await _viewModel.InitializeAsync();
        await _viewModel.DiscardPastTaskAsync(past.Id!.Value);

        await _viewModel.UndoAsync();

        await _taskCache.Received(1).AddTaskAsync(Arg.Is<TaskDto>(restored =>
            restored.Id == past.Id &&
            restored.SeriesId == newStartId &&
            restored.RepeatUnit == TaskRepeatUnit.None));
    }

    [Fact]
    public async Task UndoAsync_DiscardedFollowingTask_RestoresItInItsSeries()
    {
        var seriesId = Guid.NewGuid();
        var past = TestTasks.Create("Sport", taskDate: TestTasks.Today.AddDays(-1));
        past.SeriesId = seriesId;
        UseInMemoryCache(past);
        await _viewModel.InitializeAsync();
        await _viewModel.DiscardPastTaskAsync(past.Id!.Value);

        await _viewModel.UndoAsync();

        await _taskCache.Received(1).AddTaskAsync(Arg.Is<TaskDto>(restored =>
            restored.Id == past.Id && restored.SeriesId == seriesId));
    }

    [Fact]
    public async Task UndoAsync_AfterDiscard_RestoresTaskWithAllProperties()
    {
        var past = TestTasks.Create(
            "Gestern",
            taskDate: TestTasks.Today.AddDays(-1),
            priority: TaskPriority.Dringend,
            timerMinutes: 40,
            breakMinutes: 9,
            isPositive: true);
        UseInMemoryCache(past);
        await _viewModel.InitializeAsync();
        await _viewModel.DiscardPastTaskAsync(past.Id!.Value);

        await _viewModel.UndoAsync();

        var restored = Assert.Single(_store);
        Assert.Equal(past.Id, restored.Id);
        Assert.Equal(past.TaskDate, restored.TaskDate);
        Assert.Equal(TaskPriority.Dringend, restored.Priority);
        Assert.Equal(40, restored.TimerMinutes);
        Assert.Equal(9, restored.BreakMinutes);
        Assert.True(restored.IsPositive);
        Assert.Null(_viewModel.CurrentUndo);
        Assert.Single(_viewModel.OverdueTasks);
    }

    [Fact]
    public async Task CurrentUndo_AfterFiveSeconds_Expires()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Today.AddDays(-1));
        UseInMemoryCache(past);
        await _viewModel.InitializeAsync();
        await _viewModel.DiscardPastTaskAsync(past.Id!.Value);

        _clock.Advance(TimeSpan.FromSeconds(4));
        Assert.NotNull(_viewModel.CurrentUndo);
        _clock.Advance(TimeSpan.FromSeconds(2));

        Assert.Null(_viewModel.CurrentUndo);
        await _viewModel.UndoAsync();
        Assert.Empty(_store);
    }

    [Fact]
    public async Task AssignAllPastTasksToTomorrowAsync_MultipleTasks_MovesAllExceptRunningOne()
    {
        var first = TestTasks.Create("A", taskDate: TestTasks.Today.AddDays(-1));
        var second = TestTasks.Create("B", taskDate: TestTasks.Today.AddDays(-5));
        var running = TestTasks.Create("Läuft", taskDate: TestTasks.Today.AddDays(-2));
        UseInMemoryCache(first, second, running);
        _activeTaskService.IsTaskActive(running.Id!.Value).Returns(true);
        await _viewModel.InitializeAsync();

        await _viewModel.AssignAllPastTasksToTomorrowAsync();

        var tomorrow = TestTasks.Today.AddDays(1);
        Assert.Equal(tomorrow, _store.Single(task => task.Id == first.Id).TaskDate);
        Assert.Equal(tomorrow, _store.Single(task => task.Id == second.Id).TaskDate);
        Assert.Equal(running.TaskDate, _store.Single(task => task.Id == running.Id).TaskDate);
        Assert.Equal(new UndoNotice(UndoNoticeKind.TasksMovedToTomorrow, 2), _viewModel.CurrentUndo);
    }

    [Fact]
    public async Task AssignAllPastTasksToTomorrowAsync_FilterActive_MovesOnlyVisibleTasks()
    {
        var sport = TestTasks.Create("Sport", taskDate: TestTasks.Today.AddDays(-1));
        var reading = TestTasks.Create("Lesen", taskDate: TestTasks.Today.AddDays(-1));
        UseInMemoryCache(sport, reading);
        await _viewModel.InitializeAsync();
        _viewModel.ApplyOverviewFilters(new FilterModel { SearchText = "Sport" });

        await _viewModel.AssignAllPastTasksToTomorrowAsync();

        Assert.Equal(TestTasks.Today.AddDays(1), _store.Single(task => task.Id == sport.Id).TaskDate);
        Assert.Equal(reading.TaskDate, _store.Single(task => task.Id == reading.Id).TaskDate);
    }

    [Fact]
    public async Task AssignAllPastTasksToTomorrowAsync_NothingToMove_DoesNothing()
    {
        UseInMemoryCache(TestTasks.Create("Heute", taskDate: TestTasks.Today));
        await _viewModel.InitializeAsync();

        await _viewModel.AssignAllPastTasksToTomorrowAsync();

        await _taskCache.DidNotReceive().MoveTaskToDateAsync(Arg.Any<TaskDto>(), Arg.Any<DateTime>());
        Assert.Null(_viewModel.CurrentUndo);
    }

    [Fact]
    public async Task UndoAsync_AfterAssignAll_RestoresOriginalDates()
    {
        var first = TestTasks.Create("A", taskDate: TestTasks.Today.AddDays(-1));
        var second = TestTasks.Create("B", taskDate: TestTasks.Today.AddDays(-5));
        UseInMemoryCache(first, second);
        await _viewModel.InitializeAsync();
        await _viewModel.AssignAllPastTasksToTomorrowAsync();

        await _viewModel.UndoAsync();

        Assert.Equal(first.TaskDate, _store.Single(task => task.Id == first.Id).TaskDate);
        Assert.Equal(second.TaskDate, _store.Single(task => task.Id == second.Id).TaskDate);
        Assert.Equal(2, _viewModel.OverdueTasks.Count);
        Assert.Null(_viewModel.CurrentUndo);
    }

    [Fact]
    public async Task AssignPastTaskAsync_UnknownTarget_Throws()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Today.AddDays(-1));
        UseInMemoryCache(past);
        await _viewModel.InitializeAsync();
        var unknownTarget = (PastTaskTarget)99;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _viewModel.AssignPastTaskAsync(past.Id!.Value, unknownTarget));
    }

    [Fact]
    public async Task DiscardPastTaskAsync_TaskNotInThePast_DeletesNothing()
    {
        var current = TestTasks.Create("Heute", taskDate: TestTasks.Today);
        UseInMemoryCache(current);
        await _viewModel.InitializeAsync();

        await _viewModel.DiscardPastTaskAsync(current.Id!.Value);

        await _taskCache.DidNotReceive().DeleteTaskAsync(Arg.Any<Guid>());
        Assert.Null(_viewModel.CurrentUndo);
    }

    [Fact]
    public async Task DiscardPastTaskAsync_ActiveTask_DeletesNothing()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Today.AddDays(-1));
        UseInMemoryCache(past);
        _activeTaskService.IsTaskActive(past.Id!.Value).Returns(true);
        await _viewModel.InitializeAsync();

        await _viewModel.DiscardPastTaskAsync(past.Id!.Value);

        await _taskCache.DidNotReceive().DeleteTaskAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DiscardPastTaskAsync_DeleteFails_CapturesErrorAndOffersNoUndo()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Today.AddDays(-1));
        UseInMemoryCache(past);
        var failure = new InvalidOperationException("kaputt");
        _taskCache.DeleteTaskAsync(Arg.Any<Guid>()).Throws(failure);
        await _viewModel.InitializeAsync();

        await _viewModel.DiscardPastTaskAsync(past.Id!.Value);

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "TaskOverViewViewModel.DiscardPastTaskAsync"));
        Assert.Null(_viewModel.CurrentUndo);
    }

    [Fact]
    public async Task AssignAllPastTasksToTomorrowAsync_UpdateFails_CapturesErrorAndOffersNoUndo()
    {
        var first = TestTasks.Create("A", taskDate: TestTasks.Today.AddDays(-1));
        var second = TestTasks.Create("B", taskDate: TestTasks.Today.AddDays(-2));
        UseInMemoryCache(first, second);
        var failure = new InvalidOperationException("kaputt");
        _taskCache.MoveTaskToDateAsync(Arg.Any<TaskDto>(), Arg.Any<DateTime>()).Throws(failure);
        await _viewModel.InitializeAsync();

        await _viewModel.AssignAllPastTasksToTomorrowAsync();

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "TaskOverViewViewModel.AssignAllPastTasksToTomorrowAsync"));
        Assert.Null(_viewModel.CurrentUndo);
    }

    [Fact]
    public async Task AssignAllPastTasksToTomorrowAsync_SecondUpdateFails_OffersUndoForTheMovedTask()
    {
        var first = TestTasks.Create("A", taskDate: TestTasks.Today.AddDays(-1));
        var second = TestTasks.Create("B", taskDate: TestTasks.Today.AddDays(-2));
        UseInMemoryCache(first, second);
        var failure = new InvalidOperationException("kaputt");
        var calls = 0;
        _taskCache.MoveTaskToDateAsync(Arg.Any<TaskDto>(), Arg.Any<DateTime>()).Returns(_ => ++calls == 2 ? Task.FromException(failure) : Task.CompletedTask);
        await _viewModel.InitializeAsync();

        await _viewModel.AssignAllPastTasksToTomorrowAsync();

        await _errorCapture.Received(1).CaptureAsync(failure, Arg.Any<AppErrorCaptureOptions?>());
        Assert.Equal(new UndoNotice(UndoNoticeKind.TasksMovedToTomorrow, 1), _viewModel.CurrentUndo);
    }

    [Fact]
    public async Task UndoAsync_RestoringFails_CapturesErrorAndClosesTheNotice()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Today.AddDays(-1));
        UseInMemoryCache(past);
        await _viewModel.InitializeAsync();
        await _viewModel.DiscardPastTaskAsync(past.Id!.Value);
        var failure = new InvalidOperationException("kaputt");
        _taskCache.AddTaskAsync(Arg.Any<TaskDto>()).Throws(failure);

        await _viewModel.UndoAsync();

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "TaskOverViewViewModel.UndoAsync"));
        Assert.Null(_viewModel.CurrentUndo);
    }

    [Fact]
    public async Task UndoAsync_NothingToUndo_DoesNothing()
    {
        await _viewModel.InitializeAsync();

        await _viewModel.UndoAsync();

        await _taskCache.DidNotReceive().AddTaskAsync(Arg.Any<TaskDto>());
        Assert.Null(_viewModel.CurrentUndo);
    }

    [Fact]
    public async Task DiscardPastTaskAsync_TwoDiscards_ReplaceTheFirstUndoNotice()
    {
        var first = TestTasks.Create("A", taskDate: TestTasks.Today.AddDays(-1));
        var second = TestTasks.Create("B", taskDate: TestTasks.Today.AddDays(-2));
        UseInMemoryCache(first, second);
        await _viewModel.InitializeAsync();

        await _viewModel.DiscardPastTaskAsync(first.Id!.Value);
        await _viewModel.DiscardPastTaskAsync(second.Id!.Value);
        _clock.Advance(TimeSpan.FromSeconds(6));

        Assert.Null(_viewModel.CurrentUndo);
    }

    [Fact]
    public async Task Dispose_WhileUndoNoticeIsShown_StopsTheTimer()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Today.AddDays(-1));
        UseInMemoryCache(past);
        await _viewModel.InitializeAsync();
        await _viewModel.DiscardPastTaskAsync(past.Id!.Value);

        _viewModel.Dispose();
        _clock.Advance(TimeSpan.FromSeconds(10));

        Assert.NotNull(_viewModel.CurrentUndo);
    }

    private static TaskDto CreateSeriesStart(DateTime date)
    {
        var task = TestTasks.Create("Sport", taskDate: date);
        task.RepeatUnit = TaskRepeatUnit.Week;
        task.RepeatWeekdays = TaskWeekdays.All;
        return task;
    }

    private void UseInMemoryCache(params TaskDto[] tasks)
    {
        _store = [.. tasks];
        _taskCache.GetNormalTasks(Arg.Any<DateTime?>())
            .Returns(_ => (IReadOnlyList<TaskDto>)_store.Select(TaskExtension.Clone).ToList());

        _taskCache.When(cache => cache.UpdateTaskAsync(Arg.Any<TaskDto>())).Do(call =>
        {
            var updated = call.Arg<TaskDto>();
            _store[_store.FindIndex(task => task.Id == updated.Id)] = TaskExtension.Clone(updated);
            _taskCache.NormalTasksChanged += Raise.EventWith(EventArgs.Empty);
        });
        _taskCache.When(cache => cache.MoveTaskToDateAsync(Arg.Any<TaskDto>(), Arg.Any<DateTime>())).Do(call =>
        {
            var task = call.Arg<TaskDto>();
            var date = call.ArgAt<DateTime>(1);
            var updated = TaskExtension.Clone(task);
            updated.TaskDate = date.Date;
            _store[_store.FindIndex(t => t.Id == updated.Id)] = updated;
            _taskCache.NormalTasksChanged += Raise.EventWith(EventArgs.Empty);
        });
        _taskCache.When(cache => cache.DeleteTaskAsync(Arg.Any<Guid>())).Do(call =>
        {
            _store.RemoveAll(task => task.Id == call.Arg<Guid>());
            _taskCache.NormalTasksChanged += Raise.EventWith(EventArgs.Empty);
        });
        _taskCache.When(cache => cache.AddTaskAsync(Arg.Any<TaskDto>())).Do(call =>
        {
            _store.Add(TaskExtension.Clone(call.Arg<TaskDto>()));
            _taskCache.NormalTasksChanged += Raise.EventWith(EventArgs.Empty);
        });
    }
}
