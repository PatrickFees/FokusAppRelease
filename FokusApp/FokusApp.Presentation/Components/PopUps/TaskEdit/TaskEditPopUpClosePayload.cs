using FokusApp.Core.Models;
using FokusApp.Core.Enums;

namespace FokusApp.Components.PopUps.TaskEdit; 
public sealed class TaskEditPopUpClosePayload
{
    public TaskEditPopUpCloseAction Action { get; init; } = TaskEditPopUpCloseAction.Cancel;

    public TaskDto Task { get; init; } = new();

    public bool IsUpdateMode { get; init; }

    public bool HasChanges { get; init; }

    public Guid? RegularTemplateTaskId { get; init; }
}
