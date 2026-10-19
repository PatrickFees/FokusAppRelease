using FokusApp.Core.Enums;
using FokusApp.Core.Services;

namespace FocusAppTests.Services;

public class TimerNotificationTextComposerTests
{
    [Theory]
    [InlineData(0, "00:00")]
    [InlineData(5, "00:05")]
    [InlineData(125, "02:05")]
    [InlineData(1500, "25:00")]
    [InlineData(7200, "120:00")]
    public void BuildRemainingTimeText_PositiveTime_FormatsAsMinutesAndSeconds(int totalSeconds, string expected)
    {
        var remainingTime = TimeSpan.FromSeconds(totalSeconds);
        var text = TimerNotificationTextComposer.BuildRemainingTimeText(remainingTime);

        Assert.Equal(expected, text);
    }

    [Fact]
    public void BuildRemainingTimeText_NegativeTime_ReturnsZero()
    {
        var remainingTime = TimeSpan.FromSeconds(-30);
        var text = TimerNotificationTextComposer.BuildRemainingTimeText(remainingTime);

        Assert.Equal("00:00", text);
    }

    [Theory]
    [InlineData(TimerSessionMode.Focus, 300, true)]
    [InlineData(TimerSessionMode.Focus, 0, false)]
    [InlineData(TimerSessionMode.Focus, -1, false)]
    [InlineData(TimerSessionMode.Break, 300, false)]
    [InlineData(TimerSessionMode.Break, 0, false)]
    public void ShouldOfferStartBreakAction_ModeAndBreakDuration_ReturnsExpectedResult(
        TimerSessionMode mode,
        int breakDurationSeconds,
        bool expected)
    {
        var result = TimerNotificationTextComposer.ShouldOfferStartBreakAction(mode, breakDurationSeconds);

        Assert.Equal(expected, result);
    }
}
