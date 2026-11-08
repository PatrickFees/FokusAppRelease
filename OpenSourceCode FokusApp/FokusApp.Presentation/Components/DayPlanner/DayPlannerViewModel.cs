using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Components.ForYouSuggestions;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Components.TaskOverViewComponent;
using FokusApp.Core.Enums;
using FokusApp.Core.Extensions;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;
using Microsoft.AspNetCore.Components;

namespace FokusApp.Components.DayPlanner;

public sealed partial class DayPlannerViewModel : ObservableObject, IDayPlannerViewModel, IDisposable
{
    private const string CalendarRoute = "/";

    private readonly ITaskCacheService _taskCacheService;
    private readonly IActiveTaskService _activeTaskService;
    private readonly IBalanceForYouToDoService _balanceService;
    private readonly IRewardingSettingsService _rewardingSettingsService;
    private readonly IWeekdayAnalysisService _weekdayAnalysisService;
    private readonly IForYouSuggestionService _suggestionService;
    private readonly TaskEditorCoordinator _taskEditor;
    private readonly IUiTextProvider _textProvider;
    private readonly IAppStartupStateService _appStartupStateService;
    private readonly IDayChangeService _dayChangeService;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly IScopeService _scopeService;
    private readonly ITaskSeriesService _taskSeriesService;
    private readonly NavigationManager _navigationManager;
    private readonly TimeProvider _timeProvider;

    private readonly UndoCoordinator _undo;

    private Guid? _rescheduleTaskId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Step))]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    private IReadOnlyList<DayPlannerStep> steps = [DayPlannerStep.Overview, DayPlannerStep.Done];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Step))]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    private int stepIndex;

    private DayTaskCount? _day;
    private int _loadVersion;

    private readonly Dictionary<bool, (int ForYou, int ToDo)> _baselines = [];
    private (int ForYou, int ToDo) _baseline;

    public DayPlannerViewModel(
        ITaskCacheService taskCacheService,
        IActiveTaskService activeTaskService,
        IBalanceForYouToDoService balanceService,
        IRewardingSettingsService rewardingSettingsService,
        IWeekdayAnalysisService weekdayAnalysisService,
        IForYouSuggestionService suggestionService,
        TaskEditorCoordinator taskEditor,
        IUiTextProvider textProvider,
        IAppStartupStateService appStartupStateService,
        IDayChangeService dayChangeService,
        IAppErrorCaptureService appErrorCaptureService,
        IScopeService scopeService,
        ITaskSeriesService taskSeriesService,
        NavigationManager navigationManager,
        TimeProvider timeProvider)
    {
        _taskCacheService = taskCacheService;
        _activeTaskService = activeTaskService;
        _balanceService = balanceService;
        _rewardingSettingsService = rewardingSettingsService;
        _weekdayAnalysisService = weekdayAnalysisService;
        _suggestionService = suggestionService;
        _taskEditor = taskEditor;
        _textProvider = textProvider;
        _appStartupStateService = appStartupStateService;
        _dayChangeService = dayChangeService;
        _appErrorCaptureService = appErrorCaptureService;
        _scopeService = scopeService;
        _taskSeriesService = taskSeriesService;
        _navigationManager = navigationManager;
        _timeProvider = timeProvider;
        _undo = new UndoCoordinator(timeProvider, () => OnPropertyChanged(nameof(CurrentUndo)));

        _taskCacheService.NormalTasksChanged += OnTasksChanged;
        _taskCacheService.DoneTasksChanged += OnTasksChanged;
        _taskCacheService.TemplatesChanged += OnTasksChanged;
        _rewardingSettingsService.RewardingChanged += OnTasksChanged;
        _scopeService.ScopeChanged += OnTasksChanged;
        _dayChangeService.DayChanged += OnDayChanged;
        _taskEditor.StateChanged += OnTaskEditorStateChanged;
    }

    public bool IsLoaded { get; private set; }

    public DayPlannerStep Step => Steps[StepIndex];

    public bool CanGoBack => StepIndex > 0 && Step != DayPlannerStep.Done;

    public IReadOnlyList<DayPlannerTask> TodayTasks { get; private set; } = [];

    public int PlannedForYou => _day?.ForYou ?? 0;

    public int PlannedToDo => _day?.ToDo ?? 0;

    public int DoneForYou => _day?.DoneForYou ?? 0;

    public int DoneToDo => _day?.DoneToDo ?? 0;

    public int PlannedTotal => PlannedForYou + PlannedToDo;

    public string OverviewText => PlannedTotal == 0
        ? _textProvider.GetText("PlannerOverviewEmpty")
        : _textProvider.GetText("PlannerOverviewSummary", PlannedForYou, PlannedToDo);

    public int GoalForYou => GoalParts.ForYou;

    public int GoalToDo => GoalParts.ToDo;

    public GoalProgress GoalProgress
    {
        get
        {
            var rewarding = _rewardingSettingsService.Rewarding;
            return BalanceForYouToDoRules.GetProgress(
                PlannedForYou,
                PlannedToDo,
                rewarding.BalanceGoal,
                rewarding.CustomForYou,
                rewarding.CustomToDo);
        }
    }

    public bool IsGoalReached => GoalProgress != GoalProgress.Open;

    public int RequiredForYou
    {
        get
        {

            var (partForYou, partToDo) = GoalParts;
            return Math.Max(1, (PlannedToDo * partForYou + partToDo - 1) / partToDo);
        }
    }

    public int AllowedToDo
    {
        get
        {

            var (partForYou, partToDo) = GoalParts;
            return PlannedForYou * partToDo / partForYou;
        }
    }

    public double ForYouDonePercent => BarPercent(DoneForYou);

    public double ForYouExistingPercent => BarPercent(ForYouLayers.Existing);

    public double ForYouPlannedStartPercent => BarPercent(ForYouLayers.PlannedStart);

    public double ForYouPlannedPercent => BarPercent(ForYouLayers.Planned);

    public double ToDoDonePercent => BarPercent(DoneToDo);

    public double ToDoExistingPercent => BarPercent(ToDoLayers.Existing);

    public double ToDoPlannedStartPercent => BarPercent(ToDoLayers.PlannedStart);

    public double ToDoPlannedPercent => BarPercent(ToDoLayers.Planned);

    public double ForYouGoalPercent => BarPercent(RequiredForYou);

    public double ToDoGoalPercent => BarPercent(AllowedToDo);

    private double BarPercent(int count) =>
        DayPlannerBarChart.BarPercent(count, PlannedForYou, PlannedToDo, RequiredForYou, AllowedToDo);

    public IReadOnlyList<DayPlannerTask> PastTasks { get; private set; } = [];

    public DayPlannerInsight? Insight { get; private set; }

    public IReadOnlyList<ForYouSuggestionItem> Suggestions { get; private set; } = [];

    public IReadOnlyList<string> PastForYous { get; private set; } = [];

    public string ForYouText => _textProvider.GetText(IsGoalReached ? "PlannerForYouTextReached" : "PlannerForYouText");

    public IReadOnlyList<DayPlannerTask> DutyTemplates { get; private set; } = [];

    public string DoneText => _textProvider.GetText(PlannedTotal == 0 ? "PlannerDoneNothing" : "PlannerDonePlanned");

    public TaskEditPopUpInputModel? TaskEditorInput => _taskEditor.Input;

    public bool IsTaskEditorOpen => _taskEditor.IsOpen;

    public bool IsReschedulePopupOpen => _rescheduleTaskId.HasValue;

    public DateTime RescheduleInitialDate => Today;

    public UndoNotice? CurrentUndo => _undo.Current;

    private DateTime Today => _timeProvider.GetLocalNow().Date;

    private (int Existing, int PlannedStart, int Planned) ForYouLayers =>
        DayPlannerBarChart.GetLayers(PlannedForYou, DoneForYou, _baseline.ForYou);

    private (int Existing, int PlannedStart, int Planned) ToDoLayers =>
        DayPlannerBarChart.GetLayers(PlannedToDo, DoneToDo, _baseline.ToDo);

    private (int ForYou, int ToDo) GoalParts
    {
        get
        {
            var rewarding = _rewardingSettingsService.Rewarding;
            return BalanceForYouToDoRules.GetParts(rewarding.BalanceGoal, rewarding.CustomForYou, rewarding.CustomToDo);
        }
    }

    public async Task InitializeAsync()
    {
        await _appStartupStateService.WaitForReadyAsync();

        _baselines.Clear();
        await LoadAsync();

        Steps = BuildSteps(includeAssign: PastTasks.Count > 0);
        StepIndex = 0;
    }

    public void Next() => Move(forward: true);

    public void Back() => Move(forward: false);

    public void Finish()
    {
        _navigationManager.NavigateTo(CalendarRoute);
    }

    public async Task AssignPastTaskAsync(Guid taskId, PastTaskTarget target)
    {
        var task = FindAssignablePastTask(taskId);
        if (task is null)
        {
            return;
        }

        var date = TaskExtension.ResolvePastTaskTargetDate(Today, target);

        await MoveTaskAsync(task, date, nameof(AssignPastTaskAsync));
    }

    public async Task AssignAllPastTasksToTomorrowAsync()
    {
        var tomorrow = Today.AddDays(1);
        var tasks = _taskCacheService.GetNormalTasks()
            .Where(task => task.Id is Guid id && TaskExtension.IsPastOpenTask(task, Today) && !_activeTaskService.IsTaskActive(id))
            .ToList();

        foreach (var task in tasks)
        {
            await MoveTaskAsync(task, tomorrow, nameof(AssignAllPastTasksToTomorrowAsync));
        }
    }

    public void OpenReschedulePopup(Guid taskId)
    {
        if (FindAssignablePastTask(taskId) is null)
        {
            return;
        }

        _rescheduleTaskId = taskId;
        OnPropertyChanged(nameof(IsReschedulePopupOpen));
    }

    public void CloseReschedulePopup()
    {
        _rescheduleTaskId = null;
        OnPropertyChanged(nameof(IsReschedulePopupOpen));
    }

    public async Task ConfirmRescheduleAsync(DateTime newDate)
    {
        var taskId = _rescheduleTaskId;
        CloseReschedulePopup();

        if (taskId is null)
        {
            return;
        }

        var task = FindAssignablePastTask(taskId.Value);
        if (task is null)
        {
            return;
        }

        await MoveTaskAsync(task, newDate.Date, nameof(ConfirmRescheduleAsync));
    }

    public async Task DiscardPastTaskAsync(Guid taskId)
    {
        var task = FindAssignablePastTask(taskId);
        if (task is null)
        {
            return;
        }

        var original = TaskExtension.Clone(task);
        Guid? newSeriesStartId = null;

        try
        {
            if (TaskExtension.GetSeriesId(task) is not null)
            {
                newSeriesStartId = await _taskSeriesService.DeleteTaskAsync(taskId, TaskSeriesScope.ThisTaskOnly);
            }
            else
            {
                await _taskCacheService.DeleteTaskAsync(taskId);
            }
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(DayPlannerViewModel)}.{nameof(DiscardPastTaskAsync)}");
            return;
        }

        if (newSeriesStartId is { } seriesStartId)
        {
            original.SeriesId = seriesStartId;
            original.RepeatUnit = TaskRepeatUnit.None;
            original.RepeatInterval = 1;
            original.RepeatWeekdays = TaskWeekdays.None;
            original.RepeatGeneratedUntil = null;
        }

        _undo.Show(new UndoNotice(UndoNoticeKind.TaskDiscarded, 1), () => _taskCacheService.AddTaskAsync(original));
    }

    public Task UndoAsync() => _undo.UndoAsync(
        ex => _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(DayPlannerViewModel)}.{nameof(UndoAsync)}"));

    public void DismissUndo() => _undo.Dismiss();

    public void PickSuggestion(ForYouSuggestionItem suggestion)
    {
        _taskEditor.OpenForYouSuggestion(suggestion, Today, _taskCacheService.GetTemplates());
    }

    public void OpenTaskEditorForForYou()
    {
        _taskEditor.Open(null, Today, _taskCacheService.GetTemplates(), initialIsPositive: true);
    }

    public void OpenTaskEditorForToDo()
    {
        _taskEditor.Open(null, Today, _taskCacheService.GetTemplates(), initialIsPositive: false);
    }

    public void PickDutyTemplate(Guid templateId)
    {
        _taskEditor.TryOpenTemplateCopy(templateId, Today, _taskCacheService.GetTemplates());
    }

    public async Task HandleTaskEditPopupCloseAsync(TaskEditPopUpClosePayload payload)
    {
        var result = await _taskEditor.HandleCloseAsync(payload, Today, _taskCacheService.GetTemplates());

        if (result.Outcome == TaskEditorCloseOutcome.SaveFailed)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(
                result.Error!,
                $"{nameof(DayPlannerViewModel)}.{nameof(HandleTaskEditPopupCloseAsync)}");
        }
    }

    public void Dispose()
    {
        _undo.Dispose();
        _taskCacheService.NormalTasksChanged -= OnTasksChanged;
        _taskCacheService.DoneTasksChanged -= OnTasksChanged;
        _taskCacheService.TemplatesChanged -= OnTasksChanged;
        _rewardingSettingsService.RewardingChanged -= OnTasksChanged;
        _scopeService.ScopeChanged -= OnTasksChanged;
        _dayChangeService.DayChanged -= OnDayChanged;
        _taskEditor.StateChanged -= OnTaskEditorStateChanged;
    }

    private void Move(bool forward) =>
        StepIndex = Math.Clamp(StepIndex + (forward ? 1 : -1), 0, Steps.Count - 1);

    private static List<DayPlannerStep> BuildSteps(bool includeAssign)
    {
        var steps = new List<DayPlannerStep> { DayPlannerStep.Overview };
        if (includeAssign)
        {
            steps.Add(DayPlannerStep.Assign);
        }

        steps.Add(DayPlannerStep.ForYou);
        steps.Add(DayPlannerStep.Duties);
        steps.Add(DayPlannerStep.Done);
        return steps;
    }

    private void UpdateSteps()
    {
        var includeAssign = PastTasks.Count > 0 || Step == DayPlannerStep.Assign;
        if (includeAssign == Steps.Contains(DayPlannerStep.Assign))
        {
            return;
        }

        var current = Step;
        var steps = BuildSteps(includeAssign);
        Steps = steps;
        StepIndex = Math.Max(0, steps.IndexOf(current));
    }

    private TaskDto? FindAssignablePastTask(Guid taskId)
    {
        if (_activeTaskService.IsTaskActive(taskId))
        {
            return null;
        }

        return _taskCacheService.GetNormalTasks()
            .FirstOrDefault(task => task.Id == taskId && TaskExtension.IsPastOpenTask(task, Today));
    }

    private async Task MoveTaskAsync(TaskDto task, DateTime date, string caller)
    {
        try
        {
            await _taskCacheService.MoveTaskToDateAsync(task, date);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(DayPlannerViewModel)}.{caller}");
        }
    }

    private async Task LoadAsync()
    {

        var version = Interlocked.Increment(ref _loadVersion);
        try
        {
            var today = Today;
            var isWorkScope = _scopeService.IsWorkScope;
            var day = await _balanceService.GetTodayTaskCountAsync(isWorkScope);
            var analysis = await _weekdayAnalysisService.AnalyzeAsync(today);
            var stored = await _suggestionService.GetStoredSuggestionsAsync();

            if (version != Volatile.Read(ref _loadVersion))
            {
                return;
            }

            _day = day;
            _baselines.TryAdd(isWorkScope, (day.ForYou, day.ToDo));
            _baseline = _baselines[isWorkScope];
            NotifyDayDataChanged();

            var tasks = _taskCacheService.GetNormalTasks();
            TodayTasks = tasks
                .Where(task => task.Id.HasValue && task.TaskDate?.Date == today)
                .Select(ToPlannerTask)
                .ToList();
            OnPropertyChanged(nameof(TodayTasks));

            PastTasks = tasks
                .Where(task => task.Id.HasValue && TaskExtension.IsPastOpenTask(task, today))
                .Select(ToPlannerTask)
                .ToList();
            OnPropertyChanged(nameof(PastTasks));

            Insight = DayPlannerInsightFormatter.Build(analysis, today, _textProvider);
            OnPropertyChanged(nameof(Insight));

            Suggestions = ForYouSuggestionList.Build(stored, _taskCacheService.GetTemplates(), _textProvider);
            OnPropertyChanged(nameof(Suggestions));

            PastForYous = ForYouHistory.Build([.. tasks, .. _taskCacheService.GetDoneTasks()], today);
            OnPropertyChanged(nameof(PastForYous));

            DutyTemplates = _taskCacheService.GetTemplates()
                .Where(task => task.Id.HasValue && !task.IsPositive && TaskFilterService.IsOpenTemplate(task))
                .Select(ToPlannerTask)
                .ToList();
            OnPropertyChanged(nameof(DutyTemplates));

            IsLoaded = true;
            OnPropertyChanged(nameof(IsLoaded));
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(DayPlannerViewModel)}.{nameof(LoadAsync)}");
        }
    }

    private static DayPlannerTask ToPlannerTask(TaskDto task) =>
        new(task.Id!.Value, string.IsNullOrWhiteSpace(task.Note) ? string.Empty : task.Note.Trim(), task.IsPositive);

    private void OnTaskEditorStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(TaskEditorInput));
        OnPropertyChanged(nameof(IsTaskEditorOpen));
    }

    private void OnTasksChanged(object? sender, EventArgs e)
    {
        _ = ReloadAsync();
    }

    private void OnDayChanged(object? sender, DayChangedEventArgs e)
    {

        _baselines.Clear();
        _ = ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        await LoadAsync();
        UpdateSteps();
    }

    private void NotifyDayDataChanged()
    {
        OnPropertyChanged(nameof(PlannedForYou));
        OnPropertyChanged(nameof(PlannedToDo));
        OnPropertyChanged(nameof(PlannedTotal));
        OnPropertyChanged(nameof(DoneForYou));
        OnPropertyChanged(nameof(DoneToDo));
        OnPropertyChanged(nameof(OverviewText));
        OnPropertyChanged(nameof(GoalForYou));
        OnPropertyChanged(nameof(GoalToDo));
        OnPropertyChanged(nameof(GoalProgress));
        OnPropertyChanged(nameof(IsGoalReached));
        OnPropertyChanged(nameof(RequiredForYou));
        OnPropertyChanged(nameof(AllowedToDo));
        OnPropertyChanged(nameof(ForYouDonePercent));
        OnPropertyChanged(nameof(ForYouExistingPercent));
        OnPropertyChanged(nameof(ForYouPlannedStartPercent));
        OnPropertyChanged(nameof(ForYouPlannedPercent));
        OnPropertyChanged(nameof(ToDoDonePercent));
        OnPropertyChanged(nameof(ToDoExistingPercent));
        OnPropertyChanged(nameof(ToDoPlannedStartPercent));
        OnPropertyChanged(nameof(ToDoPlannedPercent));
        OnPropertyChanged(nameof(ForYouGoalPercent));
        OnPropertyChanged(nameof(ToDoGoalPercent));
        OnPropertyChanged(nameof(ForYouText));
        OnPropertyChanged(nameof(DoneText));
    }
}
