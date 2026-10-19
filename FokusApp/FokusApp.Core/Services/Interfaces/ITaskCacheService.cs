using FokusApp.Core.Models;

namespace FokusApp.Core.Services.Interfaces;

public interface ITaskCacheService
{
    event EventHandler? TasksLoaded;
    event EventHandler? NormalTasksChanged;
    event EventHandler? RegularTasksChanged;
    event EventHandler? DoneTasksChanged;

    Task InitializeAsync();
    Task ReloadAsync();

    IReadOnlyList<TaskDto> GetNormalTasks(DateTime? selectedDay = null);
    IReadOnlyList<TaskDto> GetRegularTasks(DateTime? selectedDay = null);
    IReadOnlyList<TaskDto> GetDoneTasks(DateTime? selectedDay = null);
    TaskDto? FindTaskById(Guid id);

    Task AddTaskAsync(TaskDto task);
    Task UpdateTaskAsync(TaskDto task);
    Task DeleteTaskAsync(Guid id);
    Task FinishTaskAsync(TaskDto task);
}
