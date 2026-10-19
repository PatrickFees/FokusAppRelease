using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Extensions;

namespace FocusAppTests.Fakes;

/// <summary>
/// Zustandsbehaftetes Fake mit denselben Kategorie-Regeln und derselben Sortierung wie das echte <c>TaskRepository</c>.
/// </summary>
public sealed class InMemoryTaskRepository : ITaskRepository
{
    private readonly List<TaskEntity> _tasks = [];

    public int GetNormalCallCount { get; private set; }
    public int GetRegularCallCount { get; private set; }
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
        var normalTasks = Query(task => !task.IsDone && !task.IsRegularTask);
        return Task.FromResult(normalTasks);
    }

    public Task<List<TaskEntity>> GetRegularTasksAsync()
    {
        GetRegularCallCount++;
        var regularTasks = Query(task => !task.IsDone && task.IsRegularTask);
        return Task.FromResult(regularTasks);
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

    public Task DeleteAllAsync()
    {
        _tasks.Clear();

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

        return copy;
    }
}
