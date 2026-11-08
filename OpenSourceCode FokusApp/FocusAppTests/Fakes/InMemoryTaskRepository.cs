using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Extensions;

namespace FocusAppTests.Fakes;

public sealed class InMemoryTaskRepository : ITaskRepository
{
    private readonly List<TaskEntity> _tasks = [];

    public int GetNormalCallCount { get; private set; }
    public int GetTemplateCallCount { get; private set; }
    public int GetDoneCallCount { get; private set; }
    public List<TaskEntity> AddedTasks { get; } = [];
    public List<TaskEntity> UpdatedTasks { get; } = [];
    public List<Guid> DeletedIds { get; } = [];

    public void Seed(params TaskEntity[] tasks)
    {
        var copies = tasks.Select(Copy).ToList();
        _tasks.AddRange(copies);
    }

    public Task<List<TaskEntity>> GetAllAsync()
    {
        var allTasks = Query(_ => true);
        return Task.FromResult(allTasks);
    }

    public Task<List<TaskEntity>> GetNormalTasksAsync()
    {
        GetNormalCallCount++;
        var normalTasks = Query(task => !task.IsDone && !task.IsTemplate);
        return Task.FromResult(normalTasks);
    }

    public Task<List<TaskEntity>> GetTemplatesAsync()
    {
        GetTemplateCallCount++;
        var templates = Query(task => !task.IsDone && task.IsTemplate);
        return Task.FromResult(templates);
    }

    public Task<List<TaskEntity>> GetDoneTasksAsync()
    {
        GetDoneCallCount++;
        var doneTasks = Query(task => task.IsDone);
        return Task.FromResult(doneTasks);
    }

    public Task AddAsync(TaskEntity task)
    {
        var loggedCopy = Copy(task);
        AddedTasks.Add(loggedCopy);

        var storedCopy = Copy(task);
        _tasks.Add(storedCopy);

        return Task.CompletedTask;
    }

    public async Task AddRangeAsync(IEnumerable<TaskEntity> tasks)
    {
        foreach (var task in tasks)
        {
            await AddAsync(task);
        }
    }

    public Task UpdateAsync(TaskEntity task)
    {
        var loggedCopy = Copy(task);
        UpdatedTasks.Add(loggedCopy);

        var existingTask = _tasks.FirstOrDefault(stored => stored.Id == task.Id);
        existingTask?.UpdateFrom(task);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        DeletedIds.Add(id);
        _tasks.RemoveAll(task => task.Id == id);

        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        DeletedIds.AddRange(idList);
        _tasks.RemoveAll(task => idList.Contains(task.Id));

        return Task.CompletedTask;
    }

    public Task DeleteAllAsync()
    {
        _tasks.Clear();

        return Task.CompletedTask;
    }

    public Task ApplyCalendarSyncUpdateAsync(
        Guid taskId, string? note, TimeSpan? taskTime, int timerMinutes, int? reminderMinutesBefore)
    {
        var existingTask = _tasks.FirstOrDefault(task => task.Id == taskId);
        if (existingTask is null)
        {
            return Task.CompletedTask;
        }

        existingTask.Note = note;
        existingTask.TaskTime = taskTime;
        existingTask.TimerMinutes = timerMinutes;
        existingTask.ReminderMinutesBefore = reminderMinutesBefore;
        existingTask.LastSyncedNote = note;
        existingTask.LastSyncedTaskTime = taskTime;
        existingTask.LastSyncedTimerMinutes = timerMinutes;
        existingTask.LastSyncedReminderMinutesBefore = reminderMinutesBefore;

        return Task.CompletedTask;
    }

    public Task DetachFromCalendarSyncAsync(Guid taskId)
    {
        var existingTask = _tasks.FirstOrDefault(task => task.Id == taskId);
        if (existingTask is null)
        {
            return Task.CompletedTask;
        }

        existingTask.SourceCalendarEventId = null;
        existingTask.LastSyncedNote = null;
        existingTask.LastSyncedTaskTime = null;
        existingTask.LastSyncedTimerMinutes = null;
        existingTask.LastSyncedReminderMinutesBefore = null;

        return Task.CompletedTask;
    }

    private List<TaskEntity> Query(Func<TaskEntity, bool> predicate)
    {
        var matchingTasks = _tasks.Where(predicate);
        var sortedTasks = matchingTasks.OrderByDescending(task => task.CreationDate);
        var copies = sortedTasks.Select(Copy);

        return copies.ToList();
    }

    private static TaskEntity Copy(TaskEntity source)
    {
        var copy = new TaskEntity { Id = source.Id };
        copy.UpdateFrom(source);
        copy.LastSyncedNote = source.LastSyncedNote;
        copy.LastSyncedTaskTime = source.LastSyncedTaskTime;
        copy.LastSyncedTimerMinutes = source.LastSyncedTimerMinutes;
        copy.LastSyncedReminderMinutesBefore = source.LastSyncedReminderMinutesBefore;

        return copy;
    }
}
