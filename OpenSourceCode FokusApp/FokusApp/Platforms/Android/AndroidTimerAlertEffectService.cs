using Android.Content;
using Android.Media;
using Android.OS;
using FokusApp.Core.Enums;
using FokusApp.Core.Interfaces;

namespace FokusApp.Platforms.Android;

public sealed class AndroidTimerAlertEffectService : ITimerAlertEffectService
{
    private const int VibrationDurationMs = 9000;

    private static readonly long[] VibrationPattern = { 0, 500, 500 };

    public bool SupportsNativeEffects => true;

    public bool IsSystemRingerModeNormal
    {
        get
        {
            var audioManager = GetAudioManager();
            return audioManager is not null && audioManager.RingerMode == RingerMode.Normal;
        }
    }

    public Task TriggerAsync(TimerAlertMode mode)
    {
        var soundSuppressedByRingerMode = mode.HasFlag(TimerAlertMode.Sound)
            && mode.HasFlag(TimerAlertMode.AdaptToRingerMode)
            && !IsSystemRingerModeNormal;

        if (mode.HasFlag(TimerAlertMode.Vibration) || soundSuppressedByRingerMode)
        {
            TriggerVibration();
        }

        return Task.CompletedTask;
    }

    private static void TriggerVibration()
    {
        var vibrator = GetVibrator();
        if (vibrator is null || !vibrator.HasVibrator)
        {
            return;
        }

        vibrator.Vibrate(VibrationEffect.CreateWaveform(VibrationPattern, repeat: 0));
        _ = StopVibrationAfterDelayAsync(vibrator);
    }

    private static Vibrator? GetVibrator()
    {
        var context = global::Android.App.Application.Context;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            var vibratorManager = context.GetSystemService(Context.VibratorManagerService) as VibratorManager;
            return vibratorManager?.DefaultVibrator;
        }

        return context.GetSystemService(Context.VibratorService) as Vibrator;
    }

    private static AudioManager? GetAudioManager()
    {
        var context = global::Android.App.Application.Context;
        return context.GetSystemService(Context.AudioService) as AudioManager;
    }

    private static async Task StopVibrationAfterDelayAsync(Vibrator vibrator)
    {
        try
        {
            await Task.Delay(VibrationDurationMs).ConfigureAwait(false);
            vibrator.Cancel();
        }
        catch
        {
        }
    }
}
