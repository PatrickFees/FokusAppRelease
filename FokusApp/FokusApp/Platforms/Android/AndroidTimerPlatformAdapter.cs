using Android.Content;
using FokusApp.Core.Enums;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Services;

namespace FokusApp.Platforms.Android;

public sealed class AndroidTimerPlatformAdapter : ITimerPlatformAdapter
{
    private bool _isNotificationPermissionMissing;

    public event EventHandler? StateChanged;

    public bool IsNotificationPermissionMissing => _isNotificationPermissionMissing;

    public async Task StartAsync(TimerStartRequest request)
    {
        await EnsureNotificationPermissionStateAsync().ConfigureAwait(false);

        var intent = CreateServiceIntent(TimerAndAudioServiceAndroid.ActionStartTimer);
        intent.PutExtra(TimerAndAudioServiceAndroid.ParameterDurationSeconds, request.DurationSeconds);
        intent.PutExtra(
            TimerAndAudioServiceAndroid.ParameterSessionMode,
            request.SessionMode == TimerSessionMode.Break
                ? TimerAndAudioServiceAndroid.SessionModeBreak
                : TimerAndAudioServiceAndroid.SessionModeFocus);
        intent.PutExtra(TimerAndAudioServiceAndroid.ParameterDisplayTaskName, request.DisplayTaskName);
        intent.PutExtra(TimerAndAudioServiceAndroid.ParameterBreakDurationSeconds, request.BreakDurationSeconds);

        global::Android.App.Application.Context.StartForegroundService(intent);
    }

    public Task PauseAsync()
    {
        global::Android.App.Application.Context.StartService(CreateServiceIntent(TimerAndAudioServiceAndroid.ActionPauseTimer));
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        global::Android.App.Application.Context.StartService(CreateServiceIntent(TimerAndAudioServiceAndroid.ActionResumeTimer));
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        global::Android.App.Application.Context.StartService(CreateServiceIntent(TimerAndAudioServiceAndroid.ActionStopTimer));
        return Task.CompletedTask;
    }

    private static Intent CreateServiceIntent(string action)
    {
        var intent = new Intent(global::Android.App.Application.Context, typeof(TimerAndAudioServiceAndroid));
        intent.SetAction(action);

        return intent;
    }

    private async Task EnsureNotificationPermissionStateAsync()
    {
        var hasPermission = await NotificationPermissionHelper
            .EnsureTimerNotificationPermissionAsync()
            .ConfigureAwait(false);

        var isPermissionMissing = !hasPermission;
        if (_isNotificationPermissionMissing == isPermissionMissing)
        {
            return;
        }

        _isNotificationPermissionMissing = isPermissionMissing;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
