using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Components.SharedUI.FilterComponent;
using FokusApp.Components.TaskOverViewComponent;
using FokusApp.Core.Enums;
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
    private readonly FakeTimeProvider _clock = TestTasks.CreateClock();
    private readonly IDayChangeService _dayChange = Substitute.For<IDayChangeService>();
    private readonly TaskOverViewViewModel _viewModel;

    public TaskOverViewViewModelTests()
    {
        IReadOnlyList<TaskDto> empty = [];
        _taskCache.GetNormalTasks(Arg.Any<DateTime?>()).Returns(empty);
        _taskCache.GetRegularTasks(Arg.Any<DateTime?>()).Returns(empty);
        _taskCache.GetDoneTasks(Arg.Any<DateTime?>()).Returns(empty);
        _scopeService.IsWorkScope.Returns(true);
        _priorityFormatter.Format(Arg.Any<TaskPriority>()).Returns(call => call.Arg<TaskPriority>().ToString());
        var taskEditor = new TaskEditorCoordinator(_scopeService, _taskCache, _textProvider, _priorityFormatter);
        _viewModel = new TaskOverViewViewModel(
            _taskCache,
            _activeTaskService,
            _scopeService,
            _startupState,
            taskEditor,
            _errorCapture,
            _clock,
            _dayChange);
    }

    [Fact]
    public void Constructor_NoInteraction_FiltersOnToday()
    {
        Assert.Equal(TestTasks.Today, _viewModel.Filter.SelectedDate);
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
        _taskCache.Received().GetRegularTasks(Arg.Any<DateTime?>());
        _taskCache.Received().GetDoneTasks(Arg.Any<DateTime?>());
    }

    [Fact]
    public async Task ApplyOverviewFilters_NoDayFilter_ShowsTasksInThreeSections()
    {
        var active = TestTasks.Create("Aktiv");
        var template = TestTasks.Create("Vorlage", isRegular: true);
        var done = TestTasks.Create("Fertig", isDone: true);
        SetTasks([active], [template], [done]);
        await _viewModel.InitializeAsync();
        var payload = new FilterModel { SelectedDate = null };

        _viewModel.ApplyOverviewFilters(payload);

        var activeShown = Assert.Single(_viewModel.ActiveTasks);
        Assert.Equal(active.Id, activeShown.Id);

        var regularShown = Assert.Single(_viewModel.RegularTasks);
        Assert.Equal(template.Id, regularShown.Id);

        var completedShown = Assert.Single(_viewModel.CompletedTasks);
        Assert.Equal(done.Id, completedShown.Id);
    }

    [Fact]
    public async Task ApplyOverviewFilters_DayFilter_RestrictsTasksButNotRegularTemplates()
    {
        var onDay = TestTasks.Create("Am Tag");
        var otherDay = TestTasks.Create("Anderer Tag", taskDate: TestTasks.Day.AddDays(1));
        var template = TestTasks.Create("Vorlage", isRegular: true);
        SetTasks([onDay, otherDay], [template], []);
        await _viewModel.InitializeAsync();
        var payload = new FilterModel { SelectedDate = TestTasks.Day };

        _viewModel.ApplyOverviewFilters(payload);

        var activeShown = Assert.Single(_viewModel.ActiveTasks);
        Assert.Equal(onDay.Id, activeShown.Id);

        var regularShown = Assert.Single(_viewModel.RegularTasks);
        Assert.Equal(template.Id, regularShown.Id);
    }

    [Fact]
    public async Task ApplyOverviewFilters_DayChanged_ReloadsTasksForNewDay()
    {
        await _viewModel.InitializeAsync();
        _taskCache.ClearReceivedCalls();
        var payload = new FilterModel { SelectedDate = TestTasks.Day };

        _viewModel.ApplyOverviewFilters(payload);

        _taskCache.Received().GetNormalTasks(TestTasks.Day);
        _taskCache.Received().GetRegularTasks(TestTasks.Day);
        _taskCache.Received().GetDoneTasks(TestTasks.Day);
    }

    [Fact]
    public async Task ApplyOverviewFilters_SameDay_FiltersWithoutReloading()
    {
        var sport = TestTasks.Create("Sport");
        var reading = TestTasks.Create("Lesen");
        SetTasks([sport, reading], [], []);
        await _viewModel.InitializeAsync();
        var dayPayload = new FilterModel { SelectedDate = TestTasks.Day };
        _viewModel.ApplyOverviewFilters(dayPayload);
        _taskCache.ClearReceivedCalls();
        var searchPayload = new FilterModel
        {
            SelectedDate = TestTasks.Day,
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
        var low = TestTasks.Create("Niedrig", priority: TaskPriority.Wenig);
        var urgent = TestTasks.Create("Dringend", priority: TaskPriority.Dringend);
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
        var older = TestTasks.Create("Älter");
        older.CreationDate = TestTasks.Day.AddHours(1);
        var newer = TestTasks.Create("Neuer");
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
    public async Task HandleTaskEditPopupCloseAsync_CopyRegularTemplate_OpensOneTimeCopyOnFilteredDay()
    {
        var template = TestTasks.Create("Vorlage", isRegular: true);
        SetTasks([], [template], []);
        await _viewModel.InitializeAsync();
        var filter = new FilterModel { SelectedDate = TestTasks.Day };
        _viewModel.ApplyOverviewFilters(filter);
        _viewModel.OpenAddNoteModal();
        var payload = CreatePayload(TaskEditPopUpCloseAction.CopyRegularTemplate, template, template.Id);

        await _viewModel.HandleTaskEditPopupCloseAsync(payload);

        var input = _viewModel.TaskEditorInput;
        Assert.NotNull(input);
        var copy = input.Task;
        Assert.NotNull(copy);
        Assert.Null(copy.Id);
        Assert.False(copy.IsRegularTask);
        Assert.Equal(TestTasks.Day, copy.TaskDate);
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_EditRegularTemplate_OpensEditorForTemplate()
    {
        var template = TestTasks.Create("Vorlage", isRegular: true);
        SetTasks([], [template], []);
        await _viewModel.InitializeAsync();
        _viewModel.OpenAddNoteModal();
        var payload = CreatePayload(TaskEditPopUpCloseAction.EditRegularTemplate, template, template.Id);

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
        var task = TestTasks.Create("Neu");
        SetTasks([task], [], []);
        var changed = TrackChanges();

        _taskCache.NormalTasksChanged += Raise.Event();

        var activeShown = Assert.Single(_viewModel.ActiveTasks);
        Assert.Equal(task.Id, activeShown.Id);
        Assert.Empty(_viewModel.CompletedTasks);
        Assert.Contains(nameof(TaskOverViewViewModel.ActiveTasks), changed);
    }

    [Fact]
    public void RegularTasksChanged_NewTemplate_UpdatesRegularSection()
    {
        var payload = new FilterModel { SelectedDate = null };
        _viewModel.ApplyOverviewFilters(payload);
        var template = TestTasks.Create("Vorlage", isRegular: true);
        SetTasks([], [template], []);

        _taskCache.RegularTasksChanged += Raise.Event();

        var regularShown = Assert.Single(_viewModel.RegularTasks);
        Assert.Equal(template.Id, regularShown.Id);
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
        var task = TestTasks.Create("Privat", isWork: false);
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
        _viewModel.Dispose();

        RaiseDayChanged(TestTasks.Today, TestTasks.Today.AddDays(1));

        Assert.Equal(TestTasks.Today, _viewModel.Filter.SelectedDate);
    }

    private void SetTasks(TaskDto[] normal, TaskDto[] regular, TaskDto[] done)
    {
        IReadOnlyList<TaskDto> normalList = normal;
        IReadOnlyList<TaskDto> regularList = regular;
        IReadOnlyList<TaskDto> doneList = done;
        _taskCache.GetNormalTasks(Arg.Any<DateTime?>()).Returns(normalList);
        _taskCache.GetRegularTasks(Arg.Any<DateTime?>()).Returns(regularList);
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

    private static TaskEditPopUpClosePayload CreatePayload(
        TaskEditPopUpCloseAction action,
        TaskDto task,
        Guid? regularTemplateTaskId = null)
    {
        return new TaskEditPopUpClosePayload
        {
            Action = action,
            Task = task,
            RegularTemplateTaskId = regularTemplateTaskId
        };
    }
}
