using FocusAppTests.Fakes;
using FokusApp.Components.DayPlanner;
using FokusApp.Components.ForYouSuggestions;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Components.TaskOverViewComponent;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using static FocusAppTests.TestSupport.WaitForAsyncHelper;

namespace FocusAppTests.ViewModels;

public class DayPlannerViewModelTests
{

    private static readonly DateTime Today = new(2026, 3, 15);

    private readonly ITaskCacheService _taskCache = Substitute.For<ITaskCacheService>();
    private readonly IActiveTaskService _activeTask = Substitute.For<IActiveTaskService>();
    private readonly IBalanceForYouToDoService _balance = Substitute.For<IBalanceForYouToDoService>();
    private readonly IRewardingSettingsService _rewardingSettings = TestSettings.Create();
    private readonly IWeekdayAnalysisService _weekdayAnalysis = Substitute.For<IWeekdayAnalysisService>();
    private readonly IForYouSuggestionService _suggestionService = Substitute.For<IForYouSuggestionService>();
    private readonly IScopeService _scopeService = Substitute.For<IScopeService>();
    private readonly ITaskPriorityFormatter _priorityFormatter = Substitute.For<ITaskPriorityFormatter>();
    private readonly ITaskSeriesService _taskSeriesService = Substitute.For<ITaskSeriesService>();
    private readonly IAppStartupStateService _startupState = Substitute.For<IAppStartupStateService>();
    private readonly IDayChangeService _dayChange = Substitute.For<IDayChangeService>();
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly FakeNavigationManager _navigation = new("http://localhost/tagesplaner");
    private readonly FakeTimeProvider _clock = TestTasks.CreateClock();
    private readonly List<TaskDto> _normalTasks = [];
    private readonly DayPlannerViewModel _viewModel;

    public DayPlannerViewModelTests()
    {
        SetPlanned(0, 0);
        _taskCache.GetNormalTasks(Arg.Any<DateTime?>()).Returns(_ => _normalTasks.ToList());
        _taskCache.GetTemplates(Arg.Any<DateTime?>()).Returns(new List<TaskDto>());
        _weekdayAnalysis.AnalyzeAsync(Arg.Any<DateTime>()).Returns(new WeekdayAnalysis([], 0, false, []));
        _suggestionService.GetStoredSuggestionsAsync().Returns(new List<ForYouSuggestionDto>());
        _scopeService.IsWorkScope.Returns(true);

        var textProvider = new KeyTextProvider();
        var taskEditor = new TaskEditorCoordinator(_scopeService, _taskCache, textProvider, _priorityFormatter, _taskSeriesService, _clock);
        _viewModel = new DayPlannerViewModel(
            _taskCache,
            _activeTask,
            _balance,
            _rewardingSettings,
            _weekdayAnalysis,
            _suggestionService,
            taskEditor,
            textProvider,
            _startupState,
            _dayChange,
            _errorCapture,
            _scopeService,
            _taskSeriesService,
            _navigation,
            _clock);
    }

    [Fact]
    public async Task InitializeAsync_AppDataLoading_WaitsForStartupAndStartsAtTheOverview()
    {
        await _viewModel.InitializeAsync();

        await _startupState.Received(1).WaitForReadyAsync(Arg.Any<CancellationToken>());
        Assert.True(_viewModel.IsLoaded);
        Assert.Equal(DayPlannerStep.Overview, _viewModel.Step);
        Assert.False(_viewModel.CanGoBack);
    }

    [Fact]
    public async Task Steps_NothingFromThePast_OfferNoAssignStep()
    {
        await _viewModel.InitializeAsync();

        Assert.Equal(
            [DayPlannerStep.Overview, DayPlannerStep.ForYou, DayPlannerStep.Duties, DayPlannerStep.Done],
            _viewModel.Steps);
    }

    [Fact]
    public async Task Steps_OpenTasksFromThePast_OfferTheAssignStep()
    {
        AddPastTask("Alt");

        await _viewModel.InitializeAsync();

        Assert.Equal(
            [DayPlannerStep.Overview, DayPlannerStep.Assign, DayPlannerStep.ForYou, DayPlannerStep.Duties, DayPlannerStep.Done],
            _viewModel.Steps);
    }

    [Fact]
    public async Task Overview_NothingPlanned_IsInvitingAndShowsEmptyBars()
    {
        await _viewModel.InitializeAsync();

        Assert.Equal("PlannerOverviewEmpty()", _viewModel.OverviewText);
        Assert.Equal(0, _viewModel.PlannedTotal);
        Assert.Equal(0, _viewModel.ForYouDonePercent);
        Assert.Equal(0, _viewModel.ForYouPlannedPercent);
        Assert.Equal(0, _viewModel.ToDoDonePercent);
        Assert.Equal(0, _viewModel.ToDoPlannedPercent);
        Assert.Empty(_viewModel.TodayTasks);
    }

    [Fact]
    public async Task Overview_PlannedTasks_ShowCountsBarsAndTheOpenTasksOfToday()
    {
        SetPlanned(2, 3);
        AddTask("Spazieren", isPositive: true, Today);
        AddTask("Steuer", isPositive: false, Today);
        AddTask("Morgen", isPositive: false, Today.AddDays(1));

        await _viewModel.InitializeAsync();

        Assert.Equal(2, _viewModel.PlannedForYou);
        Assert.Equal(3, _viewModel.PlannedToDo);
        Assert.Equal("PlannerOverviewSummary(2, 3)", _viewModel.OverviewText);
        var todayTaskNotes = _viewModel.TodayTasks.Select(task => task.Note);
        Assert.Equal(["Spazieren", "Steuer"], todayTaskNotes);
        Assert.Equal(100.0 * 2 / 3, _viewModel.ForYouExistingPercent, 6);
        Assert.Equal(100, _viewModel.ToDoExistingPercent, 6);
    }

    [Fact]
    public async Task Bars_TasksCreatedBeforeOpening_StandGreyBehindTheFilledDoneOnes()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { BalanceGoal = BalanceGoal.TwoToThree });
        SetPlanned(3, 4, doneForYou: 1, doneToDo: 2);

        await _viewModel.InitializeAsync();

        Assert.Equal(1, _viewModel.DoneForYou);
        Assert.Equal(2, _viewModel.DoneToDo);
        Assert.Equal(25, _viewModel.ForYouDonePercent, 6);
        Assert.Equal(75, _viewModel.ForYouExistingPercent, 6);
        Assert.Equal(50, _viewModel.ToDoDonePercent, 6);
        Assert.Equal(100, _viewModel.ToDoExistingPercent, 6);
        Assert.Equal(0, _viewModel.ForYouPlannedPercent);
        Assert.Equal(0, _viewModel.ToDoPlannedPercent);
    }

    [Fact]
    public async Task Bars_TasksPlannedAfterOpening_AreDashedOnTopOfTheGreyOnes()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { BalanceGoal = BalanceGoal.TwoToThree });
        SetPlanned(2, 3, doneForYou: 1, doneToDo: 1);
        await _viewModel.InitializeAsync();
        SetPlanned(3, 4, doneForYou: 1, doneToDo: 1);

        _taskCache.NormalTasksChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.PlannedForYou == 3);
        Assert.Equal(25, _viewModel.ForYouDonePercent, 6);
        Assert.Equal(50, _viewModel.ForYouExistingPercent, 6);
        Assert.Equal(50, _viewModel.ForYouPlannedStartPercent, 6);
        Assert.Equal(25, _viewModel.ForYouPlannedPercent, 6);
        Assert.Equal(25, _viewModel.ToDoDonePercent, 6);
        Assert.Equal(75, _viewModel.ToDoExistingPercent, 6);
        Assert.Equal(75, _viewModel.ToDoPlannedStartPercent, 6);
        Assert.Equal(25, _viewModel.ToDoPlannedPercent, 6);
    }

    [Fact]
    public async Task Bars_NewTaskDoneStraightAway_IsFilledAndTheDashedPartStartsAboveIt()
    {
        SetPlanned(2, 0, doneForYou: 0);
        await _viewModel.InitializeAsync();
        SetPlanned(5, 0, doneForYou: 4);

        _taskCache.NormalTasksChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.PlannedForYou == 5);
        Assert.Equal(80, _viewModel.ForYouDonePercent, 6);
        Assert.Equal(40, _viewModel.ForYouExistingPercent, 6);
        Assert.Equal(80, _viewModel.ForYouPlannedStartPercent, 6);
        Assert.Equal(20, _viewModel.ForYouPlannedPercent, 6);
    }

    [Fact]
    public async Task Bars_TasksDeletedAfterOpening_ShrinkTheGreyBackground()
    {
        SetPlanned(4, 0);
        await _viewModel.InitializeAsync();
        SetPlanned(1, 0);

        _taskCache.NormalTasksChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.PlannedForYou == 1);
        Assert.Equal(100, _viewModel.ForYouExistingPercent, 6);
        Assert.Equal(0, _viewModel.ForYouPlannedPercent);
    }

    [Fact]
    public async Task Bars_ScopeSwitched_TreatsWhatExistsInTheOtherScopeAsGreyToo()
    {
        _balance.GetTodayTaskCountAsync(true).Returns(new DayTaskCount(Today, 1, 1));
        _balance.GetTodayTaskCountAsync(false).Returns(new DayTaskCount(Today, 0, 3, 0, 1));
        await _viewModel.InitializeAsync();
        _scopeService.IsWorkScope.Returns(false);

        _scopeService.ScopeChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.PlannedToDo == 3);
        Assert.Equal(100, _viewModel.ToDoExistingPercent, 6);
        Assert.Equal(0, _viewModel.ToDoPlannedPercent);
    }

    [Fact]
    public async Task Bars_PlannerOpenedAgain_StartsANewRunWithEverythingGrey()
    {
        SetPlanned(1, 0);
        await _viewModel.InitializeAsync();
        SetPlanned(2, 0);
        _taskCache.NormalTasksChanged += Raise.Event();
        await WaitForAsync(_viewModel, () => _viewModel.PlannedForYou == 2);
        Assert.Equal(50, _viewModel.ForYouPlannedPercent, 6);

        await _viewModel.InitializeAsync();

        Assert.Equal(100, _viewModel.ForYouExistingPercent, 6);
        Assert.Equal(0, _viewModel.ForYouPlannedPercent);
    }

    [Fact]
    public async Task Bars_DayChanged_TreatsAllTasksOfTheNewDayAsExisting()
    {
        SetPlanned(1, 0);
        await _viewModel.InitializeAsync();
        SetPlanned(3, 0);

        _dayChange.DayChanged += Raise.Event<EventHandler<DayChangedEventArgs>>(this, new DayChangedEventArgs(Today, Today.AddDays(1)));

        await WaitForAsync(_viewModel, () => _viewModel.PlannedForYou == 3);
        Assert.Equal(100, _viewModel.ForYouExistingPercent, 6);
        Assert.Equal(0, _viewModel.ForYouPlannedPercent);
    }

    [Fact]
    public async Task Bars_AllTasksDone_HaveNoDashedPart()
    {
        SetPlanned(2, 2, doneForYou: 2, doneToDo: 2);

        await _viewModel.InitializeAsync();

        Assert.Equal(100, _viewModel.ForYouDonePercent, 6);
        Assert.Equal(0, _viewModel.ForYouPlannedPercent);
        Assert.Equal(100, _viewModel.ToDoDonePercent, 6);
        Assert.Equal(0, _viewModel.ToDoPlannedPercent);
    }

    [Fact]
    public async Task Goal_Marks_SitAtTheCountsThatFitTheOtherBar()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { BalanceGoal = BalanceGoal.TwoToThree });
        SetPlanned(3, 4);

        await _viewModel.InitializeAsync();

        Assert.Equal(3, _viewModel.RequiredForYou);
        Assert.Equal(4, _viewModel.AllowedToDo);
        Assert.Equal(75, _viewModel.ForYouGoalPercent, 6);
        Assert.Equal(100, _viewModel.ToDoGoalPercent, 6);
    }

    [Theory]
    [InlineData(BalanceGoal.OneToOne, 0, 0)]
    [InlineData(BalanceGoal.TwoToThree, 1, 1)]
    [InlineData(BalanceGoal.TwoToThree, 2, 3)]
    [InlineData(BalanceGoal.ThreeToTwo, 1, 0)]
    [InlineData(BalanceGoal.ThreeToTwo, 4, 2)]
    public async Task AllowedToDo_PlannedForYou_IsTheMostToDosTheRatioCarries(BalanceGoal goal, int forYou, int expected)
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { BalanceGoal = goal });
        SetPlanned(forYou, 0);

        await _viewModel.InitializeAsync();

        Assert.Equal(expected, _viewModel.AllowedToDo);
    }

    [Fact]
    public async Task Goal_PresetFromTheSettings_IsExposedAsRatio()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { BalanceGoal = BalanceGoal.TwoToThree });

        await _viewModel.InitializeAsync();

        Assert.Equal(2, _viewModel.GoalForYou);
        Assert.Equal(3, _viewModel.GoalToDo);
    }

    [Theory]
    [InlineData(BalanceGoal.OneToOne, 0, 1)]
    [InlineData(BalanceGoal.OneToOne, 3, 3)]
    [InlineData(BalanceGoal.TwoToThree, 3, 2)]
    [InlineData(BalanceGoal.TwoToThree, 4, 3)]
    [InlineData(BalanceGoal.ThreeToTwo, 2, 3)]
    [InlineData(BalanceGoal.ThreeToTwo, 5, 8)]
    public async Task RequiredForYou_PlannedToDos_IsTheSmallestNumberReachingTheRatio(BalanceGoal goal, int toDo, int expected)
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { BalanceGoal = goal });
        SetPlanned(0, toDo);

        await _viewModel.InitializeAsync();

        Assert.Equal(expected, _viewModel.RequiredForYou);
    }

    [Fact]
    public async Task Bars_GoalMarkerHigherThanTheBars_ScalesEverythingToTheMarker()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { BalanceGoal = BalanceGoal.ThreeToTwo });
        SetPlanned(1, 4);

        await _viewModel.InitializeAsync();

        Assert.Equal(6, _viewModel.RequiredForYou);
        Assert.Equal(100, _viewModel.ForYouGoalPercent, 6);
        Assert.Equal(100.0 / 6, _viewModel.ForYouExistingPercent, 6);
        Assert.Equal(100.0 * 4 / 6, _viewModel.ToDoExistingPercent, 6);
    }

    [Fact]
    public async Task Goal_PlannedAgainstRatio_SaysOpenReachedOrExceeded()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { BalanceGoal = BalanceGoal.TwoToThree });

        SetPlanned(1, 3);
        await _viewModel.InitializeAsync();
        Assert.Equal(GoalProgress.Open, _viewModel.GoalProgress);
        Assert.False(_viewModel.IsGoalReached);

        SetPlanned(2, 3);
        _taskCache.NormalTasksChanged += Raise.Event();
        await WaitForAsync(_viewModel, () => _viewModel.IsGoalReached);
        Assert.Equal(GoalProgress.Reached, _viewModel.GoalProgress);

        SetPlanned(3, 3);
        _taskCache.NormalTasksChanged += Raise.Event();
        await WaitForAsync(_viewModel, () => _viewModel.GoalProgress == GoalProgress.Exceeded);
        Assert.True(_viewModel.IsGoalReached);
    }

    [Fact]
    public async Task Goal_ForYouWithoutAnyToDo_IsReached()
    {
        SetPlanned(1, 0);

        await _viewModel.InitializeAsync();

        Assert.True(_viewModel.IsGoalReached);
    }

    [Fact]
    public async Task Goal_NothingPlanned_IsNotReached()
    {
        await _viewModel.InitializeAsync();

        Assert.False(_viewModel.IsGoalReached);
        Assert.Equal(1, _viewModel.RequiredForYou);
    }

    [Fact]
    public async Task Next_FromTheOverview_GoesThroughAllStepsToTheEnd()
    {
        AddPastTask("Alt");
        await _viewModel.InitializeAsync();

        _viewModel.Next();
        Assert.Equal(DayPlannerStep.Assign, _viewModel.Step);
        Assert.True(_viewModel.CanGoBack);

        _viewModel.Next();
        Assert.Equal(DayPlannerStep.ForYou, _viewModel.Step);

        _viewModel.Next();
        Assert.Equal(DayPlannerStep.Duties, _viewModel.Step);

        _viewModel.Next();
        Assert.Equal(DayPlannerStep.Done, _viewModel.Step);
        Assert.False(_viewModel.CanGoBack);
    }

    [Fact]
    public async Task Next_AtTheEnd_StaysOnTheLastStep()
    {
        await _viewModel.InitializeAsync();
        _viewModel.Next();
        _viewModel.Next();
        _viewModel.Next();

        _viewModel.Next();

        Assert.Equal(DayPlannerStep.Done, _viewModel.Step);
    }

    [Fact]
    public void Skip_IsGone_NoStepCanBeSkippedByItsOwnButton()
    {
        Assert.Null(typeof(IDayPlannerViewModel).GetMethod("Skip"));
        Assert.Null(typeof(IDayPlannerViewModel).GetProperty("CanSkip"));
    }

    [Fact]
    public async Task Back_FromLaterSteps_GoesBackButNotBeforeTheOverview()
    {
        await _viewModel.InitializeAsync();
        _viewModel.Next();
        Assert.Equal(DayPlannerStep.ForYou, _viewModel.Step);

        _viewModel.Back();
        Assert.Equal(DayPlannerStep.Overview, _viewModel.Step);
        _viewModel.Back();

        Assert.Equal(DayPlannerStep.Overview, _viewModel.Step);
    }

    [Fact]
    public async Task ForYouStep_GoalAlreadyReached_IsStillShown()
    {
        SetPlanned(1, 1);
        await _viewModel.InitializeAsync();

        _viewModel.Next();

        Assert.Equal(DayPlannerStep.ForYou, _viewModel.Step);
    }

    [Fact]
    public async Task ForYouStep_GoalNotReached_IsShown()
    {
        SetPlanned(0, 3);
        await _viewModel.InitializeAsync();

        _viewModel.Next();

        Assert.Equal(DayPlannerStep.ForYou, _viewModel.Step);
    }

    [Fact]
    public async Task ForYouText_GoalNotReached_KeepsTheInvitation()
    {
        SetPlanned(0, 3);

        await _viewModel.InitializeAsync();

        Assert.Equal("PlannerForYouText()", _viewModel.ForYouText);
    }

    [Fact]
    public async Task ForYouText_GoalReached_PraisesThatTheUserKeepsPlanning()
    {
        SetPlanned(1, 1);

        await _viewModel.InitializeAsync();

        Assert.Equal("PlannerForYouTextReached()", _viewModel.ForYouText);
    }

    [Fact]
    public async Task ForYouText_GoalReachedByAssigningOnTheWay_ChangesTheText()
    {
        SetPlanned(0, 1);
        AddPastTask("Alt", isPositive: true);
        await _viewModel.InitializeAsync();
        _viewModel.Next();
        _viewModel.Next();
        Assert.Equal("PlannerForYouText()", _viewModel.ForYouText);
        SetPlanned(1, 1);

        _taskCache.NormalTasksChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.IsGoalReached);
        Assert.Equal("PlannerForYouTextReached()", _viewModel.ForYouText);
    }

    [Fact]
    public async Task ForYouText_RespectsTheChosenBalanceGoal()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { BalanceGoal = BalanceGoal.ThreeToTwo });
        SetPlanned(1, 1);

        await _viewModel.InitializeAsync();

        Assert.Equal("PlannerForYouText()", _viewModel.ForYouText);
    }

    [Fact]
    public async Task DutyTemplates_OpenToDoTemplates_AreListedWithoutForYouAndDoneOnes()
    {
        var duty = new TaskDto { Id = Guid.NewGuid(), Note = "  Steuer  ", IsPositive = false, IsTemplate = true };
        var forYou = new TaskDto { Id = Guid.NewGuid(), Note = "Yoga", IsPositive = true, IsTemplate = true };
        var done = new TaskDto { Id = Guid.NewGuid(), Note = "Erledigt", IsPositive = false, IsTemplate = true, IsDone = true };
        _taskCache.GetTemplates(Arg.Any<DateTime?>()).Returns(new List<TaskDto> { duty, forYou, done });

        await _viewModel.InitializeAsync();

        var listed = Assert.Single(_viewModel.DutyTemplates);
        Assert.Equal(duty.Id, listed.Id);
        Assert.Equal("Steuer", listed.Note);
        Assert.False(listed.IsPositive);
    }

    [Fact]
    public async Task DutyTemplates_NoTemplates_IsEmpty()
    {
        await _viewModel.InitializeAsync();

        Assert.Empty(_viewModel.DutyTemplates);
    }

    [Fact]
    public async Task TemplatesChanged_DutyTemplateAdded_RefreshesTheList()
    {
        await _viewModel.InitializeAsync();
        _taskCache.GetTemplates(Arg.Any<DateTime?>()).Returns(new List<TaskDto>
        {
            new() { Id = Guid.NewGuid(), Note = "Miete", IsPositive = false, IsTemplate = true }
        });

        _taskCache.TemplatesChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.DutyTemplates.Count == 1);
        Assert.Equal("Miete", _viewModel.DutyTemplates[0].Note);
    }

    [Fact]
    public async Task OpenTaskEditorForToDo_Button_OpensAnEmptyToDoEditorForToday()
    {
        await _viewModel.InitializeAsync();

        _viewModel.OpenTaskEditorForToDo();

        var input = _viewModel.TaskEditorInput!;
        Assert.Null(input.Task);
        Assert.False(input.InitialIsPositive);
        Assert.Equal(Today, input.ContextualTaskDate);
        Assert.True(_viewModel.IsTaskEditorOpen);
    }

    [Fact]
    public async Task OpenTaskEditorForToDo_TemplatesExist_OffersThemInTheEditor()
    {
        var duty = new TaskDto { Id = Guid.NewGuid(), Note = "Steuer", IsPositive = false, IsTemplate = true };
        _taskCache.GetTemplates(Arg.Any<DateTime?>()).Returns(new List<TaskDto> { duty });
        await _viewModel.InitializeAsync();

        _viewModel.OpenTaskEditorForToDo();

        Assert.Contains(_viewModel.TaskEditorInput!.TemplateOptions, option => option.TaskId == duty.Id);
    }

    [Fact]
    public async Task PickDutyTemplate_ExistingTemplate_OpensACopyForToday()
    {
        var duty = new TaskDto { Id = Guid.NewGuid(), Note = "Steuer", IsPositive = false, IsTemplate = true, TimerMinutes = 30 };
        _taskCache.GetTemplates(Arg.Any<DateTime?>()).Returns(new List<TaskDto> { duty });
        await _viewModel.InitializeAsync();

        _viewModel.PickDutyTemplate(duty.Id!.Value);

        var task = _viewModel.TaskEditorInput!.Task!;
        Assert.Null(task.Id);
        Assert.Equal("Steuer", task.Note);
        Assert.False(task.IsPositive);
        Assert.False(task.IsTemplate);
        Assert.Equal(30, task.TimerMinutes);
        Assert.Equal(Today, task.TaskDate);
    }

    [Fact]
    public async Task PickDutyTemplate_UnknownTemplate_OpensNothing()
    {
        await _viewModel.InitializeAsync();

        _viewModel.PickDutyTemplate(Guid.NewGuid());

        Assert.False(_viewModel.IsTaskEditorOpen);
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_SaveOfADuty_AddsTheTaskAndClosesTheEditor()
    {
        await _viewModel.InitializeAsync();
        _viewModel.OpenTaskEditorForToDo();
        var task = new TaskDto { Id = Guid.NewGuid(), Note = "Steuer", IsPositive = false };

        await _viewModel.HandleTaskEditPopupCloseAsync(new TaskEditPopUpClosePayload
        {
            Action = TaskEditPopUpCloseAction.Save,
            Task = task,
            IsUpdateMode = false
        });

        await _taskCache.Received(1).AddTaskAsync(Arg.Is<TaskDto>(item => item.Note == "Steuer" && !item.IsPositive));
        Assert.False(_viewModel.IsTaskEditorOpen);
    }

    [Theory]
    [InlineData(PastTaskTarget.Today, 15)]
    [InlineData(PastTaskTarget.Tomorrow, 16)]
    [InlineData(PastTaskTarget.Weekend, 21)]
    public async Task AssignPastTaskAsync_Target_SetsTheRightDate(PastTaskTarget target, int expectedDay)
    {
        var task = AddPastTask("Alt");
        await _viewModel.InitializeAsync();

        await _viewModel.AssignPastTaskAsync(task.Id!.Value, target);

        await _taskCache.Received(1).MoveTaskToDateAsync(
            Arg.Is<TaskDto>(item => item.Id == task.Id),
            new DateTime(2026, 3, expectedDay));
    }

    [Fact]
    public async Task AssignPastTaskAsync_ActiveTask_IsNotMoved()
    {
        var task = AddPastTask("Alt");
        _activeTask.IsTaskActive(task.Id!.Value).Returns(true);
        await _viewModel.InitializeAsync();

        await _viewModel.AssignPastTaskAsync(task.Id.Value, PastTaskTarget.Today);

        await _taskCache.DidNotReceiveWithAnyArgs().MoveTaskToDateAsync(default!, default);
    }

    [Fact]
    public async Task AssignPastTaskAsync_TaskOfTodayOrUnknown_IsIgnored()
    {
        var current = AddTask("Heute", isPositive: false, Today);
        await _viewModel.InitializeAsync();

        await _viewModel.AssignPastTaskAsync(current.Id!.Value, PastTaskTarget.Tomorrow);
        await _viewModel.AssignPastTaskAsync(Guid.NewGuid(), PastTaskTarget.Tomorrow);

        await _taskCache.DidNotReceiveWithAnyArgs().MoveTaskToDateAsync(default!, default);
    }

    [Fact]
    public async Task AssignAllPastTasksToTomorrowAsync_SeveralTasks_MovesAllButTheActiveOne()
    {
        var first = AddPastTask("Eins");
        var second = AddPastTask("Zwei");
        var active = AddPastTask("Aktiv");
        _activeTask.IsTaskActive(active.Id!.Value).Returns(true);
        await _viewModel.InitializeAsync();

        await _viewModel.AssignAllPastTasksToTomorrowAsync();

        await _taskCache.Received(1).MoveTaskToDateAsync(Arg.Is<TaskDto>(item => item.Id == first.Id), Today.AddDays(1));
        await _taskCache.Received(1).MoveTaskToDateAsync(Arg.Is<TaskDto>(item => item.Id == second.Id), Today.AddDays(1));
        await _taskCache.Received(2).MoveTaskToDateAsync(Arg.Any<TaskDto>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task AssignPastTaskAsync_UpdateFails_CapturesTheError()
    {
        var task = AddPastTask("Alt");
        var failure = new InvalidOperationException("Datenbank gesperrt");
        _taskCache.MoveTaskToDateAsync(Arg.Any<TaskDto>(), Arg.Any<DateTime>()).ThrowsAsync(failure);
        await _viewModel.InitializeAsync();

        await _viewModel.AssignPastTaskAsync(task.Id!.Value, PastTaskTarget.Today);

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null && options.Source == "DayPlannerViewModel.AssignPastTaskAsync" && options.IsUiFailure));
    }

    [Fact]
    public async Task OpenReschedulePopup_PastTask_OpensTheCalendarPopup()
    {
        var task = AddPastTask("Alt");
        await _viewModel.InitializeAsync();

        _viewModel.OpenReschedulePopup(task.Id!.Value);

        Assert.True(_viewModel.IsReschedulePopupOpen);
    }

    [Fact]
    public async Task OpenReschedulePopup_ActiveTask_DoesNotOpen()
    {
        var task = AddPastTask("Alt");
        _activeTask.IsTaskActive(task.Id!.Value).Returns(true);
        await _viewModel.InitializeAsync();

        _viewModel.OpenReschedulePopup(task.Id!.Value);

        Assert.False(_viewModel.IsReschedulePopupOpen);
    }

    [Fact]
    public async Task CloseReschedulePopup_OpenPopup_Closes()
    {
        var task = AddPastTask("Alt");
        await _viewModel.InitializeAsync();
        _viewModel.OpenReschedulePopup(task.Id!.Value);

        _viewModel.CloseReschedulePopup();

        Assert.False(_viewModel.IsReschedulePopupOpen);
    }

    [Fact]
    public async Task ConfirmRescheduleAsync_OpenPopup_MovesTheTaskAndCloses()
    {
        var task = AddPastTask("Alt");
        await _viewModel.InitializeAsync();
        _viewModel.OpenReschedulePopup(task.Id!.Value);
        var newDate = Today.AddDays(5);

        await _viewModel.ConfirmRescheduleAsync(newDate);

        await _taskCache.Received(1).MoveTaskToDateAsync(Arg.Is<TaskDto>(item => item.Id == task.Id), newDate);
        Assert.False(_viewModel.IsReschedulePopupOpen);
    }

    [Fact]
    public async Task ConfirmRescheduleAsync_NothingOpen_DoesNothing()
    {
        await _viewModel.InitializeAsync();

        await _viewModel.ConfirmRescheduleAsync(Today.AddDays(2));

        await _taskCache.DidNotReceiveWithAnyArgs().MoveTaskToDateAsync(default!, default);
    }

    [Fact]
    public async Task DiscardPastTaskAsync_PastTask_DeletesAndOffersUndo()
    {
        var task = AddPastTask("Alt");
        await _viewModel.InitializeAsync();

        await _viewModel.DiscardPastTaskAsync(task.Id!.Value);

        await _taskCache.Received(1).DeleteTaskAsync(task.Id!.Value);
        Assert.Equal(new UndoNotice(UndoNoticeKind.TaskDiscarded, 1), _viewModel.CurrentUndo);
    }

    [Fact]
    public async Task DiscardPastTaskAsync_SeriesTask_DeletesOnlyThatTaskThroughTheSeriesService()
    {
        var task = AddPastTask("Sport");
        task.RepeatUnit = TaskRepeatUnit.Week;
        task.RepeatWeekdays = TaskWeekdays.All;
        await _viewModel.InitializeAsync();

        await _viewModel.DiscardPastTaskAsync(task.Id!.Value);

        await _taskSeriesService.Received(1).DeleteTaskAsync(task.Id!.Value, TaskSeriesScope.ThisTaskOnly, null);
        await _taskCache.DidNotReceive().DeleteTaskAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DiscardPastTaskAsync_ActiveTask_DoesNotDelete()
    {
        var task = AddPastTask("Alt");
        _activeTask.IsTaskActive(task.Id!.Value).Returns(true);
        await _viewModel.InitializeAsync();

        await _viewModel.DiscardPastTaskAsync(task.Id!.Value);

        await _taskCache.DidNotReceive().DeleteTaskAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task UndoAsync_AfterDiscard_RestoresTheTask()
    {
        var task = AddPastTask("Alt");
        await _viewModel.InitializeAsync();
        await _viewModel.DiscardPastTaskAsync(task.Id!.Value);

        await _viewModel.UndoAsync();

        await _taskCache.Received(1).AddTaskAsync(Arg.Is<TaskDto>(restored => restored.Id == task.Id));
        Assert.Null(_viewModel.CurrentUndo);
    }

    [Fact]
    public async Task CurrentUndo_AfterFiveSeconds_Expires()
    {
        var task = AddPastTask("Alt");
        await _viewModel.InitializeAsync();
        await _viewModel.DiscardPastTaskAsync(task.Id!.Value);

        _clock.Advance(TimeSpan.FromSeconds(4));
        Assert.NotNull(_viewModel.CurrentUndo);
        _clock.Advance(TimeSpan.FromSeconds(2));

        Assert.Null(_viewModel.CurrentUndo);
    }

    [Fact]
    public async Task TasksChanged_AfterAssigning_ReloadsTheListsAndTheCounts()
    {
        var task = AddPastTask("Alt");
        await _viewModel.InitializeAsync();
        Assert.Single(_viewModel.PastTasks);
        task.TaskDate = Today;
        SetPlanned(0, 1);

        _taskCache.NormalTasksChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.PastTasks.Count == 0);
        Assert.Empty(_viewModel.PastTasks);
        Assert.Equal(1, _viewModel.PlannedToDo);
        Assert.Single(_viewModel.TodayTasks);
    }

    [Fact]
    public async Task DoneTasksChanged_TaskCompleted_ReloadsTheDoneCounts()
    {
        SetPlanned(1, 0);
        await _viewModel.InitializeAsync();
        Assert.Equal(0, _viewModel.DoneForYou);
        SetPlanned(1, 0, doneForYou: 1);

        _taskCache.DoneTasksChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.DoneForYou == 1);
        Assert.Equal(1, _viewModel.DoneForYou);
        Assert.Equal(0, _viewModel.ForYouPlannedPercent);
    }

    [Fact]
    public async Task NormalTasksChanged_TaskCreated_ShowsItInTheListAndTheCounts()
    {
        await _viewModel.InitializeAsync();
        Assert.Empty(_viewModel.TodayTasks);
        AddTask("Neu", isPositive: true, Today);
        SetPlanned(1, 0);

        _taskCache.NormalTasksChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.PlannedForYou == 1);
        Assert.Equal(1, _viewModel.PlannedForYou);
        var todayTaskNotes = _viewModel.TodayTasks.Select(task => task.Note);
        Assert.Equal(["Neu"], todayTaskNotes);
    }

    [Fact]
    public async Task ScopeChanged_OtherScope_ReloadsTheCountsAndTheLists()
    {
        var workTask = new TaskDto { Id = Guid.NewGuid(), Note = "Arbeit", IsPositive = true, TaskDate = Today, CreationDate = Today.AddDays(-1) };
        var privateTask = new TaskDto { Id = Guid.NewGuid(), Note = "Privat", IsPositive = false, TaskDate = Today, CreationDate = Today.AddDays(-1) };
        _taskCache.GetNormalTasks(Arg.Any<DateTime?>()).Returns(_ => _scopeService.IsWorkScope ? [workTask] : [privateTask]);
        _balance.GetTodayTaskCountAsync(true).Returns(new DayTaskCount(Today, 1, 0));
        _balance.GetTodayTaskCountAsync(false).Returns(new DayTaskCount(Today, 0, 2, 0, 1));
        await _viewModel.InitializeAsync();
        Assert.Equal(1, _viewModel.PlannedForYou);
        var todayTaskNotesBeforeScopeChange = _viewModel.TodayTasks.Select(task => task.Note);
        Assert.Equal(["Arbeit"], todayTaskNotesBeforeScopeChange);
        _scopeService.IsWorkScope.Returns(false);

        _scopeService.ScopeChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.PlannedToDo == 2);
        Assert.Equal(0, _viewModel.PlannedForYou);
        Assert.Equal(2, _viewModel.PlannedToDo);
        Assert.Equal(1, _viewModel.DoneToDo);
        var todayTaskNotesAfterScopeChange = _viewModel.TodayTasks.Select(task => task.Note);
        Assert.Equal(["Privat"], todayTaskNotesAfterScopeChange);
        await _balance.Received().GetTodayTaskCountAsync(false);
    }

    [Fact]
    public async Task ScopeChanged_OtherScopeHasDutyTemplates_ListsThoseOfTheNewScope()
    {
        var workDuty = new TaskDto { Id = Guid.NewGuid(), Note = "Bericht", IsPositive = false, IsTemplate = true };
        var privateDuty = new TaskDto { Id = Guid.NewGuid(), Note = "Steuer", IsPositive = false, IsTemplate = true };
        _taskCache.GetTemplates(Arg.Any<DateTime?>()).Returns(_ => _scopeService.IsWorkScope ? [workDuty] : [privateDuty]);
        await _viewModel.InitializeAsync();
        var dutyTemplateNotesBeforeScopeChange = _viewModel.DutyTemplates.Select(task => task.Note);
        Assert.Equal(["Bericht"], dutyTemplateNotesBeforeScopeChange);
        _scopeService.IsWorkScope.Returns(false);

        _scopeService.ScopeChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.DutyTemplates.Any(task => task.Note == "Steuer"));
        var dutyTemplateNotesAfterScopeChange = _viewModel.DutyTemplates.Select(task => task.Note);
        Assert.Equal(["Steuer"], dutyTemplateNotesAfterScopeChange);
    }

    [Fact]
    public async Task ScopeChanged_OtherScopeHasTasksFromThePast_OffersTheAssignStepAndStaysOnTheCurrentStep()
    {
        var pastTask = new TaskDto { Id = Guid.NewGuid(), Note = "Alt", TaskDate = Today.AddDays(-3), CreationDate = Today.AddDays(-4) };
        _taskCache.GetNormalTasks(Arg.Any<DateTime?>()).Returns(_ => _scopeService.IsWorkScope ? new List<TaskDto>() : [pastTask]);
        await _viewModel.InitializeAsync();
        _viewModel.Next();
        Assert.Equal(DayPlannerStep.ForYou, _viewModel.Step);
        _scopeService.IsWorkScope.Returns(false);

        _scopeService.ScopeChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.PastTasks.Count == 1);
        Assert.Contains(DayPlannerStep.Assign, _viewModel.Steps);
        Assert.Equal(DayPlannerStep.ForYou, _viewModel.Step);
        _viewModel.Back();
        Assert.Equal(DayPlannerStep.Assign, _viewModel.Step);
    }

    [Fact]
    public async Task ScopeChanged_OtherScopeHasNoTasksFromThePast_DropsTheAssignStepBehindTheUser()
    {
        var pastTask = new TaskDto { Id = Guid.NewGuid(), Note = "Alt", TaskDate = Today.AddDays(-3), CreationDate = Today.AddDays(-4) };
        _taskCache.GetNormalTasks(Arg.Any<DateTime?>()).Returns(_ => _scopeService.IsWorkScope ? [pastTask] : new List<TaskDto>());
        await _viewModel.InitializeAsync();
        _viewModel.Next();
        _viewModel.Next();
        Assert.Equal(DayPlannerStep.ForYou, _viewModel.Step);
        _scopeService.IsWorkScope.Returns(false);

        _scopeService.ScopeChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.PastTasks.Count == 0);
        Assert.DoesNotContain(DayPlannerStep.Assign, _viewModel.Steps);
        Assert.Equal(DayPlannerStep.ForYou, _viewModel.Step);
    }

    [Fact]
    public async Task ScopeChanged_UserIsOnTheAssignStep_KeepsTheStepWhileItIsEmpty()
    {
        var pastTask = new TaskDto { Id = Guid.NewGuid(), Note = "Alt", TaskDate = Today.AddDays(-3), CreationDate = Today.AddDays(-4) };
        _taskCache.GetNormalTasks(Arg.Any<DateTime?>()).Returns(_ => _scopeService.IsWorkScope ? [pastTask] : new List<TaskDto>());
        await _viewModel.InitializeAsync();
        _viewModel.Next();
        Assert.Equal(DayPlannerStep.Assign, _viewModel.Step);
        _scopeService.IsWorkScope.Returns(false);

        _scopeService.ScopeChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.PastTasks.Count == 0);
        Assert.Equal(DayPlannerStep.Assign, _viewModel.Step);
        Assert.Contains(DayPlannerStep.Assign, _viewModel.Steps);
    }

    [Fact]
    public async Task ScopeChanged_ReloadsStillRunning_OnlyTheNewestOneApplies()
    {
        await _viewModel.InitializeAsync();
        var slowWorkCount = new TaskCompletionSource<DayTaskCount>();
        _balance.GetTodayTaskCountAsync(true).Returns(slowWorkCount.Task);
        _balance.GetTodayTaskCountAsync(false).Returns(new DayTaskCount(Today, 0, 2));
        _taskCache.NormalTasksChanged += Raise.Event();
        _scopeService.IsWorkScope.Returns(false);
        _scopeService.ScopeChanged += Raise.Event();
        await WaitForAsync(_viewModel, () => _viewModel.PlannedToDo == 2);

        slowWorkCount.SetResult(new DayTaskCount(Today, 5, 0));
        await Task.Delay(50);

        Assert.Equal(0, _viewModel.PlannedForYou);
        Assert.Equal(2, _viewModel.PlannedToDo);
    }

    [Fact]
    public async Task Dispose_AfterDispose_IgnoresScopeChanges()
    {
        await _viewModel.InitializeAsync();
        _viewModel.Dispose();

        _scopeService.ScopeChanged += Raise.Event();
        await Task.Delay(50);

        await _balance.Received(1).GetTodayTaskCountAsync(Arg.Any<bool>());
    }

    [Fact]
    public async Task Insight_ClearPatternOnTodaysWeekday_IsOfferedWithItsText()
    {
        _weekdayAnalysis.AnalyzeAsync(Arg.Any<DateTime>()).Returns(new WeekdayAnalysis(
            [],
            4,
            true,
            [
                new WeekdayInsight(DayOfWeek.Wednesday, WeekdayInsightKind.MostlyDuties),
                new WeekdayInsight(DayOfWeek.Sunday, WeekdayInsightKind.ForYouPlannedButRarelyDone)
            ]));

        await _viewModel.InitializeAsync();

        Assert.StartsWith("WeekdayInsightRarelyDone(", _viewModel.Insight!.Text);
    }

    [Fact]
    public async Task Insight_OnlyForOtherWeekdays_IsNull()
    {
        _weekdayAnalysis.AnalyzeAsync(Arg.Any<DateTime>()).Returns(new WeekdayAnalysis(
            [],
            4,
            true,
            [new WeekdayInsight(DayOfWeek.Wednesday, WeekdayInsightKind.MostlyDuties)]));

        await _viewModel.InitializeAsync();

        Assert.Null(_viewModel.Insight);
    }

    [Fact]
    public async Task Suggestions_Always_ListHabitsOwnAndAllPoolIdeasInThatOrder()
    {
        _suggestionService.GetStoredSuggestionsAsync().Returns(new List<ForYouSuggestionDto>
        {
            new() { Id = Guid.NewGuid(), Text = "Mein Vorschlag" }
        });
        _taskCache.GetTemplates(Arg.Any<DateTime?>()).Returns(new List<TaskDto>
        {
            new() { Id = Guid.NewGuid(), Note = "Yoga", IsPositive = true, IsTemplate = true }
        });

        await _viewModel.InitializeAsync();

        Assert.Equal(2 + ForYouSuggestionPool.Entries.Count, _viewModel.Suggestions.Count);
        Assert.Equal("Yoga", _viewModel.Suggestions[0].Text);
        Assert.NotNull(_viewModel.Suggestions[0].TemplateId);
        Assert.Equal("Mein Vorschlag", _viewModel.Suggestions[1].Text);
        Assert.All(_viewModel.Suggestions.Skip(2), item => Assert.NotNull(item.Category));
    }

    [Fact]
    public async Task PastForYous_DoneAndPastOpenForYouTasks_AreListedOnceNewestFirst()
    {
        AddPastTask("Lesen", isPositive: true);
        AddPastTask("Steuer", isPositive: false);
        AddTask("Heute", isPositive: true, Today);
        _taskCache.GetDoneTasks(Arg.Any<DateTime?>()).Returns(new List<TaskDto>
        {
            new() { Id = Guid.NewGuid(), Note = "Yoga", IsPositive = true, IsDone = true, TaskDate = Today.AddDays(-1), CreationDate = Today.AddDays(-2) },
            new() { Id = Guid.NewGuid(), Note = " yoga ", IsPositive = true, IsDone = true, TaskDate = Today.AddDays(-8), CreationDate = Today.AddDays(-9) },
            new() { Id = Guid.NewGuid(), Note = "Steuer", IsPositive = false, IsDone = true, TaskDate = Today.AddDays(-1), CreationDate = Today.AddDays(-2) }
        });

        await _viewModel.InitializeAsync();

        Assert.Equal(["Yoga", "Lesen"], _viewModel.PastForYous);
    }

    [Fact]
    public async Task PastForYous_NothingBefore_IsEmpty()
    {
        await _viewModel.InitializeAsync();

        Assert.Empty(_viewModel.PastForYous);
    }

    [Fact]
    public async Task Suggestions_NoOwnOnes_AreAllPoolIdeasOfAllCategoriesOnce()
    {
        await _viewModel.InitializeAsync();

        var texts = _viewModel.Suggestions.Select(item => item.Text).ToList();
        var categories = _viewModel.Suggestions.Select(item => item.Category).Distinct().ToList();
        var distinctTextCount = texts.Distinct().Count();
        var poolCategoryCount = ForYouSuggestionPool.Entries.Select(entry => entry.Category).Distinct().Count();
        Assert.Equal(ForYouSuggestionPool.Entries.Count, texts.Count);
        Assert.Equal(texts.Count, distinctTextCount);
        Assert.Equal(poolCategoryCount, categories.Count);
    }

    [Fact]
    public async Task PickSuggestion_Pool_OpensTheEditorForANewForYouTaskForToday()
    {
        await _viewModel.InitializeAsync();

        _viewModel.PickSuggestion(new ForYouSuggestionItem("Lesen", false, ForYouSuggestionCategory.Calm));

        Assert.True(_viewModel.IsTaskEditorOpen);
        var task = _viewModel.TaskEditorInput!.Task!;
        Assert.Null(task.Id);
        Assert.Equal("Lesen", task.Note);
        Assert.True(task.IsPositive);
        Assert.Equal(Today, task.TaskDate);
    }

    [Fact]
    public async Task PickSuggestion_Template_OpensACopyOfTheTemplate()
    {
        var template = new TaskDto { Id = Guid.NewGuid(), Note = "Yoga", IsPositive = true, IsTemplate = true, TimerMinutes = 20 };
        _taskCache.GetTemplates(Arg.Any<DateTime?>()).Returns(new List<TaskDto> { template });
        await _viewModel.InitializeAsync();

        _viewModel.PickSuggestion(_viewModel.Suggestions[0]);

        var task = _viewModel.TaskEditorInput!.Task!;
        Assert.Null(task.Id);
        Assert.Equal("Yoga", task.Note);
        Assert.False(task.IsTemplate);
        Assert.Equal(20, task.TimerMinutes);
    }

    [Fact]
    public async Task OpenTaskEditorForForYou_Button_OpensAnEmptyForYouEditorForToday()
    {
        await _viewModel.InitializeAsync();

        _viewModel.OpenTaskEditorForForYou();

        var input = _viewModel.TaskEditorInput!;
        Assert.Null(input.Task);
        Assert.True(input.InitialIsPositive);
        Assert.Equal(Today, input.ContextualTaskDate);
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_Cancel_ClosesTheEditor()
    {
        await _viewModel.InitializeAsync();
        _viewModel.OpenTaskEditorForForYou();

        await _viewModel.HandleTaskEditPopupCloseAsync(new TaskEditPopUpClosePayload { Action = TaskEditPopUpCloseAction.Cancel });

        Assert.False(_viewModel.IsTaskEditorOpen);
    }

    [Fact]
    public async Task DoneText_NothingPlanned_ReleasesTheUserFromPlanning()
    {
        await _viewModel.InitializeAsync();

        Assert.Equal("PlannerDoneNothing()", _viewModel.DoneText);
    }

    [Fact]
    public async Task DoneText_SomethingPlanned_IsAFriendlyConfirmation()
    {
        SetPlanned(1, 0);
        await _viewModel.InitializeAsync();

        Assert.Equal("PlannerDonePlanned()", _viewModel.DoneText);
    }

    [Fact]
    public void Finish_Anytime_ReturnsToTheCalendar()
    {
        _viewModel.Finish();

        Assert.Equal(["/"], _navigation.NavigatedTo);
    }

    [Fact]
    public async Task InitializeAsync_LoadingFails_CapturesErrorAndStaysNotLoaded()
    {
        var failure = new InvalidOperationException("Datenbank gesperrt");
        _balance.GetTodayTaskCountAsync(Arg.Any<bool>()).ThrowsAsync(failure);

        await _viewModel.InitializeAsync();

        Assert.False(_viewModel.IsLoaded);
        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null && options.Source == "DayPlannerViewModel.LoadAsync" && options.IsUiFailure));
    }

    [Fact]
    public async Task Dispose_AfterDispose_IgnoresChanges()
    {
        await _viewModel.InitializeAsync();
        _viewModel.Dispose();

        _taskCache.NormalTasksChanged += Raise.Event();
        await Task.Delay(50);

        await _balance.Received(1).GetTodayTaskCountAsync(Arg.Any<bool>());
    }

    [Fact]
    public void Constructor_BeforeInitialization_ShowsEmptyOverviewWithoutPlannedTasks()
    {
        Assert.False(_viewModel.IsLoaded);
        Assert.Equal(DayPlannerStep.Overview, _viewModel.Step);
        Assert.Equal(0, _viewModel.PlannedForYou);
        Assert.Equal(0, _viewModel.PlannedToDo);
        Assert.Equal(0, _viewModel.PlannedTotal);
        Assert.Empty(_viewModel.TodayTasks);
        Assert.Empty(_viewModel.PastTasks);
        Assert.Empty(_viewModel.Suggestions);
        Assert.Null(_viewModel.Insight);
        Assert.Empty(_viewModel.DutyTemplates);
        Assert.Equal(0, _viewModel.DoneForYou);
        Assert.Equal(0, _viewModel.DoneToDo);
        Assert.Equal(0, _viewModel.ForYouDonePercent);
        Assert.Equal(0, _viewModel.ForYouPlannedPercent);
        Assert.Equal(0, _viewModel.ToDoDonePercent);
        Assert.Equal(0, _viewModel.ToDoPlannedPercent);
    }

    [Fact]
    public async Task AssignPastTaskAsync_UnknownTarget_Throws()
    {
        var task = AddPastTask("Alt");
        await _viewModel.InitializeAsync();
        var unknownTarget = (PastTaskTarget)99;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _viewModel.AssignPastTaskAsync(task.Id!.Value, unknownTarget));
    }

    [Fact]
    public async Task AssignAllPastTasksToTomorrowAsync_UpdateFails_CapturesTheErrorForEveryTask()
    {
        AddPastTask("Eins");
        AddPastTask("Zwei");
        var failure = new InvalidOperationException("Datenbank gesperrt");
        _taskCache.MoveTaskToDateAsync(Arg.Any<TaskDto>(), Arg.Any<DateTime>()).ThrowsAsync(failure);
        await _viewModel.InitializeAsync();

        await _viewModel.AssignAllPastTasksToTomorrowAsync();

        await _errorCapture.Received(2).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "DayPlannerViewModel.AssignAllPastTasksToTomorrowAsync" &&
                options.IsUiFailure));
    }

    [Fact]
    public async Task AssignAllPastTasksToTomorrowAsync_NoPastTasks_UpdatesNothing()
    {
        AddTask("Heute", isPositive: false, Today);
        await _viewModel.InitializeAsync();

        await _viewModel.AssignAllPastTasksToTomorrowAsync();

        await _taskCache.DidNotReceiveWithAnyArgs().MoveTaskToDateAsync(default!, default);
    }

    [Fact]
    public async Task InitializeAsync_TasksWithoutId_AreNeitherListedNorMoved()
    {
        var withoutId = new TaskDto { Id = null, Note = "Ohne Id", TaskDate = Today.AddDays(-3), CreationDate = Today.AddDays(-4) };
        var todayWithoutId = new TaskDto { Id = null, Note = "Heute ohne Id", TaskDate = Today, CreationDate = Today };
        _normalTasks.Add(withoutId);
        _normalTasks.Add(todayWithoutId);

        await _viewModel.InitializeAsync();
        await _viewModel.AssignAllPastTasksToTomorrowAsync();

        Assert.Empty(_viewModel.PastTasks);
        Assert.Empty(_viewModel.TodayTasks);
        await _taskCache.DidNotReceiveWithAnyArgs().MoveTaskToDateAsync(default!, default);
    }

    [Fact]
    public async Task InitializeAsync_TaskWithoutNote_IsListedWithEmptyText()
    {
        var task = AddTask("   ", isPositive: true, Today);

        await _viewModel.InitializeAsync();

        var listed = Assert.Single(_viewModel.TodayTasks);
        Assert.Equal(task.Id, listed.Id);
        Assert.Equal(string.Empty, listed.Note);
        Assert.True(listed.IsPositive);
    }

    [Fact]
    public async Task Insight_MostlyDutiesOnTodaysWeekday_UsesTheDutiesText()
    {
        _weekdayAnalysis.AnalyzeAsync(Arg.Any<DateTime>()).Returns(new WeekdayAnalysis(
            [],
            4,
            true,
            [new WeekdayInsight(DayOfWeek.Sunday, WeekdayInsightKind.MostlyDuties)]));

        await _viewModel.InitializeAsync();

        Assert.StartsWith("WeekdayInsightMostlyDuties(", _viewModel.Insight!.Text);
    }

    [Fact]
    public async Task Suggestions_AllPoolIdeasHidden_ListsOnlyPersonalOnes()
    {
        var hiddenPool = ForYouSuggestionPool.Entries
            .Select(entry => new ForYouSuggestionDto { Id = Guid.NewGuid(), Text = string.Empty, PoolKey = entry.TextKey, IsDeleted = true })
            .ToList();
        hiddenPool.Add(new ForYouSuggestionDto { Id = Guid.NewGuid(), Text = "Mein Vorschlag" });
        _suggestionService.GetStoredSuggestionsAsync().Returns(hiddenPool);

        await _viewModel.InitializeAsync();

        var suggestion = Assert.Single(_viewModel.Suggestions);
        Assert.Equal("Mein Vorschlag", suggestion.Text);
    }

    [Fact]
    public async Task Suggestions_ManyPersonalOnes_ListAllOfThemBeforePoolIdeas()
    {
        var own = Enumerable.Range(1, 5)
            .Select(number => new ForYouSuggestionDto { Id = Guid.NewGuid(), Text = $"Eigene {number}" })
            .ToList();
        _suggestionService.GetStoredSuggestionsAsync().Returns(own);

        await _viewModel.InitializeAsync();

        Assert.Equal(5 + ForYouSuggestionPool.Entries.Count, _viewModel.Suggestions.Count);
        Assert.All(_viewModel.Suggestions.Take(5), item => Assert.True(item.IsOwn));
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_SaveFails_CapturesTheErrorAndKeepsTheEditorOpen()
    {
        await _viewModel.InitializeAsync();
        _viewModel.OpenTaskEditorForForYou();
        var failure = new InvalidOperationException("Speichern fehlgeschlagen");
        _taskCache.AddTaskAsync(Arg.Any<TaskDto>()).ThrowsAsync(failure);
        var payload = new TaskEditPopUpClosePayload
        {
            Action = TaskEditPopUpCloseAction.Save,
            Task = new TaskDto { Id = Guid.NewGuid(), Note = "Neu" },
            IsUpdateMode = false,
            HasChanges = true
        };

        await _viewModel.HandleTaskEditPopupCloseAsync(payload);

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "DayPlannerViewModel.HandleTaskEditPopupCloseAsync" &&
                options.IsUiFailure));
        Assert.True(_viewModel.IsTaskEditorOpen);
    }

    [Fact]
    public async Task Back_FromTheDoneStep_StaysBecauseTheRunIsOver()
    {
        await _viewModel.InitializeAsync();
        _viewModel.Next();
        _viewModel.Next();
        _viewModel.Next();
        var stepAtTheEnd = _viewModel.Step;

        Assert.Equal(DayPlannerStep.Done, stepAtTheEnd);
        Assert.False(_viewModel.CanGoBack);
    }

    [Fact]
    public async Task UserSettingsChanged_RewardingChanged_ReloadsTheDay()
    {
        await _viewModel.InitializeAsync();
        SetPlanned(2, 1);

        _rewardingSettings.RewardingChanged += Raise.Event();
        await WaitForAsync(_viewModel, () => _viewModel.PlannedForYou == 2);

        Assert.Equal(2, _viewModel.PlannedForYou);
        Assert.Equal(1, _viewModel.PlannedToDo);
    }

    [Fact]
    public async Task Back_FromTheDutiesStepWithGoalReached_GoesBackThroughTheForYouStep()
    {
        AddPastTask("Alt");
        SetPlanned(1, 1);
        await _viewModel.InitializeAsync();
        _viewModel.Next();
        _viewModel.Next();
        _viewModel.Next();
        Assert.Equal(DayPlannerStep.Duties, _viewModel.Step);

        _viewModel.Back();
        Assert.Equal(DayPlannerStep.ForYou, _viewModel.Step);

        _viewModel.Back();
        Assert.Equal(DayPlannerStep.Assign, _viewModel.Step);
    }

    [Fact]
    public async Task AssignPastTaskAsync_SeveralTasks_MovesOnlyTheRequestedOne()
    {
        var first = AddPastTask("Eins");
        var second = AddPastTask("Zwei");
        await _viewModel.InitializeAsync();

        await _viewModel.AssignPastTaskAsync(second.Id!.Value, PastTaskTarget.Tomorrow);

        await _taskCache.Received(1).MoveTaskToDateAsync(Arg.Is<TaskDto>(item => item.Id == second.Id), Arg.Any<DateTime>());
        await _taskCache.DidNotReceive().MoveTaskToDateAsync(Arg.Is<TaskDto>(item => item.Id == first.Id), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task InitializeAsync_OpenTaskWithoutTaskDate_IsNotListedForToday()
    {
        var undated = new TaskDto { Id = Guid.NewGuid(), Note = "Ohne Tag", TaskDate = null, CreationDate = Today };
        _normalTasks.Add(undated);

        await _viewModel.InitializeAsync();

        Assert.Empty(_viewModel.TodayTasks);
    }

    [Fact]
    public async Task Suggestions_OnlyTwoPoolIdeasLeft_ListsJustThose()
    {
        var keepKeys = ForYouSuggestionPool.Entries.Take(2).Select(entry => entry.TextKey).ToList();
        var hidden = ForYouSuggestionPool.Entries
            .Where(entry => !keepKeys.Contains(entry.TextKey))
            .Select(entry => new ForYouSuggestionDto { Id = Guid.NewGuid(), Text = string.Empty, PoolKey = entry.TextKey, IsDeleted = true })
            .ToList();
        _suggestionService.GetStoredSuggestionsAsync().Returns(hidden);

        await _viewModel.InitializeAsync();

        Assert.Equal(2, _viewModel.Suggestions.Count);
    }

    private void SetPlanned(int forYou, int toDo, int doneForYou = 0, int doneToDo = 0) =>
        _balance.GetTodayTaskCountAsync(Arg.Any<bool>()).Returns(new DayTaskCount(Today, forYou, toDo, doneForYou, doneToDo));

    private TaskDto AddPastTask(string note, bool isPositive = false) => AddTask(note, isPositive, Today.AddDays(-3));

    private TaskDto AddTask(string note, bool isPositive, DateTime date)
    {
        var task = new TaskDto
        {
            Id = Guid.NewGuid(),
            Note = note,
            IsPositive = isPositive,
            TaskDate = date,
            CreationDate = date.AddDays(-1)
        };
        _normalTasks.Add(task);
        return task;
    }

    private sealed class KeyTextProvider : IUiTextProvider
    {
        public string GetText(string key, params object[] arguments) => $"{key}({string.Join(", ", arguments)})";
    }
}
