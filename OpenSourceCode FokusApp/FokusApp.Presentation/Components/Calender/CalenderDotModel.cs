using FokusApp.Core.Enums;

namespace FokusApp.Components.Calender;

public sealed class CalenderDotModel
{
    public TaskPriority Priority { get; init; }

    public bool IsDone { get; init; }
}
