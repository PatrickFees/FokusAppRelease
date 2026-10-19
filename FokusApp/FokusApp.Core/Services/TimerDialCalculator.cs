namespace FokusApp.Core.Services;

public static class TimerDialCalculator
{
    private const double ClockRadius = 130.0;
    private const double ProgressRingRadius = 95.0;
    private const double OuterRingRadius = 118.0;
    private const double OuterRingStrokeWidth = 12.0;
    private const double ProgressRingStrokeWidth = 14.0;
    private const double AdditionalAdjustmentMargin = 20.0;
    private const int MaxSelectableMinutes = AppConstants.MaximumMinutes;
    private const double MinimumAdjustmentRadius = ProgressRingRadius - (ProgressRingStrokeWidth / 2.0);
    private const double MaximumAdjustmentRadius = OuterRingRadius + (OuterRingStrokeWidth / 2.0) + AdditionalAdjustmentMargin;

    public static double OuterRingCircumference { get; } = 2 * Math.PI * OuterRingRadius;
    public static double ProgressRingCircumference { get; } = 2 * Math.PI * ProgressRingRadius;

    public static double GetSelectionStrokeOffset(double selectedMinuteRatio)
    {
        return OuterRingCircumference * (1 - selectedMinuteRatio);
    }

    public static double GetProgressStrokeOffset(double countdownProgress)
    {
        return ProgressRingCircumference * (1 - countdownProgress);
    }


    public static int CalculateMinutesFromPointer(
        double offsetX,
        double offsetY)
    {
        var deltaX = offsetX - ClockRadius;
        var deltaY = offsetY - ClockRadius;
        var angleRadians = Math.Atan2(deltaX, -deltaY);
        var angleDegrees = (angleRadians * 180.0 / Math.PI + 360.0) % 360.0;

        return (int)Math.Round(angleDegrees / 360.0 * MaxSelectableMinutes);
    }

    public static bool IsPointerWithinAdjustmentRing(double offsetX, double offsetY)
    {
        var normalizedX = NormalizeOffset(offsetX);
        var normalizedY = NormalizeOffset(offsetY);

        var deltaX = normalizedX - ClockRadius;
        var deltaY = normalizedY - ClockRadius;
        var distanceFromCenter = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

        return distanceFromCenter is >= MinimumAdjustmentRadius
            and <= MaximumAdjustmentRadius;
    }

    private static double NormalizeOffset(double offset)
    {
        return offset == 0 ? ClockRadius : offset;
    }
}
