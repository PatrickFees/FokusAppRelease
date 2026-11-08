using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Core.Enums;
using FokusApp.Components.SharedUI;
using FokusApp.Components.SharedUI.FilterComponent;
using FokusApp.Core.Extensions;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Components.TaskOverViewComponent;

public partial class TaskOverViewViewModel : ObservableObject, ITaskOverViewViewModel, IDisposable
{
    private readonly ITaskCacheService _taskCacheService;
    private readonly IActiveTaskService _activeTaskService;
    private readonly IScopeService _scopeService;
    private readonly IAppStartupStateService _appStartupStateService;
    private readonly TaskEditorCoordinator _taskEditor;
    private readonly ITaskSeriesService _taskSeriesService;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly TaskDaySourceCache _taskSource;
    private readonly TimeProvider _timeProvider;
    private readonly IDayChangeService _dayChangeService;

    private readonly UndoCoordinator _undo;

    public IReadOnlyList<TaskDto> ActiveTasks { get; private set; } = [];
    public IReadOnlyList<TaskDto> OverdueTasks { get; private set; } = [];
    public IReadOnlyList<TaskDto> Templates { get; private set; } = [];
    public IReadOnlyList<TaskDto> CompletedTasks { get; private set; } = [];
    public FilterModel Filter { get; } = new();
    public UndoNotice? CurrentUndo => _undo.Current;

    [ObservableProperty]
    private bool isFilterPanelOpen;

    private Guid? _rescheduleTaskId;

    public bool IsReschedulePopupOpen => _rescheduleTaskId.HasValue;
    public DateTime RescheduleInitialDate => GetToday();

    public TaskEditPopUpInputModel? TaskEditorInput => _taskEditor.Input;
    public bool IsAddNoteModalOpen => _taskEditor.IsOpen;

    public TaskOverViewViewModel(
        ITaskCacheService taskCacheService,
        IActiveTaskService activeTaskService,
        IScopeService scopeService,
        IAppStartupStateService appStartupStateService,
        TaskEditorCoordinator taskEditor,
        ITaskSeriesService taskSeriesService,
        IAppErrorCaptureService appErrorCaptureService,
        TimeProvider timeProvider,
        IDayChangeService dayChangeService)
    {
        _timeProvider = timeProvider;
        _dayChangeService = dayChangeService;
        _taskCacheService = taskCacheService;
        _activeTaskService = activeTaskService;
        _scopeService = scopeService;
        _appStartupStateService = appStartupStateService;
        _taskEditor = taskEditor;
        _taskSeriesService = taskSeriesService;
        _appErrorCaptureService = appErrorCaptureService;
        _taskSource = new TaskDaySourceCache(taskCacheService);
        _undo = new UndoCoordinator(timeProvider, () => OnPropertyChanged(nameof(CurrentUndo)));

        _taskCacheService.NormalTasksChanged += OnNormalTasksChanged;
        _taskCacheService.TemplatesChanged += OnTemplatesChanged;
        _taskCacheService.DoneTasksChanged += OnDoneTasksChanged;
        _activeTaskService.ActiveTaskChanged += OnActiveTaskChanged;
        _dayChangeService.DayChanged += OnDayChanged;
        _scopeService.ScopeChanged += OnTaskScopeChanged;
        _taskEditor.StateChanged += OnTaskEditorStateChanged;
    }
    public async Task InitializeAsync()
    {
        await _appStartupStateService.WaitForReadyAsync();
        RefreshAllTasksFromCache();
        NotifyTaskListsChanged();
    }
    public void Dispose()
    {
        _undo.Dispose();
        _taskCacheService.NormalTasksChanged -= OnNormalTasksChanged;
        _taskCacheService.TemplatesChanged -= OnTemplatesChanged;
        _taskCacheService.DoneTasksChanged -= OnDoneTasksChanged;
        _activeTaskService.ActiveTaskChanged -= OnActiveTaskChanged;
        _dayChangeService.DayChanged -= OnDayChanged;
        _scopeService.ScopeChanged -= OnTaskScopeChanged;
        _taskEditor.StateChanged -= OnTaskEditorStateChanged;
    }

    public void ToggleFilterPanel()
    {
        IsFilterPanelOpen = !IsFilterPanelOpen;
    }

    public void CloseFilterPanel()
    {
        IsFilterPanelOpen = false;
    }

    public void ApplyOverviewFilters(FilterModel payload)
    {
        var previousDate = Filter.SelectedDate?.Date;
        Filter.Apply(payload);
        var shouldReloadTasks = previousDate != Filter.SelectedDate?.Date;

        if (shouldReloadTasks)
        {
            RefreshAllTasksFromCache();
        }
        else
        {
            ApplyTaskFilters();
        }

        NotifyTaskListsChanged();
    }

    public async Task HandleTaskEditPopupCloseAsync(TaskEditPopUpClosePayload payload)
    {
        var contextualDate = Filter.SelectedDate ?? GetToday();
        var result = await _taskEditor.HandleCloseAsync(payload, contextualDate, _taskSource.Templates);

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
                    .CaptureUiErrorAsync(result.Error!, $"{nameof(TaskOverViewViewModel)}.{nameof(HandleTaskEditPopupCloseAsync)}")
                    .ConfigureAwait(false);
                break;
        }
    }

    public void OpenAddNoteModal()
    {
        _taskEditor.Open(null, Filter.SelectedDate ?? GetToday(), _taskSource.Templates);
    }

    public void CloseAddNoteModal()
    {
        _taskEditor.Close();
    }

    public void StartEditTask(Guid taskId)
    {
        if (_activeTaskService.IsTaskActive(taskId))
        {
            return;
        }

        var task = _taskSource.FindById(taskId);
        if (task is null)
        {
            return;
        }

        _taskEditor.Open(
            TaskExtension.Clone(task),
            task.TaskDate ?? Filter.SelectedDate ?? GetToday(),
            _taskSource.Templates,
            seriesScope: GetSeriesScope(taskId));
    }

    public void OpenReschedulePopup(Guid taskId)
    {
        if (_activeTaskService.IsTaskActive(taskId) || _taskSource.FindById(taskId) is null)
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

        var task = _taskSource.FindById(taskId.Value);
        if (task is null || _activeTaskService.IsTaskActive(taskId.Value))
        {
            return;
        }

        await MoveTaskToDateAsync(task, newDate.Date, nameof(ConfirmRescheduleAsync));
    }

    public async Task AssignPastTaskAsync(Guid taskId, PastTaskTarget target)
    {
        var task = FindAssignablePastTask(taskId);
        if (task is null)
        {
            return;
        }

        var date = TaskExtension.ResolvePastTaskTargetDate(GetToday(), target);

        await MoveTaskToDateAsync(task, date, nameof(AssignPastTaskAsync));
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
            await CaptureErrorAsync(ex, nameof(DiscardPastTaskAsync));
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

    public async Task AssignAllPastTasksToTomorrowAsync()
    {
        var originals = OverdueTasks
            .Where(task => task.Id is Guid id && !_activeTaskService.IsTaskActive(id))
            .Select(TaskExtension.Clone)
            .ToList();
        if (originals.Count == 0)
        {
            return;
        }

        var tomorrow = GetToday().AddDays(1);
        var moved = new List<TaskDto>();

        try
        {
            foreach (var original in originals)
            {
                await _taskCacheService.MoveTaskToDateAsync(original, tomorrow);
                moved.Add(original);
            }
        }
        catch (Exception ex)
        {
            await CaptureErrorAsync(ex, nameof(AssignAllPastTasksToTomorrowAsync));
        }

        if (moved.Count > 0)
        {
            _undo.Show(
                new UndoNotice(UndoNoticeKind.TasksMovedToTomorrow, moved.Count),
                () => RestoreTasksAsync(moved));
        }
    }

    public Task UndoAsync() => _undo.UndoAsync(ex => CaptureErrorAsync(ex, nameof(UndoAsync)));

    public void DismissUndo() => _undo.Dismiss();

    public async Task DeleteTaskAsync(Guid taskId)
    {
        var task = _taskSource.FindById(taskId);
        if (_activeTaskService.IsTaskActive(taskId) || task is null)
        {
            return;
        }

        try
        {
            if (TaskExtension.GetSeriesId(task) is not null)
            {
                await _taskSeriesService
                    .DeleteTaskAsync(taskId, GetSeriesScope(taskId), _activeTaskService.ActiveTaskId)
                    .ConfigureAwait(false);
            }
            else
            {
                await _taskCacheService.DeleteTaskAsync(taskId).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService
                .CaptureUiErrorAsync(ex, $"{nameof(TaskOverViewViewModel)}.{nameof(DeleteTaskAsync)}")
                .ConfigureAwait(false);
        }
    }

    public bool IsTaskActive(Guid? taskId) => taskId.HasValue && _activeTaskService.IsTaskActive(taskId.Value);

    private void OnNormalTasksChanged(object? sender, EventArgs e)
    {
        _taskSource.RefreshNormal(Filter.SelectedDate);
        ApplyActiveTaskFilter();
        NotifyTaskListsChanged();
    }

    private void OnTemplatesChanged(object? sender, EventArgs e)
    {
        _taskSource.RefreshTemplate(Filter.SelectedDate);
        ApplyTemplateFilter();
        NotifyTaskListsChanged();
    }

    private void OnDoneTasksChanged(object? sender, EventArgs e)
    {
        _taskSource.RefreshDone(Filter.SelectedDate);
        ApplyCompletedTaskFilter();
        NotifyTaskListsChanged();
    }

    private void OnActiveTaskChanged(object? sender, EventArgs e) => NotifyTaskListsChanged();

    private void OnDayChanged(object? sender, DayChangedEventArgs e)
    {
        if (Filter.SelectedDate?.Date == e.PreviousDay)
        {
            Filter.SelectedDate = e.CurrentDay;
            RefreshAllTasksFromCache();
        }
        else
        {
            ApplyActiveTaskFilter();
        }

        NotifyTaskListsChanged();
    }

    private void OnTaskScopeChanged(object? sender, EventArgs e)
    {
        RefreshAllTasksFromCache();
        NotifyTaskListsChanged();
    }

    private void RefreshAllTasksFromCache()
    {
        _taskSource.RefreshNormal(Filter.SelectedDate);
        _taskSource.RefreshTemplate(Filter.SelectedDate);
        _taskSource.RefreshDone(Filter.SelectedDate);
        ApplyTaskFilters();
    }

    private DateTime GetToday() => _timeProvider.GetLocalNow().Date;

    private void NotifyTaskListsChanged()
    {
        OnPropertyChanged(nameof(ActiveTasks));
        OnPropertyChanged(nameof(OverdueTasks));
        OnPropertyChanged(nameof(Templates));
        OnPropertyChanged(nameof(CompletedTasks));
    }
    private void OnTaskEditorStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(TaskEditorInput));
        OnPropertyChanged(nameof(IsAddNoteModalOpen));
    }
    private void ApplyTaskFilters()
    {
        ApplyActiveTaskFilter();
        ApplyTemplateFilter();
        ApplyCompletedTaskFilter();
    }

    private void ApplyActiveTaskFilter()
    {
        var today = GetToday();
        var options = Filter.ToTaskFilterOptions();
        var upcomingTasks = _taskSource.NormalTasks.Where(task => !IsOverdue(task, today));
        var overdueTasks = _taskSource.NormalTasks.Where(task => IsOverdue(task, today));

        ActiveTasks = CollapseSeries(TaskFilterService.Apply(options, upcomingTasks));
        OverdueTasks = TaskFilterService.Apply(options, overdueTasks);
    }

    private static IReadOnlyList<TaskDto> CollapseSeries(IReadOnlyList<TaskDto> tasks)
    {
        var nextOfSeries = new Dictionary<Guid, TaskDto>();
        foreach (var task in tasks)
        {
            if (TaskExtension.GetSeriesId(task) is not { } seriesId)
            {
                continue;
            }

            if (!nextOfSeries.TryGetValue(seriesId, out var current) || task.TaskDate < current.TaskDate)
            {
                nextOfSeries[seriesId] = task;
            }
        }

        return tasks
            .Where(task => TaskExtension.GetSeriesId(task) is not { } seriesId ||
                ReferenceEquals(nextOfSeries[seriesId], task))
            .ToList();
    }

    private TaskSeriesScope GetSeriesScope(Guid taskId)
    {
        return ActiveTasks.Any(task => task.Id == taskId)
            ? TaskSeriesScope.WholeSeries
            : TaskSeriesScope.ThisTaskOnly;
    }

    private static bool IsOverdue(TaskDto task, DateTime today) => TaskExtension.IsPastOpenTask(task, today);

    private TaskDto? FindAssignablePastTask(Guid taskId)
    {
        if (_activeTaskService.IsTaskActive(taskId))
        {
            return null;
        }

        var task = _taskSource.FindById(taskId);
        return task is not null && IsOverdue(task, GetToday()) ? task : null;
    }

    private async Task MoveTaskToDateAsync(TaskDto task, DateTime date, string caller)
    {
        try
        {
            await _taskCacheService.MoveTaskToDateAsync(task, date);
        }
        catch (Exception ex)
        {
            await CaptureErrorAsync(ex, caller);
        }
    }

    private async Task RestoreTasksAsync(IReadOnlyList<TaskDto> originals)
    {
        foreach (var original in originals)
        {
            await _taskCacheService.UpdateTaskAsync(original);
        }
    }

    private Task CaptureErrorAsync(Exception exception, string caller) =>
        _appErrorCaptureService.CaptureUiErrorAsync(exception, $"{nameof(TaskOverViewViewModel)}.{caller}");

    private void ApplyTemplateFilter()
    {
        Templates = TaskFilterService.Apply(
            Filter.ToTaskFilterOptions(keepTemplatesVisibleWhenDayFiltered: true),
            _taskSource.Templates);
    }

    private void ApplyCompletedTaskFilter()
    {
        CompletedTasks = TaskFilterService.Apply(Filter.ToTaskFilterOptions(), _taskSource.DoneTasks);
    }
}
