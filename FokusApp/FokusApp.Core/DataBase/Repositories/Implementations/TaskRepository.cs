using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FokusApp.Core.DataBase.Repositories.Implementations;

public sealed class TaskRepository : ITaskRepository
{
    private readonly IDbContextFactory<FokusAppDbContext> _contextFactory;

    public TaskRepository(IDbContextFactory<FokusAppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public Task<List<TaskEntity>> GetAllAsync() => GetTasksAsync(_ => true);

    public Task<List<TaskEntity>> GetNormalTasksAsync()
    {
        return GetTasksAsync(task => !task.IsDone && !task.IsRegularTask);
    }

    public Task<List<TaskEntity>> GetRegularTasksAsync()
    {
        return GetTasksAsync(task => !task.IsDone && task.IsRegularTask);
    }

    public Task<List<TaskEntity>> GetDoneTasksAsync()
    {
        return GetTasksAsync(task => task.IsDone);
    }

    public async Task AddAsync(TaskEntity task)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await context.Tasks.AddAsync(task).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(TaskEntity task)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var existingTask = await context.Tasks
            .FirstOrDefaultAsync(taskEntity => taskEntity.Id == task.Id)
            .ConfigureAwait(false);

        if (existingTask is null)
        {
            return;
        }

        existingTask.UpdateFrom(task);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var entity = await context.Tasks
            .FirstOrDefaultAsync(task => task.Id == id)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return;
        }

        context.Tasks.Remove(entity);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task AddRangeAsync(IEnumerable<TaskEntity> tasks)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await context.Tasks.AddRangeAsync(tasks).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await context.Tasks.ExecuteDeleteAsync().ConfigureAwait(false);
    }

    private async Task<List<TaskEntity>> GetTasksAsync(Expression<Func<TaskEntity, bool>> predicate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.Tasks
            .AsNoTracking()
            .Where(predicate)
            .OrderByDescending(task => task.CreationDate)
            .ToListAsync()
            .ConfigureAwait(false);
    }
}
