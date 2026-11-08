using FokusApp.Core.Interfaces;

namespace FokusApp.Core.Services;

public sealed class NoOpCalendarAutoSyncBackgroundScheduler : ICalendarAutoSyncBackgroundScheduler
{
    public void Enable()
    {
    }

    public void Disable()
    {
    }
}
