using FokusApp.Core.Interfaces;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class DefaultTimerPlatformAdapter : ITimerPlatformAdapter
{
    private readonly ITimerAndAudioService _timerAndAudioService;

    public DefaultTimerPlatformAdapter(ITimerAndAudioService timerAndAudioService)
    {
        _timerAndAudioService = timerAndAudioService;
    }

    public event EventHandler? StateChanged
    {
        add { }
        remove { }
    }

    public bool IsNotificationPermissionMissing => false;

    public Task StartAsync(TimerStartRequest request)
    {
        _timerAndAudioService.StartTimer(request.DurationSeconds);
        return Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        _timerAndAudioService.PauseTimer();
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        _timerAndAudioService.ResumeTimer();
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _timerAndAudioService.ClearTimer();
        return Task.CompletedTask;
    }
}
