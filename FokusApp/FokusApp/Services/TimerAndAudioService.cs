using FokusApp.Core.Enums;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using Plugin.Maui.Audio;

namespace FokusApp.Services;

public sealed class TimerAndAudioService : ITimerAndAudioService
{
    private readonly object _timerStateLock = new();
    private readonly SemaphoreSlim _playbackLock = new(1, 1);
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _oneSecond = TimeSpan.FromSeconds(1);
    private readonly IAudioManager _audioManager;
    private readonly IUserSettingsService _userSettingsService;
    private readonly IRingtoneCatalogService _ringtoneCatalogService;
    private readonly IAppErrorCaptureService _appErrorCaptureService;

    private DateTimeOffset? _timerEndUtc;
    private System.Timers.Timer? _countdownTimer;
    private Stream? _activeFileStream;
    private IAudioPlayer? _activePlayer;
    private EventHandler? _activePlaybackEndedHandler;

    public TimerAndAudioService(
        IUserSettingsService userSettingsService,
        IRingtoneCatalogService ringtoneCatalogService,
        IAudioManager audioManager,
        IAppErrorCaptureService appErrorCaptureService,
        TimeProvider? timeProvider = null)
    {
        _userSettingsService = userSettingsService;
        _ringtoneCatalogService = ringtoneCatalogService;
        _audioManager = audioManager;
        _appErrorCaptureService = appErrorCaptureService;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public bool IsTimerRunning { get; private set; }
    public bool IsTimerPaused { get; private set; }
    public TimeSpan RemainingTime { get; private set; } = TimeSpan.Zero;
    public event EventHandler<TimerChangedEventArgs>? TimerChanged;

    public void StartTimer(int timeInSeconds)
    {
        if (timeInSeconds <= 0)
        {
            return;
        }

        var duration = TimeSpan.FromSeconds(timeInSeconds);

        lock (_timerStateLock)
        {
            ClearTimerState();
            RemainingTime = duration;
            _timerEndUtc = _timeProvider.GetUtcNow().Add(duration);
            IsTimerRunning = true;
            IsTimerPaused = false;
            EnsureCountdownTimer();
            _countdownTimer!.Start();
        }

        RaiseTimerChanged(TimerStateChangeReason.Started);
    }

    public void PauseTimer()
    {
        lock (_timerStateLock)
        {
            if (!IsTimerRunning)
            {
                return;
            }

            _countdownTimer?.Stop();
            RemainingTime = CalculateRemainingTime(_timeProvider.GetUtcNow());
            _timerEndUtc = null;
            IsTimerRunning = false;
            IsTimerPaused = true;
        }

        RaiseTimerChanged(TimerStateChangeReason.Paused);
    }

    public void ResumeTimer()
    {
        lock (_timerStateLock)
        {
            if (!IsTimerPaused || RemainingTime <= TimeSpan.Zero)
            {
                return;
            }

            _timerEndUtc = _timeProvider.GetUtcNow().Add(RemainingTime);
            EnsureCountdownTimer();
            _countdownTimer!.Start();
            IsTimerRunning = true;
            IsTimerPaused = false;
        }

        RaiseTimerChanged(TimerStateChangeReason.Resumed);
    }

    public void ClearTimer()
    {
        lock (_timerStateLock)
        {
            ClearTimerState();
        }

        RaiseTimerChanged(TimerStateChangeReason.Cleared);
    }

    public async Task PlaySoundAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        await PlayAudioAsync(filePath).ConfigureAwait(false);
    }

    private System.Timers.Timer InitializeTimer()
    {
        return new System.Timers.Timer(_oneSecond.TotalMilliseconds)
        {
            AutoReset = true,
            Enabled = false
        };
    }

    // async void is unavoidable here (Timer.Elapsed signature); an unhandled exception escaping
    // it would terminate the process via AppDomain.UnhandledException, so every path must be caught.
    private async void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            var timerCompleted = false;

            lock (_timerStateLock)
            {
                if (!IsTimerRunning || _timerEndUtc is null)
                {
                    return;
                }

                RemainingTime = CalculateRemainingTime(_timeProvider.GetUtcNow());

                if (RemainingTime <= TimeSpan.Zero)
                {
                    RemainingTime = TimeSpan.Zero;
                    ClearTimerState();
                    timerCompleted = true;
                }
            }

            if (timerCompleted)
            {
                RaiseTimerChanged(TimerStateChangeReason.Completed);
                await PlayCompletionSoundAsync().ConfigureAwait(false);
            }

            RaiseTimerChanged(TimerStateChangeReason.Tick);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureAsync(
                ex,
                new AppErrorCaptureOptions
                {
                    Source = $"{nameof(TimerAndAudioService)}.{nameof(OnTimerElapsed)}",
                    IsFatal = false,
                    IsUiFailure = false
                }).ConfigureAwait(false);
        }
    }

    private void ClearTimerState()
    {
        if (_countdownTimer is not null)
        {
            _countdownTimer.Stop();
            _countdownTimer.Elapsed -= OnTimerElapsed;
            _countdownTimer.Dispose();
            _countdownTimer = null;
        }

        _timerEndUtc = null;
        RemainingTime = TimeSpan.Zero;
        IsTimerRunning = false;
        IsTimerPaused = false;
    }

    private TimeSpan CalculateRemainingTime(DateTimeOffset currentUtcTime)
    {
        if (_timerEndUtc is null)
        {
            return RemainingTime;
        }

        var remaining = _timerEndUtc.Value - currentUtcTime;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private void EnsureCountdownTimer()
    {
        if (_countdownTimer is not null)
        {
            return;
        }

        _countdownTimer = InitializeTimer();
        _countdownTimer.Elapsed += OnTimerElapsed;
    }

    private void RaiseTimerChanged(TimerStateChangeReason reason)
    {
        TimerChanged?.Invoke(this, new TimerChangedEventArgs(RemainingTime, IsTimerRunning, reason));
    }

    private async Task PlayCompletionSoundAsync()
    {
#pragma warning disable CA1416 // Validate platform compatibility
        try
        {
            var ringtone = await _userSettingsService.GetSelectedRingtoneAsync().ConfigureAwait(false);
            var ringtonePath = ringtone?.RingtoneFilePath ?? _ringtoneCatalogService.DefaultRingtoneFilePath;
            await PlaySoundAsync(ringtonePath).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
        }
#pragma warning restore CA1416 // Validate platform compatibility
    }

    private async Task PlayAudioAsync(string filePath)
    {
        await _playbackLock.WaitAsync().ConfigureAwait(false);
        try
        {
            StopActivePlayback();

            var fileStream = await FileSystem.OpenAppPackageFileAsync(filePath).ConfigureAwait(false);
            var player = _audioManager.CreatePlayer(fileStream);

            EventHandler? playbackEndedHandler = null;
            playbackEndedHandler = (_, _) => CleanupIfStillActive(player);
            player.PlaybackEnded += playbackEndedHandler;

            lock (_timerStateLock)
            {
                _activeFileStream = fileStream;
                _activePlayer = player;
                _activePlaybackEndedHandler = playbackEndedHandler;
            }

            await MainThread.InvokeOnMainThreadAsync(player.Play);
        }
        catch
        {
            StopActivePlayback();
            throw;
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    // Must only be called while holding _playbackLock, so a new playback can't start mid-teardown.
    private void StopActivePlayback()
    {
        IAudioPlayer? player;
        Stream? fileStream;
        EventHandler? handler;

        lock (_timerStateLock)
        {
            player = _activePlayer;
            fileStream = _activeFileStream;
            handler = _activePlaybackEndedHandler;

            _activePlayer = null;
            _activeFileStream = null;
            _activePlaybackEndedHandler = null;
        }

        if (player is null)
        {
            return;
        }

        if (handler is not null)
        {
            player.PlaybackEnded -= handler;
        }

        try
        {
            if (player.IsPlaying)
            {
                player.Stop();
            }
        }
        catch (Exception)
        {
            // Best-effort stop; the player is being disposed regardless.
        }

        player.Dispose();
        fileStream?.Dispose();
    }

    // PlaybackEnded can fire after a newer PlayAudioAsync already replaced this player; only clean up if it's still current.
    private void CleanupIfStillActive(IAudioPlayer player)
    {
        Stream? fileStream;
        EventHandler? handler;

        lock (_timerStateLock)
        {
            if (!ReferenceEquals(_activePlayer, player))
            {
                return;
            }

            fileStream = _activeFileStream;
            handler = _activePlaybackEndedHandler;

            _activePlayer = null;
            _activeFileStream = null;
            _activePlaybackEndedHandler = null;
        }

        if (handler is not null)
        {
            player.PlaybackEnded -= handler;
        }

        player.Dispose();
        fileStream?.Dispose();
    }
}
