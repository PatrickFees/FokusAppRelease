using Android.Content;
using AndroidX.Work;
using Java.Util.Concurrent;

namespace FokusApp.Platforms.Android;

public static class CalendarAutoSyncScheduler
{
    private const string UniqueWorkName = "FokusApp.CalendarAutoSync";
    private const long RepeatIntervalMinutes = 60;

    public static void Enable(Context context)
    {
        var request = new PeriodicWorkRequest.Builder(typeof(CalendarAutoSyncWorker), RepeatIntervalMinutes, TimeUnit.Minutes!)
            .Build();

        WorkManager.GetInstance(context)!.EnqueueUniquePeriodicWork(
            UniqueWorkName,
            ExistingPeriodicWorkPolicy.Keep!,
            request);
    }

    public static void Disable(Context context)
    {
        WorkManager.GetInstance(context)!.CancelUniqueWork(UniqueWorkName);
    }
}
