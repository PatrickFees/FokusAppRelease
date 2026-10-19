using FokusApp.Core.Extensions;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace FokusApp.Core.Services;

public sealed class ActiveTaskService : IActiveTaskService
{
    public event EventHandler? ActiveTaskChanged;

    private readonly ITaskCacheService _taskCacheService;
    private readonly IScopeService _scopeService;
    private readonly TimeProvider _timeProvider;
    private readonly ReaderWriterLockSlim _cacheLock = new();
    private TaskDto _activeTask;

    public ActiveTaskService(
        ITaskCacheService taskCacheService,
        IScopeService scopeService,
        TimeProvider timeProvider)
    {
        _taskCacheService = taskCacheService;
        _scopeService = scopeService;
        _timeProvider = timeProvider;
        _activeTask = CreateDefaultActiveTask();

        _taskCacheService.TasksLoaded += OnTasksLoaded;
        _taskCacheService.NormalTasksChanged += OnTaskCacheChanged;
        _taskCacheService.RegularTasksChanged += OnTaskCacheChanged;
        _taskCacheService.DoneTasksChanged += OnTaskCacheChanged;
        _scopeService.ScopeChanged += OnScopeChanged;
    }

    private TaskDto CreateDefaultActiveTask()
    {
        var today = _timeProvider.GetLocalNow().Date;
        return TaskExtension.CreateDefaultActiveTask(_scopeService.IsWorkScope, today);
    }

    public bool IsTaskActive(Guid taskId) => taskId != Guid.Empty && ActiveTaskId == taskId;
    public Guid? ActiveTaskId
    {
        get
        {
            _cacheLock.EnterReadLock();
            try
            {
                return NormalizeId(_activeTask.Id);
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }
    }

    public TaskDto ActiveTask
    {
        get
        {
            _cacheLock.EnterReadLock();
            try
            {
                return TaskExtension.Clone(_activeTask);
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }
        }
    }

    public void SetActiveTask(Guid? taskId)
    {
        var normalizedId = NormalizeId(taskId);

        _cacheLock.EnterWriteLock();
        try
        {
            var task = normalizedId.HasValue
                ? _taskCacheService.FindTaskById(normalizedId.Value)
                : null;

            if (!CanBecomeActiveTask(task))
            {
                task = CreateDefaultActiveTask();
            }

            if (NormalizeId(_activeTask.Id) == NormalizeId(task.Id))
            {
                return;
            }

            _activeTask = task;
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }

        ActiveTaskChanged?.Invoke(this, EventArgs.Empty);
    }



    private void OnTasksLoaded(object? sender, EventArgs e)
    {
        _cacheLock.EnterWriteLock();
        try
        {
            _activeTask = CreateDefaultActiveTask();
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }

        ActiveTaskChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnTaskCacheChanged(object? sender, EventArgs e)
    {
        var changed = false;

        _cacheLock.EnterWriteLock();
        try
        {
            var currentId = NormalizeId(_activeTask.Id);
            if (currentId is null)
            {
                return;
            }

            var found = _taskCacheService.FindTaskById(currentId.Value);
            if (!CanBecomeActiveTask(found))
            {
                found = CreateDefaultActiveTask();
            }

            _activeTask = found;


            changed = true;
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }

        if (changed)
        {
            ActiveTaskChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnScopeChanged(object? sender, EventArgs e)
    {
        _cacheLock.EnterWriteLock();
        try
        {
            _activeTask = CreateDefaultActiveTask();
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }

        ActiveTaskChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool CanBecomeActiveTask([NotNullWhen(true)] TaskDto? task) => task is not null && task.IsWorkTask == _scopeService.IsWorkScope && !task.IsDone;

    private static Guid? NormalizeId(Guid? id) => id is null || id == Guid.Empty ? null : id;
}
