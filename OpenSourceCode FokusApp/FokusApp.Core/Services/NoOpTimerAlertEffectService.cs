using FokusApp.Core.Enums;
using FokusApp.Core.Interfaces;

namespace FokusApp.Core.Services;

public sealed class NoOpTimerAlertEffectService : ITimerAlertEffectService
{
    public bool SupportsNativeEffects => false;

    public bool IsSystemRingerModeNormal => true;

    public Task TriggerAsync(TimerAlertMode mode) => Task.CompletedTask;
}
