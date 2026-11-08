using FokusApp.Core.Enums;

namespace FokusApp.Core.Services;

public sealed class TimerChangedEventArgs : EventArgs
{
    public TimerChangedEventArgs(
        TimeSpan remainingTime,
        bool isRunning,
        TimerStateChangeReason reason)
    {
        RemainingTime = remainingTime;
        IsRunning = isRunning;
        Reason = reason;
    }

    public TimeSpan RemainingTime { get; }
    public bool IsRunning { get; }
    public TimerStateChangeReason Reason { get; }
}
