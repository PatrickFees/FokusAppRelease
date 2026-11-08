namespace FokusApp.Core.Services.Interfaces;

public interface ITimerSessionController
{
    event EventHandler<TimerChangedEventArgs>? TimerChanged;
    event EventHandler? SessionStateChanged;

    bool IsRunning { get; }
    bool IsPaused { get; }
    bool IsTimerActive { get; }
    bool IsBreakMode { get; }
    TimeSpan RemainingTime { get; }
    bool IsNotificationPermissionMissing { get; }
    bool ShouldShowCountdown { get; }

    Task SwitchTimerToStartPauseOrResumeAsync(TimerStartRequest startRequest);
    Task StartAsync(TimerStartRequest request);
    Task PauseAsync();
    Task ResumeAsync();
    Task StopAsync();
    TimerSessionCompletionResult Complete();
    void ClearSession();
    void ExitBreakMode();
}
