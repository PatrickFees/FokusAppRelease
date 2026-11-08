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

public class TaskOverViewViewModelTests
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

    public TaskOverViewViewModelTests()
    {
        IReadOnlyList<TaskDto> empty = [];
        _taskCache.GetNormalTasks(Arg.Any<DateTime?>()).Returns(empty);
        _taskCache.GetTemplates(Arg.Any<DateTime?>()).Returns(empty);
        _taskCache.GetDoneTasks(Arg.Any<DateTime?>()).Returns(empty);
        _scopeService.IsWorkScope.Returns(true);
        _priorityFormatter.Format(Arg.Any<TaskPriority>()).Returns(call => call.Arg<TaskPriority>().ToString());
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

    [Fact]
    public void Constructor_NoInteraction_HasNoDayFilter()
    {
        Assert.Null(_viewModel.Filter.SelectedDate);
    }

    [Fact]
    public void OpenAddNoteModal_NoDayFilter_UsesTodayOfClock()
    {
        var payload = new FilterModel { SelectedDate = null };
        _viewModel.ApplyOverviewFilters(payload);

        _viewModel.OpenAddNoteModal();

        var input = _viewModel.TaskEditorInput;
        Assert.NotNull(input);
        Assert.Equal(TestTasks.Today, input.ContextualTaskDate);
    }

    [Fact]
    public async Task InitializeAsync_StartupNotReady_WaitsBeforeLoading()
    {
        await _viewModel.InitializeAsync();

        await _startupState.Received(1).WaitForReadyAsync(Arg.Any<CancellationToken>());
        _taskCache.Received().GetNormalTasks(Arg.Any<DateTime?>());
        _taskCache.Received().GetTemplates(Arg.Any<DateTime?>());
        _taskCache.Received().GetDoneTasks(Arg.Any<DateTime?>());
    }

    [Fact]
    public async Task ApplyOverviewFilters_NoDayFilter_ShowsTasksInThreeSections()
    {
        var active = TestTasks.Create("Aktiv", taskDate: TestTasks.Today);
        var template = TestTasks.Create("Vorlage", isTemplate: true);
        var done = TestTasks.Create("Fertig", isDone: true);
        SetTasks([active], [template], [done]);
        await _viewModel.InitializeAsync();
        var payload = new FilterModel { SelectedDate = null };

        _viewModel.ApplyOverviewFilters(payload);

        var activeShown = Assert.Single(_viewModel.ActiveTasks);
        Assert.Equal(active.Id, activeShown.Id);

        var templateShown = Assert.Single(_viewModel.Templates);
        Assert.Equal(template.Id, templateShown.Id);

        var completedShown = Assert.Single(_viewModel.CompletedTasks);
        Assert.Equal(done.Id, completedShown.Id);
    }

    [Fact]
    public async Task InitializeAsync_OpenTaskInThePast_ShowsItOnlyInOverdueSection()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Today.AddDays(-1));
        var today = TestTasks.Create("Heute", taskDate: TestTasks.Today);
        var future = TestTasks.Create("Morgen", taskDate: TestTasks.Today.AddDays(1));
        SetTasks([past, today, future], [], []);

        await _viewModel.InitializeAsync();

        var overdueShown = Assert.Single(_viewModel.OverdueTasks);
        Assert.Equal(past.Id, overdueShown.Id);
        Assert.Equal(2, _viewModel.ActiveTasks.Count);
        Assert.DoesNotContain(_viewModel.ActiveTasks, task => task.Id == past.Id);
    }

    [Fact]
    public async Task ApplyOverviewFilters_SearchText_AppliesToOverdueSection()
    {
        var sport = TestTasks.Create("Sport", taskDate: TestTasks.Day);
        var reading = TestTasks.Create("Lesen", taskDate: TestTasks.Day);
        SetTasks([sport, reading], [], []);
        await _viewModel.InitializeAsync();
        var payload = new FilterModel { SelectedDate = null, SearchText = "Sport" };

        _viewModel.ApplyOverviewFilters(payload);

        var overdueShown = Assert.Single(_viewModel.OverdueTasks);
        Assert.Equal(sport.Id, overdueShown.Id);
    }

    [Fact]
    public async Task DayChanged_TaskBecomesPast_MovesFromActiveToOverdueSection()
    {
        var task = TestTasks.Create("Heute", taskDate: TestTasks.Today);
        SetTasks([task], [], []);
        await _viewModel.InitializeAsync();
        Assert.Single(_viewModel.ActiveTasks);
        Assert.Empty(_viewModel.OverdueTasks);

        _clock.Advance(TimeSpan.FromDays(1));
        RaiseDayChanged(TestTasks.Today, TestTasks.Today.AddDays(1));

        Assert.Empty(_viewModel.ActiveTasks);
        Assert.Single(_viewModel.OverdueTasks);
    }

    [Fact]
    public async Task OpenReschedulePopup_KnownTask_OpensPopupWithTodayAsInitialDate()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Day);
        SetTasks([past], [], []);
        await _viewModel.InitializeAsync();

        _viewModel.OpenReschedulePopup(past.Id!.Value);

        Assert.True(_viewModel.IsReschedulePopupOpen);
        Assert.Equal(TestTasks.Today, _viewModel.RescheduleInitialDate);
    }

    [Fact]
    public async Task OpenReschedulePopup_ActiveTask_DoesNotOpenPopup()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Day);
        SetTasks([past], [], []);
        await _viewModel.InitializeAsync();
        _activeTaskService.IsTaskActive(past.Id!.Value).Returns(true);

        _viewModel.OpenReschedulePopup(past.Id!.Value);

        Assert.False(_viewModel.IsReschedulePopupOpen);
    }

    [Fact]
    public async Task ConfirmRescheduleAsync_ChosenDate_UpdatesTaskDateAndClosesPopup()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Day);
        SetTasks([past], [], []);
        await _viewModel.InitializeAsync();
        _viewModel.OpenReschedulePopup(past.Id!.Value);
        var newDate = TestTasks.Today.AddDays(3);

        await _viewModel.ConfirmRescheduleAsync(newDate.AddHours(5));

        Assert.False(_viewModel.IsReschedulePopupOpen);
        await _taskCache.Received(1).MoveTaskToDateAsync(
            Arg.Is<TaskDto>(task => task.Id == past.Id && !task.IsTemplate),
            newDate);
    }

    [Fact]
    public async Task ConfirmRescheduleAsync_NoPopupOpen_DoesNotUpdate()
    {
        await _viewModel.InitializeAsync();

        await _viewModel.ConfirmRescheduleAsync(TestTasks.Today);

        await _taskCache.DidNotReceive().MoveTaskToDateAsync(Arg.Any<TaskDto>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task CloseReschedulePopup_OpenPopup_ClosesWithoutUpdate()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Day);
        SetTasks([past], [], []);
        await _viewModel.InitializeAsync();
        _viewModel.OpenReschedulePopup(past.Id!.Value);

        _viewModel.CloseReschedulePopup();

        Assert.False(_viewModel.IsReschedulePopupOpen);
        await _taskCache.DidNotReceive().MoveTaskToDateAsync(Arg.Any<TaskDto>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task ApplyOverviewFilters_DayFilter_RestrictsTasksButNotTemplates()
    {
        var onDay = TestTasks.Create("Am Tag", taskDate: TestTasks.Today);
        var otherDay = TestTasks.Create("Anderer Tag", taskDate: TestTasks.Today.AddDays(1));
        var template = TestTasks.Create("Vorlage", isTemplate: true);
        SetTasks([onDay, otherDay], [template], []);
        await _viewModel.InitializeAsync();
        var payload = new FilterModel { SelectedDate = TestTasks.Today };

        _viewModel.ApplyOverviewFilters(payload);

        var activeShown = Assert.Single(_viewModel.ActiveTasks);
        Assert.Equal(onDay.Id, activeShown.Id);

        var templateShown = Assert.Single(_viewModel.Templates);
        Assert.Equal(template.Id, templateShown.Id);
    }

    [Fact]
    public async Task ApplyOverviewFilters_DayChanged_ReloadsTasksForNewDay()
    {
        await _viewModel.InitializeAsync();
        _taskCache.ClearReceivedCalls();
        var payload = new FilterModel { SelectedDate = TestTasks.Today };

        _viewModel.ApplyOverviewFilters(payload);

        _taskCache.Received().GetNormalTasks(TestTasks.Today);
        _taskCache.Received().GetTemplates(TestTasks.Today);
        _taskCache.Received().GetDoneTasks(TestTasks.Today);
    }

    [Fact]
    public async Task ApplyOverviewFilters_SameDay_FiltersWithoutReloading()
    {
        var sport = TestTasks.Create("Sport", taskDate: TestTasks.Today);
        var reading = TestTasks.Create("Lesen", taskDate: TestTasks.Today);
        SetTasks([sport, reading], [], []);
        await _viewModel.InitializeAsync();
        var dayPayload = new FilterModel { SelectedDate = TestTasks.Today };
        _viewModel.ApplyOverviewFilters(dayPayload);
        _taskCache.ClearReceivedCalls();
        var searchPayload = new FilterModel
        {
            SelectedDate = TestTasks.Today,
            SearchText = "Sport"
        };

        _viewModel.ApplyOverviewFilters(searchPayload);

        var activeShown = Assert.Single(_viewModel.ActiveTasks);
        Assert.Equal(sport.Id, activeShown.Id);
        _taskCache.DidNotReceive().GetNormalTasks(Arg.Any<DateTime?>());
    }

    [Fact]
    public async Task ApplyOverviewFilters_PriorityFilter_ShowsOnlySelectedPriorities()
    {
        var low = TestTasks.Create("Niedrig", priority: TaskPriority.Wenig, taskDate: TestTasks.Today);
        var urgent = TestTasks.Create("Dringend", priority: TaskPriority.Dringend, taskDate: TestTasks.Today);
        SetTasks([low, urgent], [], []);
        await _viewModel.InitializeAsync();
        var payload = new FilterModel
        {
            SelectedDate = null,
            SelectedPriorities = [TaskPriority.Dringend]
        };

        _viewModel.ApplyOverviewFilters(payload);

        var activeShown = Assert.Single(_viewModel.ActiveTasks);
        Assert.Equal(urgent.Id, activeShown.Id);
    }

    [Fact]
    public async Task ApplyOverviewFilters_SortDirection_OrdersTasks()
    {
        var older = TestTasks.Create("Älter", taskDate: TestTasks.Today);
        older.CreationDate = TestTasks.Day.AddHours(1);
        var newer = TestTasks.Create("Neuer", taskDate: TestTasks.Today);
        newer.CreationDate = TestTasks.Day.AddHours(5);
        SetTasks([older, newer], [], []);
        await _viewModel.InitializeAsync();
        var descending = new FilterModel { SelectedDate = null, IsSortDescending = true };
        var ascending = new FilterModel { SelectedDate = null, IsSortDescending = false };

        _viewModel.ApplyOverviewFilters(descending);
        var descendingFirst = _viewModel.ActiveTasks[0].Id;
        _viewModel.ApplyOverviewFilters(ascending);
        var ascendingFirst = _viewModel.ActiveTasks[0].Id;

        Assert.NotEqual(descendingFirst, ascendingFirst);
    }

    [Fact]
    public void ToggleFilterPanel_Twice_OpensAndClosesPanel()
    {
        _viewModel.ToggleFilterPanel();
        Assert.True(_viewModel.IsFilterPanelOpen);

        _viewModel.ToggleFilterPanel();

        Assert.False(_viewModel.IsFilterPanelOpen);
    }

    [Fact]
    public void CloseFilterPanel_OpenPanel_ClosesIt()
    {
        _viewModel.ToggleFilterPanel();

        _viewModel.CloseFilterPanel();

        Assert.False(_viewModel.IsFilterPanelOpen);
    }

    [Fact]
    public void OpenAddNoteModal_DayFilterSet_OpensEditorForFilteredDay()
    {
        var payload = new FilterModel { SelectedDate = TestTasks.Day };
        _viewModel.ApplyOverviewFilters(payload);

        _viewModel.OpenAddNoteModal();

        var input = _viewModel.TaskEditorInput;
        Assert.True(_viewModel.IsAddNoteModalOpen);
        Assert.NotNull(input);
        Assert.Null(input.Task);
        Assert.Equal(TestTasks.Day, input.ContextualTaskDate);
    }

    [Fact]
    public void CloseAddNoteModal_OpenEditor_ClosesIt()
    {
        _viewModel.OpenAddNoteModal();

        _viewModel.CloseAddNoteModal();

        Assert.False(_viewModel.IsAddNoteModalOpen);
        Assert.Null(_viewModel.TaskEditorInput);
    }

    [Fact]
    public async Task StartEditTask_InactiveTask_OpensEditorWithCopyOnTaskDay()
    {
        var task = TestTasks.Create("Bearbeiten");
        SetTasks([task], [], []);
        await _viewModel.InitializeAsync();

        _viewModel.StartEditTask(task.Id!.Value);

        var input = _viewModel.TaskEditorInput;
        Assert.NotNull(input);
        Assert.NotNull(input.Task);
        Assert.Equal(task.Id, input.Task.Id);
        Assert.NotSame(task, input.Task);
        Assert.Equal(TestTasks.Day, input.ContextualTaskDate);
    }

    [Fact]
    public async Task StartEditTask_ActiveTask_DoesNotOpenEditor()
    {
        var task = TestTasks.Create();
        SetTasks([task], [], []);
        _activeTaskService.IsTaskActive(task.Id!.Value).Returns(true);
        await _viewModel.InitializeAsync();

        _viewModel.StartEditTask(task.Id!.Value);

        Assert.False(_viewModel.IsAddNoteModalOpen);
    }

    [Fact]
    public void StartEditTask_UnknownTask_DoesNotOpenEditor()
    {
        var unknownId = Guid.NewGuid();

        _viewModel.StartEditTask(unknownId);

        Assert.False(_viewModel.IsAddNoteModalOpen);
    }

    [Fact]
    public async Task DeleteTaskAsync_InactiveTask_DeletesTask()
    {
        var task = TestTasks.Create();
        SetTasks([task], [], []);
        await _viewModel.InitializeAsync();

        await _viewModel.DeleteTaskAsync(task.Id!.Value);

        await _taskCache.Received(1).DeleteTaskAsync(task.Id!.Value);
    }

    [Fact]
    public async Task DeleteTaskAsync_ActiveTask_IsNotDeleted()
    {
        var task = TestTasks.Create();
        SetTasks([task], [], []);
        _activeTaskService.IsTaskActive(task.Id!.Value).Returns(true);
        await _viewModel.InitializeAsync();

        await _viewModel.DeleteTaskAsync(task.Id!.Value);

        await _taskCache.DidNotReceive().DeleteTaskAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteTaskAsync_UnknownTask_IsIgnored()
    {
        var unknownId = Guid.NewGuid();

        await _viewModel.DeleteTaskAsync(unknownId);

        await _taskCache.DidNotReceive().DeleteTaskAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteTaskAsync_DeletingFails_CapturesUiError()
    {
        var task = TestTasks.Create();
        SetTasks([task], [], []);
        await _viewModel.InitializeAsync();
        var failure = new InvalidOperationException("Fehler");
        _taskCache.DeleteTaskAsync(Arg.Any<Guid>()).ThrowsAsync(failure);

        await _viewModel.DeleteTaskAsync(task.Id!.Value);

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "TaskOverViewViewModel.DeleteTaskAsync" &&
                options.IsUiFailure));
    }

    [Fact]
    public async Task InitializeAsync_SeriesWithSeveralOpenTasks_ListsOnlyTheNextOne()
    {
        var start = CreateSeriesStart("Sport", TestTasks.Today);
        var second = CreateFollowingTask(start, TestTasks.Today.AddDays(14));
        var first = CreateFollowingTask(start, TestTasks.Today.AddDays(7));
        var single = TestTasks.Create("Einzeln", taskDate: TestTasks.Today.AddDays(3));
        SetTasks([second, start, first, single], [], []);

        await _viewModel.InitializeAsync();

        var listedIds = _viewModel.ActiveTasks.Select(task => task.Id).ToList();
        Assert.Equal(2, listedIds.Count);
        Assert.Contains(start.Id, listedIds);
        Assert.Contains(single.Id, listedIds);
    }

    [Fact]
    public async Task InitializeAsync_SeriesStartInThePast_ListsTheNextFollowingTaskAndKeepsTheStartOverdue()
    {
        var start = CreateSeriesStart("Sport", TestTasks.Today.AddDays(-2));
        var next = CreateFollowingTask(start, TestTasks.Today.AddDays(1));
        var later = CreateFollowingTask(start, TestTasks.Today.AddDays(8));
        SetTasks([start, later, next], [], []);

        await _viewModel.InitializeAsync();

        var listed = Assert.Single(_viewModel.ActiveTasks);
        Assert.Equal(next.Id, listed.Id);
        var overdue = Assert.Single(_viewModel.OverdueTasks);
        Assert.Equal(start.Id, overdue.Id);
    }

    [Fact]
    public async Task InitializeAsync_TwoSeries_ListsEachSeriesOnce()
    {
        var sport = CreateSeriesStart("Sport", TestTasks.Today);
        var reading = CreateSeriesStart("Lesen", TestTasks.Today.AddDays(1));
        var sportNext = CreateFollowingTask(sport, TestTasks.Today.AddDays(7));
        var readingNext = CreateFollowingTask(reading, TestTasks.Today.AddDays(8));
        SetTasks([sport, sportNext, reading, readingNext], [], []);

        await _viewModel.InitializeAsync();

        var listedIds = _viewModel.ActiveTasks.Select(task => task.Id).ToList();
        Assert.Equal(2, listedIds.Count);
        Assert.Contains(sport.Id, listedIds);
        Assert.Contains(reading.Id, listedIds);
    }

    [Fact]
    public async Task InitializeAsync_DoneTasksOfASeries_StayListedIndividually()
    {
        var start = CreateSeriesStart("Sport", TestTasks.Today);
        var doneOne = CreateFollowingTask(start, TestTasks.Today.AddDays(-7));
        doneOne.IsDone = true;
        var doneTwo = CreateFollowingTask(start, TestTasks.Today.AddDays(-14));
        doneTwo.IsDone = true;
        SetTasks([start], [], [doneOne, doneTwo]);

        await _viewModel.InitializeAsync();

        Assert.Equal(2, _viewModel.CompletedTasks.Count);
        Assert.Single(_viewModel.ActiveTasks);
    }

    [Fact]
    public async Task DeleteTaskAsync_ListedSeriesEntry_DeletesTheWholeSeries()
    {
        var start = CreateSeriesStart("Sport", TestTasks.Today);
        var next = CreateFollowingTask(start, TestTasks.Today.AddDays(7));
        SetTasks([start, next], [], []);
        await _viewModel.InitializeAsync();

        await _viewModel.DeleteTaskAsync(start.Id!.Value);

        await _taskSeriesService.Received(1).DeleteTaskAsync(start.Id!.Value, TaskSeriesScope.WholeSeries, null);
        await _taskCache.DidNotReceive().DeleteTaskAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteTaskAsync_ListedEntryIsAFollowingTask_DeletesTheWholeSeries()
    {
        var start = CreateSeriesStart("Sport", TestTasks.Today.AddDays(-2));
        var next = CreateFollowingTask(start, TestTasks.Today.AddDays(1));
        SetTasks([start, next], [], []);
        await _viewModel.InitializeAsync();

        await _viewModel.DeleteTaskAsync(next.Id!.Value);

        await _taskSeriesService.Received(1).DeleteTaskAsync(next.Id!.Value, TaskSeriesScope.WholeSeries, null);
    }

    [Fact]
    public async Task DeleteTaskAsync_ListedSeriesEntry_ProtectsTheActiveTask()
    {
        var start = CreateSeriesStart("Sport", TestTasks.Today);
        var next = CreateFollowingTask(start, TestTasks.Today.AddDays(7));
        var activeTaskId = next.Id;
        SetTasks([start, next], [], []);
        _activeTaskService.ActiveTaskId.Returns(activeTaskId);
        await _viewModel.InitializeAsync();

        await _viewModel.DeleteTaskAsync(start.Id!.Value);

        await _taskSeriesService.Received(1).DeleteTaskAsync(start.Id!.Value, TaskSeriesScope.WholeSeries, activeTaskId);
    }

    [Fact]
    public async Task DeleteTaskAsync_DoneSeriesTask_DeletesOnlyThatTask()
    {
        var start = CreateSeriesStart("Sport", TestTasks.Today);
        var done = CreateFollowingTask(start, TestTasks.Today.AddDays(-7));
        done.IsDone = true;
        SetTasks([start], [], [done]);
        await _viewModel.InitializeAsync();

        await _viewModel.DeleteTaskAsync(done.Id!.Value);

        await _taskSeriesService.Received(1).DeleteTaskAsync(done.Id!.Value, TaskSeriesScope.ThisTaskOnly, null);
        await _taskCache.DidNotReceive().DeleteTaskAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteTaskAsync_SeriesDeletionFails_CapturesUiError()
    {
        var start = CreateSeriesStart("Sport", TestTasks.Today);
        SetTasks([start], [], []);
        await _viewModel.InitializeAsync();
        var failure = new InvalidOperationException("Fehler");
        _taskSeriesService.DeleteTaskAsync(Arg.Any<Guid>(), Arg.Any<TaskSeriesScope>(), Arg.Any<Guid?>()).ThrowsAsync(failure);

        await _viewModel.DeleteTaskAsync(start.Id!.Value);

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "TaskOverViewViewModel.DeleteTaskAsync" &&
                options.IsUiFailure));
    }

    [Fact]
    public async Task StartEditTask_ListedSeriesEntry_SavesForTheWholeSeries()
    {
        var start = CreateSeriesStart("Sport", TestTasks.Today);
        var next = CreateFollowingTask(start, TestTasks.Today.AddDays(7));
        SetTasks([start, next], [], []);
        await _viewModel.InitializeAsync();
        _viewModel.StartEditTask(start.Id!.Value);
        var payload = CreateSavePayload(TaskExtension.Clone(start));

        await _viewModel.HandleTaskEditPopupCloseAsync(payload);

        await _taskSeriesService.Received(1).HandleSavedTaskAsync(Arg.Any<TaskDto>(), Arg.Any<TaskDto?>(), TaskSeriesScope.WholeSeries);
    }

    [Fact]
    public async Task StartEditTask_DoneSeriesTask_SavesForThisTaskOnly()
    {
        var start = CreateSeriesStart("Sport", TestTasks.Today);
        var done = CreateFollowingTask(start, TestTasks.Today.AddDays(-7));
        done.IsDone = true;
        SetTasks([start], [], [done]);
        await _viewModel.InitializeAsync();
        _viewModel.StartEditTask(done.Id!.Value);
        var payload = CreateSavePayload(TaskExtension.Clone(done));

        await _viewModel.HandleTaskEditPopupCloseAsync(payload);

        await _taskSeriesService.Received(1).HandleSavedTaskAsync(Arg.Any<TaskDto>(), Arg.Any<TaskDto?>(), TaskSeriesScope.ThisTaskOnly);
    }

    [Fact]
    public void IsTaskActive_NoId_ReturnsFalse()
    {
        var isActive = _viewModel.IsTaskActive(null);

        Assert.False(isActive);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsTaskActive_Id_DelegatesToActiveTaskService(bool serviceResult)
    {
        var taskId = Guid.NewGuid();
        _activeTaskService.IsTaskActive(taskId).Returns(serviceResult);

        var isActive = _viewModel.IsTaskActive(taskId);

        Assert.Equal(serviceResult, isActive);
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_EditorNotOpen_IgnoresPayload()
    {
        var task = TestTasks.Create("Neu");
        var payload = CreatePayload(TaskEditPopUpCloseAction.Save, task);

        await _viewModel.HandleTaskEditPopupCloseAsync(payload);

        await _taskCache.DidNotReceive().AddTaskAsync(Arg.Any<TaskDto>());
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_Cancel_ClosesEditor()
    {
        _viewModel.OpenAddNoteModal();
        var task = TestTasks.Create("Neu");
        var payload = CreatePayload(TaskEditPopUpCloseAction.Cancel, task);

        await _viewModel.HandleTaskEditPopupCloseAsync(payload);

        Assert.False(_viewModel.IsAddNoteModalOpen);
        await _taskCache.DidNotReceive().AddTaskAsync(Arg.Any<TaskDto>());
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_Save_AddsTaskAndClosesEditor()
    {
        _viewModel.OpenAddNoteModal();
        var task = TestTasks.Create("Neu");
        var payload = CreatePayload(TaskEditPopUpCloseAction.Save, task);

        await _viewModel.HandleTaskEditPopupCloseAsync(payload);

        await _taskCache.Received(1).AddTaskAsync(task);
        Assert.False(_viewModel.IsAddNoteModalOpen);
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_SaveFails_CapturesErrorAndKeepsEditorOpen()
    {
        _viewModel.OpenAddNoteModal();
        var task = TestTasks.Create("Neu");
        var payload = CreatePayload(TaskEditPopUpCloseAction.Save, task);
        var failure = new InvalidOperationException("Fehler");
        _taskCache.AddTaskAsync(Arg.Any<TaskDto>()).ThrowsAsync(failure);

        await _viewModel.HandleTaskEditPopupCloseAsync(payload);

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null && options.Source == "TaskOverViewViewModel.HandleTaskEditPopupCloseAsync"));
        Assert.True(_viewModel.IsAddNoteModalOpen);
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_CopyTemplate_OpensOneTimeCopyOnFilteredDay()
    {
        var template = TestTasks.Create("Vorlage", isTemplate: true);
        SetTasks([], [template], []);
        await _viewModel.InitializeAsync();
        var filter = new FilterModel { SelectedDate = TestTasks.Day };
        _viewModel.ApplyOverviewFilters(filter);
        _viewModel.OpenAddNoteModal();
        var payload = CreatePayload(TaskEditPopUpCloseAction.CopyTemplate, template, template.Id);

        await _viewModel.HandleTaskEditPopupCloseAsync(payload);

        var input = _viewModel.TaskEditorInput;
        Assert.NotNull(input);
        var copy = input.Task;
        Assert.NotNull(copy);
        Assert.Null(copy.Id);
        Assert.False(copy.IsTemplate);
        Assert.Equal(TestTasks.Day, copy.TaskDate);
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_EditTemplate_OpensEditorForTemplate()
    {
        var template = TestTasks.Create("Vorlage", isTemplate: true);
        SetTasks([], [template], []);
        await _viewModel.InitializeAsync();
        _viewModel.OpenAddNoteModal();
        var payload = CreatePayload(TaskEditPopUpCloseAction.EditTemplate, template, template.Id);

        await _viewModel.HandleTaskEditPopupCloseAsync(payload);

        var input = _viewModel.TaskEditorInput;
        Assert.NotNull(input);
        Assert.NotNull(input.Task);
        Assert.Equal(template.Id, input.Task.Id);
    }

    [Fact]
    public void NormalTasksChanged_NewTask_UpdatesOnlyActiveSection()
    {
        var payload = new FilterModel { SelectedDate = null };
        _viewModel.ApplyOverviewFilters(payload);
        var task = TestTasks.Create("Neu", taskDate: TestTasks.Today);
        SetTasks([task], [], []);
        var changed = TrackChanges();

        _taskCache.NormalTasksChanged += Raise.Event();

        var activeShown = Assert.Single(_viewModel.ActiveTasks);
        Assert.Equal(task.Id, activeShown.Id);
        Assert.Empty(_viewModel.CompletedTasks);
        Assert.Contains(nameof(TaskOverViewViewModel.ActiveTasks), changed);
    }

    [Fact]
    public void TemplatesChanged_NewTemplate_UpdatesTemplateSection()
    {
        var payload = new FilterModel { SelectedDate = null };
        _viewModel.ApplyOverviewFilters(payload);
        var template = TestTasks.Create("Vorlage", isTemplate: true);
        SetTasks([], [template], []);

        _taskCache.TemplatesChanged += Raise.Event();

        var templateShown = Assert.Single(_viewModel.Templates);
        Assert.Equal(template.Id, templateShown.Id);
    }

    [Fact]
    public void DoneTasksChanged_FinishedTask_UpdatesCompletedSection()
    {
        var payload = new FilterModel { SelectedDate = null };
        _viewModel.ApplyOverviewFilters(payload);
        var done = TestTasks.Create("Fertig", isDone: true);
        SetTasks([], [], [done]);

        _taskCache.DoneTasksChanged += Raise.Event();

        var completedShown = Assert.Single(_viewModel.CompletedTasks);
        Assert.Equal(done.Id, completedShown.Id);
    }

    [Fact]
    public void ScopeChanged_ReloadsAllSections()
    {
        var payload = new FilterModel { SelectedDate = null };
        _viewModel.ApplyOverviewFilters(payload);
        var task = TestTasks.Create("Privat", isWork: false, taskDate: TestTasks.Today);
        SetTasks([task], [], []);

        _scopeService.ScopeChanged += Raise.Event();

        var activeShown = Assert.Single(_viewModel.ActiveTasks);
        Assert.Equal(task.Id, activeShown.Id);
    }

    [Fact]
    public void ActiveTaskChanged_AnyTask_RaisesListChange()
    {
        var changed = TrackChanges();

        _activeTaskService.ActiveTaskChanged += Raise.Event();

        Assert.Contains(nameof(TaskOverViewViewModel.ActiveTasks), changed);
    }

    [Fact]
    public void Dispose_AfterwardsEvents_AreIgnored()
    {
        _viewModel.Dispose();
        _taskCache.ClearReceivedCalls();

        _scopeService.ScopeChanged += Raise.Event();
        _taskCache.NormalTasksChanged += Raise.Event();

        _taskCache.DidNotReceive().GetNormalTasks(Arg.Any<DateTime?>());
    }

    [Fact]
    public async Task DayChanged_TodayWasFiltered_FollowsToNewDayAndReloadsTasks()
    {
        _viewModel.ApplyOverviewFilters(new FilterModel { SelectedDate = TestTasks.Today });
        await _viewModel.InitializeAsync();
        var tomorrow = TestTasks.Today.AddDays(1);
        var tomorrowTask = TestTasks.Create("Morgen", taskDate: tomorrow);
        SetTasks([tomorrowTask], [], []);

        RaiseDayChanged(TestTasks.Today, tomorrow);

        Assert.Equal(tomorrow, _viewModel.Filter.SelectedDate);
        _taskCache.Received().GetNormalTasks(tomorrow);
        var activeShown = Assert.Single(_viewModel.ActiveTasks);
        Assert.Equal(tomorrowTask.Id, activeShown.Id);
    }

    [Fact]
    public void DayChanged_OtherDayWasFiltered_KeepsFilterDay()
    {
        var payload = new FilterModel { SelectedDate = TestTasks.Day };
        _viewModel.ApplyOverviewFilters(payload);

        RaiseDayChanged(TestTasks.Today, TestTasks.Today.AddDays(1));

        Assert.Equal(TestTasks.Day, _viewModel.Filter.SelectedDate);
    }

    [Fact]
    public void DayChanged_NoDayFilter_KeepsNoDayFilter()
    {
        var payload = new FilterModel { SelectedDate = null };
        _viewModel.ApplyOverviewFilters(payload);

        RaiseDayChanged(TestTasks.Today, TestTasks.Today.AddDays(1));

        Assert.Null(_viewModel.Filter.SelectedDate);
    }

    [Fact]
    public void DayChanged_AfterDispose_IsIgnored()
    {
        _viewModel.ApplyOverviewFilters(new FilterModel { SelectedDate = TestTasks.Today });
        _viewModel.Dispose();

        RaiseDayChanged(TestTasks.Today, TestTasks.Today.AddDays(1));

        Assert.Equal(TestTasks.Today, _viewModel.Filter.SelectedDate);
    }

    [Fact]
    public void OpenReschedulePopup_UnknownTask_DoesNotOpenPopup()
    {
        var unknownId = Guid.NewGuid();

        _viewModel.OpenReschedulePopup(unknownId);

        Assert.False(_viewModel.IsReschedulePopupOpen);
    }

    [Fact]
    public async Task ConfirmRescheduleAsync_TaskBecameActiveInTheMeantime_DoesNotUpdate()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Day);
        SetTasks([past], [], []);
        await _viewModel.InitializeAsync();
        _viewModel.OpenReschedulePopup(past.Id!.Value);
        _activeTaskService.IsTaskActive(past.Id!.Value).Returns(true);

        await _viewModel.ConfirmRescheduleAsync(TestTasks.Today);

        Assert.False(_viewModel.IsReschedulePopupOpen);
        await _taskCache.DidNotReceive().MoveTaskToDateAsync(Arg.Any<TaskDto>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task ConfirmRescheduleAsync_TaskDisappearedInTheMeantime_DoesNotUpdate()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Day);
        SetTasks([past], [], []);
        await _viewModel.InitializeAsync();
        _viewModel.OpenReschedulePopup(past.Id!.Value);
        SetTasks([], [], []);
        _taskCache.NormalTasksChanged += Raise.Event();

        await _viewModel.ConfirmRescheduleAsync(TestTasks.Today);

        await _taskCache.DidNotReceive().MoveTaskToDateAsync(Arg.Any<TaskDto>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task StartEditTask_TaskWithoutDateAndDayFilter_UsesTheFilterDay()
    {
        var task = TestTasks.Create("Ohne Datum", isTemplate: true);
        SetTasks([], [task], []);
        await _viewModel.InitializeAsync();
        _viewModel.ApplyOverviewFilters(new FilterModel { SelectedDate = TestTasks.Day });

        _viewModel.StartEditTask(task.Id!.Value);

        var input = _viewModel.TaskEditorInput;
        Assert.NotNull(input);
        Assert.Equal(TestTasks.Day, input.ContextualTaskDate);
    }

    [Fact]
    public async Task StartEditTask_TaskWithoutDateAndNoDayFilter_UsesToday()
    {
        var task = TestTasks.Create("Ohne Datum", isTemplate: true);
        SetTasks([], [task], []);
        await _viewModel.InitializeAsync();

        _viewModel.StartEditTask(task.Id!.Value);

        var input = _viewModel.TaskEditorInput;
        Assert.NotNull(input);
        Assert.Equal(TestTasks.Today, input.ContextualTaskDate);
    }

    [Fact]
    public void Dispose_WithoutUndoNotice_CompletesWithoutError()
    {
        var exception = Record.Exception(() => _viewModel.Dispose());

        Assert.Null(exception);
    }

    private void SetTasks(TaskDto[] normal, TaskDto[] template, TaskDto[] done)
    {
        IReadOnlyList<TaskDto> normalList = normal;
        IReadOnlyList<TaskDto> templateList = template;
        IReadOnlyList<TaskDto> doneList = done;
        _taskCache.GetNormalTasks(Arg.Any<DateTime?>()).Returns(normalList);
        _taskCache.GetTemplates(Arg.Any<DateTime?>()).Returns(templateList);
        _taskCache.GetDoneTasks(Arg.Any<DateTime?>()).Returns(doneList);
    }

    private void RaiseDayChanged(DateTime previousDay, DateTime currentDay)
    {
        var args = new DayChangedEventArgs(previousDay, currentDay);
        _dayChange.DayChanged += Raise.EventWith(args);
    }

    private List<string?> TrackChanges()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        return changed;
    }

    private static TaskDto CreateSeriesStart(string note, DateTime date)
    {
        var task = TestTasks.Create(note, taskDate: date);
        task.RepeatUnit = TaskRepeatUnit.Week;
        task.RepeatWeekdays = TaskWeekdays.All;
        return task;
    }

    private static TaskDto CreateFollowingTask(TaskDto seriesStart, DateTime date)
    {
        var task = TestTasks.Create(seriesStart.Note!, taskDate: date);
        task.SeriesId = seriesStart.Id;
        return task;
    }

    private static TaskEditPopUpClosePayload CreateSavePayload(TaskDto task)
    {
        return new TaskEditPopUpClosePayload
        {
            Action = TaskEditPopUpCloseAction.Save,
            Task = task,
            IsUpdateMode = true,
            HasChanges = true
        };
    }

    private static TaskEditPopUpClosePayload CreatePayload(
        TaskEditPopUpCloseAction action,
        TaskDto task,
        Guid? templateTaskId = null)
    {
        return new TaskEditPopUpClosePayload
        {
            Action = action,
            Task = task,
            TemplateTaskId = templateTaskId
        };
    }
}
