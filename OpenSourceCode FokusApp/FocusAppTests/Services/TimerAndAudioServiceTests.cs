using System.Reflection;
using FocusAppTests.MauiShims;
using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.Enums;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Plugin.Maui.Audio;

namespace FocusAppTests.Services;

[Collection(SerialMauiShimsCollection.Name)]
public class TimerAndAudioServiceTests : IDisposable
{
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(5);

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 3, 15, 9, 30, 0, TimeSpan.Zero));
    private readonly IRingtoneSettingsService _ringtoneSettings = Substitute.For<IRingtoneSettingsService>();
    private readonly IRingtoneCatalogService _ringtoneCatalog = Substitute.For<IRingtoneCatalogService>();
    private readonly IAudioManager _audioManager = Substitute.For<IAudioManager>();
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly ITimerAlertModeSettingsService _timerAlertModeSettings = Substitute.For<ITimerAlertModeSettingsService>();
    private readonly ITimerAlertEffectService _timerAlertEffectService = Substitute.For<ITimerAlertEffectService>();
    private readonly List<string> _openedFiles = [];
    private readonly List<TrackingStream> _streams = [];
    private readonly List<IAudioPlayer> _players = [];
    private readonly List<TimerChangedEventArgs> _events = [];
    private readonly List<(TimerStateChangeReason Reason, TaskCompletionSource<TimerChangedEventArgs> Source)> _eventWaiters = [];
    private readonly TaskCompletionSource<bool> _errorCaptured = new();
    private readonly TimerAndAudioService _service;
    private readonly Func<string, Task<Stream>> _previousFileHandler = FileSystem.OpenAppPackageFileHandler;

    public TimerAndAudioServiceTests()
    {
        _ringtoneCatalog.DefaultRingtoneFilePath.Returns("Raw/Standard.mp3");
        _ringtoneSettings.GetSelectedRingtoneAsync().Returns(new RingtonesEntity { Id = Guid.NewGuid(), RingtoneFilePath = "Raw/Gewaehlt.mp3" });
        _audioManager.CreatePlayer(Arg.Any<Stream>()).Returns(_ => CreatePlayer());
        _errorCapture.CaptureAsync(Arg.Any<Exception>(), Arg.Any<AppErrorCaptureOptions?>()).Returns(_ =>
        {
            _errorCaptured.TrySetResult(true);
            return Task.CompletedTask;
        });
        _timerAlertModeSettings.TimerAlertMode.Returns(TimerAlertMode.Sound);
        FileSystem.OpenAppPackageFileHandler = OpenFile;
        _service = new TimerAndAudioService(
            _ringtoneSettings,
            _ringtoneCatalog,
            _audioManager,
            _errorCapture,
            _timerAlertModeSettings,
            _timerAlertEffectService,
            _clock);
        _service.TimerChanged += (_, args) =>
        {
            lock (_events)
            {
                _events.Add(args);
                foreach (var waiter in _eventWaiters.Where(waiter => waiter.Reason == args.Reason).ToList())
                {
                    waiter.Source.TrySetResult(args);
                    _eventWaiters.Remove(waiter);
                }
            }
        };
    }

    public void Dispose()
    {
        _service.ClearTimer();
        FileSystem.OpenAppPackageFileHandler = _previousFileHandler;
    }

    [Fact]
    public void Constructor_NewService_IsIdleWithoutRemainingTime()
    {
        Assert.False(_service.IsTimerRunning);
        Assert.False(_service.IsTimerPaused);
        Assert.Equal(TimeSpan.Zero, _service.RemainingTime);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void StartTimer_NoPositiveDuration_IsIgnored(int seconds)
    {
        _service.StartTimer(seconds);

        Assert.False(_service.IsTimerRunning);
        Assert.Empty(_events);
    }

    [Fact]
    public void StartTimer_Seconds_RunsWithThatDurationAndRaisesStarted()
    {
        _service.StartTimer(90);

        Assert.True(_service.IsTimerRunning);
        Assert.False(_service.IsTimerPaused);
        Assert.Equal(TimeSpan.FromSeconds(90), _service.RemainingTime);
        var started = Assert.Single(_events);
        Assert.Equal(TimerStateChangeReason.Started, started.Reason);
        Assert.Equal(TimeSpan.FromSeconds(90), started.RemainingTime);
        Assert.True(started.IsRunning);
    }

    [Fact]
    public void StartTimer_WhileRunning_RestartsWithTheNewDuration()
    {
        _service.StartTimer(90);
        _clock.Advance(TimeSpan.FromSeconds(30));

        _service.StartTimer(120);

        Assert.Equal(TimeSpan.FromSeconds(120), _service.RemainingTime);
        Assert.True(_service.IsTimerRunning);
        Assert.Equal(2, _events.Count(args => args.Reason == TimerStateChangeReason.Started));
    }

    [Fact]
    public void PauseTimer_Running_KeepsTheRemainingTimeAndRaisesPaused()
    {
        _service.StartTimer(60);
        _clock.Advance(TimeSpan.FromSeconds(20));

        _service.PauseTimer();

        Assert.False(_service.IsTimerRunning);
        Assert.True(_service.IsTimerPaused);
        Assert.Equal(TimeSpan.FromSeconds(40), _service.RemainingTime);
        var paused = _events.Last();
        Assert.Equal(TimerStateChangeReason.Paused, paused.Reason);
        Assert.False(paused.IsRunning);
    }

    [Fact]
    public void PauseTimer_NotRunning_IsIgnored()
    {
        _service.PauseTimer();

        Assert.False(_service.IsTimerPaused);
        Assert.Empty(_events);
    }

    [Fact]
    public void PauseTimer_AfterTheEndWasReached_KeepsZeroRemainingTime()
    {
        _service.StartTimer(10);
        _clock.Advance(TimeSpan.FromSeconds(30));

        _service.PauseTimer();

        Assert.Equal(TimeSpan.Zero, _service.RemainingTime);
        Assert.True(_service.IsTimerPaused);
    }

    [Fact]
    public void ResumeTimer_Paused_ContinuesWithTheRemainingTimeAndRaisesResumed()
    {
        _service.StartTimer(60);
        _clock.Advance(TimeSpan.FromSeconds(20));
        _service.PauseTimer();
        _clock.Advance(TimeSpan.FromMinutes(5));

        _service.ResumeTimer();

        Assert.True(_service.IsTimerRunning);
        Assert.False(_service.IsTimerPaused);
        Assert.Equal(TimeSpan.FromSeconds(40), _service.RemainingTime);
        Assert.Equal(TimerStateChangeReason.Resumed, _events.Last().Reason);
    }

    [Fact]
    public void ResumeTimer_NotPaused_IsIgnored()
    {
        _service.StartTimer(60);
        _events.Clear();

        _service.ResumeTimer();

        Assert.Empty(_events);
        Assert.True(_service.IsTimerRunning);
    }

    [Fact]
    public void ResumeTimer_PausedWithoutRemainingTime_IsIgnored()
    {
        _service.StartTimer(10);
        _clock.Advance(TimeSpan.FromSeconds(30));
        _service.PauseTimer();
        _events.Clear();

        _service.ResumeTimer();

        Assert.Empty(_events);
        Assert.False(_service.IsTimerRunning);
    }

    [Fact]
    public void ClearTimer_Running_ResetsEverythingAndRaisesCleared()
    {
        _service.StartTimer(60);

        _service.ClearTimer();

        Assert.False(_service.IsTimerRunning);
        Assert.False(_service.IsTimerPaused);
        Assert.Equal(TimeSpan.Zero, _service.RemainingTime);
        var cleared = _events.Last();
        Assert.Equal(TimerStateChangeReason.Cleared, cleared.Reason);
    }

    [Fact]
    public void ClearTimer_Idle_StillRaisesCleared()
    {
        _service.ClearTimer();

        var cleared = Assert.Single(_events);
        Assert.Equal(TimerStateChangeReason.Cleared, cleared.Reason);
    }

    [Fact]
    public void ClearTimer_Paused_ResetsThePausedState()
    {
        _service.StartTimer(60);
        _service.PauseTimer();

        _service.ClearTimer();

        Assert.False(_service.IsTimerPaused);
        Assert.Equal(TimeSpan.Zero, _service.RemainingTime);
    }

    [Fact]
    public void StartTimer_WithoutSubscribers_RunsWithoutError()
    {
        var service = new TimerAndAudioService(
            _ringtoneSettings,
            _ringtoneCatalog,
            _audioManager,
            _errorCapture,
            _timerAlertModeSettings,
            _timerAlertEffectService,
            _clock);

        service.StartTimer(30);

        Assert.True(service.IsTimerRunning);
        service.ClearTimer();
    }

    [Fact]
    public void Constructor_WithoutTimeProvider_UsesTheSystemClock()
    {
        var service = new TimerAndAudioService(
            _ringtoneSettings,
            _ringtoneCatalog,
            _audioManager,
            _errorCapture,
            _timerAlertModeSettings,
            _timerAlertEffectService);

        service.StartTimer(30);

        Assert.InRange(service.RemainingTime.TotalSeconds, 29, 30);
        service.ClearTimer();
    }

    [Fact]
    public async Task TimerElapsed_TimeLeft_RaisesTickWithTheUpdatedRemainingTime()
    {
        _service.StartTimer(10);
        _clock.Advance(TimeSpan.FromSeconds(4));

        var tick = await WaitForEventAsync(TimerStateChangeReason.Tick);

        Assert.Equal(TimeSpan.FromSeconds(6), tick.RemainingTime);
        Assert.True(tick.IsRunning);
        Assert.True(_service.IsTimerRunning);
    }

    [Fact]
    public async Task TimerElapsed_EndReached_CompletesPlaysTheChosenRingtoneAndRaisesTick()
    {
        _service.StartTimer(2);
        _clock.Advance(TimeSpan.FromSeconds(5));

        var completed = await WaitForEventAsync(TimerStateChangeReason.Completed);
        await WaitForEventAsync(TimerStateChangeReason.Tick);

        Assert.Equal(TimeSpan.Zero, completed.RemainingTime);
        Assert.False(completed.IsRunning);
        Assert.False(_service.IsTimerRunning);
        Assert.Equal(TimeSpan.Zero, _service.RemainingTime);
        Assert.Equal(["Raw/Gewaehlt.mp3"], _openedFiles);
        var player = Assert.Single(_players);
        player.Received(1).Play();
    }

    [Fact]
    public async Task TimerElapsed_EndReachedWithoutChosenRingtone_PlaysTheDefaultRingtone()
    {
        _ringtoneSettings.GetSelectedRingtoneAsync().Returns((RingtonesEntity?)null);
        _service.StartTimer(2);
        _clock.Advance(TimeSpan.FromSeconds(5));

        await WaitForEventAsync(TimerStateChangeReason.Completed);
        await WaitForEventAsync(TimerStateChangeReason.Tick);

        Assert.Equal(["Raw/Standard.mp3"], _openedFiles);
    }

    [Fact]
    public async Task TimerElapsed_RingtoneFileMissing_StillFinishesWithTickAndCapturesNothing()
    {
        FileSystem.OpenAppPackageFileHandler = _ => throw new FileNotFoundException("fehlt");
        _service.StartTimer(2);
        _clock.Advance(TimeSpan.FromSeconds(5));

        await WaitForEventAsync(TimerStateChangeReason.Completed);
        await WaitForEventAsync(TimerStateChangeReason.Tick);

        await _errorCapture.DidNotReceiveWithAnyArgs().CaptureAsync(default!, default);
    }

    [Fact]
    public async Task TimerElapsed_RingtoneLookupFails_CapturesTheErrorAsNonFatalNonUiFailure()
    {
        var failure = new InvalidOperationException("Einstellungen kaputt");
        _ringtoneSettings.GetSelectedRingtoneAsync().ThrowsAsync(failure);
        _service.StartTimer(2);
        _clock.Advance(TimeSpan.FromSeconds(5));

        await WaitForEventAsync(TimerStateChangeReason.Completed);
        await WaitWithTimeoutAsync(_errorCaptured.Task);

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "TimerAndAudioService.OnTimerElapsed" &&
                !options.IsFatal &&
                !options.IsUiFailure));
    }

    [Fact]
    public async Task TimerElapsed_SoundOnlyMode_PlaysRingtoneAndTriggersEffectServiceWithSoundOnly()
    {
        _timerAlertModeSettings.TimerAlertMode.Returns(TimerAlertMode.Sound);
        _service.StartTimer(2);
        _clock.Advance(TimeSpan.FromSeconds(5));

        await WaitForEventAsync(TimerStateChangeReason.Completed);
        await WaitForEventAsync(TimerStateChangeReason.Tick);

        Assert.Equal(["Raw/Gewaehlt.mp3"], _openedFiles);
        await _timerAlertEffectService.Received(1).TriggerAsync(TimerAlertMode.Sound);
    }

    [Fact]
    public async Task TimerElapsed_CombinedMode_PlaysRingtoneAndTriggersEffectServiceWithFullMode()
    {
        var combined = TimerAlertMode.Sound | TimerAlertMode.Vibration;
        _timerAlertModeSettings.TimerAlertMode.Returns(combined);
        _service.StartTimer(2);
        _clock.Advance(TimeSpan.FromSeconds(5));

        await WaitForEventAsync(TimerStateChangeReason.Completed);
        await WaitForEventAsync(TimerStateChangeReason.Tick);

        Assert.Equal(["Raw/Gewaehlt.mp3"], _openedFiles);
        await _timerAlertEffectService.Received(1).TriggerAsync(combined);
    }

    [Fact]
    public async Task TimerElapsed_VibrationOnlyMode_DoesNotPlayRingtoneButTriggersEffectService()
    {
        _timerAlertModeSettings.TimerAlertMode.Returns(TimerAlertMode.Vibration);
        _service.StartTimer(2);
        _clock.Advance(TimeSpan.FromSeconds(5));

        await WaitForEventAsync(TimerStateChangeReason.Completed);
        await WaitForEventAsync(TimerStateChangeReason.Tick);

        Assert.Empty(_openedFiles);
        await _timerAlertEffectService.Received(1).TriggerAsync(TimerAlertMode.Vibration);
    }

    [Fact]
    public async Task TimerElapsed_NoneMode_PlaysNothingButStillCallsEffectService()
    {
        _timerAlertModeSettings.TimerAlertMode.Returns(TimerAlertMode.None);
        _service.StartTimer(2);
        _clock.Advance(TimeSpan.FromSeconds(5));

        await WaitForEventAsync(TimerStateChangeReason.Completed);
        await WaitForEventAsync(TimerStateChangeReason.Tick);

        Assert.Empty(_openedFiles);
        await _timerAlertEffectService.Received(1).TriggerAsync(TimerAlertMode.None);
    }

    [Fact]
    public async Task TimerElapsed_SoundWithAdaptToRingerModeAndRingerNormal_PlaysRingtone()
    {
        var mode = TimerAlertMode.Sound | TimerAlertMode.AdaptToRingerMode;
        _timerAlertModeSettings.TimerAlertMode.Returns(mode);
        _timerAlertEffectService.IsSystemRingerModeNormal.Returns(true);
        _service.StartTimer(2);
        _clock.Advance(TimeSpan.FromSeconds(5));

        await WaitForEventAsync(TimerStateChangeReason.Completed);
        await WaitForEventAsync(TimerStateChangeReason.Tick);

        Assert.Equal(["Raw/Gewaehlt.mp3"], _openedFiles);
        await _timerAlertEffectService.Received(1).TriggerAsync(mode);
    }

    [Fact]
    public async Task TimerElapsed_SoundWithAdaptToRingerModeAndRingerNotNormal_DoesNotPlayRingtoneButStillTriggersEffectService()
    {
        var mode = TimerAlertMode.Sound | TimerAlertMode.AdaptToRingerMode;
        _timerAlertModeSettings.TimerAlertMode.Returns(mode);
        _timerAlertEffectService.IsSystemRingerModeNormal.Returns(false);
        _service.StartTimer(2);
        _clock.Advance(TimeSpan.FromSeconds(5));

        await WaitForEventAsync(TimerStateChangeReason.Completed);
        await WaitForEventAsync(TimerStateChangeReason.Tick);

        Assert.Empty(_openedFiles);
        await _timerAlertEffectService.Received(1).TriggerAsync(mode);
    }

    [Fact]
    public async Task TimerElapsed_SoundWithoutAdaptToRingerModeAndRingerNotNormal_StillPlaysRingtone()
    {
        _timerAlertModeSettings.TimerAlertMode.Returns(TimerAlertMode.Sound);
        _timerAlertEffectService.IsSystemRingerModeNormal.Returns(false);
        _service.StartTimer(2);
        _clock.Advance(TimeSpan.FromSeconds(5));

        await WaitForEventAsync(TimerStateChangeReason.Completed);
        await WaitForEventAsync(TimerStateChangeReason.Tick);

        Assert.Equal(["Raw/Gewaehlt.mp3"], _openedFiles);
        await _timerAlertEffectService.Received(1).TriggerAsync(TimerAlertMode.Sound);
    }

    [Fact]
    public void TimerElapsed_TimerNotRunning_DoesNothing()
    {
        var onTimerElapsed = typeof(TimerAndAudioService).GetMethod("OnTimerElapsed", BindingFlags.Instance | BindingFlags.NonPublic)!;

        onTimerElapsed.Invoke(_service, [null, null]);

        Assert.Empty(_events);
    }

    [Fact]
    public async Task PlaySoundAsync_Blank_PlaysNothing()
    {
        await _service.PlaySoundAsync("  ");
        await _service.PlaySoundAsync(string.Empty);

        Assert.Empty(_openedFiles);
        Assert.Empty(_players);
    }

    [Fact]
    public async Task PlaySoundAsync_Path_OpensTheFileAndStartsAPlayerForIt()
    {
        await _service.PlaySoundAsync("Raw/Klingel.mp3");

        Assert.Equal(["Raw/Klingel.mp3"], _openedFiles);
        var player = Assert.Single(_players);
        _audioManager.Received(1).CreatePlayer(_streams[0]);
        player.Received(1).Play();
    }

    [Fact]
    public async Task PlaySoundAsync_SecondSoundWhilePlaying_StopsAndReleasesTheFirstOne()
    {
        await _service.PlaySoundAsync("Raw/Eins.mp3");
        var first = _players[0];
        first.IsPlaying.Returns(true);

        await _service.PlaySoundAsync("Raw/Zwei.mp3");

        first.Received(1).Stop();
        first.Received(1).Dispose();
        Assert.True(_streams[0].WasDisposed);
        Assert.False(_streams[1].WasDisposed);
        _players[1].Received(1).Play();
    }

    [Fact]
    public async Task PlaySoundAsync_FirstSoundAlreadyFinishedPlaying_ReleasesItWithoutStopping()
    {
        await _service.PlaySoundAsync("Raw/Eins.mp3");
        var first = _players[0];
        first.IsPlaying.Returns(false);

        await _service.PlaySoundAsync("Raw/Zwei.mp3");

        first.DidNotReceive().Stop();
        first.Received(1).Dispose();
    }

    [Fact]
    public async Task PlaySoundAsync_StoppingTheFirstSoundFails_StillPlaysTheSecondOne()
    {
        await _service.PlaySoundAsync("Raw/Eins.mp3");
        var first = _players[0];
        first.IsPlaying.Returns(true);
        first.When(player => player.Stop()).Do(_ => throw new InvalidOperationException("Stop fehlgeschlagen"));

        await _service.PlaySoundAsync("Raw/Zwei.mp3");

        first.Received(1).Dispose();
        _players[1].Received(1).Play();
    }

    [Fact]
    public async Task PlaybackEnded_ActivePlayer_ReleasesPlayerAndFile()
    {
        await _service.PlaySoundAsync("Raw/Eins.mp3");
        var player = _players[0];

        player.PlaybackEnded += Raise.Event();

        player.Received(1).Dispose();
        Assert.True(_streams[0].WasDisposed);
    }

    [Fact]
    public async Task PlaybackEnded_AfterReleaseAlreadyHappened_DoesNotReleaseTwice()
    {
        await _service.PlaySoundAsync("Raw/Eins.mp3");
        var player = _players[0];
        player.PlaybackEnded += Raise.Event();

        player.PlaybackEnded += Raise.Event();

        player.Received(1).Dispose();
    }

    [Fact]
    public async Task PlaybackEnded_OfAReplacedPlayer_IsIgnored()
    {
        var staleHandlers = new List<EventHandler>();
        var stalePlayer = new StaleHandlerPlayer(staleHandlers);
        _audioManager.CreatePlayer(Arg.Any<Stream>()).Returns(stalePlayer, CreatePlayer());
        await _service.PlaySoundAsync("Raw/Eins.mp3");
        await _service.PlaySoundAsync("Raw/Zwei.mp3");
        var disposalsBefore = stalePlayer.DisposeCount;

        stalePlayer.RaisePlaybackEnded();

        Assert.Equal(disposalsBefore, stalePlayer.DisposeCount);
        Assert.NotEmpty(staleHandlers);
        _players[0].DidNotReceive().Dispose();
    }

    [Fact]
    public async Task PlaySoundAsync_CreatingThePlayerFails_ThrowsTheError()
    {
        var failure = new InvalidOperationException("Kein Audio");
        _audioManager.CreatePlayer(Arg.Any<Stream>()).Throws(failure);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.PlaySoundAsync("Raw/Eins.mp3"));

        Assert.Same(failure, thrown);
    }

    [Fact]
    public async Task PlaySoundAsync_FileMissing_ThrowsFileNotFound()
    {
        FileSystem.OpenAppPackageFileHandler = _ => throw new FileNotFoundException("fehlt");

        await Assert.ThrowsAsync<FileNotFoundException>(() => _service.PlaySoundAsync("Raw/Fehlt.mp3"));
    }

    [Fact]
    public async Task PlaySoundAsync_AfterAFailure_CanPlayAgain()
    {
        _audioManager.CreatePlayer(Arg.Any<Stream>()).Returns(
            _ => throw new InvalidOperationException("Kein Audio"),
            _ => CreatePlayer());
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.PlaySoundAsync("Raw/Eins.mp3"));

        await _service.PlaySoundAsync("Raw/Zwei.mp3");

        _players[0].Received(1).Play();
    }

    private Task<Stream> OpenFile(string path)
    {
        var stream = new TrackingStream();
        _openedFiles.Add(path);
        _streams.Add(stream);
        return Task.FromResult<Stream>(stream);
    }

    private IAudioPlayer CreatePlayer()
    {
        var player = Substitute.For<IAudioPlayer>();
        _players.Add(player);
        return player;
    }

    private async Task<TimerChangedEventArgs> WaitForEventAsync(TimerStateChangeReason reason)
    {
        TaskCompletionSource<TimerChangedEventArgs> source;
        lock (_events)
        {
            var existingMatch = _events.FirstOrDefault(args => args.Reason == reason);
            if (existingMatch is not null)
            {
                return existingMatch;
            }

            source = new TaskCompletionSource<TimerChangedEventArgs>();
            _eventWaiters.Add((reason, source));
        }

        return await WaitWithTimeoutAsync(source.Task);
    }

    private static async Task<T> WaitWithTimeoutAsync<T>(Task<T> task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(EventTimeout));
        if (completed != task)
        {
            throw new TimeoutException("Das erwartete Ereignis wurde nicht ausgelöst.");
        }

        return await task;
    }

    private sealed class TrackingStream : MemoryStream
    {
        public bool WasDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class StaleHandlerPlayer(List<EventHandler> handlers) : IAudioPlayer
    {
        public int DisposeCount { get; private set; }

        public bool IsPlaying => false;

        public event EventHandler? PlaybackEnded
        {
            add => handlers.Add(value!);
            remove
            {

            }
        }

        public void Play()
        {
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
            DisposeCount++;
        }

        public void RaisePlaybackEnded()
        {
            foreach (var handler in handlers.ToArray())
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
