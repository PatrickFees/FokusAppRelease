using FokusApp.Core.Enums;

namespace FokusApp.Core.Services;

public static class TimerNotificationTextComposer
{
    public const string FocusFallbackName = "Fokus Timer";

    public static string BuildRemainingTimeText(TimeSpan remainingTime)
    {
        var safeRemainingTime = remainingTime < TimeSpan.Zero ? TimeSpan.Zero : remainingTime;

        return $"{(int)safeRemainingTime.TotalMinutes:00}:{safeRemainingTime.Seconds:00}";
    }

    public static bool ShouldOfferStartBreakAction(TimerSessionMode completedSessionMode, int breakDurationSeconds)
    {
        return completedSessionMode == TimerSessionMode.Focus && breakDurationSeconds > 0;
    }
}
