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

    public static CompletionNotificationText BuildCompletionText(TimerSessionMode completedSessionMode, int breakDurationSeconds)
    {
        if (completedSessionMode == TimerSessionMode.Break)
        {
            return new CompletionNotificationText("NotificationBreakCompleted", "NotificationBreakElapsed", null);
        }

        if (breakDurationSeconds <= 0)
        {
            return new CompletionNotificationText("NotificationFocusCompleted", "NotificationTaskElapsed", null);
        }

        var breakMinutes = (int)Math.Ceiling(breakDurationSeconds / 60d);
        return new CompletionNotificationText(
            "NotificationFocusCompletedWithBreak",
            "NotificationTaskElapsedWithBreak",
            breakMinutes);
    }
}

public sealed record CompletionNotificationText(string TitleKey, string ContentKey, int? BreakMinutes);
