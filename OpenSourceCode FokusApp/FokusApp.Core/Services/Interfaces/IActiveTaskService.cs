using FokusApp.Core.Models;

namespace FokusApp.Core.Services.Interfaces;

public interface IActiveTaskService
{
    event EventHandler? ActiveTaskChanged;

    Guid? ActiveTaskId { get; }
    TaskDto ActiveTask { get; }

    void SetActiveTask(Guid? taskId);
    bool IsTaskActive(Guid taskId);
}
