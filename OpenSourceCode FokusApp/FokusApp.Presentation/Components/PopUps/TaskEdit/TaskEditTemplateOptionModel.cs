
namespace FokusApp.Components.PopUps.TaskEdit;

public sealed class TaskEditTemplateOptionModel
{
    public Guid TaskId { get; init; }
    public string Text { get; init; } = string.Empty;

    public bool IsPositive { get; init; }
}
