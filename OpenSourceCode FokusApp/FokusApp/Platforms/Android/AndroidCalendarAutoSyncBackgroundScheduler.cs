using FokusApp.Core.Interfaces;

namespace FokusApp.Platforms.Android;

public sealed class AndroidCalendarAutoSyncBackgroundScheduler : ICalendarAutoSyncBackgroundScheduler
{
    public void Enable() => CalendarAutoSyncScheduler.Enable(global::Android.App.Application.Context);

    public void Disable() => CalendarAutoSyncScheduler.Disable(global::Android.App.Application.Context);
}
