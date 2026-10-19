using FokusApp.Core.Enums;

namespace FokusApp.Core.Models;

public sealed class TaskDto
{
    public Guid? Id { get; set; }
    public bool IsDone { get; set; }
    public string? Note { get; set; }
    public bool IsRegularTask { get; set; }
    public bool IsWorkTask { get; set; } = true;
    public bool IsPositive { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? TaskDate { get; set; }
    public TaskPriority Priority { get; set; }
    public int BreakMinutes { get; set; } = 5;
    public int TimerMinutes { get; set; } = 5;
}
