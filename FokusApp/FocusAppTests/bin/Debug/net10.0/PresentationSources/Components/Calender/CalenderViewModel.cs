using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Components.PopUps.Calendar;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Components.SharedUI;
using FokusApp.Core.Extensions;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Components.Calender;

public sealed partial class CalenderViewModel : ObservableObject, ICalenderViewModel, IDisposable
{
    private const int MaxDotsPerDay = 3;

    private readonly ITaskCacheService _taskCacheService;
    private readonly IActiveTaskService _activeTaskService;
    private readonly IScopeService _scopeService;
    private readonly IAppStartupStateService _appStartupStateService;
    private readonly ITimerSessionController _timerSessionController;
    private readonly TaskEditorCoordinator _taskEditor;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly TimeProvider _timeProvider;
    private readonly IDayChangeService _dayChangeService;
    private readonly TaskDaySourceCache _taskSource;

    private DateTime _visibleMonth;
    private DateTime _selectedDay;
    private bool _isTaskActionPopupOpen;
    private Guid? _selectedTaskActionId;

    public CalenderViewModel(
        ITaskCacheService taskCacheService,
        IActiveTaskService activeTaskService,
        IScopeService scopeService,
        IAppStartupStateService appStartupStateService,
        ITimerSessionController timerSessionController,
        TaskEditorCoordinator taskEditor,
        IAppErrorCaptureService appErrorCaptureService,
        TimeProvider timeProvider,
        IDayChangeService dayChangeService)
    {
        _taskCacheService = taskCacheService;
        _activeTaskService = activeTaskService;
        _scopeService = scopeService;
        _appStartupStateService = appStartupStateService;
        _timerSessionController = timerSessionController;
        _taskEditor = taskEditor;
        _taskEditor.LockTaskDate = true;
        _appErrorCaptureService = appErrorCaptureService;
        _timeProvider = timeProvider;
        _dayChangeService = dayChangeService;
        _taskSource = new TaskDaySourceCache(taskCacheService);

        _selectedDay = _timeProvider.GetLocalNow().Date;
        _visibleMonth = CalendarGridHelper.GetFirstDayOfMonth(_selectedDay);

        _taskCacheService.NormalTasksChanged += OnTasksChanged;
        _taskCacheService.RegularTasksChanged += OnTasksChanged;
        _taskCacheService.DoneTasksChanged += OnTasksChanged;
        _scopeService.ScopeChanged += OnScopeChanged;
        _activeTaskService.ActiveTaskChanged += OnActiveTaskChanged;
        _dayChangeService.DayChanged += OnDayChanged;

        RefreshFromCache();
    }

    public DateTime VisibleMonth => _visibleMonth;
    public DateTime SelectedDay => _selectedDay;
    public IReadOnlyList<CalenderWeekModel> Weeks { get; private set; } = [];
    public bool IsCalendarCollapsed { get; private set; }
    public IReadOnlyList<TaskDto> ForYouTasks { get; private set; } = [];
    public IReadOnlyList<TaskDto> ToDoTasks { get; private set; } = [];
    public TaskEditPopUpInputModel? TaskEditorInput => _taskEditor.Input;
    public bool IsTaskEditorOpen => _taskEditor.IsOpen;
    public bool IsTaskActionPopupOpen => _isTaskActionPopupOpen;
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
        _taskCacheService.RegularTasksChanged -= OnTasksChanged;
        _taskCacheService.DoneTasksChanged -= OnTasksChanged;
        _scopeService.ScopeChanged -= OnScopeChanged;
        _activeTaskService.ActiveTaskChanged -= OnActiveTaskChanged;
        _dayChangeService.DayChanged -= OnDayChanged;
    }

    public void GoToPreviousMonth()
    {
        ShowMonth(_visibleMonth.AddMonths(-1));
    }

    public void GoToNextMonth()
    {
        ShowMonth(_visibleMonth.AddMonths(1));
    }

    public void ToggleCalendarCollapsed()
    {
        IsCalendarCollapsed = !IsCalendarCollapsed;
        OnPropertyChanged(nameof(IsCalendarCollapsed));
    }

    public void MoveSelectedDay(bool forwards)
    {
        SelectDay(_selectedDay.AddDays(forwards ? 1 : -1));
    }

    public void GoToToday()
    {
        var today = _timeProvider.GetLocalNow().Date;
        _selectedDay = today;
        _visibleMonth = CalendarGridHelper.GetFirstDayOfMonth(today);
        CloseTaskActionPopupInternal();
        RefreshFromCache();

        NotifyMonthChanged();
        NotifySelectedDayChanged();
        NotifyTaskDataChanged();
        NotifyTaskActionStateChanged();
    }

    public void SelectDay(DateTime date)
    {
        var day = date.Date;
        var monthStart = CalendarGridHelper.GetFirstDayOfMonth(day);
        var monthChanged = monthStart != _visibleMonth;

        _selectedDay = day;
        _visibleMonth = monthStart;
        CloseTaskActionPopupInternal();
        RefreshFromCache();

        if (monthChanged)
        {
            NotifyMonthChanged();
        }

        NotifySelectedDayChanged();
        NotifyTaskDataChanged();
        NotifyTaskActionStateChanged();
    }

    public void OpenTaskEditorForNewTask(DateTime date)
    {
        SelectDay(date);
        _taskEditor.Open(null, _selectedDay, _taskSource.RegularTasks);

        NotifyEditorStateChanged();
    }

    public void OpenTaskEditorForNewTask(bool isPositive)
    {
        _taskEditor.Open(null, _selectedDay, _taskSource.RegularTasks, isPositive);
        CloseTaskActionPopupInternal();

        NotifyEditorStateChanged();
        NotifyTaskActionStateChanged();
    }

    public async Task HandleTaskEditPopupCloseAsync(TaskEditPopUpClosePayload payload)
    {
        var result = await _taskEditor.HandleCloseAsync(payload, _selectedDay, _taskSource.RegularTasks);

        switch (result.Outcome)
        {
            case TaskEditorCloseOutcome.Closed:
                NotifyEditorStateChanged();
                break;
            case TaskEditorCloseOutcome.TemplateCopyOpened:
                NotifyEditorStateChanged();
                break;
            case TaskEditorCloseOutcome.TemplateEditRequested:
                StartEditTask(payload.RegularTemplateTaskId!.Value);
                break;
            case TaskEditorCloseOutcome.SaveFailed:
                await _appErrorCaptureService
                    .CaptureUiErrorAsync(result.Error!, $"{nameof(CalenderViewModel)}.{nameof(HandleTaskEditPopupCloseAsync)}")
                    .ConfigureAwait(false);
                break;
        }
    }

    public void OpenTaskActionPopup(Guid taskId)
    {
        _selectedTaskActionId = taskId;
        _isTaskActionPopupOpen = true;
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

        StartEditTask(_selectedTaskActionId.Value);
    }

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
                .CaptureUiErrorAsync(ex, $"{nameof(CalenderViewModel)}.{nameof(MarkSelectedTaskAsDoneAsync)}");
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
                .CaptureUiErrorAsync(ex, $"{nameof(CalenderViewModel)}.{nameof(DeleteSelectedTaskAsync)}");
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
                    .CaptureUiErrorAsync(ex, $"{nameof(CalenderViewModel)}.{nameof(ExecutePrimarySelectedTaskActionAsync)}");
            }
            finally
            {
                CloseTaskActionPopup();
            }

            return false;
        }

        _activeTaskService.SetActiveTask(task.Id);
        CloseTaskActionPopup();
        return true;
    }

    private void ShowMonth(DateTime monthStart)
    {
        _visibleMonth = CalendarGridHelper.GetFirstDayOfMonth(monthStart);
        RefreshFromCache();

        NotifyMonthChanged();
        NotifyTaskDataChanged();
    }

    private void StartEditTask(Guid taskId)
    {
        var task = _taskSource.FindById(taskId);
        if (task is null || IsActiveTask(task))
        {
            return;
        }

        _taskEditor.Open(TaskExtension.Clone(task), task.TaskDate ?? _selectedDay, _taskSource.RegularTasks);
        CloseTaskActionPopupInternal();

        NotifyEditorStateChanged();
        NotifyTaskActionStateChanged();
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

    private void OnDayChanged(object? sender, DayChangedEventArgs e)
    {
        if (_selectedDay == e.PreviousDay)
        {
            _selectedDay = e.CurrentDay;
            _visibleMonth = CalendarGridHelper.GetFirstDayOfMonth(_selectedDay);
            NotifyMonthChanged();
            NotifySelectedDayChanged();
        }

        CloseTaskActionPopupInternal();
        RefreshFromCache();

        NotifyTaskDataChanged();
        NotifyTaskActionStateChanged();
    }

    private void RefreshFromCache()
    {
        _taskSource.RefreshNormal(null);
        _taskSource.RefreshRegular(null);
        _taskSource.RefreshDone(null);

        if (_selectedTaskActionId.HasValue && _taskSource.FindVisibleById(_selectedTaskActionId.Value) is null)
        {
            CloseTaskActionPopupInternal();
        }

        Weeks = BuildWeeks();
        ForYouTasks = BuildDayTasks(isPositive: true);
        ToDoTasks = BuildDayTasks(isPositive: false);
    }

    private IReadOnlyList<CalenderWeekModel> BuildWeeks()
    {
        var today = _timeProvider.GetLocalNow().Date;
        var firstVisibleDay = CalendarGridHelper.GetFirstVisibleCalendarDay(_visibleMonth);
        var tasksByDay = _taskSource.NormalTasks
            .Concat(_taskSource.RegularTasks.Where(task => !TaskFilterService.IsRegularTemplate(task)))
            .Concat(_taskSource.DoneTasks)
            .GroupBy(task => (task.TaskDate ?? task.CreationDate).Date)
            .ToDictionary(group => group.Key, group => group.ToList());

        var weeks = new List<CalenderWeekModel>(CalendarGridHelper.WeeksInGrid);
        for (var weekIndex = 0; weekIndex < CalendarGridHelper.WeeksInGrid; weekIndex++)
        {
            var weekStart = firstVisibleDay.AddDays(weekIndex * CalendarGridHelper.DaysInWeek);
            var days = new List<CalenderDayCellModel>(CalendarGridHelper.DaysInWeek);
            for (var dayIndex = 0; dayIndex < CalendarGridHelper.DaysInWeek; dayIndex++)
            {
                var date = weekStart.AddDays(dayIndex);
                tasksByDay.TryGetValue(date, out var dayTasks);
                days.Add(BuildDayCell(date, today, dayTasks ?? []));
            }

            weeks.Add(new CalenderWeekModel
            {
                WeekNumber = ISOWeek.GetWeekOfYear(weekStart),
                Days = days
            });
        }

        return weeks;
    }

    private CalenderDayCellModel BuildDayCell(DateTime date, DateTime today, List<TaskDto> dayTasks)
    {
        var dots = dayTasks
            .OrderBy(task => task.IsDone)
            .ThenByDescending(task => task.Priority)
            .Take(MaxDotsPerDay)
            .Select(task => new CalenderDotModel { Priority = task.Priority, IsDone = task.IsDone })
            .ToList();

        return new CalenderDayCellModel
        {
            Date = date,
            IsInCurrentMonth = date.Month == _visibleMonth.Month && date.Year == _visibleMonth.Year,
            IsToday = date == today,
            IsSelected = date == _selectedDay,
            Dots = dots,
            HasMoreTasks = dayTasks.Count > MaxDotsPerDay
        };
    }

    private IReadOnlyList<TaskDto> BuildDayTasks(bool isPositive)
    {
        return TaskFilterService.Apply(
            new TaskFilterOptions
            {
                SelectedDate = _selectedDay,
                IsPositive = isPositive,
                IsRegularTemplate = false,
                IsSortDescending = true,
                SortDoneTasksLast = true
            },
            _taskSource.NormalTasks,
            _taskSource.RegularTasks,
            _taskSource.DoneTasks);
    }

    private bool IsActiveTask(TaskDto task)
    {
        return task.Id is Guid taskId && _activeTaskService.IsTaskActive(taskId);
    }

    private void CloseTaskActionPopupInternal()
    {
        _isTaskActionPopupOpen = false;
        _selectedTaskActionId = null;
    }

    private void NotifyMonthChanged()
    {
        OnPropertyChanged(nameof(VisibleMonth));
    }

    private void NotifySelectedDayChanged()
    {
        OnPropertyChanged(nameof(SelectedDay));
    }

    private void NotifyTaskDataChanged()
    {
        OnPropertyChanged(nameof(Weeks));
        OnPropertyChanged(nameof(ForYouTasks));
        OnPropertyChanged(nameof(ToDoTasks));
    }

    private void NotifyEditorStateChanged()
    {
        OnPropertyChanged(nameof(TaskEditorInput));
        OnPropertyChanged(nameof(IsTaskEditorOpen));
    }

    private void NotifyTaskActionStateChanged()
    {
        OnPropertyChanged(nameof(IsTaskActionPopupOpen));
        OnPropertyChanged(nameof(CanStartSelectedTask));
        OnPropertyChanged(nameof(CanCompleteSelectedTask));
        OnPropertyChanged(nameof(CanDeleteSelectedTask));
        OnPropertyChanged(nameof(IsSelectedActionTaskActive));
    }
}
