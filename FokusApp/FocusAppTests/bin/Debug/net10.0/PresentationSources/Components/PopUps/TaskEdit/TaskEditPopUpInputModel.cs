using FokusApp.Core.Models;

namespace FokusApp.Components.PopUps.TaskEdit;

public sealed class TaskEditPopUpInputModel
{
    public TaskDto? Task { get; init; }
    public required DateTime ContextualTaskDate { get; init; }
    public bool IsWorkScope { get; init; } = true;
    public bool? InitialIsPositive { get; init; }
    public bool IsTaskDateLocked { get; init; }
    public IReadOnlyList<TaskEditRegularTemplateOptionModel> RegularTemplateOptions { get; init; } = [];
}
