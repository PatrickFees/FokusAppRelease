using FokusApp.Core.Enums;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class TimerAlertModeSettingsService : ITimerAlertModeSettingsService
{
    private const TimerAlertMode AllFlags = TimerAlertMode.Sound | TimerAlertMode.Vibration | TimerAlertMode.AdaptToRingerMode;

    public event EventHandler? TimerAlertModeChanged;

    private readonly ISettingStore _settingStore;

    public TimerAlertModeSettingsService(ISettingStore settingStore)
    {
        _settingStore = settingStore;
        _settingStore.SettingsLoaded += OnSettingsLoaded;
    }

    public TimerAlertMode TimerAlertMode => _settingStore.Read(setting => setting is null
        ? TimerAlertMode.Sound
        : Normalize(setting.TimerAlertMode));

    public async Task SetTimerAlertModeAsync(TimerAlertMode mode)
    {
        var normalized = Normalize((int)mode);

        var changed = await _settingStore.UpdateAsync(current =>
        {
            if (current.TimerAlertMode == (int)normalized)
            {
                return null;
            }

            var updated = SettingSnapshot.Copy(current);
            updated.TimerAlertMode = (int)normalized;
            return updated;
        }).ConfigureAwait(false);

        if (changed)
        {
            TimerAlertModeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnSettingsLoaded(object? sender, EventArgs e) => TimerAlertModeChanged?.Invoke(this, EventArgs.Empty);

    private static TimerAlertMode Normalize(int mode) => (TimerAlertMode)(mode & (int)AllFlags);
}
