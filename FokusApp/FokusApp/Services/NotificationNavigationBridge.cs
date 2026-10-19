namespace FokusApp.Services;

public static class NotificationNavigationBridge
{
    private static readonly object SyncRoot = new();
    private static string? _pendingRoute;

    public static event EventHandler<string>? RouteRequested;

    public static void RequestRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return;
        }

        lock (SyncRoot)
        {
            _pendingRoute = route;
        }

        RouteRequested?.Invoke(null, route);
    }

    public static bool TryConsumePendingRoute(out string route)
    {
        lock (SyncRoot)
        {
            if (string.IsNullOrWhiteSpace(_pendingRoute))
            {
                route = string.Empty;
                return false;
            }

            route = _pendingRoute;
            _pendingRoute = null;
            return true;
        }
    }
}
