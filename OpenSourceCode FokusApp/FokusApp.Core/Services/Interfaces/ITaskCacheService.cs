using FokusApp.Core.Models;

namespace FokusApp.Core.Services.Interfaces;

public interface ITaskCacheService
{
    event EventHandler? TasksLoaded;
    event EventHandler? NormalTasksChanged;
    event EventHandler? TemplatesChanged;
    event EventHandler? DoneTasksChanged;
    event EventHandler<TaskCompletedEventArgs>? TaskCompleted;

    Task InitializeAsync();
    Task ReloadAsync();

    IReadOnlyList<TaskDto> GetNormalTasks(DateTime? selectedDay = null);
    IReadOnlyList<TaskDto> GetTemplates(DateTime? selectedDay = null);
    IReadOnlyList<TaskDto> GetDoneTasks(DateTime? selectedDay = null);
    TaskDto? FindTaskById(Guid id);

    Task AddTaskAsync(TaskDto task);
    Task UpdateTaskAsync(TaskDto task);
    Task MoveTaskToDateAsync(TaskDto task, DateTime date);
    Task DeleteTaskAsync(Guid id);
    Task FinishTaskAsync(TaskDto task);
}
