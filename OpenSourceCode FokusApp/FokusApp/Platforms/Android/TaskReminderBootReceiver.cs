using Android.App;
using Android.Content;

namespace FokusApp.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter([Intent.ActionBootCompleted, Intent.ActionMyPackageReplaced])]
public class TaskReminderBootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null)
        {
            return;
        }

        if (intent.Action is Intent.ActionBootCompleted or Intent.ActionMyPackageReplaced)
        {
            TaskReminderAlarmScheduler.Restore(context);
        }
    }
}
