using Android.Content;
using AndroidX.Work;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Platforms.Android;

public sealed class CalendarAutoSyncWorker : Worker
{
    public CalendarAutoSyncWorker(Context context, WorkerParameters workerParameters)
        : base(context, workerParameters)
    {
    }

    public override Result DoWork()
    {
        var serviceProvider = IPlatformApplication.Current?.Services;
        if (serviceProvider is null)
        {
            return Result.InvokeSuccess()!;
        }

        using var scope = serviceProvider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<ICalendarAutoSyncRunner>();
        runner.RunIfDueAsync().GetAwaiter().GetResult();

        return Result.InvokeSuccess()!;
    }
}
