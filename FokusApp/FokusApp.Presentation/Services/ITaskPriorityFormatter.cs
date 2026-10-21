using FokusApp.Core.Enums;

namespace FokusApp.Services;

public interface ITaskPriorityFormatter
{
    string Format(TaskPriority priority);
    string GetCssClass(TaskPriority? priority);

    // Colour-independent level indicator, e.g. "●●●○○" for Mittel.
    string GetSymbol(TaskPriority priority);
}
