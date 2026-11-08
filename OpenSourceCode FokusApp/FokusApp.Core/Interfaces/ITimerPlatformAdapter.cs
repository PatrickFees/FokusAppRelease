using FokusApp.Core.Services;

namespace FokusApp.Core.Interfaces;

public interface ITimerPlatformAdapter
{
    event EventHandler? StateChanged;

    bool IsNotificationPermissionMissing { get; }

    Task StartAsync(TimerStartRequest request);

    Task PauseAsync();

    Task ResumeAsync();

    Task StopAsync();
}
