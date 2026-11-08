using FocusAppTests.Fakes;
using FokusApp.Components.Calender;
using FokusApp.Components.YourDay;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Components.PopUps.TaskSeriesChoice;
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

public class YourDayViewModelTests
{
    private readonly ITaskCacheService _taskCache = Substitute.For<ITaskCacheService>();
    private readonly IActiveTaskService _activeTaskService = Substitute.For<IActiveTaskService>();
    private readonly IScopeService _scopeService = Substitute.For<IScopeService>();
    private readonly IAppStartupStateService _startupState = Substitute.For<IAppStartupStateService>();
    private readonly ITimerSessionController _timer = Substitute.For<ITimerSessionController>();
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly IUiTextProvider _textProvider = TestTexts.Create();
    private readonly ITaskPriorityFormatter _priorityFormatter = Substitute.For<ITaskPriorityFormatter>();
    private readonly ITaskSeriesService _taskSeriesService = Substitute.For<ITaskSeriesService>();
    private readonly FakeTimeProvider _clock = TestTasks.CreateClock();
    private readonly IDayChangeService _dayChange = Substitute.For<IDayChangeService>();
    private readonly FakeNavigationManager _navigation = new("http://localhost/");
    private readonly IRewardingSettingsService _rewardingSettings = TestSettings.Create();
    private readonly YourDayViewModel _viewModel;

    public YourDayViewModelTests()
    {
        IReadOnlyList<TaskDto> empty = [];
        _taskCache.GetNormalTasks(Arg.Any<DateTime?>()).Returns(empty);
        _taskCache.GetTemplates(Arg.Any<DateTime?>()).Returns(empty);
        _taskCache.GetDoneTasks(Arg.Any<DateTime?>()).Returns(empty);
        _scopeService.IsWorkScope.Returns(true);
        _priorityFormatter.Format(Arg.Any<TaskPriority>()).Returns(call => call.Arg<TaskPriority>().ToString());
        var taskEditor = new TaskEditorCoordinator(_scopeService, _taskCache, _textProvider, _priorityFormatter, _taskSeriesService, _clock);
        _viewModel = new YourDayViewModel(
            _taskCache,
            _activeTaskService,
            _scopeService,
            _startupState,
            _timer,
            taskEditor,
            _taskSeriesService,
            _errorCapture,
            _clock,
            _dayChange,
            _navigation,
            _rewardingSettings);
    }

    [Fact]
    public void Constructor_NoInteraction_ShowsMonthOfTodayWithTodaySelected()
    {
        Assert.Equal(new DateTime(2026, 3, 1), _viewModel.VisibleMonth);
        Assert.Equal(TestTasks.Today, _viewModel.SelectedDay);
    }

    [Fact]
    public void Constructor_NoInteraction_BuildsSixWeeksStartingOnMonday()
    {
        Assert.Equal(6, _viewModel.Weeks.Count);
        Assert.All(_viewModel.Weeks, week => Assert.Equal(7, week.Days.Count));
        Assert.Equal(new DateTime(2026, 2, 23), _viewModel.Weeks[0].Days[0].Date);
        Assert.Equal(new DateTime(2026, 4, 5), _viewModel.Weeks[5].Days[6].Date);
    }

    [Fact]
    public void Constructor_NoInteraction_MarksTodayAndSelectionAndMonthMembership()
    {
        var today = FindDay(TestTasks.Today);
        var februaryDay = FindDay(new DateTime(2026, 2, 23));
        var otherDay = FindDay(TestTasks.Day);

        Assert.True(today.IsToday);
        Assert.True(today.IsSelected);
        Assert.True(today.IsInCurrentMonth);
        Assert.False(februaryDay.IsInCurrentMonth);
        Assert.False(otherDay.IsToday);
        Assert.False(otherDay.IsSelected);
    }

    [Fact]
    public void Constructor_NoInteraction_NumbersWeeksByIsoCalendarWeek()
    {
        var weekNumbers = _viewModel.Weeks.Select(week => week.WeekNumber).ToArray();

        Assert.Equal([9, 10, 11, 12, 13, 14], weekNumbers);
    }

    [Fact]
    public void Constructor_FourTasksOnOneDay_ShowsThreeDotsAndMoreMarker()
    {
        SetNormalTasks(
            TestTasks.Create("A", priority: TaskPriority.Keine),
            TestTasks.Create("B", priority: TaskPriority.Hoch),
            TestTasks.Create("C", priority: TaskPriority.Mittel),
            TestTasks.Create("D", priority: TaskPriority.Wenig));
        var viewModel = CreateViewModel();

        var day = FindDay(viewModel, TestTasks.Day);

        Assert.Equal(3, day.Dots.Count);
        Assert.True(day.HasMoreTasks);
    }

    [Fact]
    public void Constructor_TasksWithDifferentPriorities_OrdersDotsByUrgency()
    {
        SetNormalTasks(
            TestTasks.Create("Wenig", priority: TaskPriority.Wenig),
            TestTasks.Create("Dringend", priority: TaskPriority.Dringend));
        var viewModel = CreateViewModel();

        var day = FindDay(viewModel, TestTasks.Day);

        var priorities = day.Dots.Select(dot => dot.Priority).ToArray();
        Assert.Equal([TaskPriority.Dringend, TaskPriority.Wenig], priorities);
        Assert.False(day.HasMoreTasks);
    }

    [Fact]
    public void Constructor_DoneTask_ShowsGreyDotAfterOpenTasks()
    {
        SetNormalTasks(TestTasks.Create("Offen", priority: TaskPriority.Keine));
        SetDoneTasks(TestTasks.Create("Fertig", isDone: true, priority: TaskPriority.Dringend));
        var viewModel = CreateViewModel();

        var day = FindDay(viewModel, TestTasks.Day);

        var doneStates = day.Dots.Select(dot => dot.IsDone).ToArray();
        Assert.Equal([false, true], doneStates);
    }

    [Fact]
    public void Constructor_Template_ShowsNoDot()
    {
        SetTemplates(TestTasks.Create("Vorlage", isTemplate: true));
        var viewModel = CreateViewModel();

        var hasDots = viewModel.Weeks.SelectMany(week => week.Days).Any(day => day.Dots.Count > 0);

        Assert.False(hasDots);
    }

    [Fact]
    public void Constructor_TaskWithoutDots_ShowsNoMoreMarker()
    {
        var day = FindDay(TestTasks.Day);

        Assert.Empty(day.Dots);
        Assert.False(day.HasMoreTasks);
    }

    [Fact]
    public void GoToNextMonth_SelectedDayStaysUnchanged()
    {
        _viewModel.GoToNextMonth();

        Assert.Equal(new DateTime(2026, 4, 1), _viewModel.VisibleMonth);
        Assert.Equal(TestTasks.Today, _viewModel.SelectedDay);
        Assert.Equal(new DateTime(2026, 3, 30), _viewModel.Weeks[0].Days[0].Date);
    }

    [Fact]
    public void Constructor_NoInteraction_CalendarIsCollapsed()
    {
        Assert.True(_viewModel.IsCalendarCollapsed);
    }

    [Fact]
    public void ToggleCalendarCollapsed_Twice_ExpandsAndCollapsesAgainKeepingSelectedDay()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        _viewModel.ToggleCalendarCollapsed();

        Assert.False(_viewModel.IsCalendarCollapsed);
        Assert.Contains(nameof(IYourDayViewModel.IsCalendarCollapsed), changed);
        Assert.Equal(TestTasks.Today, _viewModel.SelectedDay);

        _viewModel.ToggleCalendarCollapsed();

        Assert.True(_viewModel.IsCalendarCollapsed);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, -1)]
    public void MoveSelectedDay_Direction_ShiftsSelectedDayByOne(bool forwards, int expectedOffset)
    {
        _viewModel.MoveSelectedDay(forwards);

        Assert.Equal(TestTasks.Today.AddDays(expectedOffset), _viewModel.SelectedDay);
    }

    [Fact]
    public void MoveSelectedDay_CollapsedAcrossWeek_ShowsWeekOfNewDay()
    {
        _viewModel.SelectDay(new DateTime(2026, 3, 29));

        _viewModel.MoveSelectedDay(forwards: true);

        var selectedWeeks = _viewModel.Weeks.Where(week => week.Days.Any(day => day.IsSelected)).ToList();
        Assert.Equal(new DateTime(2026, 3, 30), _viewModel.SelectedDay);
        var selectedWeek = Assert.Single(selectedWeeks);
        Assert.Contains(selectedWeek.Days, day => day.Date == new DateTime(2026, 3, 30) && day.IsSelected);
    }

    [Fact]
    public void MoveSelectedDay_CollapsedAcrossMonth_FollowsIntoNextMonth()
    {
        _viewModel.SelectDay(new DateTime(2026, 3, 31));

        _viewModel.MoveSelectedDay(forwards: true);

        Assert.Equal(new DateTime(2026, 4, 1), _viewModel.SelectedDay);
        Assert.Equal(new DateTime(2026, 4, 1), _viewModel.VisibleMonth);
    }

    [Fact]
    public void GoToPreviousMonth_ShowsPreviousMonth()
    {
        _viewModel.GoToPreviousMonth();

        Assert.Equal(new DateTime(2026, 2, 1), _viewModel.VisibleMonth);
        Assert.Equal(TestTasks.Today, _viewModel.SelectedDay);
    }

    [Fact]
    public void SelectDay_DayOfOtherMonth_SwitchesToThatMonth()
    {
        _viewModel.SelectDay(new DateTime(2026, 4, 2, 17, 0, 0));

        Assert.Equal(new DateTime(2026, 4, 1), _viewModel.VisibleMonth);
        Assert.Equal(new DateTime(2026, 4, 2), _viewModel.SelectedDay);
    }

    [Fact]
    public void SelectDay_DayOfCurrentMonth_KeepsMonthAndMarksDay()
    {
        _viewModel.SelectDay(TestTasks.Day);

        Assert.Equal(new DateTime(2026, 3, 1), _viewModel.VisibleMonth);
        Assert.True(FindDay(TestTasks.Day).IsSelected);
        Assert.False(FindDay(TestTasks.Today).IsSelected);
    }

    [Fact]
    public void SelectDay_TasksOfDay_AreSplitIntoForYouAndToDoWithDoneLast()
    {
        var forYou = TestTasks.Create("Für mich", isPositive: true);
        var forYouDone = TestTasks.Create("Fertig", isPositive: true, isDone: true);
        var toDo = TestTasks.Create("Erledigen", isPositive: false);
        var otherDay = TestTasks.Create("Anderer Tag", isPositive: false, taskDate: TestTasks.Day.AddDays(1));
        SetNormalTasks(forYou, toDo, otherDay);
        SetDoneTasks(forYouDone);
        var viewModel = CreateViewModel();

        viewModel.SelectDay(TestTasks.Day);

        var forYouTaskIds = viewModel.ForYouTasks.Select(task => task.Id).ToArray();
        Assert.Equal([forYou.Id, forYouDone.Id], forYouTaskIds);
        var shownToDo = Assert.Single(viewModel.ToDoTasks);
        Assert.Equal(toDo.Id, shownToDo.Id);
    }

    [Fact]
    public void GoToToday_AfterBrowsing_ReturnsToCurrentMonthAndSelectsToday()
    {
        _viewModel.SelectDay(new DateTime(2026, 6, 20));

        _viewModel.GoToToday();

        Assert.Equal(new DateTime(2026, 3, 1), _viewModel.VisibleMonth);
        Assert.Equal(TestTasks.Today, _viewModel.SelectedDay);
    }

    [Fact]
    public void OpenTaskEditorForNewTask_Day_SelectsDayAndOpensEditorWithThatDate()
    {
        var day = new DateTime(2026, 3, 20);

        _viewModel.OpenTaskEditorForNewTask(day);

        Assert.Equal(day, _viewModel.SelectedDay);
        Assert.True(_viewModel.IsTaskEditorOpen);
        Assert.NotNull(_viewModel.TaskEditorInput);
        Assert.Equal(day, _viewModel.TaskEditorInput.ContextualTaskDate);
        Assert.Null(_viewModel.TaskEditorInput.Task);
        Assert.False(_viewModel.TaskEditorInput.IsTaskDateLocked);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OpenTaskEditorForNewTask_Positivity_OpensEditorForSelectedDay(bool isPositive)
    {
        _viewModel.SelectDay(TestTasks.Day);

        _viewModel.OpenTaskEditorForNewTask(isPositive);

        var input = _viewModel.TaskEditorInput;
        Assert.True(_viewModel.IsTaskEditorOpen);
        Assert.NotNull(input);
        Assert.Null(input.Task);
        Assert.Equal(isPositive, input.InitialIsPositive);
        Assert.Equal(TestTasks.Day, input.ContextualTaskDate);
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_Cancel_ClosesEditor()
    {
        _viewModel.OpenTaskEditorForNewTask(TestTasks.Day);
        var payload = CreateCancelPayload();

        await _viewModel.HandleTaskEditPopupCloseAsync(payload);

        Assert.False(_viewModel.IsTaskEditorOpen);
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_SaveNewTemplate_AddsTemplateAndTaskForToday()
    {
        _viewModel.OpenTaskEditorForNewTask(TestTasks.Day);
        var template = TestTasks.Create("Sport", isTemplate: true);
        var payload = new TaskEditPopUpClosePayload
        {
            Action = TaskEditPopUpCloseAction.Save,
            Task = template,
            IsUpdateMode = false,
            HasChanges = true
        };

        await _viewModel.HandleTaskEditPopupCloseAsync(payload);

        await _taskCache.Received(1).AddTaskAsync(template);
        await _taskCache.Received(1).AddTaskAsync(Arg.Is<TaskDto>(task =>
            !task.IsTemplate && task.Note == "Sport" && task.TaskDate == TestTasks.Today));
        Assert.False(_viewModel.IsTaskEditorOpen);
    }

    [Fact]
    public async Task InitializeAsync_StartupNotReady_WaitsBeforeLoading()
    {
        await _viewModel.InitializeAsync();

        await _startupState.Received(1).WaitForReadyAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void TasksChanged_NewTaskInCache_UpdatesDotsAndNotifiesWeeks()
    {
        var changedProperties = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);
        SetNormalTasks(TestTasks.Create("Neu", priority: TaskPriority.Hoch));

        _taskCache.NormalTasksChanged += Raise.Event();

        Assert.Single(FindDay(TestTasks.Day).Dots);
        Assert.Contains(nameof(IYourDayViewModel.Weeks), changedProperties);
    }

    [Fact]
    public void ScopeChanged_OtherScopeTasks_RefreshesDots()
    {
        SetNormalTasks(TestTasks.Create("Privat", isWork: false));
        _scopeService.IsWorkScope.Returns(false);

        _scopeService.ScopeChanged += Raise.Event();

        Assert.Single(FindDay(TestTasks.Day).Dots);
    }

    [Fact]
    public void DayChanged_SelectedDayWasYesterday_MovesSelectionToNewToday()
    {
        var tomorrow = TestTasks.Today.AddDays(1);

        _dayChange.DayChanged += Raise.EventWith(new DayChangedEventArgs(TestTasks.Today, tomorrow));

        Assert.Equal(tomorrow, _viewModel.SelectedDay);
        Assert.True(FindDay(tomorrow).IsSelected);
    }

    [Fact]
    public void DayChanged_OtherDaySelected_KeepsSelection()
    {
        _viewModel.SelectDay(TestTasks.Day);
        var tomorrow = TestTasks.Today.AddDays(1);
        _clock.Advance(TimeSpan.FromDays(1));

        _dayChange.DayChanged += Raise.EventWith(new DayChangedEventArgs(TestTasks.Today, tomorrow));

        Assert.Equal(TestTasks.Day, _viewModel.SelectedDay);
        Assert.True(FindDay(tomorrow).IsToday);
    }

    [Fact]
    public async Task MarkSelectedTaskAsDoneAsync_OpenTask_FinishesTaskAndClosesPopup()
    {
        var task = TestTasks.Create("Offen");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);

        await viewModel.MarkSelectedTaskAsDoneAsync();

        await _taskCache.Received(1).FinishTaskAsync(Arg.Is<TaskDto>(finished => finished.Id == task.Id));
        Assert.False(viewModel.IsTaskActionPopupOpen);
    }

    [Fact]
    public async Task DeleteSelectedTaskAsync_InactiveTask_DeletesTask()
    {
        var task = TestTasks.Create("Weg damit");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);

        await viewModel.DeleteSelectedTaskAsync();

        await _taskCache.Received(1).DeleteTaskAsync(task.Id!.Value);
    }

    [Fact]
    public async Task DeleteSelectedTaskAsync_ActiveTask_DoesNotDelete()
    {
        var task = TestTasks.Create("Aktiv");
        SetNormalTasks(task);
        _activeTaskService.IsTaskActive(task.Id!.Value).Returns(true);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);

        await viewModel.DeleteSelectedTaskAsync();

        await _taskCache.DidNotReceive().DeleteTaskAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteSelectedTaskAsync_SeriesTask_AsksBeforeDeletingAnything()
    {
        var task = CreateSeriesTask("Sport");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);

        await viewModel.DeleteSelectedTaskAsync();

        Assert.True(viewModel.IsSeriesChoiceOpen);
        Assert.Equal(TaskSeriesAction.Delete, viewModel.PendingSeriesAction);
        Assert.False(viewModel.IsTaskActionPopupOpen);
        await _taskCache.DidNotReceive().DeleteTaskAsync(Arg.Any<Guid>());
        await _taskSeriesService.DidNotReceive().DeleteTaskAsync(Arg.Any<Guid>(), Arg.Any<TaskSeriesScope>(), Arg.Any<Guid?>());
    }

    [Fact]
    public async Task DeleteSelectedTaskAsync_SeriesStart_AsksBeforeDeletingAnything()
    {
        var task = CreateSeriesStartTask("Sport");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);

        await viewModel.DeleteSelectedTaskAsync();

        Assert.True(viewModel.IsSeriesChoiceOpen);
        await _taskCache.DidNotReceive().DeleteTaskAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteSelectedTaskAsync_PlainTask_DoesNotAskAboutTheSeries()
    {
        var task = TestTasks.Create("Einzeln");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);

        await viewModel.DeleteSelectedTaskAsync();

        Assert.False(viewModel.IsSeriesChoiceOpen);
    }

    [Theory]
    [InlineData(TaskSeriesScope.ThisTaskOnly)]
    [InlineData(TaskSeriesScope.WholeSeries)]
    public async Task ChooseSeriesScopeAsync_DeleteChosen_DeletesWithTheChosenScope(TaskSeriesScope scope)
    {
        var task = CreateSeriesTask("Sport");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);
        await viewModel.DeleteSelectedTaskAsync();

        await viewModel.ChooseSeriesScopeAsync(scope);

        await _taskSeriesService.Received(1).DeleteTaskAsync(task.Id!.Value, scope, Arg.Any<Guid?>());
        await _taskCache.DidNotReceive().DeleteTaskAsync(Arg.Any<Guid>());
        Assert.False(viewModel.IsSeriesChoiceOpen);
    }

    [Fact]
    public async Task ChooseSeriesScopeAsync_DeleteWholeSeries_ProtectsTheActiveTask()
    {
        var task = CreateSeriesTask("Sport");
        var activeTaskId = Guid.NewGuid();
        SetNormalTasks(task);
        _activeTaskService.ActiveTaskId.Returns(activeTaskId);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);
        await viewModel.DeleteSelectedTaskAsync();

        await viewModel.ChooseSeriesScopeAsync(TaskSeriesScope.WholeSeries);

        await _taskSeriesService.Received(1).DeleteTaskAsync(task.Id!.Value, TaskSeriesScope.WholeSeries, activeTaskId);
    }

    [Fact]
    public async Task ChooseSeriesScopeAsync_DeletingFails_CapturesUiError()
    {
        var task = CreateSeriesTask("Sport");
        SetNormalTasks(task);
        var failure = new InvalidOperationException("kaputt");
        _taskSeriesService.DeleteTaskAsync(Arg.Any<Guid>(), Arg.Any<TaskSeriesScope>(), Arg.Any<Guid?>()).ThrowsAsync(failure);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);
        await viewModel.DeleteSelectedTaskAsync();

        await viewModel.ChooseSeriesScopeAsync(TaskSeriesScope.WholeSeries);

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "YourDayViewModel.ChooseSeriesScopeAsync" &&
                options.IsUiFailure));
    }

    [Fact]
    public async Task CancelSeriesChoice_DeleteAsked_DeletesNothing()
    {
        var task = CreateSeriesTask("Sport");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);
        await viewModel.DeleteSelectedTaskAsync();

        viewModel.CancelSeriesChoice();

        Assert.False(viewModel.IsSeriesChoiceOpen);
        await _taskCache.DidNotReceive().DeleteTaskAsync(Arg.Any<Guid>());
        await _taskSeriesService.DidNotReceive().DeleteTaskAsync(Arg.Any<Guid>(), Arg.Any<TaskSeriesScope>(), Arg.Any<Guid?>());
    }

    [Fact]
    public async Task ChooseSeriesScopeAsync_NothingAsked_DoesNothing()
    {
        await _viewModel.ChooseSeriesScopeAsync(TaskSeriesScope.WholeSeries);

        await _taskSeriesService.DidNotReceive().DeleteTaskAsync(Arg.Any<Guid>(), Arg.Any<TaskSeriesScope>(), Arg.Any<Guid?>());
        Assert.False(_viewModel.IsTaskEditorOpen);
    }

    [Fact]
    public void StartEditSelectedTask_SeriesTask_AsksBeforeOpeningTheEditor()
    {
        var task = CreateSeriesTask("Sport");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);

        viewModel.StartEditSelectedTask();

        Assert.True(viewModel.IsSeriesChoiceOpen);
        Assert.Equal(TaskSeriesAction.Edit, viewModel.PendingSeriesAction);
        Assert.False(viewModel.IsTaskEditorOpen);
        Assert.False(viewModel.IsTaskActionPopupOpen);
    }

    [Fact]
    public void StartEditSelectedTask_ActiveSeriesTask_DoesNotAsk()
    {
        var task = CreateSeriesTask("Sport");
        SetNormalTasks(task);
        _activeTaskService.IsTaskActive(task.Id!.Value).Returns(true);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);

        viewModel.StartEditSelectedTask();

        Assert.False(viewModel.IsSeriesChoiceOpen);
        Assert.False(viewModel.IsTaskEditorOpen);
    }

    [Fact]
    public async Task ChooseSeriesScopeAsync_EditChosen_OpensTheEditorForTheTask()
    {
        var task = CreateSeriesTask("Sport");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);
        viewModel.StartEditSelectedTask();

        await viewModel.ChooseSeriesScopeAsync(TaskSeriesScope.WholeSeries);

        Assert.True(viewModel.IsTaskEditorOpen);
        Assert.Equal(task.Id, viewModel.TaskEditorInput!.Task!.Id);
        Assert.False(viewModel.IsSeriesChoiceOpen);
    }

    [Theory]
    [InlineData(TaskSeriesScope.ThisTaskOnly)]
    [InlineData(TaskSeriesScope.WholeSeries)]
    public async Task HandleTaskEditPopupCloseAsync_SeriesEditSaved_PassesTheChosenScopeOn(TaskSeriesScope scope)
    {
        var task = CreateSeriesTask("Sport");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);
        viewModel.StartEditSelectedTask();
        await viewModel.ChooseSeriesScopeAsync(scope);
        var payload = CreateSavePayload(TaskExtension.Clone(task));

        await viewModel.HandleTaskEditPopupCloseAsync(payload);

        await _taskSeriesService.Received(1).HandleSavedTaskAsync(Arg.Any<TaskDto>(), Arg.Any<TaskDto?>(), scope);
    }

    [Fact]
    public void StartEditSelectedTask_InactiveTask_OpensEditorForTask()
    {
        var task = TestTasks.Create("Bearbeiten", taskDate: TestTasks.Day);
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);

        viewModel.StartEditSelectedTask();

        Assert.True(viewModel.IsTaskEditorOpen);
        Assert.Equal(task.Id, viewModel.TaskEditorInput!.Task!.Id);
        Assert.False(viewModel.IsTaskActionPopupOpen);
    }

    [Fact]
    public async Task ExecutePrimarySelectedTaskActionAsync_InactiveTask_SetsActiveTask()
    {
        var task = TestTasks.Create("Starten");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);

        var shouldNavigate = await viewModel.ExecutePrimarySelectedTaskActionAsync();

        Assert.True(shouldNavigate);
        _activeTaskService.Received(1).SetActiveTask(task.Id);
    }

    [Fact]
    public void RecentlyCompletedTaskId_MicroFeedbackOff_StaysNull()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { MicroFeedbackEnabled = false });

        _taskCache.TaskCompleted += Raise.EventWith(new TaskCompletedEventArgs(Guid.NewGuid(), isPositive: true));

        Assert.Null(_viewModel.RecentlyCompletedTaskId);
    }

    [Fact]
    public void RecentlyCompletedTaskId_NoCompletion_IsNull()
    {
        Assert.Null(_viewModel.RecentlyCompletedTaskId);
    }

    [Fact]
    public void RecentlyCompletedTaskId_TaskCompleted_IsSetOnceAndNotifies()
    {
        var taskId = Guid.NewGuid();
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        _taskCache.TaskCompleted += Raise.EventWith(new TaskCompletedEventArgs(taskId, isPositive: true));

        Assert.Equal(taskId, _viewModel.RecentlyCompletedTaskId);
        Assert.Contains(nameof(IYourDayViewModel.RecentlyCompletedTaskId), changed);
    }

    [Fact]
    public void RecentlyCompletedTaskId_DayChanged_IsClearedSoAnimationDoesNotRepeat()
    {
        _taskCache.TaskCompleted += Raise.EventWith(new TaskCompletedEventArgs(Guid.NewGuid(), isPositive: false));

        _viewModel.SelectDay(TestTasks.Today.AddDays(1));

        Assert.Null(_viewModel.RecentlyCompletedTaskId);
    }

    [Fact]
    public void RecentlyCompletedTaskId_GoToToday_IsCleared()
    {
        _taskCache.TaskCompleted += Raise.EventWith(new TaskCompletedEventArgs(Guid.NewGuid(), isPositive: false));

        _viewModel.GoToToday();

        Assert.Null(_viewModel.RecentlyCompletedTaskId);
    }

    [Fact]
    public void RecentlyCompletedTaskId_TwoCompletions_KeepsOnlyTheLatest()
    {
        var latest = Guid.NewGuid();
        _taskCache.TaskCompleted += Raise.EventWith(new TaskCompletedEventArgs(Guid.NewGuid(), isPositive: false));
        _taskCache.TaskCompleted += Raise.EventWith(new TaskCompletedEventArgs(latest, isPositive: true));

        Assert.Equal(latest, _viewModel.RecentlyCompletedTaskId);
    }

    [Fact]
    public void PastTaskCount_OpenTasksBeforeToday_CountsOnlyThose()
    {
        SetNormalTasks(
            TestTasks.Create("Gestern", taskDate: TestTasks.Today.AddDays(-1)),
            TestTasks.Create("Vor Wochen", taskDate: TestTasks.Today.AddDays(-20)),
            TestTasks.Create("Heute", taskDate: TestTasks.Today),
            TestTasks.Create("Morgen", taskDate: TestTasks.Today.AddDays(1)));
        var viewModel = CreateViewModel();

        Assert.Equal(2, viewModel.PastTaskCount);
    }

    [Fact]
    public void PastTaskCount_NoPastTasks_IsZero()
    {
        SetNormalTasks(TestTasks.Create("Heute", taskDate: TestTasks.Today));
        var viewModel = CreateViewModel();

        Assert.Equal(0, viewModel.PastTaskCount);
    }

    [Fact]
    public void OpenPastTasks_Always_NavigatesToTaskOverviewWithOnlyPastSectionOpen()
    {
        _viewModel.OpenPastTasks();

        Assert.Equal(["/aufgaben?bereich=vergangenheit"], _navigation.NavigatedTo);
    }

    [Fact]
    public void Dispose_AfterDispose_IgnoresAllSubscribedEvents()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        var tomorrow = TestTasks.Today.AddDays(1);
        var completed = new TaskCompletedEventArgs(Guid.NewGuid(), isPositive: true);
        var dayChanged = new DayChangedEventArgs(TestTasks.Today, tomorrow);

        _viewModel.Dispose();
        _taskCache.NormalTasksChanged += Raise.Event();
        _taskCache.TemplatesChanged += Raise.Event();
        _taskCache.DoneTasksChanged += Raise.Event();
        _taskCache.TaskCompleted += Raise.EventWith(completed);
        _scopeService.ScopeChanged += Raise.Event();
        _activeTaskService.ActiveTaskChanged += Raise.Event();
        _dayChange.DayChanged += Raise.EventWith(dayChanged);

        Assert.Empty(changed);
    }

    [Fact]
    public void ActiveTaskChanged_PopupOpenForTask_NotifiesActionState()
    {
        var task = TestTasks.Create("Aktiv");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        _activeTaskService.IsTaskActive(task.Id.Value).Returns(true);

        _activeTaskService.ActiveTaskChanged += Raise.Event();

        Assert.Contains(nameof(IYourDayViewModel.IsSelectedActionTaskActive), changed);
        Assert.Contains(nameof(IYourDayViewModel.CanDeleteSelectedTask), changed);
        Assert.True(viewModel.IsSelectedActionTaskActive);
        Assert.False(viewModel.CanDeleteSelectedTask);
    }

    [Fact]
    public void ActionState_NoTaskSelected_AllowsNothing()
    {
        Assert.False(_viewModel.IsTaskActionPopupOpen);
        Assert.False(_viewModel.CanStartSelectedTask);
        Assert.False(_viewModel.CanCompleteSelectedTask);
        Assert.False(_viewModel.CanDeleteSelectedTask);
        Assert.False(_viewModel.IsSelectedActionTaskActive);
    }

    [Fact]
    public void ActionState_OpenInactiveTask_AllowsStartCompleteAndDelete()
    {
        var task = TestTasks.Create("Offen");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);

        viewModel.OpenTaskActionPopup(task.Id!.Value);

        Assert.True(viewModel.IsTaskActionPopupOpen);
        Assert.True(viewModel.CanStartSelectedTask);
        Assert.True(viewModel.CanCompleteSelectedTask);
        Assert.True(viewModel.CanDeleteSelectedTask);
        Assert.False(viewModel.IsSelectedActionTaskActive);
    }

    [Fact]
    public void ActionState_DoneTask_AllowsOnlyDelete()
    {
        var task = TestTasks.Create("Fertig", isDone: true);
        SetDoneTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);

        viewModel.OpenTaskActionPopup(task.Id!.Value);

        Assert.False(viewModel.CanStartSelectedTask);
        Assert.False(viewModel.CanCompleteSelectedTask);
        Assert.True(viewModel.CanDeleteSelectedTask);
    }

    [Fact]
    public void ActionState_ActiveTask_IsActiveAndCannotBeDeleted()
    {
        var task = TestTasks.Create("Aktiv");
        SetNormalTasks(task);
        _activeTaskService.IsTaskActive(task.Id!.Value).Returns(true);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);

        viewModel.OpenTaskActionPopup(task.Id.Value);

        Assert.True(viewModel.IsSelectedActionTaskActive);
        Assert.False(viewModel.CanDeleteSelectedTask);
        Assert.True(viewModel.CanStartSelectedTask);
    }

    [Fact]
    public void ActionState_TaskNotVisibleOnSelectedDay_AllowsNothing()
    {
        var unknownId = Guid.NewGuid();

        _viewModel.OpenTaskActionPopup(unknownId);

        Assert.True(_viewModel.IsTaskActionPopupOpen);
        Assert.False(_viewModel.CanStartSelectedTask);
        Assert.False(_viewModel.CanCompleteSelectedTask);
        Assert.False(_viewModel.CanDeleteSelectedTask);
        Assert.False(_viewModel.IsSelectedActionTaskActive);
    }

    [Fact]
    public void ActionState_TemplateOnly_IsNotActionable()
    {
        var template = TestTasks.Create("Vorlage", isTemplate: true);
        SetTemplates(template);
        var viewModel = CreateViewModel();

        viewModel.OpenTaskActionPopup(template.Id!.Value);

        Assert.False(viewModel.CanStartSelectedTask);
        Assert.False(viewModel.CanDeleteSelectedTask);
    }

    [Fact]
    public void CloseTaskActionPopup_PopupOpen_ClosesAndNotifies()
    {
        var task = TestTasks.Create("Offen");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        viewModel.CloseTaskActionPopup();

        Assert.False(viewModel.IsTaskActionPopupOpen);
        Assert.False(viewModel.CanStartSelectedTask);
        Assert.Contains(nameof(IYourDayViewModel.IsTaskActionPopupOpen), changed);
    }

    [Fact]
    public void TasksChanged_SelectedTaskRemovedFromCache_ClosesActionPopup()
    {
        var task = TestTasks.Create("Weg");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);
        SetNormalTasks();

        _taskCache.NormalTasksChanged += Raise.Event();

        Assert.False(viewModel.IsTaskActionPopupOpen);
    }

    [Fact]
    public void TasksChanged_SelectedTaskStillInCache_KeepsActionPopupOpen()
    {
        var task = TestTasks.Create("Bleibt");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);

        _taskCache.NormalTasksChanged += Raise.Event();

        Assert.True(viewModel.IsTaskActionPopupOpen);
    }

    [Fact]
    public void ScopeChanged_ActionPopupOpen_ClosesPopup()
    {
        var task = TestTasks.Create("Offen");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);

        _scopeService.ScopeChanged += Raise.Event();

        Assert.False(viewModel.IsTaskActionPopupOpen);
    }

    [Fact]
    public void DayChanged_ActionPopupOpen_ClosesPopup()
    {
        var task = TestTasks.Create("Offen");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);
        var tomorrow = TestTasks.Today.AddDays(1);

        _dayChange.DayChanged += Raise.EventWith(new DayChangedEventArgs(TestTasks.Today, tomorrow));

        Assert.False(viewModel.IsTaskActionPopupOpen);
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_CopyTemplate_KeepsEditorOpenWithOneTimeCopy()
    {
        var template = TestTasks.Create("Sport", isTemplate: true);
        SetTemplates(template);
        var viewModel = CreateViewModel();
        viewModel.OpenTaskEditorForNewTask(TestTasks.Day);
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        var payload = new TaskEditPopUpClosePayload
        {
            Action = TaskEditPopUpCloseAction.CopyTemplate,
            TemplateTaskId = template.Id
        };

        await viewModel.HandleTaskEditPopupCloseAsync(payload);

        Assert.True(viewModel.IsTaskEditorOpen);
        var copy = viewModel.TaskEditorInput!.Task!;
        Assert.Null(copy.Id);
        Assert.False(copy.IsTemplate);
        Assert.Equal("Sport", copy.Note);
        Assert.Contains(nameof(IYourDayViewModel.TaskEditorInput), changed);
        Assert.Contains(nameof(IYourDayViewModel.IsTaskEditorOpen), changed);
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_EditTemplate_OpensEditorForTheTemplate()
    {
        var template = TestTasks.Create("Sport", isTemplate: true);
        SetTemplates(template);
        var viewModel = CreateViewModel();
        viewModel.OpenTaskEditorForNewTask(TestTasks.Day);
        var payload = new TaskEditPopUpClosePayload
        {
            Action = TaskEditPopUpCloseAction.EditTemplate,
            TemplateTaskId = template.Id
        };

        await viewModel.HandleTaskEditPopupCloseAsync(payload);

        Assert.True(viewModel.IsTaskEditorOpen);
        Assert.Equal(template.Id, viewModel.TaskEditorInput!.Task!.Id);
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_EditUnknownTemplate_KeepsTheOpenEditor()
    {
        var viewModel = CreateViewModel();
        viewModel.OpenTaskEditorForNewTask(TestTasks.Day);
        var originalInput = viewModel.TaskEditorInput;
        var payload = new TaskEditPopUpClosePayload
        {
            Action = TaskEditPopUpCloseAction.EditTemplate,
            TemplateTaskId = Guid.NewGuid()
        };

        await viewModel.HandleTaskEditPopupCloseAsync(payload);

        Assert.True(viewModel.IsTaskEditorOpen);
        Assert.Same(originalInput, viewModel.TaskEditorInput);
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_SaveFails_CapturesUiErrorAndKeepsEditorOpen()
    {
        _viewModel.OpenTaskEditorForNewTask(TestTasks.Day);
        var failure = new InvalidOperationException("Speichern fehlgeschlagen");
        _taskCache.AddTaskAsync(Arg.Any<TaskDto>()).ThrowsAsync(failure);
        var payload = new TaskEditPopUpClosePayload
        {
            Action = TaskEditPopUpCloseAction.Save,
            Task = TestTasks.Create("Neu"),
            IsUpdateMode = false,
            HasChanges = true
        };

        await _viewModel.HandleTaskEditPopupCloseAsync(payload);

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "YourDayViewModel.HandleTaskEditPopupCloseAsync" &&
                options.IsUiFailure &&
                !options.IsFatal));
        Assert.True(_viewModel.IsTaskEditorOpen);
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_SaveWithoutNote_KeepsEditorOpenWithoutError()
    {
        _viewModel.OpenTaskEditorForNewTask(TestTasks.Day);
        var payload = new TaskEditPopUpClosePayload
        {
            Action = TaskEditPopUpCloseAction.Save,
            Task = TestTasks.Create(" "),
            IsUpdateMode = false,
            HasChanges = true
        };

        await _viewModel.HandleTaskEditPopupCloseAsync(payload);

        Assert.True(_viewModel.IsTaskEditorOpen);
        await _errorCapture.DidNotReceiveWithAnyArgs().CaptureAsync(default!, default);
    }

    [Fact]
    public void StartEditSelectedTask_NoPopupOpen_OpensNoEditor()
    {
        _viewModel.StartEditSelectedTask();

        Assert.False(_viewModel.IsTaskEditorOpen);
    }

    [Fact]
    public void StartEditSelectedTask_ActiveTask_OpensNoEditor()
    {
        var task = TestTasks.Create("Aktiv", taskDate: TestTasks.Day);
        SetNormalTasks(task);
        _activeTaskService.IsTaskActive(task.Id!.Value).Returns(true);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id.Value);

        viewModel.StartEditSelectedTask();

        Assert.False(viewModel.IsTaskEditorOpen);
        Assert.True(viewModel.IsTaskActionPopupOpen);
    }

    [Fact]
    public async Task MarkSelectedTaskAsDoneAsync_NoPopupOpen_FinishesNothing()
    {
        await _viewModel.MarkSelectedTaskAsDoneAsync();

        await _taskCache.DidNotReceive().FinishTaskAsync(Arg.Any<TaskDto>());
        Assert.False(_viewModel.IsTaskActionPopupOpen);
    }

    [Fact]
    public async Task MarkSelectedTaskAsDoneAsync_DoneTask_FinishesNothingAndClosesPopup()
    {
        var task = TestTasks.Create("Fertig", isDone: true);
        SetDoneTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);

        await viewModel.MarkSelectedTaskAsDoneAsync();

        await _taskCache.DidNotReceive().FinishTaskAsync(Arg.Any<TaskDto>());
        Assert.False(viewModel.IsTaskActionPopupOpen);
    }

    [Fact]
    public async Task MarkSelectedTaskAsDoneAsync_ActiveTask_StopsTimerBeforeFinishing()
    {
        var task = TestTasks.Create("Aktiv");
        SetNormalTasks(task);
        _activeTaskService.IsTaskActive(task.Id!.Value).Returns(true);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id.Value);

        await viewModel.MarkSelectedTaskAsDoneAsync();

        Received.InOrder(() =>
        {
            _timer.StopAsync();
            _taskCache.FinishTaskAsync(Arg.Any<TaskDto>());
        });
        Assert.False(viewModel.IsTaskActionPopupOpen);
    }

    [Fact]
    public async Task MarkSelectedTaskAsDoneAsync_ActiveTaskWithRunningFocusSession_StopsTimerAndFinishes()
    {
        var task = TestTasks.Create("Aktiv");
        SetNormalTasks(task);
        _activeTaskService.IsTaskActive(task.Id!.Value).Returns(true);
        _timer.IsTimerActive.Returns(true);
        _timer.IsBreakMode.Returns(false);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id.Value);

        await viewModel.MarkSelectedTaskAsDoneAsync();

        Received.InOrder(() =>
        {
            _timer.StopAsync();
            _taskCache.FinishTaskAsync(Arg.Any<TaskDto>());
        });
        Assert.False(viewModel.IsTaskActionPopupOpen);
    }

    [Fact]
    public async Task MarkSelectedTaskAsDoneAsync_InactiveTask_DoesNotStopTimer()
    {
        var task = TestTasks.Create("Offen");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);

        await viewModel.MarkSelectedTaskAsDoneAsync();

        await _timer.DidNotReceive().StopAsync();
    }

    [Fact]
    public async Task MarkSelectedTaskAsDoneAsync_FinishFails_CapturesUiErrorAndClosesPopup()
    {
        var task = TestTasks.Create("Offen");
        SetNormalTasks(task);
        var failure = new InvalidOperationException("kaputt");
        _taskCache.FinishTaskAsync(Arg.Any<TaskDto>()).ThrowsAsync(failure);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);

        await viewModel.MarkSelectedTaskAsDoneAsync();

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "YourDayViewModel.MarkSelectedTaskAsDoneAsync" &&
                options.IsUiFailure));
        Assert.False(viewModel.IsTaskActionPopupOpen);
    }

    [Fact]
    public async Task DeleteSelectedTaskAsync_NoPopupOpen_DeletesNothing()
    {
        await _viewModel.DeleteSelectedTaskAsync();

        await _taskCache.DidNotReceive().DeleteTaskAsync(Arg.Any<Guid>());
        Assert.False(_viewModel.IsTaskActionPopupOpen);
    }

    [Fact]
    public async Task DeleteSelectedTaskAsync_ActiveTask_ClosesPopup()
    {
        var task = TestTasks.Create("Aktiv");
        SetNormalTasks(task);
        _activeTaskService.IsTaskActive(task.Id!.Value).Returns(true);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id.Value);

        await viewModel.DeleteSelectedTaskAsync();

        Assert.False(viewModel.IsTaskActionPopupOpen);
    }

    [Fact]
    public async Task DeleteSelectedTaskAsync_DeleteFails_CapturesUiErrorAndClosesPopup()
    {
        var task = TestTasks.Create("Offen");
        SetNormalTasks(task);
        var failure = new InvalidOperationException("kaputt");
        _taskCache.DeleteTaskAsync(task.Id!.Value).ThrowsAsync(failure);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id.Value);

        await viewModel.DeleteSelectedTaskAsync();

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "YourDayViewModel.DeleteSelectedTaskAsync" &&
                options.IsUiFailure));
        Assert.False(viewModel.IsTaskActionPopupOpen);
    }

    [Fact]
    public async Task ExecutePrimarySelectedTaskActionAsync_NoPopupOpen_ReturnsFalse()
    {
        var shouldNavigate = await _viewModel.ExecutePrimarySelectedTaskActionAsync();

        Assert.False(shouldNavigate);
        _activeTaskService.DidNotReceive().SetActiveTask(Arg.Any<Guid?>());
    }

    [Fact]
    public async Task ExecutePrimarySelectedTaskActionAsync_DoneTask_ReturnsFalse()
    {
        var task = TestTasks.Create("Fertig", isDone: true);
        SetDoneTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);

        var shouldNavigate = await viewModel.ExecutePrimarySelectedTaskActionAsync();

        Assert.False(shouldNavigate);
        _activeTaskService.DidNotReceive().SetActiveTask(Arg.Any<Guid?>());
    }

    [Fact]
    public async Task ExecutePrimarySelectedTaskActionAsync_ActiveTask_PausesTimerAndClosesPopup()
    {
        var task = TestTasks.Create("Aktiv");
        SetNormalTasks(task);
        _activeTaskService.IsTaskActive(task.Id!.Value).Returns(true);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id.Value);

        var shouldNavigate = await viewModel.ExecutePrimarySelectedTaskActionAsync();

        Assert.False(shouldNavigate);
        await _timer.Received(1).PauseAsync();
        Assert.False(viewModel.IsTaskActionPopupOpen);
    }

    [Fact]
    public async Task ExecutePrimarySelectedTaskActionAsync_PauseFails_CapturesUiErrorAndClosesPopup()
    {
        var task = TestTasks.Create("Aktiv");
        SetNormalTasks(task);
        _activeTaskService.IsTaskActive(task.Id!.Value).Returns(true);
        var failure = new InvalidOperationException("kaputt");
        _timer.PauseAsync().ThrowsAsync(failure);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id.Value);

        var shouldNavigate = await viewModel.ExecutePrimarySelectedTaskActionAsync();

        Assert.False(shouldNavigate);
        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "YourDayViewModel.ExecutePrimarySelectedTaskActionAsync" &&
                options.IsUiFailure));
        Assert.False(viewModel.IsTaskActionPopupOpen);
    }

    [Fact]
    public async Task ExecutePrimarySelectedTaskActionAsync_InactiveTask_ClosesPopup()
    {
        var task = TestTasks.Create("Starten");
        SetNormalTasks(task);
        var viewModel = CreateViewModel();
        viewModel.SelectDay(TestTasks.Day);
        viewModel.OpenTaskActionPopup(task.Id!.Value);

        await viewModel.ExecutePrimarySelectedTaskActionAsync();

        Assert.False(viewModel.IsTaskActionPopupOpen);
        await _timer.DidNotReceive().PauseAsync();
    }

    [Fact]
    public void Constructor_DoneTaskWithoutTaskDate_ShowsDotOnItsCreationDay()
    {
        var task = TestTasks.Create("Ohne Tag", isDone: true);
        task.TaskDate = null;
        task.CreationDate = TestTasks.Day.AddHours(8);
        SetDoneTasks(task);
        var viewModel = CreateViewModel();

        var dots = FindDay(viewModel, TestTasks.Day).Dots;

        var dot = Assert.Single(dots);
        Assert.True(dot.IsDone);
    }

    private YourDayViewModel CreateViewModel()
    {
        var taskEditor = new TaskEditorCoordinator(_scopeService, _taskCache, _textProvider, _priorityFormatter, _taskSeriesService, _clock);
        return new YourDayViewModel(
            _taskCache,
            _activeTaskService,
            _scopeService,
            _startupState,
            _timer,
            taskEditor,
            _taskSeriesService,
            _errorCapture,
            _clock,
            _dayChange,
            _navigation,
            _rewardingSettings);
    }

    private CalenderDayCellModel FindDay(DateTime date)
    {
        return FindDay(_viewModel, date);
    }

    private static CalenderDayCellModel FindDay(YourDayViewModel viewModel, DateTime date)
    {
        return viewModel.Weeks.SelectMany(week => week.Days).Single(day => day.Date == date);
    }

    private static TaskDto CreateSeriesTask(string note)
    {
        var task = TestTasks.Create(note);
        task.SeriesId = Guid.NewGuid();
        return task;
    }

    private static TaskDto CreateSeriesStartTask(string note)
    {
        var task = TestTasks.Create(note);
        task.RepeatUnit = TaskRepeatUnit.Week;
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

    private static TaskEditPopUpClosePayload CreateCancelPayload()
    {
        return new TaskEditPopUpClosePayload
        {
            Action = TaskEditPopUpCloseAction.Cancel,
            Task = new TaskDto()
        };
    }

    private void SetNormalTasks(params TaskDto[] tasks)
    {
        IReadOnlyList<TaskDto> list = tasks;
        _taskCache.GetNormalTasks(Arg.Any<DateTime?>()).Returns(list);
    }

    private void SetTemplates(params TaskDto[] tasks)
    {
        IReadOnlyList<TaskDto> list = tasks;
        _taskCache.GetTemplates(Arg.Any<DateTime?>()).Returns(list);
    }

    private void SetDoneTasks(params TaskDto[] tasks)
    {
        IReadOnlyList<TaskDto> list = tasks;
        _taskCache.GetDoneTasks(Arg.Any<DateTime?>()).Returns(list);
    }
}
