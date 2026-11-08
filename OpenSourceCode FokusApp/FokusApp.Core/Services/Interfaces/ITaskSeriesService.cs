using FokusApp.Core.Enums;
using FokusApp.Core.Models;

namespace FokusApp.Core.Services.Interfaces;

public interface ITaskSeriesService
{

    Task EnsureTasksAsync();

    Task HandleSavedTaskAsync(TaskDto savedTask, TaskDto? previousTask, TaskSeriesScope scope = TaskSeriesScope.WholeSeries);

    Task<Guid?> DeleteTaskAsync(Guid taskId, TaskSeriesScope scope, Guid? keepTaskId = null);
}
