using FokusApp.Core.Enums;

namespace FokusApp.Services;

public interface ITaskPriorityFormatter
{
    string Format(TaskPriority priority);
    string GetCssClass(TaskPriority? priority);
}
