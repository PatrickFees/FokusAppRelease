using FokusApp.Core.Enums;
using FokusApp.Core.Services;

namespace FocusAppTests.Services;

public class TimerChangedEventArgsTests
{
    [Theory]
    [InlineData(true, TimerStateChangeReason.Tick)]
    [InlineData(false, TimerStateChangeReason.Tick)]
    public void Constructor_Values_AreExposedUnchanged(bool isRunning, TimerStateChangeReason reason)
    {
        var remainingTime = TimeSpan.FromSeconds(90);

        var args = new TimerChangedEventArgs(remainingTime, isRunning, reason);

        Assert.Equal(remainingTime, args.RemainingTime);
        Assert.Equal(isRunning, args.IsRunning);
        Assert.Equal(reason, args.Reason);
    }

    [Theory]
    [MemberData(nameof(AllReasons))]
    public void Constructor_EveryReason_IsExposedUnchanged(TimerStateChangeReason reason)
    {
        var args = new TimerChangedEventArgs(TimeSpan.Zero, false, reason);

        Assert.Equal(reason, args.Reason);
    }

    public static TheoryData<TimerStateChangeReason> AllReasons()
    {
        var data = new TheoryData<TimerStateChangeReason>();
        foreach (var reason in Enum.GetValues<TimerStateChangeReason>())
        {
            data.Add(reason);
        }

        return data;
    }
}
