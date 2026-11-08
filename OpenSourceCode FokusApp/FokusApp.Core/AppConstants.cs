namespace FokusApp.Core;

public static class AppConstants
{

    public const string DeveloperEmailAddress = "fokusappdevelopmentbypat@gmail.com";

    public const string LatestAppReleaseDownloadUrl = "https://github.com/PatrickFees/FokusAppRelease/releases/download/v1.4.1/FokusApp-1.4.1.apk";

    public const string AppRepositoryUrl = "https://github.com/PatrickFees/FokusAppRelease";

    public const int MinimumMinutes = 1;
    public const int MaximumMinutes = 120;

    public static readonly IReadOnlyList<int> ReminderMinuteOptions = [0, 5, 10, 15, 30, 60, 120, 1440];

    public const int MaximumReminderMinutes = 10080;

    public const int RepeatHorizonDays = 90;

    public const int RepeatSearchYears = 100;

    public const int CalendarAutoSyncWindowDays = 365;

    public const int OnboardingCalendarImportHorizonDays = 730;

    public static readonly TimeSpan CalendarAutoSyncMinInterval = TimeSpan.FromHours(1);
}
