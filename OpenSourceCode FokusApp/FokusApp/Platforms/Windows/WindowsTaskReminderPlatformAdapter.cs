using FokusApp.Core.Interfaces;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Resources.Localization;
using FokusApp.Services;
using Microsoft.Extensions.Localization;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace FokusApp.Platforms.Windows;

public sealed class WindowsTaskReminderPlatformAdapter : ITaskReminderPlatformAdapter, IDisposable
{
    private readonly IStringLocalizer<AppStrings> _localizer;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly TimeProvider _timeProvider;
    private readonly object _sync = new();

    private List<TaskReminder> _pending = [];
    private ITimer? _timer;
    private bool _isNotificationUnavailable;

    public WindowsTaskReminderPlatformAdapter(
        IStringLocalizer<AppStrings> localizer,
        IAppErrorCaptureService appErrorCaptureService,
        TimeProvider timeProvider)
    {
        _localizer = localizer;
        _appErrorCaptureService = appErrorCaptureService;
        _timeProvider = timeProvider;
    }

    public Task SyncAsync(IReadOnlyList<TaskReminder> reminders)
    {
        lock (_sync)
        {
            _pending = reminders.OrderBy(reminder => reminder.TriggerAt).ToList();
            ArmTimer();
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }

    private void ArmTimer()
    {
        _timer?.Dispose();
        _timer = null;

        if (_pending.Count == 0)
        {
            return;
        }

        var delay = _pending[0].TriggerAt - _timeProvider.GetLocalNow().DateTime;
        if (delay < TimeSpan.Zero)
        {
            delay = TimeSpan.Zero;
        }

        _timer = _timeProvider.CreateTimer(_ => OnTimerElapsed(), null, delay, Timeout.InfiniteTimeSpan);
    }

    private void OnTimerElapsed()
    {
        List<TaskReminder> due;
        lock (_sync)
        {
            var now = _timeProvider.GetLocalNow().DateTime;
            due = _pending.Where(reminder => reminder.TriggerAt <= now).ToList();
            _pending = _pending.Where(reminder => reminder.TriggerAt > now).ToList();
            ArmTimer();
        }

        foreach (var reminder in due)
        {
            Show(reminder);
        }
    }

    private void Show(TaskReminder reminder)
    {
        if (_isNotificationUnavailable)
        {
            return;
        }

        try
        {
            WindowsNotificationHost.EnsureRegistered();

            var note = string.IsNullOrWhiteSpace(reminder.Note) ? _localizer["Untitled"].Value : reminder.Note;
            var content = _localizer["TaskReminderContent", note, reminder.DueAt.ToString("HH:mm")];
            var notification = new AppNotificationBuilder()
                .AddText(_localizer["TaskReminderTitle"])
                .AddText(content)
                .AddArgument(WindowsNotificationHost.RouteArgument, NotificationNavigationContract.TaskReminderRoute)
                .SetAudioEvent(AppNotificationSoundEvent.Default)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
        catch (Exception ex)
        {

            _isNotificationUnavailable = true;
            _ = _appErrorCaptureService.CaptureAsync(
                ex,
                new AppErrorCaptureOptions
                {
                    Source = $"{nameof(WindowsTaskReminderPlatformAdapter)}.{nameof(Show)}",
                    IsFatal = false,
                    IsUiFailure = false
                });
        }
    }
}
