using FokusApp.Core.Enums;

namespace FokusApp.Core.Interfaces;

public interface ITimerAlertEffectService
{

    bool SupportsNativeEffects { get; }

    bool IsSystemRingerModeNormal { get; }

    Task TriggerAsync(TimerAlertMode mode);
}
