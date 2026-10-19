using FokusApp.Core;
using FokusApp.Core.Services;

namespace FocusAppTests.Services;

public class TimerDialCalculatorTests
{
    private const double Center = 130.0;

    [Theory]
    [InlineData(130.0, 0.0, 0)]
    [InlineData(260.0, 130.0, 30)]
    [InlineData(130.0, 260.0, 60)]
    [InlineData(1.0, 130.0, 90)]
    public void CalculateMinutesFromPointer_PointerAtCompassPoint_ReturnsExpectedMinutes(
        double offsetX,
        double offsetY,
        int expectedMinutes)
    {
        var minutes = TimerDialCalculator.CalculateMinutesFromPointer(offsetX, offsetY);

        Assert.Equal(expectedMinutes, minutes);
    }

    [Fact]
    public void CalculateMinutesFromPointer_AnyPointerPosition_StaysWithinMaximumMinutes()
    {
        for (var angle = 0; angle < 360; angle++)
        {
            var radians = angle * Math.PI / 180.0;
            var offsetX = Center + (100 * Math.Sin(radians));
            var offsetY = Center - (100 * Math.Cos(radians));

            var minutes = TimerDialCalculator.CalculateMinutesFromPointer(offsetX, offsetY);

            Assert.InRange(minutes, 0, AppConstants.MaximumMinutes);
        }
    }

    [Theory]
    [InlineData(218.0, 130.0, true)]
    [InlineData(274.0, 130.0, true)]
    [InlineData(217.0, 130.0, false)]
    [InlineData(275.0, 130.0, false)]
    [InlineData(130.0, 130.0, false)]
    [InlineData(0.0, 0.0, false)]
    public void IsPointerWithinAdjustmentRing_PointerAtDistance_ReturnsExpectedResult(
        double offsetX,
        double offsetY,
        bool expected)
    {
        var result = TimerDialCalculator.IsPointerWithinAdjustmentRing(offsetX, offsetY);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsPointerWithinAdjustmentRing_ZeroOffsetOnOneAxis_IsTreatedAsCenterOnThatAxis()
    {
        var result = TimerDialCalculator.IsPointerWithinAdjustmentRing(0.0, 218.0);

        Assert.True(result);
    }

    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(0.5, 0.5)]
    [InlineData(1.0, 0.0)]
    public void GetSelectionStrokeOffset_Ratio_ReturnsRemainingShareOfCircumference(double ratio, double expectedShare)
    {
        var offset = TimerDialCalculator.GetSelectionStrokeOffset(ratio);

        Assert.Equal(TimerDialCalculator.OuterRingCircumference * expectedShare, offset, precision: 6);
    }

    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(0.25, 0.75)]
    [InlineData(1.0, 0.0)]
    public void GetProgressStrokeOffset_Progress_ReturnsRemainingShareOfCircumference(double progress, double expectedShare)
    {
        var offset = TimerDialCalculator.GetProgressStrokeOffset(progress);

        Assert.Equal(TimerDialCalculator.ProgressRingCircumference * expectedShare, offset, precision: 6);
    }
}
