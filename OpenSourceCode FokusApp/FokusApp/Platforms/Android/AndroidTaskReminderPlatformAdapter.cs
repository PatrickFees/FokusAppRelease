using FokusApp.Core.Interfaces;
using FokusApp.Core.Models;
using FokusApp.Resources.Localization;
using Microsoft.Extensions.Localization;

namespace FokusApp.Platforms.Android;

public sealed class AndroidTaskReminderPlatformAdapter : ITaskReminderPlatformAdapter
{
    private readonly IStringLocalizer<AppStrings> _localizer;

    public AndroidTaskReminderPlatformAdapter(IStringLocalizer<AppStrings> localizer)
    {
        _localizer = localizer;
    }

    public async Task SyncAsync(IReadOnlyList<TaskReminder> reminders)
    {
        if (reminders.Count > 0)
        {
            await NotificationPermissionHelper.EnsureTimerNotificationPermissionAsync().ConfigureAwait(false);
            ExactAlarmPermissionHelper.EnsureRequested(global::Android.App.Application.Context);
        }

        var channelName = _localizer["TaskReminderTitle"].Value;
        var plan = reminders.Select(reminder => ToStoredReminder(reminder, channelName)).ToList();
        TaskReminderAlarmScheduler.Replace(global::Android.App.Application.Context, plan);
    }

    private StoredTaskReminder ToStoredReminder(TaskReminder reminder, string channelName)
    {
        var note = string.IsNullOrWhiteSpace(reminder.Note) ? _localizer["Untitled"].Value : reminder.Note;
        var content = _localizer["TaskReminderContent", note, reminder.DueAt.ToString("HH:mm")].Value;
        var triggerAt = new DateTimeOffset(DateTime.SpecifyKind(reminder.TriggerAt, DateTimeKind.Local));

        return new StoredTaskReminder(
            reminder.TaskId.GetHashCode() & int.MaxValue,
            triggerAt.ToUnixTimeMilliseconds(),
            channelName,
            channelName,
            content);
    }
}
