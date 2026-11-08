using FokusApp.Components.ForYouSuggestions;
using FokusApp.Components.PopUps.TaskEdit;
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

public class PlantHelperViewModelTests
{
    private static readonly DateTime Today = new(2026, 3, 15);

    private readonly IBalanceForYouToDoService _balance = Substitute.For<IBalanceForYouToDoService>();
    private readonly IRewardingSettingsService _rewardingSettings = TestSettings.Create();
    private readonly IForYouSuggestionService _suggestionService = Substitute.For<IForYouSuggestionService>();
    private readonly ITaskCacheService _taskCache = Substitute.For<ITaskCacheService>();
    private readonly IScopeService _scopeService = Substitute.For<IScopeService>();
    private readonly ITaskPriorityFormatter _priorityFormatter = Substitute.For<ITaskPriorityFormatter>();
    private readonly ITaskSeriesService _taskSeriesService = Substitute.For<ITaskSeriesService>();
    private readonly IAppStartupStateService _startupState = Substitute.For<IAppStartupStateService>();
    private readonly IDayChangeService _dayChange = Substitute.For<IDayChangeService>();
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly ICompanionMoodService _moodService = Substitute.For<ICompanionMoodService>();
    private FakeTimeProvider _clock = null!;
    private PlantHelperViewModel _viewModel = null!;

    public PlantHelperViewModelTests()
    {
        SetToday(plannedForYou: 0, doneForYou: 0);
        _suggestionService.GetStoredSuggestionsAsync().Returns(new List<ForYouSuggestionDto>());
        _taskCache.GetTemplates(Arg.Any<DateTime?>()).Returns(new List<TaskDto>());
        _scopeService.IsWorkScope.Returns(true);
        _moodService.GetRecentMoodAsync().Returns(CompanionMood.Awake);

        Build(new DateTimeOffset(2026, 3, 15, 13, 0, 0, TimeSpan.Zero));
    }

    private void Build(DateTimeOffset now)
    {
        _clock = new FakeTimeProvider(now);
        var textProvider = new KeyTextProvider();
        var taskEditor = new TaskEditorCoordinator(_scopeService, _taskCache, textProvider, _priorityFormatter, _taskSeriesService, _clock);
        _viewModel = new PlantHelperViewModel(
            _balance,
            _rewardingSettings,
            _suggestionService,
            _taskCache,
            _scopeService,
            taskEditor,
            textProvider,
            _startupState,
            _dayChange,
            _errorCapture,
            _clock,
            _moodService);
    }

    [Fact]
    public async Task InitializeAsync_AppDataLoading_WaitsForStartupAndReadsToday()
    {
        await _viewModel.InitializeAsync();

        await _startupState.Received(1).WaitForReadyAsync(Arg.Any<CancellationToken>());
        await _balance.Received(1).GetTodayTaskCountAsync(true);
    }

    [Fact]
    public async Task IsBubbleVisible_AfterNoonWithoutForYouToday_IsTrue()
    {
        await _viewModel.InitializeAsync();

        Assert.True(_viewModel.IsPlantVisible);
        Assert.True(_viewModel.IsBubbleVisible);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(18)]
    public async Task BubbleKind_NothingPlannedOrDone_InvitesToPlanTheDayAtAnyTime(int hour)
    {
        Build(new DateTimeOffset(2026, 3, 15, hour, 0, 0, TimeSpan.Zero));
        SetToday(plannedForYou: 0, doneForYou: 0, plannedToDo: 0);

        await _viewModel.InitializeAsync();

        Assert.Equal(PlantBubbleKind.PlanDay, _viewModel.BubbleKind);
        Assert.True(_viewModel.IsBubbleVisible);
    }

    [Fact]
    public async Task BubbleKind_BeforeNoonWithOnlyAToDo_IsNone()
    {
        Build(new DateTimeOffset(2026, 3, 15, 11, 59, 0, TimeSpan.Zero));
        SetToday(plannedForYou: 0, doneForYou: 0, plannedToDo: 1);

        await _viewModel.InitializeAsync();

        Assert.Equal(PlantBubbleKind.None, _viewModel.BubbleKind);
        Assert.False(_viewModel.IsBubbleVisible);
    }

    [Fact]
    public async Task BubbleKind_NoonWithOnlyAToDo_AsksForAForYouInsteadOfPlanning()
    {
        SetToday(plannedForYou: 0, doneForYou: 0, plannedToDo: 1);

        await _viewModel.InitializeAsync();

        Assert.Equal(PlantBubbleKind.ForYou, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task BubbleKind_OtherScopeHasTasksButThisOneNone_InvitesToPlanTheDay()
    {
        _scopeService.IsWorkScope.Returns(false);
        SetCount(isWorkScope: true, forYou: 2, toDo: 3);
        SetCount(isWorkScope: false, forYou: 0, toDo: 0);

        await _viewModel.InitializeAsync();

        Assert.Equal(PlantBubbleKind.PlanDay, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task BubbleKind_OtherScopeHasNoForYouButThisOneHas_IsNone()
    {
        _scopeService.IsWorkScope.Returns(false);
        SetCount(isWorkScope: true, forYou: 0, toDo: 3);
        SetCount(isWorkScope: false, forYou: 1, toDo: 0);

        await _viewModel.InitializeAsync();

        Assert.Equal(PlantBubbleKind.None, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task ScopeChanged_OtherScopeIsEmpty_SwitchesTheBubble()
    {
        SetCount(isWorkScope: true, forYou: 1, toDo: 1);
        SetCount(isWorkScope: false, forYou: 0, toDo: 0);
        await _viewModel.InitializeAsync();
        Assert.Equal(PlantBubbleKind.None, _viewModel.BubbleKind);

        _scopeService.IsWorkScope.Returns(false);
        _scopeService.ScopeChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.BubbleKind == PlantBubbleKind.PlanDay);
        Assert.Equal(PlantBubbleKind.PlanDay, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task DismissBubble_InOneScope_DoesNotHideTheBubbleOfTheOtherScope()
    {
        SetCount(isWorkScope: true, forYou: 0, toDo: 0);
        SetCount(isWorkScope: false, forYou: 0, toDo: 0);
        await _viewModel.InitializeAsync();
        _viewModel.DismissBubble();
        Assert.Equal(PlantBubbleKind.None, _viewModel.BubbleKind);

        _scopeService.IsWorkScope.Returns(false);
        _scopeService.ScopeChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.BubbleKind == PlantBubbleKind.PlanDay);
        Assert.Equal(PlantBubbleKind.PlanDay, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task TasksChanged_TaskPlannedAfterNoon_TakesAwayThePlanDayBubble()
    {
        SetToday(plannedForYou: 0, doneForYou: 0, plannedToDo: 0);
        await _viewModel.InitializeAsync();
        Assert.Equal(PlantBubbleKind.PlanDay, _viewModel.BubbleKind);
        SetToday(plannedForYou: 0, doneForYou: 0, plannedToDo: 1);

        _taskCache.NormalTasksChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.BubbleKind != PlantBubbleKind.PlanDay);
        Assert.Equal(PlantBubbleKind.ForYou, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task BubbleKind_NoonWithForYouPlanned_IsNone()
    {
        SetToday(plannedForYou: 1, doneForYou: 0);

        await _viewModel.InitializeAsync();

        Assert.Equal(PlantBubbleKind.None, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task BubbleKind_AfterNoonWithoutForYou_AsksForAForYou()
    {
        await _viewModel.InitializeAsync();

        Assert.Equal(PlantBubbleKind.ForYou, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task DismissBubble_PlanDayBubble_StaysAwayForTheRestOfTheDay()
    {
        SetToday(plannedForYou: 0, doneForYou: 0, plannedToDo: 0);
        await _viewModel.InitializeAsync();
        _viewModel.DismissBubble();
        Assert.Equal(PlantBubbleKind.None, _viewModel.BubbleKind);

        _clock.Advance(TimeSpan.FromHours(4));

        Assert.Equal(PlantBubbleKind.None, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task DismissBubble_ForYouBubble_StaysAwayForTheRestOfTheDay()
    {
        await _viewModel.InitializeAsync();

        _viewModel.DismissBubble();
        _clock.Advance(TimeSpan.FromHours(3));

        Assert.Equal(PlantBubbleKind.None, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task IsBubbleVisible_ExactlyNoon_IsTrue()
    {
        Build(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero));

        await _viewModel.InitializeAsync();

        Assert.True(_viewModel.IsBubbleVisible);
    }

    [Fact]
    public async Task IsBubbleVisible_ForYouPlannedToday_IsFalse()
    {
        SetToday(plannedForYou: 1, doneForYou: 0);

        await _viewModel.InitializeAsync();

        Assert.False(_viewModel.IsBubbleVisible);
    }

    [Fact]
    public async Task IsBubbleVisible_ForYouDoneToday_IsFalse()
    {
        SetToday(plannedForYou: 0, doneForYou: 1);

        await _viewModel.InitializeAsync();

        Assert.False(_viewModel.IsBubbleVisible);
    }

    [Fact]
    public async Task IsBubbleVisible_OnlyToDoToday_IsStillTrue()
    {
        SetToday(plannedForYou: 0, doneForYou: 0, plannedToDo: 4);

        await _viewModel.InitializeAsync();

        Assert.True(_viewModel.IsBubbleVisible);
    }

    [Fact]
    public async Task IsBubbleVisible_NotLoadedYet_IsFalse()
    {
        Assert.False(_viewModel.IsBubbleVisible);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task IsPlantVisible_CompanionSwitchedOff_HidesPlantAndBubble()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { CompanionEnabled = false });

        await _viewModel.InitializeAsync();

        Assert.False(_viewModel.IsPlantVisible);
        Assert.False(_viewModel.IsBubbleVisible);
    }

    [Fact]
    public async Task RewardingChanged_CompanionSwitchedOffAndOn_FollowsTheSetting()
    {
        await _viewModel.InitializeAsync();
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { CompanionEnabled = false });

        _rewardingSettings.RewardingChanged += Raise.EventWith(EventArgs.Empty);

        Assert.False(_viewModel.IsPlantVisible);
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default);
        _rewardingSettings.RewardingChanged += Raise.EventWith(EventArgs.Empty);
        Assert.True(_viewModel.IsBubbleVisible);
    }

    [Fact]
    public async Task Timer_NoonPasses_ShowsTheBubbleWithoutAnyOtherEvent()
    {
        Build(new DateTimeOffset(2026, 3, 15, 11, 59, 0, TimeSpan.Zero));
        await _viewModel.InitializeAsync();
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        _clock.Advance(TimeSpan.FromMinutes(2));

        Assert.Contains(nameof(IPlantHelperViewModel.BubbleKind), changed);
        Assert.Equal(PlantBubbleKind.ForYou, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task DismissBubble_Today_HidesItUntilTomorrowButKeepsThePlant()
    {
        await _viewModel.InitializeAsync();

        _viewModel.DismissBubble();

        Assert.False(_viewModel.IsBubbleVisible);
        Assert.True(_viewModel.IsPlantVisible);

        _clock.SetUtcNow(new DateTimeOffset(2026, 3, 16, 13, 0, 0, TimeSpan.Zero));
        _dayChange.DayChanged += Raise.EventWith(new DayChangedEventArgs(Today, Today.AddDays(1)));
        await WaitForAsync(_viewModel, () => _viewModel.IsBubbleVisible);

        Assert.True(_viewModel.IsBubbleVisible);
    }

    [Fact]
    public async Task IsPlantVisible_OldStoredHiddenDayForToday_IsIgnored()
    {
        await _viewModel.InitializeAsync();

        Assert.True(_viewModel.IsPlantVisible);
    }

    [Fact]
    public async Task IsPlantVisible_CompanionSwitchedOffInTheSettings_IsHidden()
    {
        await _viewModel.InitializeAsync();

        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { CompanionEnabled = false });
        _rewardingSettings.RewardingChanged += Raise.EventWith(EventArgs.Empty);

        Assert.False(_viewModel.IsPlantVisible);
        Assert.False(_viewModel.IsBubbleVisible);
    }

    [Fact]
    public async Task TasksChanged_ForYouAdded_HidesTheBubble()
    {
        await _viewModel.InitializeAsync();
        SetToday(plannedForYou: 1, doneForYou: 0);

        _taskCache.NormalTasksChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => !_viewModel.IsBubbleVisible);
        Assert.False(_viewModel.IsBubbleVisible);
    }

    [Theory]
    [InlineData(CompanionMood.Resting)]
    [InlineData(CompanionMood.Awake)]
    [InlineData(CompanionMood.Budding)]
    [InlineData(CompanionMood.Blooming)]
    public async Task Mood_FromTheRecentDays_IsExposed(CompanionMood mood)
    {
        _moodService.GetRecentMoodAsync().Returns(mood);

        await _viewModel.InitializeAsync();

        Assert.Equal(mood, _viewModel.Mood);
    }

    [Fact]
    public void Mood_BeforeLoading_RestsFriendly()
    {
        Assert.Equal(CompanionMood.Resting, _viewModel.Mood);
    }

    [Fact]
    public async Task TasksChanged_BalanceDayCompleted_UpdatesTheMood()
    {
        await _viewModel.InitializeAsync();
        _moodService.GetRecentMoodAsync().Returns(CompanionMood.Budding);

        _taskCache.DoneTasksChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.Mood == CompanionMood.Budding);
        Assert.Equal(CompanionMood.Budding, _viewModel.Mood);
    }

    [Fact]
    public async Task OpenSuggestions_Tap_OpensTheListAndCloseClosesIt()
    {
        await _viewModel.InitializeAsync();

        _viewModel.OpenSuggestions();
        Assert.True(_viewModel.IsSuggestionListOpen);

        _viewModel.CloseSuggestions();
        Assert.False(_viewModel.IsSuggestionListOpen);
    }

    [Fact]
    public async Task Suggestions_OwnAndPool_AreListedWithOwnFirst()
    {
        _suggestionService.GetStoredSuggestionsAsync()
            .Returns(new List<ForYouSuggestionDto> { new() { Id = Guid.NewGuid(), Text = "Mein Vorschlag" } });

        await _viewModel.InitializeAsync();

        Assert.Equal("Mein Vorschlag", _viewModel.Suggestions[0].Text);
        Assert.True(_viewModel.Suggestions[0].IsOwn);
        Assert.Equal(1 + ForYouSuggestionPool.Entries.Count, _viewModel.Suggestions.Count);
    }

    [Fact]
    public async Task SuggestionsChanged_OwnSuggestionAdded_RefreshesTheList()
    {
        await _viewModel.InitializeAsync();
        _suggestionService.GetStoredSuggestionsAsync()
            .Returns(new List<ForYouSuggestionDto> { new() { Id = Guid.NewGuid(), Text = "Neu" } });

        _suggestionService.SuggestionsChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.Suggestions[0].Text == "Neu");
        Assert.Equal("Neu", _viewModel.Suggestions[0].Text);
    }

    [Fact]
    public async Task PickSuggestion_Text_ClosesTheListAndOpensTheEditorForANewForYouTaskForToday()
    {
        await _viewModel.InitializeAsync();
        _viewModel.OpenSuggestions();

        _viewModel.PickSuggestion(Item("Spazieren gehen"));

        Assert.False(_viewModel.IsSuggestionListOpen);
        Assert.True(_viewModel.IsTaskEditorOpen);
        var input = Assert.IsType<TaskEditPopUpInputModel>(_viewModel.TaskEditorInput);
        var task = Assert.IsType<TaskDto>(input.Task);
        Assert.Null(task.Id);
        Assert.Equal("Spazieren gehen", task.Note);
        Assert.True(task.IsPositive);
        Assert.False(task.IsTemplate);
        Assert.Equal(Today, task.TaskDate);
        Assert.Equal(Today, input.ContextualTaskDate);
    }

    [Fact]
    public async Task Suggestions_ForYouTemplates_AreListedFirstAndToDoTemplatesAreNot()
    {
        var forYou = Template("Spazieren", isPositive: true);
        var toDo = Template("Steuer", isPositive: false);
        _taskCache.GetTemplates(Arg.Any<DateTime?>()).Returns(new List<TaskDto> { toDo, forYou });

        await _viewModel.InitializeAsync();

        Assert.Equal("Spazieren", _viewModel.Suggestions[0].Text);
        Assert.Equal(forYou.Id, _viewModel.Suggestions[0].TemplateId);
        Assert.DoesNotContain(_viewModel.Suggestions, item => item.Text == "Steuer");
    }

    [Fact]
    public async Task TemplatesChanged_TemplateAdded_RefreshesTheList()
    {
        await _viewModel.InitializeAsync();
        var template = Template("Yoga", isPositive: true);
        _taskCache.GetTemplates(Arg.Any<DateTime?>()).Returns(new List<TaskDto> { template });

        _taskCache.TemplatesChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.Suggestions[0].TemplateId == template.Id);
        Assert.Equal("Yoga", _viewModel.Suggestions[0].Text);
    }

    [Fact]
    public async Task PickSuggestion_Template_OpensTheEditorWithACopyOfTheTemplateForToday()
    {
        var template = Template("Spazieren", isPositive: true);
        template.TimerMinutes = 20;
        _taskCache.GetTemplates(Arg.Any<DateTime?>()).Returns(new List<TaskDto> { template });
        await _viewModel.InitializeAsync();

        _viewModel.PickSuggestion(_viewModel.Suggestions[0]);

        Assert.True(_viewModel.IsTaskEditorOpen);
        var task = _viewModel.TaskEditorInput!.Task!;
        Assert.Null(task.Id);
        Assert.Equal("Spazieren", task.Note);
        Assert.True(task.IsPositive);
        Assert.False(task.IsTemplate);
        Assert.Equal(20, task.TimerMinutes);
        Assert.Equal(Today, task.TaskDate);
        Assert.False(_viewModel.IsSuggestionListOpen);
    }

    [Fact]
    public async Task PickSuggestion_TemplateGoneMeanwhile_FallsBackToANewTaskWithTheText()
    {
        await _viewModel.InitializeAsync();

        _viewModel.PickSuggestion(new ForYouSuggestionItem("Spazieren", false, null, Guid.NewGuid()));

        var task = _viewModel.TaskEditorInput!.Task!;
        Assert.Equal("Spazieren", task.Note);
        Assert.True(task.IsPositive);
    }

    [Fact]
    public async Task PickSuggestion_PrivateScope_CreatesAPrivateTask()
    {
        _scopeService.IsWorkScope.Returns(false);
        await _viewModel.InitializeAsync();

        _viewModel.PickSuggestion(Item("Lesen"));

        Assert.False(_viewModel.TaskEditorInput!.Task!.IsWorkTask);
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_Cancel_ClosesTheEditor()
    {
        await _viewModel.InitializeAsync();
        _viewModel.PickSuggestion(Item("Lesen"));

        await _viewModel.HandleTaskEditPopupCloseAsync(new TaskEditPopUpClosePayload { Action = TaskEditPopUpCloseAction.Cancel });

        Assert.False(_viewModel.IsTaskEditorOpen);
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_Save_AddsTheTaskAndClosesTheEditor()
    {
        await _viewModel.InitializeAsync();
        _viewModel.PickSuggestion(Item("Lesen"));
        var task = _viewModel.TaskEditorInput!.Task!;
        task.TimerMinutes = 15;

        await _viewModel.HandleTaskEditPopupCloseAsync(new TaskEditPopUpClosePayload
        {
            Action = TaskEditPopUpCloseAction.Save,
            Task = task,
            IsUpdateMode = false
        });

        await _taskCache.Received(1).AddTaskAsync(Arg.Is<TaskDto>(item => item.Note == "Lesen" && item.IsPositive));
        Assert.False(_viewModel.IsTaskEditorOpen);
    }

    [Fact]
    public async Task InitializeAsync_LoadingFails_CapturesErrorAndShowsNoBubble()
    {
        var failure = new InvalidOperationException("Datenbank gesperrt");
        _balance.GetTodayTaskCountAsync(Arg.Any<bool>()).ThrowsAsync(failure);

        await _viewModel.InitializeAsync();

        Assert.False(_viewModel.IsBubbleVisible);
        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null && options.Source == "PlantHelperViewModel.RefreshAsync" && options.IsUiFailure));
    }

    [Fact]
    public async Task Dispose_AfterDispose_IgnoresEventsAndStopsTheTimer()
    {
        await _viewModel.InitializeAsync();
        _viewModel.Dispose();
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        _taskCache.NormalTasksChanged += Raise.Event();
        _clock.Advance(TimeSpan.FromMinutes(5));
        await Task.Delay(50);

        Assert.Empty(changed);
        await _balance.Received(1).GetTodayTaskCountAsync(true);
    }

    [Fact]
    public async Task HandleTaskEditPopupCloseAsync_SaveFails_CapturesTheErrorAndKeepsTheEditorOpen()
    {
        await _viewModel.InitializeAsync();
        _viewModel.PickSuggestion(Item("Lesen"));
        var task = _viewModel.TaskEditorInput!.Task!;
        var failure = new InvalidOperationException("Speichern fehlgeschlagen");
        _taskCache.AddTaskAsync(Arg.Any<TaskDto>()).ThrowsAsync(failure);

        await _viewModel.HandleTaskEditPopupCloseAsync(new TaskEditPopUpClosePayload
        {
            Action = TaskEditPopUpCloseAction.Save,
            Task = task,
            IsUpdateMode = false
        });

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "PlantHelperViewModel.HandleTaskEditPopupCloseAsync" &&
                options.IsUiFailure));
        Assert.True(_viewModel.IsTaskEditorOpen);
    }

    [Fact]
    public void Dispose_BeforeInitialization_CompletesWithoutError()
    {
        var exception = Record.Exception(() => _viewModel.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public async Task TapPlant_ExactlyTheRatio_SaysTheGoalIsReached()
    {
        SetCount(isWorkScope: true, forYou: 2, toDo: 2);
        await _viewModel.InitializeAsync();

        _viewModel.TapPlant();

        Assert.Equal(PlantBubbleKind.GoalReached, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task TapPlant_MoreForYouThanTheRatio_SaysTheGoalIsExceeded()
    {
        SetCount(isWorkScope: true, forYou: 3, toDo: 2);
        await _viewModel.InitializeAsync();

        _viewModel.TapPlant();

        Assert.Equal(PlantBubbleKind.GoalExceeded, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task TapPlant_ForYouWithoutAnyToDo_IsFineAndSaysTheGoalIsExceeded()
    {
        SetCount(isWorkScope: true, forYou: 1, toDo: 0);
        await _viewModel.InitializeAsync();

        _viewModel.TapPlant();

        Assert.Equal(PlantBubbleKind.GoalExceeded, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task TapPlant_GoalNotReached_AsksForAnotherForYou()
    {
        SetCount(isWorkScope: true, forYou: 1, toDo: 3);
        await _viewModel.InitializeAsync();

        _viewModel.TapPlant();

        Assert.Equal(PlantBubbleKind.AskForYou, _viewModel.BubbleKind);
        Assert.False(_viewModel.IsSuggestionListOpen);
    }

    [Fact]
    public async Task TapPlant_NothingPlannedOrDoneYet_AsksForAnotherForYou()
    {
        SetCount(isWorkScope: true, forYou: 0, toDo: 0);
        await _viewModel.InitializeAsync();

        _viewModel.TapPlant();

        Assert.Equal(PlantBubbleKind.AskForYou, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task TapPlant_OnlyToDos_AsksForAnotherForYou()
    {
        SetCount(isWorkScope: true, forYou: 0, toDo: 2);
        await _viewModel.InitializeAsync();

        _viewModel.TapPlant();

        Assert.Equal(PlantBubbleKind.AskForYou, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task TapPlant_NotLoadedYet_AsksForAnotherForYou()
    {
        _viewModel.TapPlant();

        Assert.Equal(PlantBubbleKind.AskForYou, _viewModel.BubbleKind);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task TapPlant_UsesTheCurrentScopeOnly()
    {
        SetCount(isWorkScope: true, forYou: 1, toDo: 1);
        SetCount(isWorkScope: false, forYou: 0, toDo: 4);
        _scopeService.IsWorkScope.Returns(false);
        await _viewModel.InitializeAsync();

        _viewModel.TapPlant();

        Assert.Equal(PlantBubbleKind.AskForYou, _viewModel.BubbleKind);
        await _balance.Received().GetTodayTaskCountAsync(false);
        await _balance.DidNotReceive().GetTodayTaskCountAsync(true);
    }

    [Fact]
    public async Task TapPlant_ScopeSwitchedWhileOpen_EvaluatesTheNewScope()
    {
        SetCount(isWorkScope: true, forYou: 1, toDo: 1);
        SetCount(isWorkScope: false, forYou: 0, toDo: 4);
        await _viewModel.InitializeAsync();
        _viewModel.TapPlant();
        Assert.Equal(PlantBubbleKind.GoalReached, _viewModel.BubbleKind);

        _scopeService.IsWorkScope.Returns(false);
        _scopeService.ScopeChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.BubbleKind == PlantBubbleKind.AskForYou);
        Assert.Equal(PlantBubbleKind.AskForYou, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task TapPlant_GoalFromTheSettings_DecidesBetweenReachedAndExceeded()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { BalanceGoal = BalanceGoal.TwoToThree });
        SetCount(isWorkScope: true, forYou: 2, toDo: 3);
        await _viewModel.InitializeAsync();

        _viewModel.TapPlant();
        Assert.Equal(PlantBubbleKind.GoalReached, _viewModel.BubbleKind);

        SetCount(isWorkScope: true, forYou: 3, toDo: 3);
        _taskCache.NormalTasksChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.BubbleKind == PlantBubbleKind.GoalExceeded);
        Assert.Equal(PlantBubbleKind.GoalExceeded, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task TapPlant_GoalChangedInTheSettingsWhileOpen_EvaluatesAgain()
    {
        SetCount(isWorkScope: true, forYou: 2, toDo: 3);
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default);
        await _viewModel.InitializeAsync();
        _viewModel.TapPlant();
        Assert.Equal(PlantBubbleKind.AskForYou, _viewModel.BubbleKind);

        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { BalanceGoal = BalanceGoal.TwoToThree });
        _rewardingSettings.RewardingChanged += Raise.EventWith(EventArgs.Empty);

        Assert.Equal(PlantBubbleKind.GoalReached, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task TapPlant_SecondTap_TakesTheBubbleAway()
    {
        SetCount(isWorkScope: true, forYou: 2, toDo: 2);
        await _viewModel.InitializeAsync();
        _viewModel.TapPlant();

        _viewModel.TapPlant();

        Assert.Equal(PlantBubbleKind.None, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task TapPlant_WhileThePlanDayBubbleShows_ShowsTheEvaluationInstead()
    {
        SetCount(isWorkScope: true, forYou: 0, toDo: 0);
        await _viewModel.InitializeAsync();
        Assert.Equal(PlantBubbleKind.PlanDay, _viewModel.BubbleKind);

        _viewModel.TapPlant();

        Assert.Equal(PlantBubbleKind.AskForYou, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task DismissBubble_TapBubble_OnlyClosesItAndKeepsTheDailyBubble()
    {
        SetCount(isWorkScope: true, forYou: 0, toDo: 0);
        await _viewModel.InitializeAsync();
        _viewModel.TapPlant();

        _viewModel.DismissBubble();

        Assert.Equal(PlantBubbleKind.PlanDay, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task TapPlant_Tap_RaisesBubbleChanged()
    {
        await _viewModel.InitializeAsync();
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        _viewModel.TapPlant();

        Assert.Contains(nameof(IPlantHelperViewModel.BubbleKind), changed);
        Assert.Contains(nameof(IPlantHelperViewModel.IsBubbleVisible), changed);
    }

    [Fact]
    public async Task OpenSuggestions_FromTheAskForYouBubble_ClosesTheBubbleAndOpensTheList()
    {
        SetCount(isWorkScope: true, forYou: 0, toDo: 2);
        await _viewModel.InitializeAsync();
        _viewModel.TapPlant();
        Assert.Equal(PlantBubbleKind.AskForYou, _viewModel.BubbleKind);

        _viewModel.OpenSuggestions();

        Assert.True(_viewModel.IsSuggestionListOpen);
        Assert.Equal(PlantBubbleKind.ForYou, _viewModel.BubbleKind);
    }

    [Fact]
    public async Task TapPlant_CompanionSwitchedOff_ShowsNoBubble()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { CompanionEnabled = false });
        await _viewModel.InitializeAsync();

        _viewModel.TapPlant();

        Assert.Equal(PlantBubbleKind.None, _viewModel.BubbleKind);
    }

    private static ForYouSuggestionItem Item(string text) => new(text, false, ForYouSuggestionCategory.Calm);

    private static TaskDto Template(string note, bool isPositive) => new()
    {
        Id = Guid.NewGuid(),
        Note = note,
        IsPositive = isPositive,
        IsTemplate = true,
        TaskDate = null
    };

    private void SetToday(int plannedForYou, int doneForYou, int plannedToDo = 1)
    {
        _balance.GetTodayTaskCountAsync(Arg.Any<bool>()).Returns(new DayTaskCount(Today, plannedForYou + doneForYou, plannedToDo));
    }

    private void SetCount(bool isWorkScope, int forYou, int toDo)
    {
        _balance.GetTodayTaskCountAsync(isWorkScope).Returns(new DayTaskCount(Today, forYou, toDo));
    }

    private sealed class KeyTextProvider : IUiTextProvider
    {
        public string GetText(string key, params object[] arguments) => key;
    }
}
