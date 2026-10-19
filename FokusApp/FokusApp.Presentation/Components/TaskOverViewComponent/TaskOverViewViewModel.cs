using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Components.PopUps.TaskEdit;
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
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly TaskDaySourceCache _taskSource;
    private readonly TimeProvider _timeProvider;
    private readonly IDayChangeService _dayChangeService;


    public IReadOnlyList<TaskDto> ActiveTasks { get; private set; } = [];
    public IReadOnlyList<TaskDto> RegularTasks { get; private set; } = [];
    public IReadOnlyList<TaskDto> CompletedTasks { get; private set; } = [];
    public FilterModel Filter { get; } = new();

    [ObservableProperty]
    private bool isFilterPanelOpen;

    public TaskEditPopUpInputModel? TaskEditorInput => _taskEditor.Input;
    public bool IsAddNoteModalOpen => _taskEditor.IsOpen;


    public TaskOverViewViewModel(
        ITaskCacheService taskCacheService,
        IActiveTaskService activeTaskService,
        IScopeService scopeService,
        IAppStartupStateService appStartupStateService,
        TaskEditorCoordinator taskEditor,
        IAppErrorCaptureService appErrorCaptureService,
        TimeProvider timeProvider,
        IDayChangeService dayChangeService)
    {
        _timeProvider = timeProvider;
        _dayChangeService = dayChangeService;
        Filter.SelectedDate = GetToday();
        _taskCacheService = taskCacheService;
        _activeTaskService = activeTaskService;
        _scopeService = scopeService;
        _appStartupStateService = appStartupStateService;
        _taskEditor = taskEditor;
        _appErrorCaptureService = appErrorCaptureService;
        _taskSource = new TaskDaySourceCache(taskCacheService);

        _taskCacheService.NormalTasksChanged += OnNormalTasksChanged;
        _taskCacheService.RegularTasksChanged += OnRegularTasksChanged;
        _taskCacheService.DoneTasksChanged += OnDoneTasksChanged;
        _activeTaskService.ActiveTaskChanged += OnActiveTaskChanged;
        _dayChangeService.DayChanged += OnDayChanged;
        _scopeService.ScopeChanged += OnTaskScopeChanged;
    }
    public async Task InitializeAsync()
    {
        await _appStartupStateService.WaitForReadyAsync();
        RefreshAllTasksFromCache();
        NotifyTaskListsChanged();
    }
    public void Dispose()
    {
        _taskCacheService.NormalTasksChanged -= OnNormalTasksChanged;
        _taskCacheService.RegularTasksChanged -= OnRegularTasksChanged;
        _taskCacheService.DoneTasksChanged -= OnDoneTasksChanged;
        _activeTaskService.ActiveTaskChanged -= OnActiveTaskChanged;
        _dayChangeService.DayChanged -= OnDayChanged;
        _scopeService.ScopeChanged -= OnTaskScopeChanged;
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
        var result = await _taskEditor.HandleCloseAsync(payload, contextualDate, _taskSource.RegularTasks);

        switch (result.Outcome)
        {
            case TaskEditorCloseOutcome.Closed:
            case TaskEditorCloseOutcome.TemplateCopyOpened:
                NotifyEditorStateChanged();
                break;
            case TaskEditorCloseOutcome.TemplateEditRequested:
                StartEditTask(payload.RegularTemplateTaskId!.Value);
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
        _taskEditor.Open(null, Filter.SelectedDate ?? GetToday(), _taskSource.RegularTasks);
        NotifyEditorStateChanged();
    }

    public void CloseAddNoteModal()
    {
        _taskEditor.Close();
        NotifyEditorStateChanged();
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
            _taskSource.RegularTasks);
        NotifyEditorStateChanged();
    }

    public async Task DeleteTaskAsync(Guid taskId)
    {
        if (_activeTaskService.IsTaskActive(taskId) || _taskSource.FindById(taskId) is null)
        {
            return;
        }

        try
        {
            await _taskCacheService.DeleteTaskAsync(taskId).ConfigureAwait(false);
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

    private void OnRegularTasksChanged(object? sender, EventArgs e)
    {
        _taskSource.RefreshRegular(Filter.SelectedDate);
        ApplyRegularTaskFilter();
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
        if (Filter.SelectedDate?.Date != e.PreviousDay)
        {
            return;
        }

        Filter.SelectedDate = e.CurrentDay;
        RefreshAllTasksFromCache();
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
        _taskSource.RefreshRegular(Filter.SelectedDate);
        _taskSource.RefreshDone(Filter.SelectedDate);
        ApplyTaskFilters();
    }

    private DateTime GetToday() => _timeProvider.GetLocalNow().Date;

    private void NotifyTaskListsChanged()
    {
        OnPropertyChanged(nameof(ActiveTasks));
        OnPropertyChanged(nameof(RegularTasks));
        OnPropertyChanged(nameof(CompletedTasks));
    }
    private void NotifyEditorStateChanged()
    {
        OnPropertyChanged(nameof(TaskEditorInput));
        OnPropertyChanged(nameof(IsAddNoteModalOpen));
    }
    private void ApplyTaskFilters()
    {
        ApplyActiveTaskFilter();
        ApplyRegularTaskFilter();
        ApplyCompletedTaskFilter();
    }

    private void ApplyActiveTaskFilter()
    {
        ActiveTasks = TaskFilterService.Apply(Filter.ToTaskFilterOptions(), _taskSource.NormalTasks);
    }

    private void ApplyRegularTaskFilter()
    {
        RegularTasks = TaskFilterService.Apply(
            Filter.ToTaskFilterOptions(keepRegularTemplatesVisibleWhenDayFiltered: true),
            _taskSource.RegularTasks);
    }

    private void ApplyCompletedTaskFilter()
    {
        CompletedTasks = TaskFilterService.Apply(Filter.ToTaskFilterOptions(), _taskSource.DoneTasks);
    }
}
