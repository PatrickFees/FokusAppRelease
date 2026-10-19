using FokusApp.Core.Enums;

namespace FokusApp.Services;

public sealed class TaskPriorityFormatter : ITaskPriorityFormatter
{
    private readonly IUiTextProvider _textProvider;

    public TaskPriorityFormatter(IUiTextProvider textProvider)
    {
        _textProvider = textProvider;
    }

    public string Format(TaskPriority priority) => priority switch
    {
        TaskPriority.Keine => _textProvider.GetText("PriorityNone"),
        TaskPriority.Wenig => _textProvider.GetText("PriorityLow"),
        TaskPriority.Mittel => _textProvider.GetText("PriorityMedium"),
        TaskPriority.Hoch => _textProvider.GetText("PriorityHigh"),
        TaskPriority.Dringend => _textProvider.GetText("PriorityUrgent"),
        _ => _textProvider.GetText("Unknown")
    };

    public string GetCssClass(TaskPriority? priority) => priority switch
    {
        TaskPriority.Keine => "priority-keine",
        TaskPriority.Wenig => "priority-wenig",
        TaskPriority.Mittel => "priority-mittel",
        TaskPriority.Hoch => "priority-hoch",
        TaskPriority.Dringend => "priority-dringend",
        _ => string.Empty
    };
}
