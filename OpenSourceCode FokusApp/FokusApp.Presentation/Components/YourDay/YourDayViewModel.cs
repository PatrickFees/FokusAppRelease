using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Components.Calender;
using FokusApp.Components.PopUps.Calendar;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Components.PopUps.TaskSeriesChoice;
using FokusApp.Components.SharedUI;
using FokusApp.Core.Enums;
using FokusApp.Core.Extensions;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using Microsoft.AspNetCore.Components;

namespace FokusApp.Components.YourDay;

public sealed partial class YourDayViewModel : ObservableObject, IYourDayViewModel, IDisposable
{
    private const string PastTasksRoute = "/aufgaben?bereich=vergangenheit";

    private readonly ITaskCacheService _taskCacheService;
    private readonly IActiveTaskService _activeTaskService;
    private readonly IScopeService _scopeService;
    private readonly IAppStartupStateService _appStartupStateService;
    private readonly ITimerSessionController _timerSessionController;
    private readonly TaskEditorCoordinator _taskEditor;
    private readonly ITaskSeriesService _taskSeriesService;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly TimeProvider _timeProvider;
    private readonly IDayChangeService _dayChangeService;
    private readonly TaskDaySourceCache _taskSource;
    private readonly NavigationManager _navigationManager;
    private readonly IRewardingSettingsService _rewardingSettingsService;
    private readonly SeriesSelectionFlow _seriesFlow;

    [ObservableProperty]
    private DateTime visibleMonth;

    [ObservableProperty]
    private DateTime selectedDay;

    [ObservableProperty]
    private bool isTaskActionPopupOpen;

    private Guid? _selectedTaskActionId;

    public YourDayViewModel(
        ITaskCacheService taskCacheService,
        IActiveTaskService activeTaskService,
        IScopeService scopeService,
        IAppStartupStateService appStartupStateService,
        ITimerSessionController timerSessionController,
        TaskEditorCoordinator taskEditor,
        ITaskSeriesService taskSeriesService,
        IAppErrorCaptureService appErrorCaptureService,
        TimeProvider timeProvider,
        IDayChangeService dayChangeService,
        NavigationManager navigationManager,
        IRewardingSettingsService rewardingSettingsService)
    {
        _navigationManager = navigationManager;
        _rewardingSettingsService = rewardingSettingsService;
        _taskCacheService = taskCacheService;
        _activeTaskService = activeTaskService;
        _scopeService = scopeService;
        _appStartupStateService = appStartupStateService;
        _timerSessionController = timerSessionController;
        _taskEditor = taskEditor;
        _taskEditor.CreateTodayTaskForNewTemplate = true;
        _taskSeriesService = taskSeriesService;
        _appErrorCaptureService = appErrorCaptureService;
        _timeProvider = timeProvider;
        _dayChangeService = dayChangeService;
        _taskSource = new TaskDaySourceCache(taskCacheService);
        _seriesFlow = new SeriesSelectionFlow(_taskSource, activeTaskService, CloseTaskActionPopupInternal, StartEditTask, DeleteSeriesTaskAsync);

        selectedDay = _timeProvider.GetLocalNow().Date;
        visibleMonth = CalendarGridHelper.GetFirstDayOfMonth(selectedDay);

        _taskCacheService.NormalTasksChanged += OnTasksChanged;
        _taskCacheService.TemplatesChanged += OnTasksChanged;
        _taskCacheService.DoneTasksChanged += OnTasksChanged;
        _taskCacheService.TaskCompleted += OnTaskCompleted;
        _scopeService.ScopeChanged += OnScopeChanged;
        _activeTaskService.ActiveTaskChanged += OnActiveTaskChanged;
        _dayChangeService.DayChanged += OnDayChanged;
        _taskEditor.StateChanged += OnTaskEditorStateChanged;
        _seriesFlow.ChoiceChanged += OnSeriesChoiceChanged;

        RefreshFromCache();
    }

    public IReadOnlyList<CalenderWeekModel> Weeks { get; private set; } = [];
    public bool IsCalendarCollapsed { get; private set; } = true;
    public IReadOnlyList<TaskDto> ForYouTasks { get; private set; } = [];
    public IReadOnlyList<TaskDto> ToDoTasks { get; private set; } = [];
    public Guid? RecentlyCompletedTaskId { get; private set; }
    public int PastTaskCount => _taskSource.NormalTasks
        .Count(task => TaskExtension.IsPastOpenTask(task, _timeProvider.GetLocalNow().Date));
    public TaskEditPopUpInputModel? TaskEditorInput => _taskEditor.Input;
    public bool IsTaskEditorOpen => _taskEditor.IsOpen;
    public bool IsSeriesChoiceOpen => _seriesFlow.IsOpen;
    public TaskSeriesAction PendingSeriesAction => _seriesFlow.PendingAction;
    public bool CanStartSelectedTask => TryGetSelectedTaskForAction(out var task) && !task.IsDone;
    public bool CanCompleteSelectedTask => TryGetSelectedTaskForAction(out var task) && !task.IsDone;
    public bool CanDeleteSelectedTask => TryGetSelectedTaskForAction(out var task) && !IsActiveTask(task);
    public bool IsSelectedActionTaskActive => TryGetSelectedTaskForAction(out var task) && IsActiveTask(task);

    public async Task InitializeAsync()
    {
        await _appStartupStateService.WaitForReadyAsync();
        RefreshFromCache();
        NotifyTaskDataChanged();
    }

    public void Dispose()
    {
        _taskCacheService.NormalTasksChanged -= OnTasksChanged;
        _taskCacheService.TemplatesChanged -= OnTasksChanged;
        _taskCacheService.DoneTasksChanged -= OnTasksChanged;
        _taskCacheService.TaskCompleted -= OnTaskCompleted;
        _scopeService.ScopeChanged -= OnScopeChanged;
        _activeTaskService.ActiveTaskChanged -= OnActiveTaskChanged;
        _dayChangeService.DayChanged -= OnDayChanged;
        _taskEditor.StateChanged -= OnTaskEditorStateChanged;
        _seriesFlow.ChoiceChanged -= OnSeriesChoiceChanged;
    }

    public void OpenPastTasks()
    {
        _navigationManager.NavigateTo(PastTasksRoute);
    }

    public void GoToPreviousMonth()
    {
        ShowMonth(VisibleMonth.AddMonths(-1));
    }

    public void GoToNextMonth()
    {
        ShowMonth(VisibleMonth.AddMonths(1));
    }

    public void ToggleCalendarCollapsed()
    {
        IsCalendarCollapsed = !IsCalendarCollapsed;
        OnPropertyChanged(nameof(IsCalendarCollapsed));
    }

    public void MoveSelectedDay(bool forwards)
    {
        SelectDay(SelectedDay.AddDays(forwards ? 1 : -1));
    }

    public void GoToToday()
    {
        ClearRecentlyCompletedTask();
        var today = _timeProvider.GetLocalNow().Date;
        SelectedDay = today;
        VisibleMonth = CalendarGridHelper.GetFirstDayOfMonth(today);
        CloseTaskActionPopupInternal();
        RefreshFromCache();

        NotifyTaskDataChanged();
        NotifyTaskActionStateChanged();
    }

    public void SelectDay(DateTime date)
    {
        ClearRecentlyCompletedTask();
        var day = date.Date;

        SelectedDay = day;
        VisibleMonth = CalendarGridHelper.GetFirstDayOfMonth(day);
        CloseTaskActionPopupInternal();
        RefreshFromCache();

        NotifyTaskDataChanged();
        NotifyTaskActionStateChanged();
    }

    public void OpenTaskEditorForNewTask(DateTime date)
    {
        SelectDay(date);
        _taskEditor.Open(null, SelectedDay, _taskSource.Templates);
    }

    public void OpenTaskEditorForNewTask(bool isPositive)
    {
        _taskEditor.Open(null, SelectedDay, _taskSource.Templates, isPositive);
        CloseTaskActionPopupInternal();

        NotifyTaskActionStateChanged();
    }

    public async Task HandleTaskEditPopupCloseAsync(TaskEditPopUpClosePayload payload)
    {
        var result = await _taskEditor.HandleCloseAsync(payload, SelectedDay, _taskSource.Templates);

        switch (result.Outcome)
        {
            case TaskEditorCloseOutcome.Closed:
            case TaskEditorCloseOutcome.TemplateCopyOpened:
                break;
            case TaskEditorCloseOutcome.TemplateEditRequested:
                StartEditTask(payload.TemplateTaskId!.Value);
                break;
            case TaskEditorCloseOutcome.SaveFailed:
                await _appErrorCaptureService
                    .CaptureUiErrorAsync(result.Error!, $"{nameof(YourDayViewModel)}.{nameof(HandleTaskEditPopupCloseAsync)}")
                    .ConfigureAwait(false);
                break;
        }
    }

    public void OpenTaskActionPopup(Guid taskId)
    {
        _selectedTaskActionId = taskId;
        IsTaskActionPopupOpen = true;
        NotifyTaskActionStateChanged();
    }

    public void CloseTaskActionPopup()
    {
        CloseTaskActionPopupInternal();
        NotifyTaskActionStateChanged();
    }

    public void StartEditSelectedTask()
    {
        if (!_selectedTaskActionId.HasValue)
        {
            return;
        }

        var taskId = _selectedTaskActionId.Value;
        if (_seriesFlow.BelongsToSeries(taskId))
        {
            _seriesFlow.AskChoice(taskId, TaskSeriesAction.Edit);
            NotifyTaskActionStateChanged();
            return;
        }

        StartEditTask(taskId);
    }

    public Task ChooseSeriesScopeAsync(TaskSeriesScope scope) => _seriesFlow.ChooseScopeAsync(scope);

    public void CancelSeriesChoice() => _seriesFlow.Cancel();

    public async Task MarkSelectedTaskAsDoneAsync()
    {
        if (!TryGetSelectedTaskForAction(out var task) || task.IsDone)
        {
            CloseTaskActionPopup();
            return;
        }

        try
        {
            if (IsActiveTask(task))
            {
                await _timerSessionController.StopAsync();
            }

            await _taskCacheService.FinishTaskAsync(task);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService
                .CaptureUiErrorAsync(ex, $"{nameof(YourDayViewModel)}.{nameof(MarkSelectedTaskAsDoneAsync)}");
        }
        finally
        {
            CloseTaskActionPopup();
        }
    }

    public async Task DeleteSelectedTaskAsync()
    {
        if (!TryGetSelectedTaskForAction(out var task) || IsActiveTask(task))
        {
            CloseTaskActionPopup();
            return;
        }

        if (task.Id is Guid seriesTaskId && TaskExtension.GetSeriesId(task) is not null)
        {
            _seriesFlow.AskChoice(seriesTaskId, TaskSeriesAction.Delete);
            NotifyTaskActionStateChanged();
            return;
        }

        try
        {
            if (task.Id is Guid taskId)
            {
                await _taskCacheService.DeleteTaskAsync(taskId);
            }
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService
                .CaptureUiErrorAsync(ex, $"{nameof(YourDayViewModel)}.{nameof(DeleteSelectedTaskAsync)}");
        }
        finally
        {
            CloseTaskActionPopup();
        }
    }

    public async Task<bool> ExecutePrimarySelectedTaskActionAsync()
    {
        if (!TryGetSelectedTaskForAction(out var task) || task.IsDone)
        {
            return false;
        }

        if (IsActiveTask(task))
        {
            try
            {
                await _timerSessionController.PauseAsync();
            }
            catch (Exception ex)
            {
                await _appErrorCaptureService
                    .CaptureUiErrorAsync(ex, $"{nameof(YourDayViewModel)}.{nameof(ExecutePrimarySelectedTaskActionAsync)}");
            }
            finally
            {
                CloseTaskActionPopup();
            }

            return false;
        }

        if (IsFocusSessionLocked)
        {
            CloseTaskActionPopup();
            return false;
        }

        _activeTaskService.SetActiveTask(task.Id);
        CloseTaskActionPopup();
        return true;
    }

    private void ShowMonth(DateTime monthStart)
    {
        VisibleMonth = CalendarGridHelper.GetFirstDayOfMonth(monthStart);
        RefreshFromCache();

        NotifyTaskDataChanged();
    }

    private void StartEditTask(Guid taskId, TaskSeriesScope seriesScope = TaskSeriesScope.WholeSeries)
    {
        var task = _taskSource.FindById(taskId);
        if (task is null || IsActiveTask(task))
        {
            return;
        }

        _taskEditor.Open(
            TaskExtension.Clone(task),
            task.TaskDate ?? SelectedDay,
            _taskSource.Templates,
            seriesScope: seriesScope);
        CloseTaskActionPopupInternal();

        NotifyTaskActionStateChanged();
    }

    private async Task DeleteSeriesTaskAsync(Guid taskId, TaskSeriesScope scope)
    {
        var task = _taskSource.FindById(taskId);
        if (task is null || IsActiveTask(task))
        {
            return;
        }

        try
        {
            await _taskSeriesService.DeleteTaskAsync(taskId, scope, _activeTaskService.ActiveTaskId);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService
                .CaptureUiErrorAsync(ex, $"{nameof(YourDayViewModel)}.{nameof(ChooseSeriesScopeAsync)}");
        }
    }

    private bool TryGetSelectedTaskForAction(out TaskDto task)
    {
        task = null!;

        if (!_selectedTaskActionId.HasValue)
        {
            return false;
        }

        var matchedTask = _taskSource.FindVisibleById(_selectedTaskActionId.Value);
        if (matchedTask is null)
        {
            return false;
        }

        task = matchedTask;
        return true;
    }

    private void OnTasksChanged(object? sender, EventArgs e)
    {
        RefreshFromCache();
        NotifyTaskDataChanged();
        NotifyTaskActionStateChanged();
    }

    private void OnScopeChanged(object? sender, EventArgs e)
    {
        CloseTaskActionPopupInternal();
        RefreshFromCache();

        NotifyTaskDataChanged();
        NotifyTaskActionStateChanged();
    }

    private void OnActiveTaskChanged(object? sender, EventArgs e)
    {
        NotifyTaskActionStateChanged();
    }

    private void ClearRecentlyCompletedTask()
    {
        if (RecentlyCompletedTaskId is null)
        {
            return;
        }

        RecentlyCompletedTaskId = null;
        OnPropertyChanged(nameof(RecentlyCompletedTaskId));
    }

    private void OnTaskCompleted(object? sender, TaskCompletedEventArgs e)
    {
        if (!_rewardingSettingsService.Rewarding.MicroFeedbackEnabled)
        {
            return;
        }

        RecentlyCompletedTaskId = e.TaskId;
        OnPropertyChanged(nameof(RecentlyCompletedTaskId));
    }

    private void OnDayChanged(object? sender, DayChangedEventArgs e)
    {
        if (SelectedDay == e.PreviousDay)
        {
            SelectedDay = e.CurrentDay;
            VisibleMonth = CalendarGridHelper.GetFirstDayOfMonth(SelectedDay);
        }

        CloseTaskActionPopupInternal();
        RefreshFromCache();

        NotifyTaskDataChanged();
        NotifyTaskActionStateChanged();
    }

    private void RefreshFromCache()
    {
        _taskSource.RefreshNormal(null);
        _taskSource.RefreshTemplate(null);
        _taskSource.RefreshDone(null);

        if (_selectedTaskActionId.HasValue && _taskSource.FindVisibleById(_selectedTaskActionId.Value) is null)
        {
            CloseTaskActionPopupInternal();
        }

        _seriesFlow.ClearIfTaskGone();

        Weeks = CalenderDayDataBuilder.BuildWeeks(VisibleMonth, SelectedDay, _timeProvider.GetLocalNow().Date, _taskSource);
        ForYouTasks = CalenderDayDataBuilder.BuildDayTasks(isPositive: true, SelectedDay, _taskSource);
        ToDoTasks = CalenderDayDataBuilder.BuildDayTasks(isPositive: false, SelectedDay, _taskSource);
    }

    private bool IsFocusSessionLocked => _timerSessionController.IsTimerActive && !_timerSessionController.IsBreakMode;

    private bool IsActiveTask(TaskDto task)
    {
        return task.Id is Guid taskId && _activeTaskService.IsTaskActive(taskId);
    }

    private void CloseTaskActionPopupInternal()
    {
        IsTaskActionPopupOpen = false;
        _selectedTaskActionId = null;
    }

    private void NotifyTaskDataChanged()
    {
        OnPropertyChanged(nameof(Weeks));
        OnPropertyChanged(nameof(ForYouTasks));
        OnPropertyChanged(nameof(ToDoTasks));
        OnPropertyChanged(nameof(PastTaskCount));
    }

    private void OnTaskEditorStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(TaskEditorInput));
        OnPropertyChanged(nameof(IsTaskEditorOpen));
    }

    private void OnSeriesChoiceChanged(object? sender, EventArgs e) => NotifySeriesChoiceChanged();

    private void NotifySeriesChoiceChanged()
    {
        OnPropertyChanged(nameof(IsSeriesChoiceOpen));
        OnPropertyChanged(nameof(PendingSeriesAction));
    }

    private void NotifyTaskActionStateChanged()
    {
        OnPropertyChanged(nameof(CanStartSelectedTask));
        OnPropertyChanged(nameof(CanCompleteSelectedTask));
        OnPropertyChanged(nameof(CanDeleteSelectedTask));
        OnPropertyChanged(nameof(IsSelectedActionTaskActive));
    }
}
