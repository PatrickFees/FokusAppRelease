using FokusApp.Core.Enums;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Resources.Localization;
using FokusApp.Services;
using Microsoft.Extensions.Localization;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace FokusApp.Platforms.Windows;

public sealed class WindowsTimerPlatformAdapter : ITimerPlatformAdapter
{
    private readonly ITimerAndAudioService _timerAndAudioService;
    private readonly IStringLocalizer<AppStrings> _localizer;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly object _sync = new();

    private TimerSessionMode _sessionMode = TimerSessionMode.Focus;
    private int _breakDurationSeconds;
    private bool _isNotificationUnavailable;

    public WindowsTimerPlatformAdapter(
        ITimerAndAudioService timerAndAudioService,
        IStringLocalizer<AppStrings> localizer,
        IAppErrorCaptureService appErrorCaptureService)
    {
        _timerAndAudioService = timerAndAudioService;
        _localizer = localizer;
        _appErrorCaptureService = appErrorCaptureService;
        _timerAndAudioService.TimerChanged += OnTimerChanged;
    }

    public event EventHandler? StateChanged
    {
        add { }
        remove { }
    }

    public bool IsNotificationPermissionMissing => false;

    public Task StartAsync(TimerStartRequest request)
    {
        lock (_sync)
        {
            _sessionMode = request.SessionMode;
            _breakDurationSeconds = request.BreakDurationSeconds;
        }

        _timerAndAudioService.StartTimer(request.DurationSeconds);
        return Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        _timerAndAudioService.PauseTimer();
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        _timerAndAudioService.ResumeTimer();
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _timerAndAudioService.ClearTimer();
        return Task.CompletedTask;
    }

    private void OnTimerChanged(object? sender, TimerChangedEventArgs e)
    {
        if (e.Reason != TimerStateChangeReason.Completed)
        {
            return;
        }

        try
        {
            ShowCompletionNotification();
        }
        catch (Exception ex)
        {

            _isNotificationUnavailable = true;
            _ = _appErrorCaptureService.CaptureAsync(
                ex,
                new AppErrorCaptureOptions
                {
                    Source = $"{nameof(WindowsTimerPlatformAdapter)}.{nameof(ShowCompletionNotification)}",
                    IsFatal = false,
                    IsUiFailure = false
                });
        }
    }

    private void ShowCompletionNotification()
    {
        if (_isNotificationUnavailable)
        {
            return;
        }

        TimerSessionMode sessionMode;
        int breakDurationSeconds;
        lock (_sync)
        {
            sessionMode = _sessionMode;
            breakDurationSeconds = _breakDurationSeconds;
        }

        WindowsNotificationHost.EnsureRegistered();

        var text = TimerNotificationTextComposer.BuildCompletionText(sessionMode, breakDurationSeconds);
        var content = text.BreakMinutes is { } breakMinutes
            ? _localizer[text.ContentKey, breakMinutes]
            : _localizer[text.ContentKey];

        var notification = new AppNotificationBuilder()
            .AddText(_localizer[text.TitleKey])
            .AddText(content)
            .AddArgument(WindowsNotificationHost.RouteArgument, NotificationNavigationContract.FocusTimerRoute)
            .SetAudioEvent(AppNotificationSoundEvent.Default)
            .BuildNotification();

        AppNotificationManager.Default.Show(notification);
    }
}
