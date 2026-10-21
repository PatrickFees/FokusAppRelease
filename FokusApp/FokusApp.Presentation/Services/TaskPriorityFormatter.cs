using FokusApp.Core.Enums;

namespace FokusApp.Services;

public sealed class TaskPriorityFormatter : ITaskPriorityFormatter
{
    private const int MaximumLevel = 5;

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

    public string GetSymbol(TaskPriority priority)
    {
        var level = Enum.IsDefined(priority) ? (int)priority + 1 : 0;
        return new string('●', level) + new string('○', MaximumLevel - level);
    }

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
