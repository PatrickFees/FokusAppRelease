namespace FokusApp.Core.Models;

public sealed class TaskCompletedEventArgs : EventArgs
{
    public TaskCompletedEventArgs(Guid taskId, bool isPositive)
    {
        TaskId = taskId;
        IsPositive = isPositive;
    }

    public Guid TaskId { get; }
    public bool IsPositive { get; }
}
