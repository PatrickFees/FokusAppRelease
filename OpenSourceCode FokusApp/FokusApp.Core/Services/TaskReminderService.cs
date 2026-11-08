using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Extensions;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class TaskReminderService : ITaskReminderService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskCacheService _taskCacheService;
    private readonly IDayChangeService _dayChangeService;
    private readonly ITaskReminderPlatformAdapter _platformAdapter;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private bool _started;

    public TaskReminderService(
        ITaskRepository taskRepository,
        ITaskCacheService taskCacheService,
        IDayChangeService dayChangeService,
        ITaskReminderPlatformAdapter platformAdapter,
        IAppErrorCaptureService appErrorCaptureService,
        TimeProvider timeProvider)
    {
        _taskRepository = taskRepository;
        _taskCacheService = taskCacheService;
        _dayChangeService = dayChangeService;
        _platformAdapter = platformAdapter;
        _appErrorCaptureService = appErrorCaptureService;
        _timeProvider = timeProvider;
    }

    public async Task StartAsync()
    {
        if (!_started)
        {
            _started = true;
            _taskCacheService.NormalTasksChanged += OnTasksChanged;
            _taskCacheService.DoneTasksChanged += OnTasksChanged;
            _dayChangeService.DayChanged += OnDayChanged;
        }

        await RefreshSafelyAsync().ConfigureAwait(false);
    }

    public async Task RefreshAsync()
    {
        await _refreshLock.WaitAsync().ConfigureAwait(false);
        try
        {

            var entities = await _taskRepository.GetNormalTasksAsync().ConfigureAwait(false);
            var tasks = entities.Select(TaskExtension.MapToDto);
            var reminders = TaskReminderPlanner.Plan(tasks, _timeProvider.GetLocalNow().DateTime);
            await _platformAdapter.SyncAsync(reminders).ConfigureAwait(false);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private void OnTasksChanged(object? sender, EventArgs e) => _ = RefreshSafelyAsync();

    private void OnDayChanged(object? sender, DayChangedEventArgs e) => _ = RefreshSafelyAsync();

    private async Task RefreshSafelyAsync()
    {
        try
        {
            await RefreshAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureAsync(
                ex,
                new AppErrorCaptureOptions
                {
                    Source = $"{nameof(TaskReminderService)}.{nameof(RefreshAsync)}",
                    IsFatal = false
                }).ConfigureAwait(false);
        }
    }
}
