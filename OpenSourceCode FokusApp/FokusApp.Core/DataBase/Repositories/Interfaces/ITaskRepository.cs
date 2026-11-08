using FokusApp.Core.DataBase.Entities;

namespace FokusApp.Core.DataBase.Repositories.Interfaces;

public interface ITaskRepository
{
    Task<List<TaskEntity>> GetAllAsync();
    Task<List<TaskEntity>> GetNormalTasksAsync();
    Task<List<TaskEntity>> GetTemplatesAsync();
    Task<List<TaskEntity>> GetDoneTasksAsync();
    Task AddAsync(TaskEntity task);
    Task AddRangeAsync(IEnumerable<TaskEntity> tasks);
    Task UpdateAsync(TaskEntity task);
    Task DeleteAsync(Guid id);
    Task DeleteRangeAsync(IEnumerable<Guid> ids);
    Task DeleteAllAsync();

    Task ApplyCalendarSyncUpdateAsync(
        Guid taskId, string? note, TimeSpan? taskTime, int timerMinutes, int? reminderMinutesBefore);

    Task DetachFromCalendarSyncAsync(Guid taskId);
}
