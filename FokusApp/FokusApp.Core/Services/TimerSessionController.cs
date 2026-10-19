using FokusApp.Core.Enums;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;



public readonly record struct TimerSessionCompletionResult(SessionCompletionType Type)
{
    public static TimerSessionCompletionResult FocusCompleted()
    {
        return new TimerSessionCompletionResult(SessionCompletionType.FocusCompleted);
    }

    public static TimerSessionCompletionResult BreakCompleted()
    {
        return new TimerSessionCompletionResult(SessionCompletionType.BreakCompleted);
    }
}

public readonly record struct TimerStartRequest(
    int DurationSeconds,
    TimerSessionMode SessionMode,
    string? DisplayTaskName,
    int BreakDurationSeconds);



public sealed class TimerSessionController : ITimerSessionController, IDisposable
{
    private readonly ITimerAndAudioService _timerAndAudioService;
    private readonly ITimerPlatformAdapter _timerPlatformAdapter;
    private readonly TimeSpan _oneSecond = TimeSpan.FromSeconds(1);

    private bool _isBreakMode;

    public TimerSessionController(
        ITimerAndAudioService timerAndAudioService,
        ITimerPlatformAdapter timerPlatformAdapter)
    {
        _timerAndAudioService = timerAndAudioService;
        _timerPlatformAdapter = timerPlatformAdapter;
        _timerPlatformAdapter.StateChanged += OnPlatformStateChanged;
    }

    public event EventHandler<TimerChangedEventArgs>? TimerChanged
    {
        add => _timerAndAudioService.TimerChanged += value;
        remove => _timerAndAudioService.TimerChanged -= value;
    }

    public event EventHandler? SessionStateChanged;

    public bool IsRunning => _timerAndAudioService.IsTimerRunning;
    public bool IsPaused => _timerAndAudioService.IsTimerPaused;
    public bool IsTimerActive => IsRunning || IsPaused;
    public bool IsBreakMode => _isBreakMode;
    public TimeSpan RemainingTime => _timerAndAudioService.RemainingTime;
    public bool IsNotificationPermissionMissing => _timerPlatformAdapter.IsNotificationPermissionMissing;
    public bool ShouldShowCountdown => IsRunning || RemainingTime >= _oneSecond;

    public async Task SwitchTimerToStartPauseOrResumeAsync(TimerStartRequest startRequest)
    {
        if (IsRunning)
        {
            await PauseAsync().ConfigureAwait(false);
            return;
        }

        if (IsPaused)
        {
            await ResumeAsync().ConfigureAwait(false);
            return;
        }

        await StartAsync(startRequest).ConfigureAwait(false);
    }

    public Task StartAsync(TimerStartRequest request)
    {
        var sessionRequest = request with
        {
            SessionMode = GetSessionMode()
        };

        return _timerPlatformAdapter.StartAsync(sessionRequest);
    }

    public Task PauseAsync()
    {
        return _timerPlatformAdapter.PauseAsync();
    }

    public Task ResumeAsync()
    {
        if (RemainingTime <= TimeSpan.Zero)
        {
            return Task.CompletedTask;
        }

        return _timerPlatformAdapter.ResumeAsync();
    }

    public Task StopAsync()
    {
        if (!IsTimerActive || RemainingTime <= TimeSpan.Zero)
        {
            return Task.CompletedTask;
        }
        if (IsBreakMode)
        {
            ExitBreakMode();
        }

        return _timerPlatformAdapter.StopAsync();
    }

    public TimerSessionCompletionResult Complete()
    {
        if (IsBreakMode)
        {
            ExitBreakMode();
            return TimerSessionCompletionResult.BreakCompleted();
        }

        _isBreakMode = true;
        return TimerSessionCompletionResult.FocusCompleted();
    }

    public void ClearSession()
    {
        ExitBreakMode();
    }

    public void ExitBreakMode()
    {
        _isBreakMode = false;
    }

    public void Dispose()
    {
        _timerPlatformAdapter.StateChanged -= OnPlatformStateChanged;
    }

    private TimerSessionMode GetSessionMode()
    {
        return IsBreakMode ? TimerSessionMode.Break : TimerSessionMode.Focus;
    }

    private void OnPlatformStateChanged(object? sender, EventArgs e)
    {
        SessionStateChanged?.Invoke(this, EventArgs.Empty);
    }
}
