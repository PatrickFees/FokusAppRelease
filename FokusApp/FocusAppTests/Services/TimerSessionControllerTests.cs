using FokusApp.Core.Enums;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.Services;

public class TimerSessionControllerTests
{
    private static readonly TimerStartRequest DefaultRequest = new(1500, TimerSessionMode.Focus, "Aufgabe", 300);

    private readonly ITimerAndAudioService _timerAndAudioService = Substitute.For<ITimerAndAudioService>();
    private readonly ITimerPlatformAdapter _platformAdapter = Substitute.For<ITimerPlatformAdapter>();
    private readonly TimerSessionController _controller;

    public TimerSessionControllerTests()
    {
        _controller = new TimerSessionController(_timerAndAudioService, _platformAdapter);
    }

    [Fact]
    public async Task StartAsync_NotInBreakMode_StartsPlatformAdapterInFocusMode()
    {
        var request = DefaultRequest with { SessionMode = TimerSessionMode.Break };

        await _controller.StartAsync(request);

        await _platformAdapter.Received(1).StartAsync(Arg.Is<TimerStartRequest>(
            r => r.SessionMode == TimerSessionMode.Focus
                && r.DurationSeconds == 1500
                && r.DisplayTaskName == "Aufgabe"
                && r.BreakDurationSeconds == 300));
    }

    [Fact]
    public async Task StartAsync_InBreakMode_StartsPlatformAdapterInBreakMode()
    {
        _controller.Complete();

        await _controller.StartAsync(DefaultRequest);

        await _platformAdapter.Received(1).StartAsync(Arg.Is<TimerStartRequest>(
            r => r.SessionMode == TimerSessionMode.Break));
    }

    [Fact]
    public async Task SwitchTimerToStartPauseOrResumeAsync_TimerRunning_PausesOnly()
    {
        _timerAndAudioService.IsTimerRunning.Returns(true);

        await _controller.SwitchTimerToStartPauseOrResumeAsync(DefaultRequest);

        await _platformAdapter.Received(1).PauseAsync();
        await _platformAdapter.DidNotReceive().ResumeAsync();
        await _platformAdapter.DidNotReceive().StartAsync(Arg.Any<TimerStartRequest>());
    }

    [Fact]
    public async Task SwitchTimerToStartPauseOrResumeAsync_TimerPaused_ResumesOnly()
    {
        _timerAndAudioService.IsTimerPaused.Returns(true);
        var remainingTime = TimeSpan.FromMinutes(10);
        _timerAndAudioService.RemainingTime.Returns(remainingTime);

        await _controller.SwitchTimerToStartPauseOrResumeAsync(DefaultRequest);

        await _platformAdapter.Received(1).ResumeAsync();
        await _platformAdapter.DidNotReceive().PauseAsync();
        await _platformAdapter.DidNotReceive().StartAsync(Arg.Any<TimerStartRequest>());
    }

    [Fact]
    public async Task SwitchTimerToStartPauseOrResumeAsync_TimerIdle_StartsOnly()
    {
        await _controller.SwitchTimerToStartPauseOrResumeAsync(DefaultRequest);

        await _platformAdapter.Received(1).StartAsync(Arg.Any<TimerStartRequest>());
        await _platformAdapter.DidNotReceive().PauseAsync();
        await _platformAdapter.DidNotReceive().ResumeAsync();
    }

    [Fact]
    public async Task ResumeAsync_RemainingTimeIsZero_DoesNotResumePlatformAdapter()
    {
        _timerAndAudioService.RemainingTime.Returns(TimeSpan.Zero);

        await _controller.ResumeAsync();

        await _platformAdapter.DidNotReceive().ResumeAsync();
    }

    [Fact]
    public async Task ResumeAsync_RemainingTimeIsPositive_ResumesPlatformAdapter()
    {
        var remainingTime = TimeSpan.FromSeconds(1);
        _timerAndAudioService.RemainingTime.Returns(remainingTime);

        await _controller.ResumeAsync();

        await _platformAdapter.Received(1).ResumeAsync();
    }

    [Fact]
    public async Task PauseAsync_Always_PausesPlatformAdapter()
    {
        await _controller.PauseAsync();

        await _platformAdapter.Received(1).PauseAsync();
    }

    [Fact]
    public async Task StopAsync_TimerNotActive_DoesNothing()
    {
        var remainingTime = TimeSpan.FromMinutes(5);
        _timerAndAudioService.RemainingTime.Returns(remainingTime);

        await _controller.StopAsync();

        await _platformAdapter.DidNotReceive().StopAsync();
    }

    [Fact]
    public async Task StopAsync_TimerActiveWithoutRemainingTime_DoesNothingAndKeepsBreakMode()
    {
        _controller.Complete();
        _timerAndAudioService.IsTimerRunning.Returns(true);
        _timerAndAudioService.RemainingTime.Returns(TimeSpan.Zero);

        await _controller.StopAsync();

        await _platformAdapter.DidNotReceive().StopAsync();
        Assert.True(_controller.IsBreakMode);
    }

    [Fact]
    public async Task StopAsync_RunningFocusTimer_StopsPlatformAdapter()
    {
        _timerAndAudioService.IsTimerRunning.Returns(true);
        var remainingTime = TimeSpan.FromMinutes(5);
        _timerAndAudioService.RemainingTime.Returns(remainingTime);

        await _controller.StopAsync();

        await _platformAdapter.Received(1).StopAsync();
    }

    [Fact]
    public async Task StopAsync_PausedTimerInBreakMode_StopsAndLeavesBreakMode()
    {
        _controller.Complete();
        _timerAndAudioService.IsTimerPaused.Returns(true);
        var remainingTime = TimeSpan.FromMinutes(2);
        _timerAndAudioService.RemainingTime.Returns(remainingTime);

        await _controller.StopAsync();

        await _platformAdapter.Received(1).StopAsync();
        Assert.False(_controller.IsBreakMode);
    }

    [Fact]
    public void Complete_InFocusMode_ReturnsFocusCompletedAndEntersBreakMode()
    {
        var result = _controller.Complete();

        Assert.Equal(SessionCompletionType.FocusCompleted, result.Type);
        Assert.True(_controller.IsBreakMode);
    }

    [Fact]
    public void Complete_InBreakMode_ReturnsBreakCompletedAndLeavesBreakMode()
    {
        _controller.Complete();

        var result = _controller.Complete();

        Assert.Equal(SessionCompletionType.BreakCompleted, result.Type);
        Assert.False(_controller.IsBreakMode);
    }

    [Fact]
    public void ClearSession_InBreakMode_LeavesBreakMode()
    {
        _controller.Complete();

        _controller.ClearSession();

        Assert.False(_controller.IsBreakMode);
    }

    [Fact]
    public void ExitBreakMode_InBreakMode_LeavesBreakMode()
    {
        _controller.Complete();

        _controller.ExitBreakMode();

        Assert.False(_controller.IsBreakMode);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    [InlineData(false, false, false)]
    public void IsTimerActive_RunningOrPaused_ReturnsExpectedResult(bool isRunning, bool isPaused, bool expected)
    {
        _timerAndAudioService.IsTimerRunning.Returns(isRunning);
        _timerAndAudioService.IsTimerPaused.Returns(isPaused);

        Assert.Equal(expected, _controller.IsTimerActive);
    }

    [Theory]
    [InlineData(true, 0, true)]
    [InlineData(false, 1000, true)]
    [InlineData(false, 999, false)]
    [InlineData(false, 0, false)]
    public void ShouldShowCountdown_RunningStateAndRemainingTime_ReturnsExpectedResult(
        bool isRunning,
        int remainingMilliseconds,
        bool expected)
    {
        _timerAndAudioService.IsTimerRunning.Returns(isRunning);
        var remainingTime = TimeSpan.FromMilliseconds(remainingMilliseconds);
        _timerAndAudioService.RemainingTime.Returns(remainingTime);

        Assert.Equal(expected, _controller.ShouldShowCountdown);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsNotificationPermissionMissing_PlatformAdapterValue_IsPassedThrough(bool isMissing)
    {
        _platformAdapter.IsNotificationPermissionMissing.Returns(isMissing);

        Assert.Equal(isMissing, _controller.IsNotificationPermissionMissing);
    }

    [Fact]
    public void TimerChanged_TimerServiceRaisesEvent_NotifiesSubscriber()
    {
        TimerChangedEventArgs? received = null;
        _controller.TimerChanged += (_, args) => received = args;
        var remainingTime = TimeSpan.FromSeconds(10);
        var eventArgs = new TimerChangedEventArgs(remainingTime, true, TimerStateChangeReason.Tick);

        _timerAndAudioService.TimerChanged += Raise.EventWith(eventArgs);

        Assert.Same(eventArgs, received);
    }

    [Fact]
    public void SessionStateChanged_PlatformAdapterRaisesStateChanged_NotifiesSubscriber()
    {
        var raised = 0;
        _controller.SessionStateChanged += (_, _) => raised++;

        _platformAdapter.StateChanged += Raise.Event();

        Assert.Equal(1, raised);
    }

    [Fact]
    public void SessionStateChanged_AfterDispose_DoesNotNotifySubscriber()
    {
        var raised = 0;
        _controller.SessionStateChanged += (_, _) => raised++;
        _controller.Dispose();

        _platformAdapter.StateChanged += Raise.Event();

        Assert.Equal(0, raised);
    }
}
