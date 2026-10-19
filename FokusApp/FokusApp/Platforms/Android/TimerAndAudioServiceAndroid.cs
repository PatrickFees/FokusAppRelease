using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using FokusApp.Core.Enums;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Resources.Localization;
using FokusApp.Services;
using Microsoft.Extensions.Localization;

namespace FokusApp.Platforms.Android;

[Service(
   Exported = false,
   ForegroundServiceType = ForegroundService.TypeMediaPlayback
)]
public class TimerAndAudioServiceAndroid : Service
{
    private const string RunningNotificationChannelId = "timer_running";
    private const string CompletedNotificationChannelId = "timer_completed";
    private const int RunningNotificationId = 1001;
    private const int CompletedNotificationId = 1002;
    private const int OpenTimerRequestCode = 7100;
    private const int PauseRequestCode = 7101;
    private const int ResumeRequestCode = 7102;
    private const int StartBreakRequestCode = 7103;

    public const string ParameterDurationSeconds = "duration_seconds";
    public const string ParameterSessionMode = "session_mode";
    public const string ParameterDisplayTaskName = "display_task_name";
    public const string ParameterBreakDurationSeconds = "break_duration_seconds";

    public const string SessionModeFocus = "focus";
    public const string SessionModeBreak = "break";

    public const string ActionStartTimer = "com.fokusapp.timer.START";
    public const string ActionStopTimer = "com.fokusapp.timer.STOP";
    public const string ActionPauseTimer = "com.fokusapp.timer.PAUSE";
    public const string ActionResumeTimer = "com.fokusapp.timer.RESUME";
    public const string ActionStartBreak = "com.fokusapp.timer.START_BREAK";

    private ITimerAndAudioService? _timerService;
    private IStringLocalizer<AppStrings>? _localizer;
    private NotificationManager? _notificationManager;
    private bool _isForegroundRunning;
    private bool _isTimerChangedSubscribed;

    private TimerSessionMode _currentSessionMode = TimerSessionMode.Focus;
    private string _currentDisplayTaskName = TimerNotificationTextComposer.FocusFallbackName;
    private int _currentBreakDurationSeconds;

    public override void OnCreate()
    {
        base.OnCreate();
        _notificationManager = GetSystemService(NotificationService) as NotificationManager;
        EnsureTimerService();
        EnsureNotificationChannels();
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        EnsureNotificationChannels();
        EnsureTimerService();

        var action = intent?.Action;

        if (string.Equals(action, ActionStopTimer, StringComparison.Ordinal))
        {
            HandleStopTimer();
            return StartCommandResult.NotSticky;
        }

        if (string.Equals(action, ActionPauseTimer, StringComparison.Ordinal))
        {
            HandlePauseTimer();
            return StartCommandResult.NotSticky;
        }

        if (string.Equals(action, ActionResumeTimer, StringComparison.Ordinal))
        {
            HandleResumeTimer();
            return StartCommandResult.NotSticky;
        }

        if (string.Equals(action, ActionStartTimer, StringComparison.Ordinal))
        {
            HandleStartTimer(intent);
            return StartCommandResult.NotSticky;
        }

        if (string.Equals(action, ActionStartBreak, StringComparison.Ordinal))
        {
            HandleStartBreak(intent);
            return StartCommandResult.NotSticky;
        }

        return StartCommandResult.NotSticky;
    }

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }

    public override void OnDestroy()
    {
        if (_timerService is not null && _isTimerChangedSubscribed)
        {
            _timerService.TimerChanged -= OnTimerChanged;
            _isTimerChangedSubscribed = false;
        }

        base.OnDestroy();
    }

    private void HandleStartTimer(Intent? intent)
    {
        if (_timerService is null)
        {
            return;
        }

        var seconds = intent?.GetIntExtra(ParameterDurationSeconds, -1) ?? -1;
        if (seconds <= 0)
        {
            return;
        }

        ApplySessionMetadata(intent);
        _timerService.StartTimer(seconds);
        StartOrUpdateForegroundNotification();
    }

    private void HandlePauseTimer()
    {
        if (_timerService is null)
        {
            return;
        }

        _timerService.PauseTimer();
        StartOrUpdateForegroundNotification();
    }

    private void HandleResumeTimer()
    {
        if (_timerService is null)
        {
            return;
        }

        _timerService.ResumeTimer();
        StartOrUpdateForegroundNotification();
    }

    private void HandleStopTimer()
    {
        if (_timerService is null)
        {
            return;
        }

        _timerService.ClearTimer();
        StopForegroundNotification();
        CancelCompletionNotification();
        StopSelf();
    }

    private void HandleStartBreak(Intent? intent)
    {
        if (_timerService is null)
        {
            return;
        }

        var breakDurationSeconds = intent?.GetIntExtra(ParameterDurationSeconds, _currentBreakDurationSeconds)
            ?? _currentBreakDurationSeconds;

        if (breakDurationSeconds <= 0)
        {
            return;
        }

        _currentSessionMode = TimerSessionMode.Break;
        _currentDisplayTaskName = GetText("FocusTimer");
        _currentBreakDurationSeconds = breakDurationSeconds;
        _timerService.StartTimer(breakDurationSeconds);
        CancelCompletionNotification();
        StartOrUpdateForegroundNotification();
    }

    private void ApplySessionMetadata(Intent? intent)
    {
        if (intent is null)
        {
            return;
        }

        var modeValue = intent.GetStringExtra(ParameterSessionMode);
        _currentSessionMode = ParseSessionMode(modeValue);

        var providedDisplayTaskName = intent.GetStringExtra(ParameterDisplayTaskName);
        _currentDisplayTaskName = string.IsNullOrWhiteSpace(providedDisplayTaskName)
            ? GetText("FocusTimer")
            : providedDisplayTaskName.Trim();

        _currentBreakDurationSeconds = Math.Max(0, intent.GetIntExtra(ParameterBreakDurationSeconds, 0));
    }

    private void OnTimerChanged(object? sender, TimerChangedEventArgs eventArgs)
    {
        switch (eventArgs.Reason)
        {
            case TimerStateChangeReason.Started:
            case TimerStateChangeReason.Tick:
            case TimerStateChangeReason.Paused:
            case TimerStateChangeReason.Resumed:
                StartOrUpdateForegroundNotification();
                break;
            case TimerStateChangeReason.Cleared:
                StopForegroundNotification();
                CancelCompletionNotification();
                break;
            case TimerStateChangeReason.Completed:
                ShowCompletionNotification();
                StopForegroundNotification();
                StopSelf();
                break;
        }
    }

    private void StartOrUpdateForegroundNotification()
    {
        if (_timerService is null || _notificationManager is null)
        {
            return;
        }

        var notification = BuildRunningNotification();

        if (!_isForegroundRunning)
        {
            StartForegroundWithCompatibility(notification);
            _isForegroundRunning = true;
            return;
        }

        _notificationManager.Notify(RunningNotificationId, notification);
    }

    private Notification BuildRunningNotification()
    {
        if (_timerService is null)
        {
            throw new InvalidOperationException("Timer service is not available.");
        }

        var title = _currentSessionMode == TimerSessionMode.Break
            ? GetText("NotificationBreakRemaining")
            : GetText("NotificationRemainingTime", NormalizeTaskName(_currentDisplayTaskName));
        var content = TimerNotificationTextComposer.BuildRemainingTimeText(_timerService.RemainingTime);
        var actionIsPause = _timerService.IsTimerRunning;
        var actionIntent = actionIsPause ? CreatePausePendingIntent() : CreateResumePendingIntent();
        var actionLabel = actionIsPause
            ? GetText("NotificationStop")
            : GetText("NotificationResume");

        var builder = new NotificationCompat.Builder(this, RunningNotificationChannelId)!;
        builder.SetSmallIcon(Resource.Mipmap.appicon);
        builder.SetContentTitle(title);
        builder.SetContentText(content);
        builder.SetVisibility((int)NotificationVisibility.Public);
        builder.SetCategory(NotificationCompat.CategoryProgress);
        builder.SetPriority((int)NotificationPriority.Low);
        builder.SetContentIntent(CreateOpenTimerPendingIntent());
        builder.SetOnlyAlertOnce(true);
        builder.SetOngoing(true);
        builder.SetAutoCancel(false);
        builder.SetSilent(true);
        builder.AddAction(0, actionLabel, actionIntent);

        return builder.Build()
            ?? throw new InvalidOperationException("Running timer notification could not be built.");
    }

    private PendingIntent CreateOpenTimerPendingIntent()
    {
        var intent = new Intent(this, typeof(MainActivity));
        intent.SetAction(Intent.ActionMain);
        intent.AddCategory(Intent.CategoryLauncher);
        intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop | ActivityFlags.NewTask);
        intent.PutExtra(NotificationNavigationContract.RouteExtraKey, NotificationNavigationContract.FocusTimerRoute);

        return PendingIntent.GetActivity(
            this,
            OpenTimerRequestCode,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)
            ?? throw new InvalidOperationException("Unable to create timer notification activity intent.");
    }

    private PendingIntent CreatePausePendingIntent()
    {
        return CreateServicePendingIntent(PauseRequestCode, ActionPauseTimer);
    }

    private PendingIntent CreateResumePendingIntent()
    {
        return CreateServicePendingIntent(ResumeRequestCode, ActionResumeTimer);
    }

    private PendingIntent CreateServicePendingIntent(int requestCode, string action)
    {
        var intent = new Intent(this, typeof(TimerAndAudioServiceAndroid));
        intent.SetAction(action);

        return PendingIntent.GetService(
            this,
            requestCode,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)
            ?? throw new InvalidOperationException("Unable to create timer service pending intent.");
    }

    private PendingIntent CreateStartBreakPendingIntent()
    {
        var intent = new Intent(this, typeof(TimerAndAudioServiceAndroid));
        intent.SetAction(ActionStartBreak);
        intent.PutExtra(ParameterDurationSeconds, _currentBreakDurationSeconds);

        return PendingIntent.GetService(
            this,
            StartBreakRequestCode,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)
            ?? throw new InvalidOperationException("Unable to create break-start pending intent.");
    }

    private void StartForegroundWithCompatibility(Notification notification)
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake)
        {
            StartForeground(RunningNotificationId, notification, ForegroundService.TypeMediaPlayback);
            return;
        }

        StartForeground(RunningNotificationId, notification);
    }

    private void StopForegroundNotification()
    {
        if (_notificationManager is not null)
        {
            _notificationManager.Cancel(RunningNotificationId);
        }

        if (_isForegroundRunning)
        {
            StopForeground(StopForegroundFlags.Remove);
        }

        _isForegroundRunning = false;
    }

    private void ShowCompletionNotification()
    {
        if (_notificationManager is null)
        {
            return;
        }

        var title = _currentSessionMode == TimerSessionMode.Break
            ? GetText("NotificationBreakCompleted")
            : GetText("NotificationTimerCompleted", NormalizeTaskName(_currentDisplayTaskName));
        var content = BuildCompletionContent();

        var builder = new NotificationCompat.Builder(this, CompletedNotificationChannelId)!;
        builder.SetSmallIcon(Resource.Mipmap.appicon);
        builder.SetContentTitle(title);
        builder.SetContentText(content);
        builder.SetVisibility((int)NotificationVisibility.Public);
        builder.SetCategory(NotificationCompat.CategoryAlarm);
        builder.SetPriority((int)NotificationPriority.High);
        builder.SetContentIntent(CreateOpenTimerPendingIntent());
        builder.SetAutoCancel(true);
        builder.SetSilent(true);

        if (TimerNotificationTextComposer.ShouldOfferStartBreakAction(_currentSessionMode, _currentBreakDurationSeconds))
        {
            builder.AddAction(0, GetText("NotificationStartBreak"), CreateStartBreakPendingIntent());
        }

        var completionNotification = builder.Build()
            ?? throw new InvalidOperationException("Completion timer notification could not be built.");

        _notificationManager.Notify(CompletedNotificationId, completionNotification);
    }

    private void CancelCompletionNotification()
    {
        _notificationManager?.Cancel(CompletedNotificationId);
    }

    private void EnsureNotificationChannels()
    {
        if (_notificationManager is null || Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return;
        }

        if (_notificationManager.GetNotificationChannel(RunningNotificationChannelId) is null)
        {
            var runningChannel = new NotificationChannel(
                RunningNotificationChannelId,
                GetText("NotificationRunningChannel"),
                NotificationImportance.Low)
            {
                LockscreenVisibility = NotificationVisibility.Public
            };

            runningChannel.SetShowBadge(false);
            _notificationManager.CreateNotificationChannel(runningChannel);
        }

        if (_notificationManager.GetNotificationChannel(CompletedNotificationChannelId) is null)
        {
            var completedChannel = new NotificationChannel(
                CompletedNotificationChannelId,
                GetText("NotificationCompletedChannel"),
                NotificationImportance.High)
            {
                LockscreenVisibility = NotificationVisibility.Public
            };

            _notificationManager.CreateNotificationChannel(completedChannel);
        }
    }

    private void EnsureTimerService()
    {
        if (_timerService is not null)
        {
            return;
        }

        var serviceProvider = IPlatformApplication.Current?.Services;
        if (serviceProvider is null)
        {
            return;
        }

        _timerService = serviceProvider.GetRequiredService<ITimerAndAudioService>();
        _localizer = serviceProvider.GetRequiredService<IStringLocalizer<AppStrings>>();

        if (!_isTimerChangedSubscribed)
        {
            _timerService.TimerChanged += OnTimerChanged;
            _isTimerChangedSubscribed = true;
        }
    }

    private static TimerSessionMode ParseSessionMode(string? modeValue)
    {
        if (string.Equals(modeValue, SessionModeBreak, StringComparison.OrdinalIgnoreCase))
        {
            return TimerSessionMode.Break;
        }

        return TimerSessionMode.Focus;
    }

    private string BuildCompletionContent()
    {
        if (_currentSessionMode == TimerSessionMode.Break)
        {
            return GetText("NotificationBreakElapsed");
        }

        if (_currentBreakDurationSeconds <= 0)
        {
            return GetText("NotificationTaskElapsed");
        }

        var breakDurationMinutes = (int)Math.Ceiling(_currentBreakDurationSeconds / 60d);
        return GetText("NotificationTaskElapsedWithBreak", breakDurationMinutes);
    }

    private string NormalizeTaskName(string? displayTaskName) => string.IsNullOrWhiteSpace(displayTaskName)
        ? GetText("FocusTimer")
        : displayTaskName.Trim();

    private string GetText(string key, params object[] arguments)
    {
        if (_localizer is null)
        {
            return key;
        }

        return arguments.Length == 0
            ? _localizer[key]
            : _localizer[key, arguments];
    }
}
