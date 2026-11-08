using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using FokusApp.Services;

namespace FokusApp.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false)]
public class TaskReminderReceiver : BroadcastReceiver
{
    private const string ReminderChannelId = "task_reminders";
    private const int OpenAppRequestCode = 7200;

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null)
        {
            return;
        }

        var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
        if (notificationManager is null)
        {
            return;
        }

        EnsureChannel(notificationManager, intent.GetStringExtra(TaskReminderAlarmScheduler.ExtraChannelName));

        var builder = new NotificationCompat.Builder(context, ReminderChannelId)!;
        builder.SetSmallIcon(Resource.Mipmap.appicon);
        builder.SetContentTitle(intent.GetStringExtra(TaskReminderAlarmScheduler.ExtraTitle));
        builder.SetContentText(intent.GetStringExtra(TaskReminderAlarmScheduler.ExtraContent));
        builder.SetVisibility((int)NotificationVisibility.Public);
        builder.SetCategory(NotificationCompat.CategoryReminder);
        builder.SetPriority((int)NotificationPriority.High);
        builder.SetContentIntent(CreateOpenAppPendingIntent(context));
        builder.SetAutoCancel(true);

        var notification = builder.Build();
        if (notification is null)
        {
            return;
        }

        var notificationId = intent.GetIntExtra(TaskReminderAlarmScheduler.ExtraNotificationId, OpenAppRequestCode);
        notificationManager.Notify(notificationId, notification);
    }

    private static void EnsureChannel(NotificationManager notificationManager, string? channelName)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O ||
            notificationManager.GetNotificationChannel(ReminderChannelId) is not null)
        {
            return;
        }

        var channel = new NotificationChannel(
            ReminderChannelId,
            string.IsNullOrWhiteSpace(channelName) ? "Reminders" : channelName,
            NotificationImportance.High)
        {
            LockscreenVisibility = NotificationVisibility.Public
        };

        notificationManager.CreateNotificationChannel(channel);
    }

    private static PendingIntent CreateOpenAppPendingIntent(Context context)
    {
        var intent = new Intent(context, typeof(MainActivity));
        intent.SetAction(Intent.ActionMain);
        intent.AddCategory(Intent.CategoryLauncher);
        intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop | ActivityFlags.NewTask);
        intent.PutExtra(NotificationNavigationContract.RouteExtraKey, NotificationNavigationContract.TaskReminderRoute);

        return PendingIntent.GetActivity(
            context,
            OpenAppRequestCode,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)
            ?? throw new InvalidOperationException("Unable to create reminder notification activity intent.");
    }
}
