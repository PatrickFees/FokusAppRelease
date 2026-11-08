using FokusApp.Core.Enums;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.Services;

public class DefaultTimerPlatformAdapterTests
{
    private readonly ITimerAndAudioService _timerAndAudioService = Substitute.For<ITimerAndAudioService>();
    private readonly DefaultTimerPlatformAdapter _adapter;

    public DefaultTimerPlatformAdapterTests()
    {
        _adapter = new DefaultTimerPlatformAdapter(_timerAndAudioService);
    }

    [Fact]
    public void IsNotificationPermissionMissing_Always_IsFalse()
    {
        Assert.False(_adapter.IsNotificationPermissionMissing);
    }

    [Fact]
    public async Task StartAsync_Request_StartsTimerWithRequestedDuration()
    {
        var request = new TimerStartRequest(1500, TimerSessionMode.Focus, "Aufgabe", 300);

        await _adapter.StartAsync(request);

        _timerAndAudioService.Received(1).StartTimer(1500);
    }

    [Fact]
    public async Task PauseAsync_Always_PausesTimer()
    {
        await _adapter.PauseAsync();

        _timerAndAudioService.Received(1).PauseTimer();
    }

    [Fact]
    public async Task ResumeAsync_Always_ResumesTimer()
    {
        await _adapter.ResumeAsync();

        _timerAndAudioService.Received(1).ResumeTimer();
    }

    [Fact]
    public async Task StopAsync_Always_ClearsTimer()
    {
        await _adapter.StopAsync();

        _timerAndAudioService.Received(1).ClearTimer();
    }

    [Fact]
    public void StateChanged_SubscribeAndUnsubscribe_DoesNotThrow()
    {
        EventHandler handler = (_, _) => { };

        var exception = Record.Exception(() =>
        {
            _adapter.StateChanged += handler;
            _adapter.StateChanged -= handler;
        });

        Assert.Null(exception);
    }
}
