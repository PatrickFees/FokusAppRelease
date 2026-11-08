using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Enums;
using FokusApp.Core.Extensions;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class TaskCacheService : ITaskCacheService
{
    public event EventHandler? TasksLoaded;
    public event EventHandler? NormalTasksChanged;
    public event EventHandler? TemplatesChanged;
    public event EventHandler? DoneTasksChanged;
    public event EventHandler<TaskCompletedEventArgs>? TaskCompleted;

    private readonly ITaskRepository _taskRepository;
    private readonly IScopeService _scopeService;
    private readonly TimeProvider _timeProvider;
    private readonly ReaderWriterLockSlim _cacheLock = new();
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly SemaphoreSlim _taskMutationLock = new(1, 1);

    private List<TaskDto> _normalTasks = [];
    private List<TaskDto> _templates = [];
    private List<TaskDto> _doneTasks = [];
    private volatile bool _initialized;

    public TaskCacheService(ITaskRepository taskRepository, IScopeService scopeService, TimeProvider timeProvider)
    {
        _taskRepository = taskRepository;
        _scopeService = scopeService;
        _timeProvider = timeProvider;
    }

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _initializationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            await LoadTasksAsync().ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async Task ReloadAsync()
    {
        await _initializationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await _taskMutationLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var taskSnapshots = await LoadTaskSnapshotsAsync(
                    TaskCategory.Normal,
                    TaskCategory.Template,
                    TaskCategory.Done).ConfigureAwait(false);

                _cacheLock.EnterWriteLock();
                try
                {
                    ReplaceTaskSnapshots(taskSnapshots);
                    _initialized = true;
                }
                finally
                {
                    _cacheLock.ExitWriteLock();
                }
            }
            finally
            {
                _taskMutationLock.Release();
            }
        }
        finally
        {
            _initializationLock.Release();
        }

        TasksLoaded?.Invoke(this, EventArgs.Empty);
        NormalTasksChanged?.Invoke(this, EventArgs.Empty);
        TemplatesChanged?.Invoke(this, EventArgs.Empty);
        DoneTasksChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task LoadTasksAsync()
    {
        await _taskMutationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var taskSnapshots = await LoadTaskSnapshotsAsync(
                TaskCategory.Normal,
                TaskCategory.Template,
                TaskCategory.Done).ConfigureAwait(false);

            _cacheLock.EnterWriteLock();
            try
            {
                ReplaceTaskSnapshots(taskSnapshots);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }
        finally
        {
            _taskMutationLock.Release();
        }

        TasksLoaded?.Invoke(this, EventArgs.Empty);
    }

    public IReadOnlyList<TaskDto> GetNormalTasks(DateTime? selectedDay = null) =>
        GetTaskSnapshot(_normalTasks, selectedDay);

    public IReadOnlyList<TaskDto> GetTemplates(DateTime? selectedDay = null) =>
        GetTaskSnapshot(_templates, selectedDay, includeUndatedTemplates: true);

    public IReadOnlyList<TaskDto> GetDoneTasks(DateTime? selectedDay = null) =>
        GetTaskSnapshot(_doneTasks, selectedDay);

    public TaskDto? FindTaskById(Guid id)
    {
        _cacheLock.EnterReadLock();
        try
        {
            var task = FindTask(id);
            return task is null ? null : TaskExtension.Clone(task);
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }
    }

    public async Task AddTaskAsync(TaskDto task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.IsDone)
        {
            throw new InvalidOperationException("Erledigte Aufgaben können nicht direkt hinzugefügt werden.");
        }

        task.Id = NormalizeId(task.Id) ?? Guid.NewGuid();
        var cachedTask = TaskExtension.NormalizeTaskForPersistence(task);
        var category = TaskExtension.GetCategory(cachedTask);

        await _taskMutationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var entity = TaskExtension.MapToEntity(cachedTask);
            await _taskRepository.AddAsync(entity).ConfigureAwait(false);
            var taskSnapshots = await LoadTaskSnapshotsAsync(category).ConfigureAwait(false);

            _cacheLock.EnterWriteLock();
            try
            {
                ReplaceTaskSnapshots(taskSnapshots);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }
        finally
        {
            _taskMutationLock.Release();
        }

        RaiseCategoryChanged(category);
    }

    public async Task UpdateTaskAsync(TaskDto task)
    {
        ArgumentNullException.ThrowIfNull(task);
        var taskId = RequireTaskId(task);
        var updatedTask = TaskExtension.NormalizeTaskForPersistence(task);
        TaskCategory previousCategory;
        TaskCategory nextCategory;

        await _taskMutationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            TaskDto existingTask;
            _cacheLock.EnterReadLock();
            try
            {
                existingTask = FindTask(taskId) is { } found
                    ? TaskExtension.Clone(found)
                    : throw new KeyNotFoundException($"Task with id '{taskId}' does not exist.");
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }

            if (updatedTask.IsDone && existingTask.IsDone && updatedTask.CompletedAt is null)
            {
                updatedTask.CompletedAt = existingTask.CompletedAt;
            }

            previousCategory = TaskExtension.GetCategory(existingTask);
            nextCategory = TaskExtension.GetCategory(updatedTask);

            await _taskRepository.UpdateAsync(TaskExtension.MapToEntity(updatedTask)).ConfigureAwait(false);
            var taskSnapshots = await LoadTaskSnapshotsAsync(previousCategory, nextCategory).ConfigureAwait(false);

            _cacheLock.EnterWriteLock();
            try
            {
                ReplaceTaskSnapshots(taskSnapshots);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }
        finally
        {
            _taskMutationLock.Release();
        }

        RaiseCategoryTransition(previousCategory, nextCategory);
    }

    public async Task MoveTaskToDateAsync(TaskDto task, DateTime date)
    {
        ArgumentNullException.ThrowIfNull(task);

        var updatedTask = TaskExtension.Clone(task);
        updatedTask.TaskDate = date.Date;
        await UpdateTaskAsync(updatedTask).ConfigureAwait(false);
    }

    public async Task DeleteTaskAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return;
        }

        TaskCategory category;

        await _taskMutationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            _cacheLock.EnterReadLock();
            try
            {
                var existingTask = FindTask(id);
                if (existingTask is null)
                {
                    return;
                }

                category = TaskExtension.GetCategory(existingTask);
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }

            await _taskRepository.DeleteAsync(id).ConfigureAwait(false);
            var taskSnapshots = await LoadTaskSnapshotsAsync(category).ConfigureAwait(false);

            _cacheLock.EnterWriteLock();
            try
            {
                ReplaceTaskSnapshots(taskSnapshots);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }
        finally
        {
            _taskMutationLock.Release();
        }

        RaiseCategoryChanged(category);
    }

    public async Task FinishTaskAsync(TaskDto task)
    {
        ArgumentNullException.ThrowIfNull(task);
        var taskId = RequireTaskId(task);
        TaskCategory previousCategory;
        TaskCategory nextCategory;
        var isTemplate = false;
        var completedTaskId = taskId;
        var isPositive = false;

        await _taskMutationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            TaskDto existingTask;
            _cacheLock.EnterReadLock();
            try
            {
                existingTask = FindTask(taskId) is { } found
                    ? TaskExtension.Clone(found)
                    : throw new KeyNotFoundException($"Task with id '{taskId}' does not exist.");
            }
            finally
            {
                _cacheLock.ExitReadLock();
            }

            previousCategory = TaskExtension.GetCategory(existingTask);
            nextCategory = TaskCategory.Done;
            isTemplate = existingTask.IsTemplate;
            isPositive = existingTask.IsPositive;

            IReadOnlyDictionary<TaskCategory, List<TaskDto>> taskSnapshots;
            if (!isTemplate)
            {
                existingTask.IsDone = true;
                existingTask.CompletedAt = _timeProvider.GetLocalNow().DateTime;
                await _taskRepository.UpdateAsync(TaskExtension.MapToEntity(existingTask)).ConfigureAwait(false);
                taskSnapshots = await LoadTaskSnapshotsAsync(previousCategory, nextCategory).ConfigureAwait(false);
            }
            else
            {
                var historyEntry = TaskExtension.Clone(existingTask);
                historyEntry.Id = Guid.NewGuid();
                completedTaskId = historyEntry.Id.Value;
                historyEntry.IsDone = true;
                historyEntry.IsTemplate = false;
                var now = _timeProvider.GetLocalNow();
                historyEntry.CreationDate = now.DateTime;
                historyEntry.CompletedAt = now.DateTime;
                historyEntry.TaskDate = now.Date;
                await _taskRepository.AddAsync(TaskExtension.MapToEntity(historyEntry)).ConfigureAwait(false);
                taskSnapshots = await LoadTaskSnapshotsAsync(TaskCategory.Done).ConfigureAwait(false);
            }

            _cacheLock.EnterWriteLock();
            try
            {
                ReplaceTaskSnapshots(taskSnapshots);
            }
            finally
            {
                _cacheLock.ExitWriteLock();
            }
        }
        finally
        {
            _taskMutationLock.Release();
        }

        if (isTemplate)
        {
            RaiseCategoryChanged(TaskCategory.Done);
        }
        else
        {
            RaiseCategoryTransition(previousCategory, nextCategory);
        }

        TaskCompleted?.Invoke(this, new TaskCompletedEventArgs(completedTaskId, isPositive));
    }

    private async Task<IReadOnlyDictionary<TaskCategory, List<TaskDto>>> LoadTaskSnapshotsAsync(
        params TaskCategory[] categories)
    {
        var distinctCategories = categories.Distinct().ToArray();
        var loadOperations = distinctCategories.ToDictionary(
            category => category,
            LoadTaskEntitiesAsync);

        await Task.WhenAll(loadOperations.Values).ConfigureAwait(false);

        var snapshots = new Dictionary<TaskCategory, List<TaskDto>>(distinctCategories.Length);
        foreach (var category in distinctCategories)
        {
            var entities = await loadOperations[category].ConfigureAwait(false);
            snapshots[category] = entities.Select(TaskExtension.MapToDto).ToList();
        }

        return snapshots;
    }

    private Task<List<TaskEntity>> LoadTaskEntitiesAsync(TaskCategory category) => category switch
    {
        TaskCategory.Normal => _taskRepository.GetNormalTasksAsync(),
        TaskCategory.Template => _taskRepository.GetTemplatesAsync(),
        TaskCategory.Done => _taskRepository.GetDoneTasksAsync(),
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    private void ReplaceTaskSnapshots(IReadOnlyDictionary<TaskCategory, List<TaskDto>> snapshots)
    {
        foreach (var (category, tasks) in snapshots)
        {
            switch (category)
            {
                case TaskCategory.Normal:
                    _normalTasks = tasks;
                    break;
                case TaskCategory.Template:
                    _templates = tasks;
                    break;
                case TaskCategory.Done:
                    _doneTasks = tasks;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category));
            }
        }
    }

    private IReadOnlyList<TaskDto> GetTaskSnapshot(
        List<TaskDto> source,
        DateTime? selectedDay,
        bool includeUndatedTemplates = false)
    {
        _cacheLock.EnterReadLock();
        try
        {
            var selectedDate = selectedDay?.Date;
            var isWorkScope = _scopeService.IsWorkScope;
            return source
                .Where(task => task.IsWorkTask == isWorkScope)
                .Where(task =>
                    !selectedDate.HasValue ||
                    task.TaskDate.HasValue && task.TaskDate.Value.Date == selectedDate.Value ||
                    includeUndatedTemplates && IsOpenTemplate(task))
                .Select(TaskExtension.Clone)
                .ToArray();
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }
    }

    private TaskDto? FindTask(Guid id)
    {
        return _normalTasks.FirstOrDefault(task => NormalizeId(task.Id) == id)
            ?? _templates.FirstOrDefault(task => NormalizeId(task.Id) == id)
            ?? _doneTasks.FirstOrDefault(task => NormalizeId(task.Id) == id);
    }

    private static bool IsOpenTemplate(TaskDto task) => task.IsTemplate && !task.IsDone;

    private static Guid RequireTaskId(TaskDto task)
    {
        return NormalizeId(task.Id)
            ?? throw new InvalidOperationException("Die Aufgabe besitzt keine persistierbare ID.");
    }

    private static Guid? NormalizeId(Guid? id) => id is null || id == Guid.Empty ? null : id;

    private void RaiseCategoryTransition(TaskCategory previousCategory, TaskCategory nextCategory)
    {
        RaiseCategoryChanged(previousCategory);
        if (nextCategory != previousCategory)
        {
            RaiseCategoryChanged(nextCategory);
        }
    }

    private void RaiseCategoryChanged(TaskCategory category)
    {
        switch (category)
        {
            case TaskCategory.Normal:
                NormalTasksChanged?.Invoke(this, EventArgs.Empty);
                break;
            case TaskCategory.Template:
                TemplatesChanged?.Invoke(this, EventArgs.Empty);
                break;
            case TaskCategory.Done:
                DoneTasksChanged?.Invoke(this, EventArgs.Empty);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(category));
        }
    }
}
