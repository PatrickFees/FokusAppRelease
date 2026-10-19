using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FocusAppTests.ViewModels;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FocusAppTests.Services;

public class DayChangeServiceTests
{
    private readonly FakeTimeProvider _clock = TestTasks.CreateClock();
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly DayChangeService _service;
    private readonly List<DayChangedEventArgs> _events = [];

    public DayChangeServiceTests()
    {
        _service = new DayChangeService(_clock, _errorCapture);
        _service.DayChanged += (_, args) => _events.Add(args);
    }

    [Fact]
    public void Today_AfterCreation_IsLocalDateOfClockWithoutTime()
    {
        Assert.Equal(TestTasks.Today, _service.Today);
    }

    [Fact]
    public void CheckNow_SameDay_RaisesNoEvent()
    {
        _clock.Advance(TimeSpan.FromHours(5));

        _service.CheckNow();

        Assert.Empty(_events);
        Assert.Equal(TestTasks.Today, _service.Today);
    }

    [Fact]
    public void CheckNow_AfterMidnight_RaisesEventWithPreviousAndCurrentDay()
    {
        _service.Dispose();
        _clock.Advance(TimeSpan.FromHours(15));

        _service.CheckNow();

        var args = Assert.Single(_events);
        Assert.Equal(TestTasks.Today, args.PreviousDay);
        Assert.Equal(TestTasks.Today.AddDays(1), args.CurrentDay);
        Assert.Equal(TestTasks.Today.AddDays(1), _service.Today);
    }

    [Fact]
    public void CheckNow_CalledTwiceAfterMidnight_RaisesEventOnce()
    {
        _service.Dispose();
        _clock.Advance(TimeSpan.FromHours(15));

        _service.CheckNow();
        _service.CheckNow();

        Assert.Single(_events);
    }

    [Fact]
    public void CheckNow_SeveralDaysLater_ReportsLastKnownDayAsPrevious()
    {
        _service.Dispose();
        _clock.Advance(TimeSpan.FromDays(3));

        _service.CheckNow();

        var args = Assert.Single(_events);
        Assert.Equal(TestTasks.Today, args.PreviousDay);
        Assert.Equal(TestTasks.Today.AddDays(3), args.CurrentDay);
    }

    [Fact]
    public void Timer_ClockPassesMidnight_RaisesEventWithoutCheckNow()
    {
        _clock.Advance(TimeSpan.FromHours(15));

        var args = Assert.Single(_events);
        Assert.Equal(TestTasks.Today, args.PreviousDay);
        Assert.Equal(TestTasks.Today.AddDays(1), args.CurrentDay);
    }

    [Fact]
    public void Timer_ClockStaysOnSameDay_RaisesNoEvent()
    {
        _clock.Advance(TimeSpan.FromHours(10));

        Assert.Empty(_events);
    }

    [Fact]
    public void Timer_AfterDispose_RaisesNoEvent()
    {
        _service.Dispose();

        _clock.Advance(TimeSpan.FromDays(2));

        Assert.Empty(_events);
    }

    [Fact]
    public async Task CheckNow_HandlerThrows_OtherHandlersRunAndErrorIsCaptured()
    {
        var failure = new InvalidOperationException("Fehler im Empfänger");
        _service.DayChanged += (_, _) => throw failure;
        var secondReceiverCalls = 0;
        _service.DayChanged += (_, _) => secondReceiverCalls++;
        _service.Dispose();
        _clock.Advance(TimeSpan.FromHours(15));

        _service.CheckNow();

        Assert.Single(_events);
        Assert.Equal(1, secondReceiverCalls);
        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "DayChangeService.DayChanged" &&
                options.IsUiFailure));
    }
}
