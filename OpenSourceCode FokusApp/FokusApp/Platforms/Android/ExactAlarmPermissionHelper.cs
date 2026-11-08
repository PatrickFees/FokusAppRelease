using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;

namespace FokusApp.Platforms.Android;

public static class ExactAlarmPermissionHelper
{
    private const string PreferencesName = "fokusapp_task_reminders";
    private const string PromptedKey = "exact_alarm_settings_opened";

    public static void EnsureRequested(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.S)
        {
            return;
        }

        if (context.GetSystemService(Context.AlarmService) is not AlarmManager alarmManager ||
            alarmManager.CanScheduleExactAlarms())
        {
            return;
        }

        var preferences = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private);
        if (preferences?.GetBoolean(PromptedKey, false) == true)
        {
            return;
        }

        var intent = new Intent(Settings.ActionRequestScheduleExactAlarm);
        intent.SetData(global::Android.Net.Uri.Parse($"package:{context.PackageName}"));
        intent.AddFlags(ActivityFlags.NewTask);
        context.StartActivity(intent);

        preferences?.Edit()?.PutBoolean(PromptedKey, true)?.Apply();
    }
}
