using FokusApp.Components.Calender;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FocusAppTests.ViewModels;

public class CalenderViewModelTests
{
    private readonly ITaskCacheService _taskCache = Substitute.For<ITaskCacheService>();
    private readonly IActiveTaskService _activeTaskService = Substitute.For<IActiveTaskService>();
    private readonly IScopeService _scopeService = Substitute.For<IScopeService>();
    private readonly IAppStartupStateService _startupState = Substitute.For<IAppStartupStateService>();
    private readonly ITimerSessionController _timer = Substitute.For<ITimerSessionController>();
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly IUiTextProvider _textProvider = TestTexts.Create();
    private readonly ITaskPriorityFormatter _priorityFormatter = Substitute.For<ITaskPriorityFormatter>();
    private readonly FakeTimeProvider _clock = TestTasks.CreateClock();
    private readonly IDayChangeService _dayChange = Substitute.For<IDayChangeService>();
    private readonly CalenderViewModel _viewModel;

    public CalenderViewModelTests()
    {
        IReadOnlyList<TaskDto> empty = [];
        _taskCache.GetNormalTasks(Arg.Any<DateTime?>()).Returns(empty);
        _taskCache.GetRegularTasks(Arg.Any<DateTime?>()).Returns(empty);
        _taskCache.GetDoneTasks(Arg.Any<DateTime?>()).Returns(empty);
        _scopeService.IsWorkScope.Returns(true);
        _priorityFormatter.Format(Arg.Any<TaskPriority>()).Returns(call => call.Arg<TaskPriority>().ToString());
        var taskEditor = new TaskEditorCoordinator(_scopeService, _taskCache, _textProvider, _priorityFormatter);
        _viewModel = new CalenderViewModel(
            _taskCache,
            _activeTaskService,
            _scopeService,
            _startupState,
            _timer,
            taskEditor,
            _errorCapture,
            _clock,
            _dayChange);
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

        Assert.Equal([TaskPriority.Dringend, TaskPriority.Wenig], day.Dots.Select(dot => dot.Priority).ToArray());
        Assert.False(day.HasMoreTasks);
    }

    [Fact]
    public void Constructor_DoneTask_ShowsGreyDotAfterOpenTasks()
    {
        SetNormalTasks(TestTasks.Create("Offen", priority: TaskPriority.Keine));
        SetDoneTasks(TestTasks.Create("Fertig", isDone: true, priority: TaskPriority.Dringend));
        var viewModel = CreateViewModel();

        var day = FindDay(viewModel, TestTasks.Day);

        Assert.Equal([false, true], day.Dots.Select(dot => dot.IsDone).ToArray());
    }

    [Fact]
    public void Constructor_RegularTemplate_ShowsNoDot()
    {
        SetRegularTasks(TestTasks.Create("Vorlage", isRegular: true));
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
    public void Constructor_NoInteraction_CalendarIsExpanded()
    {
        Assert.False(_viewModel.IsCalendarCollapsed);
    }

    [Fact]
    public void ToggleCalendarCollapsed_Twice_CollapsesAndExpandsAgainKeepingSelectedDay()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        _viewModel.ToggleCalendarCollapsed();

        Assert.True(_viewModel.IsCalendarCollapsed);
        Assert.Contains(nameof(ICalenderViewModel.IsCalendarCollapsed), changed);
        Assert.Equal(TestTasks.Today, _viewModel.SelectedDay);

        _viewModel.ToggleCalendarCollapsed();

        Assert.False(_viewModel.IsCalendarCollapsed);
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
        _viewModel.ToggleCalendarCollapsed();

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
        _viewModel.ToggleCalendarCollapsed();

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

        Assert.Equal([forYou.Id, forYouDone.Id], viewModel.ForYouTasks.Select(task => task.Id).ToArray());
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
        Assert.True(_viewModel.TaskEditorInput.IsTaskDateLocked);
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
        Assert.Contains(nameof(ICalenderViewModel.Weeks), changedProperties);
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

    private CalenderViewModel CreateViewModel()
    {
        var taskEditor = new TaskEditorCoordinator(_scopeService, _taskCache, _textProvider, _priorityFormatter);
        return new CalenderViewModel(
            _taskCache,
            _activeTaskService,
            _scopeService,
            _startupState,
            _timer,
            taskEditor,
            _errorCapture,
            _clock,
            _dayChange);
    }

    private CalenderDayCellModel FindDay(DateTime date)
    {
        return FindDay(_viewModel, date);
    }

    private static CalenderDayCellModel FindDay(CalenderViewModel viewModel, DateTime date)
    {
        return viewModel.Weeks.SelectMany(week => week.Days).Single(day => day.Date == date);
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

    private void SetRegularTasks(params TaskDto[] tasks)
    {
        IReadOnlyList<TaskDto> list = tasks;
        _taskCache.GetRegularTasks(Arg.Any<DateTime?>()).Returns(list);
    }

    private void SetDoneTasks(params TaskDto[] tasks)
    {
        IReadOnlyList<TaskDto> list = tasks;
        _taskCache.GetDoneTasks(Arg.Any<DateTime?>()).Returns(list);
    }
}
