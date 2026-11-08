using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Components.SharedUI;
using FokusApp.Components.SharedUI.FilterComponent;
using FokusApp.Core;
using FokusApp.Core.Extensions;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Enums;
using FokusApp.Core.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using System.Diagnostics.CodeAnalysis;

namespace FokusApp.Components.FocusTimerComponent;
public partial class FocusTimerViewModel : ObservableObject, IFocusTimerViewModel, IDisposable
{
    #region private Properties
    private const int MaxSelectableMinutes = AppConstants.MaximumMinutes;
    private const int DefaultBreakMinutes = 5;
    private const string CalendarRoute = "/";
    private readonly ITaskCacheService _taskCacheService;
    private readonly IActiveTaskService _activeTaskService;
    private readonly IScopeService _scopeService;
    private readonly IAppStartupStateService _appStartupStateService;
    private readonly ITimerSessionController _timerSessionController;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly TaskDaySourceCache _taskSource;
    private readonly TimeProvider _timeProvider;
    private readonly IDayChangeService _dayChangeService;
    private readonly IConfettiViewModel _confettiViewModel;
    private readonly NavigationManager _navigationManager;
    private int _sessionMinutes;
    private int TotalInputSeconds => InputMinutes * 60;
    #endregion

    #region public Properties
    public bool IsRunning => _timerSessionController.IsRunning;
    public bool IsPaused => _timerSessionController.IsPaused;
    public bool IsTimerActive => _timerSessionController.IsTimerActive;
    public bool IsBreakMode => _timerSessionController.IsBreakMode;
    public bool IsNotificationPermissionMissing => _timerSessionController.IsNotificationPermissionMissing;
    public string SelectedTaskNote => SelectedTask.Note ?? string.Empty;
    public TaskPriority SelectedTaskPriority => SelectedTask.Priority;
    public bool CanResetTimer => IsBreakMode || !IsRunning;
    public bool HasAvailableTasks => _taskSource.NormalTasks.Count > 0 || _taskSource.Templates.Count > 0;

    public bool IsSelectedTaskOutsideScope =>
        IsFocusSessionLocked && SelectedTaskId is not null && SelectedTask.IsWorkTask != _scopeService.IsWorkScope;
    public bool IsSelectedTaskWorkTask => SelectedTask.IsWorkTask;
    public FilterModel TaskFilter { get; } = new();
    public IReadOnlyList<TaskDto> NormalTasks { get; private set; } = [];
    public IReadOnlyList<TaskDto> Templates { get; private set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CountdownProgress))]
    private int initialDurationInSeconds;

    [ObservableProperty]
    private DateTime selectedTaskDate;

    [ObservableProperty]
    private bool isTaskDayCalendarOpen;

    [ObservableProperty]
    private TaskDto selectedTask;

    public double CountdownProgress
    {
        get
        {
            if (InitialDurationInSeconds <= 0)
            {
                return 0;
            }

            return Math.Clamp(_timerSessionController.RemainingTime.TotalSeconds / InitialDurationInSeconds, 0, 1);
        }
    }

    public double SelectedMinuteRatio =>
        MaxSelectableMinutes > 0 ? GetMinutesInRange(InputMinutes) / (double)MaxSelectableMinutes : 0;

    public int InputMinutes
    {
        get => _sessionMinutes;
        set
        {
            if (IsTimerActive)
            {
                return;
            }

            UpdateInputMinutes(value);
        }
    }

    public Guid? SelectedTaskId => SelectedTask.Id == Guid.Empty ? null : SelectedTask.Id;

    public bool CanToggleTimer
    {
        get
        {
            if (IsRunning)
            {
                return true;
            }

            if (IsPaused)
            {
                return _timerSessionController.RemainingTime > TimeSpan.Zero;
            }

            return TotalInputSeconds > 0;
        }
    }

    public string FormattedTime
    {
        get
        {
            var time = _timerSessionController.ShouldShowCountdown
                ? _timerSessionController.RemainingTime
                : TimeSpan.FromSeconds(TotalInputSeconds);

            var totalMinutes = (int)time.TotalMinutes;
            return $"{totalMinutes:00}:{time.Seconds:00}";
        }
    }
    #endregion

    public FocusTimerViewModel(
        ITaskCacheService taskCacheService,
        IActiveTaskService activeTaskService,
        IScopeService scopeService,
        IAppStartupStateService appStartupStateService,
        ITimerSessionController timerSessionController,
        IAppErrorCaptureService appErrorCaptureService,
        TimeProvider timeProvider,
        IDayChangeService dayChangeService,
        IConfettiViewModel confettiViewModel,
        NavigationManager navigationManager)
    {
        _confettiViewModel = confettiViewModel;
        _navigationManager = navigationManager;
        _timeProvider = timeProvider;
        _dayChangeService = dayChangeService;
        var today = GetToday();
        selectedTaskDate = today;
        TaskFilter.SelectedDate = today;
        selectedTask = CreateEmptyTask();
        _sessionMinutes = selectedTask.TimerMinutes;
        _taskCacheService = taskCacheService;
        _activeTaskService = activeTaskService;
        _scopeService = scopeService;
        _appStartupStateService = appStartupStateService;
        _timerSessionController = timerSessionController;
        _appErrorCaptureService = appErrorCaptureService;
        _taskSource = new TaskDaySourceCache(taskCacheService);
        _timerSessionController.TimerChanged += OnTimerSessionChanged;
        _timerSessionController.SessionStateChanged += OnTimerSessionStateChanged;
        _taskCacheService.NormalTasksChanged += OnNormalTasksChanged;
        _taskCacheService.TemplatesChanged += OnTemplatesChanged;
        _scopeService.ScopeChanged += OnTaskScopeChanged;
        _activeTaskService.ActiveTaskChanged += OnActiveTaskChanged;
        _dayChangeService.DayChanged += OnDayChanged;
    }

    public async Task InitializeAsync()
    {
        await _appStartupStateService.WaitForReadyAsync();
        RefreshAllTaskSources();
        await LoadActiveTaskAsync().ConfigureAwait(false);
    }

    public void Dispose()
    {
        _timerSessionController.TimerChanged -= OnTimerSessionChanged;
        _timerSessionController.SessionStateChanged -= OnTimerSessionStateChanged;
        _taskCacheService.NormalTasksChanged -= OnNormalTasksChanged;
        _taskCacheService.TemplatesChanged -= OnTemplatesChanged;
        _scopeService.ScopeChanged -= OnTaskScopeChanged;
        _activeTaskService.ActiveTaskChanged -= OnActiveTaskChanged;
        _dayChangeService.DayChanged -= OnDayChanged;
    }

    public async Task SwitchTimerToStartOrStop()
    {
        if (!IsRunning && !IsBreakMode)
        {
            _activeTaskService.SetActiveTask(SelectedTask.Id);
        }

        var timerStartRequest = BuildTimerStartRequest();
        await _timerSessionController.SwitchTimerToStartPauseOrResumeAsync(timerStartRequest);
    }

    public async Task ResetTimer()
    {
        await _timerSessionController.StopAsync();

        SelectedTask = GetAvailableTaskOrDefault(SelectedTask.Id);
        ApplyStoredFocusTimeToSession();

        _activeTaskService.SetActiveTask(SelectedTask.Id);
        NotifyTimerStateChanged();
    }

    public void SetMinutes(int minutes)
    {
        InputMinutes = minutes;
    }

    public void PrepareTaskPicker()
    {
        TaskFilter.Apply(new FilterModel
        {
            SelectedDate = SelectedTaskDate
        });

        ApplyTaskFilters();
    }

    public async Task ApplyFilterChangesAsync(FilterModel payload)
    {
        var selectedDate = payload.SelectedDate?.Date ?? GetToday();
        var hasSelectedDateChanged = SelectedTaskDate.Date != selectedDate;

        TaskFilter.Apply(payload);

        if (!hasSelectedDateChanged)
        {
            ApplyTaskFilters();
            return;
        }

        SelectedTaskDate = selectedDate;
        RefreshAllTaskSources();
        await ApplyActiveTaskForNewDayAsync(selectedDate);
    }

    private bool IsFocusSessionLocked => IsTimerActive && !IsBreakMode;

    private async Task ApplyActiveTaskForNewDayAsync(DateTime day)
    {
        if (IsFocusSessionLocked)
        {
            return;
        }

        var activeTask = _activeTaskService.ActiveTask;
        var hasActiveTask = HasTask(activeTask);

        if (hasActiveTask && IsTaskValidForDay(activeTask, day))
        {
            await SetSelectedTaskAsync(activeTask.Id);
            return;
        }

        await SetSelectedTaskAsync(null);

        if (hasActiveTask)
        {
            _activeTaskService.SetActiveTask(null);
        }
    }

    private static Guid? NormalizeTaskId(Guid? id)
    {
        return id == Guid.Empty ? null : id;
    }

    private static bool HasTask([NotNullWhen(true)] TaskDto? task)
    {
        return task?.Id is Guid id && id != Guid.Empty;
    }

    private static bool IsTaskValidForDay(TaskDto task, DateTime day)
    {
        return TaskFilterService.IsOpenTemplate(task) || task.TaskDate?.Date == day.Date;
    }

    public async Task SelectTaskAsync(Guid? taskId)
    {
        await SetSelectedTaskAsync(taskId);
        _activeTaskService.SetActiveTask(SelectedTask.Id);
    }

    private void RefreshAllTaskSources()
    {
        _taskSource.RefreshNormal(SelectedTaskDate);
        _taskSource.RefreshTemplate(SelectedTaskDate);

        OnPropertyChanged(nameof(HasAvailableTasks));
        ApplyTaskFilters();
    }

    private void ApplyTaskFilters()
    {
        var options = TaskFilter.ToTaskFilterOptions(
            keepTemplatesVisibleWhenDayFiltered: true);

        var hideTasks = IsSelectedTaskOutsideScope;
        NormalTasks = hideTasks ? [] : TaskFilterService.Apply(options, _taskSource.NormalTasks);
        Templates = hideTasks ? [] : TaskFilterService.Apply(options, _taskSource.Templates);
        OnPropertyChanged(nameof(NormalTasks));
        OnPropertyChanged(nameof(Templates));
        OnPropertyChanged(nameof(IsSelectedTaskOutsideScope));
        OnPropertyChanged(nameof(IsSelectedTaskWorkTask));
    }

    private async Task LoadActiveTaskAsync()
    {
        var activeTask = _activeTaskService.ActiveTask;

        if (HasTask(activeTask))
        {
            SetSelectedTaskDateFromActiveTask(activeTask.TaskDate);
        }

        await SetSelectedTaskAsync(activeTask?.Id).ConfigureAwait(false);
    }

    private void SetSelectedTaskDateFromActiveTask(DateTime? date)
    {
        if (date is null || SelectedTaskDate.Date == date.Value.Date)
        {
            return;
        }

        SelectedTaskDate = date.Value.Date;
        TaskFilter.SelectedDate = SelectedTaskDate;
        RefreshAllTaskSources();
    }

    private async Task SetSelectedTaskAsync(Guid? taskId)
    {

        if (NormalizeTaskId(SelectedTask.Id) == NormalizeTaskId(taskId) || IsFocusSessionLocked)
        {
            return;
        }

        var nextTask = GetAvailableTaskOrDefault(taskId);

        await _timerSessionController.StopAsync();
        SelectedTask = nextTask;
        ApplyStoredFocusTimeToSession();
        NotifyTimerStateChanged();
    }

    private async Task<int> CompleteSelectedTaskAsync()
    {
        var selectedTaskId = SelectedTask.Id ?? Guid.Empty;
        var taskDto = _taskSource.FindById(selectedTaskId) ?? _taskCacheService.FindTaskById(selectedTaskId);
        if (taskDto is null)
        {
            return DefaultBreakMinutes;
        }

        var breakMinutes = GetMinutesInRange(SelectedTask.BreakMinutes);
        await _taskCacheService.FinishTaskAsync(taskDto);

        SelectedTask = CreateEmptyTask();
        return breakMinutes;
    }

    private void ApplyStoredFocusTimeToSession()
    {
        _sessionMinutes = SelectedTask.TimerMinutes;
        InitialDurationInSeconds = _sessionMinutes * 60;
        NotifyInputMinutesChanged();
    }

    private void UpdateInputMinutes(int minutes)
    {
        var minutesInRange = GetMinutesInRange(minutes);

        if (_sessionMinutes == minutesInRange)
        {
            return;
        }

        _sessionMinutes = minutesInRange;
        InitialDurationInSeconds = minutesInRange * 60;
        NotifyInputMinutesChanged();
    }

    private void OnTimerSessionChanged(object? sender, TimerChangedEventArgs e)
    {
        switch (e.Reason)
        {
            case TimerStateChangeReason.Started:
                HandleTimerStarted(e.RemainingTime);
                break;

            case TimerStateChangeReason.Tick:
                NotifyTimerTickChanged();
                break;

            case TimerStateChangeReason.Paused:
            case TimerStateChangeReason.Resumed:
                NotifyTimerStateChanged();
                break;

            case TimerStateChangeReason.Completed:
                _ = RunFireAndForgetAsync(HandleTimerCompletedAsync, nameof(HandleTimerCompletedAsync));
                break;

            case TimerStateChangeReason.Cleared:
                HandleTimerCleared();
                break;
        }
    }

    private void OnTimerSessionStateChanged(object? sender, EventArgs e)
    {
        NotifyTimerStateChanged();
    }

    private void HandleTimerStarted(TimeSpan remainingTime)
    {
        InitialDurationInSeconds = (int)remainingTime.TotalSeconds;
        _sessionMinutes = InitialDurationInSeconds / 60;
        NotifyInputMinutesChanged();
        NotifyTimerStateChanged();
    }

    private async Task HandleTimerCompletedAsync()
    {
        var result = _timerSessionController.Complete();

        if (result.Type == SessionCompletionType.BreakCompleted)
        {
            ApplyBreakModeEnded();
            _navigationManager.NavigateTo(CalendarRoute);
            _confettiViewModel.ShowBurst();
            return;
        }
        if(result.Type == SessionCompletionType.FocusCompleted)
        {
            var breakMinutes = await CompleteSelectedTaskAsync();
            ApplyBreakModeStarted(breakMinutes);
        }

        _activeTaskService.SetActiveTask(SelectedTask.Id);
    }

    private void ApplyBreakModeStarted(int breakMinutes)
    {
        _sessionMinutes = GetMinutesInRange(breakMinutes);
        InitialDurationInSeconds = _sessionMinutes * 60;
        NotifyInputMinutesChanged();
        NotifyTimerStateChanged();
    }

    private void ApplyBreakModeEnded()
    {
        _sessionMinutes = 0;
        InitialDurationInSeconds = 0;
        NotifyInputMinutesChanged();
        NotifyTimerStateChanged();
    }

    private void HandleTimerCleared()
    {
        SelectedTask = CreateEmptyTask();
        InitialDurationInSeconds = 0;
        _timerSessionController.ClearSession();
        _activeTaskService.SetActiveTask(SelectedTask.Id);
        NotifyTimerStateChanged();
    }

    private TimerStartRequest BuildTimerStartRequest()
    {
        return new TimerStartRequest(
            TotalInputSeconds,
            IsBreakMode ? TimerSessionMode.Break : TimerSessionMode.Focus,
            BuildNotificationDisplayTaskName(),
            ResolveBreakDurationSeconds());
    }

    private TaskDto GetAvailableTaskOrDefault(Guid? id)
    {
        var task = id.HasValue ? _taskSource.FindById(id.Value) : null;
        return task ?? CreateEmptyTask();
    }

    private string BuildNotificationDisplayTaskName()
    {
        if (IsBreakMode)
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(SelectedTask.Note))
        {
            return TimerNotificationTextComposer.FocusFallbackName;
        }

        return SelectedTask.Note.Trim();
    }

    private int ResolveBreakDurationSeconds()
    {
        if (IsBreakMode)
        {
            return Math.Max(0, TotalInputSeconds);
        }

        var breakMinutes = SelectedTask.Id == Guid.Empty
            ? DefaultBreakMinutes
            : GetMinutesInRange(SelectedTask.BreakMinutes);

        return Math.Max(0, breakMinutes * 60);
    }

    partial void OnSelectedTaskChanged(TaskDto value)
    {
        _sessionMinutes = value.TimerMinutes;
        NotifySelectedTaskChanged();
    }

    private void NotifySelectedTaskChanged()
    {
        NotifyInputMinutesChanged();
        OnPropertyChanged(nameof(SelectedTaskNote));
        OnPropertyChanged(nameof(SelectedTaskPriority));
        OnPropertyChanged(nameof(SelectedTaskId));
    }

    private void NotifyTimerTickChanged()
    {
        OnPropertyChanged(nameof(FormattedTime));
        OnPropertyChanged(nameof(CountdownProgress));
    }

    private void NotifyTimerStateChanged()
    {
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(IsTimerActive));
        OnPropertyChanged(nameof(IsBreakMode));
        OnPropertyChanged(nameof(FormattedTime));
        OnPropertyChanged(nameof(CountdownProgress));
        OnPropertyChanged(nameof(CanToggleTimer));
        OnPropertyChanged(nameof(CanResetTimer));
        OnPropertyChanged(nameof(IsNotificationPermissionMissing));
        ApplyTaskFilters();
    }

    private void NotifyInputMinutesChanged()
    {
        OnPropertyChanged(nameof(InputMinutes));
        OnPropertyChanged(nameof(SelectedMinuteRatio));
        OnPropertyChanged(nameof(FormattedTime));
        OnPropertyChanged(nameof(CanToggleTimer));
        OnPropertyChanged(nameof(CountdownProgress));
    }

    private void OnNormalTasksChanged(object? sender, EventArgs e)
    {
        _taskSource.RefreshNormal(SelectedTaskDate);
        RefreshSelectedTaskFromSource();
        OnPropertyChanged(nameof(HasAvailableTasks));
        ApplyTaskFilters();
    }

    private void OnTemplatesChanged(object? sender, EventArgs e)
    {
        _taskSource.RefreshTemplate(SelectedTaskDate);
        RefreshSelectedTaskFromSource();
        OnPropertyChanged(nameof(HasAvailableTasks));
        ApplyTaskFilters();
    }

    private void RefreshSelectedTaskFromSource()
    {
        if (SelectedTask.Id is not Guid selectedTaskId || selectedTaskId == Guid.Empty)
        {
            return;
        }

        var currentTask = _taskSource.FindById(selectedTaskId);
        if (currentTask is null || HasSameDisplayedValues(SelectedTask, currentTask))
        {
            return;
        }

        var sessionMinutes = _sessionMinutes;
        SelectedTask = TaskExtension.Clone(currentTask);
        _sessionMinutes = sessionMinutes;
        NotifyInputMinutesChanged();
    }

    private static bool HasSameDisplayedValues(TaskDto first, TaskDto second)
    {
        return string.Equals(first.Note, second.Note, StringComparison.Ordinal) &&
            first.Priority == second.Priority &&
            first.TimerMinutes == second.TimerMinutes &&
            first.BreakMinutes == second.BreakMinutes;
    }

    private void OnTaskScopeChanged(object? sender, EventArgs e)
    {
        RefreshAllTaskSources();
    }

    private void OnActiveTaskChanged(object? sender, EventArgs e)
    {
        _ = RunFireAndForgetAsync(LoadActiveTaskAsync, nameof(LoadActiveTaskAsync));
    }

    private void OnDayChanged(object? sender, DayChangedEventArgs e)
    {
        if (SelectedTaskDate.Date != e.PreviousDay)
        {
            return;
        }

        if (_timerSessionController.IsTimerActive)
        {
            SelectedTaskDate = e.CurrentDay;
            TaskFilter.SelectedDate = e.CurrentDay;
            RefreshAllTaskSources();
            return;
        }

        var payload = TaskFilter.Snapshot();
        payload.SelectedDate = e.CurrentDay;
        _ = RunFireAndForgetAsync(() => ApplyFilterChangesAsync(payload), nameof(OnDayChanged));
    }

    private async Task RunFireAndForgetAsync(Func<Task> operation, string source)
    {
        try
        {
            await operation().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService
                .CaptureUiErrorAsync(ex, $"{nameof(FocusTimerViewModel)}.{source}")
                .ConfigureAwait(false);
        }
    }

    private int GetMinutesInRange(int minutes)
    {
        return Math.Clamp(minutes, 0, MaxSelectableMinutes);
    }

    private DateTime GetToday() => _timeProvider.GetLocalNow().Date;

    private TaskDto CreateEmptyTask()
    {
        return new TaskDto
        {
            Id = Guid.Empty,
            Note = string.Empty,
            TaskDate = GetToday(),
            Priority = TaskPriority.Keine,
            BreakMinutes = DefaultBreakMinutes,
            TimerMinutes = 1
        };
    }

}
