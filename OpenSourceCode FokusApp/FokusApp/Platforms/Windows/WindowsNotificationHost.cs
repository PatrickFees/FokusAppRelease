using FokusApp.Services;
using Microsoft.Windows.AppNotifications;

namespace FokusApp.Platforms.Windows;

internal static class WindowsNotificationHost
{
    public const string RouteArgument = "route";

    private static readonly object SyncRoot = new();
    private static bool _isRegistered;

    public static void EnsureRegistered()
    {
        lock (SyncRoot)
        {
            if (_isRegistered)
            {
                return;
            }

            AppNotificationManager.Default.NotificationInvoked += (_, args) =>
            {
                var route = args.Arguments.TryGetValue(RouteArgument, out var requestedRoute)
                    ? requestedRoute
                    : NotificationNavigationContract.FocusTimerRoute;
                NotificationNavigationBridge.RequestRoute(route);
            };
            AppNotificationManager.Default.Register();
            _isRegistered = true;
        }
    }
}
