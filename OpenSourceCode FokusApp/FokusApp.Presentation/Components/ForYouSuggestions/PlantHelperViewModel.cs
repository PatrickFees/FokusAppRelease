using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;

namespace FokusApp.Components.ForYouSuggestions;

public sealed partial class PlantHelperViewModel : ObservableObject, IPlantHelperViewModel, IDisposable
{

    public const int ForYouBubbleStartHour = 12;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private readonly IBalanceForYouToDoService _balanceService;
    private readonly IRewardingSettingsService _rewardingSettingsService;
    private readonly IForYouSuggestionService _suggestionService;
    private readonly ITaskCacheService _taskCacheService;
    private readonly IScopeService _scopeService;
    private readonly TaskEditorCoordinator _taskEditor;
    private readonly IUiTextProvider _textProvider;
    private readonly IAppStartupStateService _appStartupStateService;
    private readonly IDayChangeService _dayChangeService;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly TimeProvider _timeProvider;
    private readonly ICompanionMoodService _moodService;

    private ITimer? _timer;
    private bool _hasForYouToday = true;
    private bool _hasAnythingToday = true;
    private DayTaskCount? _scopeToday;
    private PlantBubbleKind _tapBubble = PlantBubbleKind.None;
    private readonly HashSet<(bool IsWorkScope, DateTime Day)> _dismissedPlanDays = [];
    private readonly HashSet<(bool IsWorkScope, DateTime Day)> _dismissedForYouDays = [];

    [ObservableProperty]
    private bool isSuggestionListOpen;

    [ObservableProperty]
    private IReadOnlyList<ForYouSuggestionItem> suggestions = [];

    [ObservableProperty]
    private CompanionMood mood = CompanionMood.Resting;

    public PlantHelperViewModel(
        IBalanceForYouToDoService balanceService,
        IRewardingSettingsService rewardingSettingsService,
        IForYouSuggestionService suggestionService,
        ITaskCacheService taskCacheService,
        IScopeService scopeService,
        TaskEditorCoordinator taskEditor,
        IUiTextProvider textProvider,
        IAppStartupStateService appStartupStateService,
        IDayChangeService dayChangeService,
        IAppErrorCaptureService appErrorCaptureService,
        TimeProvider timeProvider,
        ICompanionMoodService moodService)
    {
        _balanceService = balanceService;
        _rewardingSettingsService = rewardingSettingsService;
        _suggestionService = suggestionService;
        _taskCacheService = taskCacheService;
        _scopeService = scopeService;
        _taskEditor = taskEditor;
        _textProvider = textProvider;
        _appStartupStateService = appStartupStateService;
        _dayChangeService = dayChangeService;
        _appErrorCaptureService = appErrorCaptureService;
        _timeProvider = timeProvider;
        _moodService = moodService;

        _rewardingSettingsService.RewardingChanged += OnRewardingChanged;
        _dayChangeService.DayChanged += OnDayChanged;
        _taskCacheService.NormalTasksChanged += OnTasksChanged;
        _taskCacheService.DoneTasksChanged += OnTasksChanged;
        _taskCacheService.TemplatesChanged += OnTasksChanged;
        _suggestionService.SuggestionsChanged += OnSuggestionsChanged;
        _scopeService.ScopeChanged += OnScopeChanged;
        _taskEditor.StateChanged += OnTaskEditorStateChanged;
    }

    public bool IsPlantVisible => _rewardingSettingsService.Rewarding.CompanionEnabled;

    public PlantBubbleKind BubbleKind
    {
        get
        {
            if (!IsPlantVisible)
            {
                return PlantBubbleKind.None;
            }

            if (_tapBubble != PlantBubbleKind.None)
            {
                return _tapBubble;
            }

            var now = Now;
            var key = (_scopeService.IsWorkScope, now.Date);

            if (!_hasAnythingToday)
            {
                return _dismissedPlanDays.Contains(key) ? PlantBubbleKind.None : PlantBubbleKind.PlanDay;
            }

            if (now.Hour < ForYouBubbleStartHour)
            {
                return PlantBubbleKind.None;
            }

            return !_hasForYouToday && !_dismissedForYouDays.Contains(key) ? PlantBubbleKind.ForYou : PlantBubbleKind.None;
        }
    }

    public bool IsBubbleVisible => BubbleKind != PlantBubbleKind.None;

    public TaskEditPopUpInputModel? TaskEditorInput => _taskEditor.Input;

    public bool IsTaskEditorOpen => _taskEditor.IsOpen;

    private DateTime Now => _timeProvider.GetLocalNow().DateTime;

    public async Task InitializeAsync()
    {
        await _appStartupStateService.WaitForReadyAsync();
        await RefreshAsync();

        _timer ??= _timeProvider.CreateTimer(OnTimerTick, null, CheckInterval, CheckInterval);
    }

    public void TapPlant()
    {
        _tapBubble = _tapBubble == PlantBubbleKind.None ? EvaluateGoal() : PlantBubbleKind.None;
        NotifyBubbleChanged();
    }

    public void OpenSuggestions()
    {
        _tapBubble = PlantBubbleKind.None;
        IsSuggestionListOpen = true;
        NotifyBubbleChanged();
    }

    public void CloseSuggestions()
    {
        IsSuggestionListOpen = false;
    }

    public void DismissBubble()
    {
        switch (BubbleKind)
        {
            case PlantBubbleKind.GoalReached:
            case PlantBubbleKind.GoalExceeded:
            case PlantBubbleKind.AskForYou:
                _tapBubble = PlantBubbleKind.None;
                break;
            case PlantBubbleKind.PlanDay:
                _dismissedPlanDays.Add((_scopeService.IsWorkScope, Now.Date));
                break;
            case PlantBubbleKind.ForYou:
                _dismissedForYouDays.Add((_scopeService.IsWorkScope, Now.Date));
                break;
        }

        NotifyBubbleChanged();
    }

    public void PickSuggestion(ForYouSuggestionItem suggestion)
    {
        CloseSuggestions();
        _taskEditor.OpenForYouSuggestion(suggestion, Now.Date, _taskCacheService.GetTemplates());
    }

    public async Task HandleTaskEditPopupCloseAsync(TaskEditPopUpClosePayload payload)
    {
        var result = await _taskEditor.HandleCloseAsync(payload, Now.Date, _taskCacheService.GetTemplates());

        if (result.Outcome == TaskEditorCloseOutcome.SaveFailed)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(
                result.Error!,
                $"{nameof(PlantHelperViewModel)}.{nameof(HandleTaskEditPopupCloseAsync)}");
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _rewardingSettingsService.RewardingChanged -= OnRewardingChanged;
        _dayChangeService.DayChanged -= OnDayChanged;
        _taskCacheService.NormalTasksChanged -= OnTasksChanged;
        _taskCacheService.DoneTasksChanged -= OnTasksChanged;
        _taskCacheService.TemplatesChanged -= OnTasksChanged;
        _suggestionService.SuggestionsChanged -= OnSuggestionsChanged;
        _scopeService.ScopeChanged -= OnScopeChanged;
        _taskEditor.StateChanged -= OnTaskEditorStateChanged;
    }

    private PlantBubbleKind EvaluateGoal()
    {
        if (_scopeToday is not { } today)
        {
            return PlantBubbleKind.AskForYou;
        }

        var rewarding = _rewardingSettingsService.Rewarding;
        var progress = BalanceForYouToDoRules.GetProgress(
            today.ForYou,
            today.ToDo,
            rewarding.BalanceGoal,
            rewarding.CustomForYou,
            rewarding.CustomToDo);

        return progress switch
        {
            GoalProgress.Exceeded => PlantBubbleKind.GoalExceeded,
            GoalProgress.Reached => PlantBubbleKind.GoalReached,
            _ => PlantBubbleKind.AskForYou
        };
    }

    private async Task RefreshAsync()
    {
        try
        {
            var today = await _balanceService.GetTodayTaskCountAsync(_scopeService.IsWorkScope);
            _scopeToday = today;
            _hasForYouToday = today.ForYou > 0;
            _hasAnythingToday = today.Total > 0;

            if (_tapBubble != PlantBubbleKind.None)
            {
                _tapBubble = EvaluateGoal();
            }

            Mood = await _moodService.GetRecentMoodAsync();

            var stored = await _suggestionService.GetStoredSuggestionsAsync();
            Suggestions = ForYouSuggestionList.Build(stored, _taskCacheService.GetTemplates(), _textProvider);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(PlantHelperViewModel)}.{nameof(RefreshAsync)}");
        }

        NotifyBubbleChanged();
    }

    private void OnTaskEditorStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(TaskEditorInput));
        OnPropertyChanged(nameof(IsTaskEditorOpen));
    }

    private void OnTimerTick(object? state)
    {

        NotifyBubbleChanged();
    }

    private void NotifyBubbleChanged()
    {
        OnPropertyChanged(nameof(IsPlantVisible));
        OnPropertyChanged(nameof(BubbleKind));
        OnPropertyChanged(nameof(IsBubbleVisible));
    }

    private void OnRewardingChanged(object? sender, EventArgs e)
    {
        if (_tapBubble != PlantBubbleKind.None)
        {
            _tapBubble = EvaluateGoal();
        }

        NotifyBubbleChanged();
    }

    private void OnDayChanged(object? sender, DayChangedEventArgs e)
    {
        _ = RefreshAsync();
    }

    private void OnScopeChanged(object? sender, EventArgs e)
    {

        _hasForYouToday = true;
        _hasAnythingToday = true;
        NotifyBubbleChanged();
        _ = RefreshAsync();
    }

    private void OnTasksChanged(object? sender, EventArgs e)
    {
        _ = RefreshAsync();
    }

    private void OnSuggestionsChanged(object? sender, EventArgs e)
    {
        _ = RefreshAsync();
    }
}
