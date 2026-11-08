using System.Text.Json;
using Android.App;
using Android.Content;
using Android.OS;

namespace FokusApp.Platforms.Android;

public sealed record StoredTaskReminder(
    int RequestCode,
    long TriggerAtMillis,
    string ChannelName,
    string Title,
    string Content);

public static class TaskReminderAlarmScheduler
{
    public const string ExtraChannelName = "fokusapp.reminder.channel_name";
    public const string ExtraTitle = "fokusapp.reminder.title";
    public const string ExtraContent = "fokusapp.reminder.content";
    public const string ExtraNotificationId = "fokusapp.reminder.notification_id";

    private const string PreferencesName = "fokusapp_task_reminders";
    private const string PlanKey = "plan";

    public static void Replace(Context context, IReadOnlyList<StoredTaskReminder> plan)
    {
        foreach (var previous in Load(context))
        {
            Cancel(context, previous.RequestCode);
        }

        foreach (var reminder in plan)
        {
            Schedule(context, reminder);
        }

        Save(context, plan);
    }

    public static void Restore(Context context)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var reminder in Load(context).Where(reminder => reminder.TriggerAtMillis > now))
        {
            Schedule(context, reminder);
        }
    }

    private static void Schedule(Context context, StoredTaskReminder reminder)
    {
        if (context.GetSystemService(Context.AlarmService) is not AlarmManager alarmManager)
        {
            return;
        }

        var pendingIntent = CreatePendingIntent(context, reminder, PendingIntentFlags.UpdateCurrent)
            ?? throw new InvalidOperationException("Unable to create the reminder alarm intent.");

        var canBeExact = Build.VERSION.SdkInt < BuildVersionCodes.S || alarmManager.CanScheduleExactAlarms();
        if (canBeExact)
        {
            alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, reminder.TriggerAtMillis, pendingIntent);
        }
        else
        {
            alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, reminder.TriggerAtMillis, pendingIntent);
        }
    }

    private static void Cancel(Context context, int requestCode)
    {
        if (context.GetSystemService(Context.AlarmService) is not AlarmManager alarmManager)
        {
            return;
        }

        var existing = CreatePendingIntent(
            context,
            new StoredTaskReminder(requestCode, 0, string.Empty, string.Empty, string.Empty),
            PendingIntentFlags.NoCreate);
        if (existing is null)
        {
            return;
        }

        alarmManager.Cancel(existing);
        existing.Cancel();
    }

    private static PendingIntent? CreatePendingIntent(
        Context context,
        StoredTaskReminder reminder,
        PendingIntentFlags flags)
    {
        var intent = new Intent(context, typeof(TaskReminderReceiver));
        intent.PutExtra(ExtraChannelName, reminder.ChannelName);
        intent.PutExtra(ExtraTitle, reminder.Title);
        intent.PutExtra(ExtraContent, reminder.Content);
        intent.PutExtra(ExtraNotificationId, reminder.RequestCode);

        return PendingIntent.GetBroadcast(context, reminder.RequestCode, intent, flags | PendingIntentFlags.Immutable);
    }

    private static List<StoredTaskReminder> Load(Context context)
    {
        var json = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)?.GetString(PlanKey, null);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<StoredTaskReminder>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static void Save(Context context, IReadOnlyList<StoredTaskReminder> plan)
    {
        var editor = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)?.Edit();
        editor?.PutString(PlanKey, JsonSerializer.Serialize(plan));
        editor?.Apply();
    }
}
