using FokusApp.Core.Enums;

namespace FokusApp.Core.Services.Interfaces;

public interface ITimerAlertModeSettingsService
{

    event EventHandler? TimerAlertModeChanged;

    TimerAlertMode TimerAlertMode { get; }

    Task SetTimerAlertModeAsync(TimerAlertMode mode);
}
