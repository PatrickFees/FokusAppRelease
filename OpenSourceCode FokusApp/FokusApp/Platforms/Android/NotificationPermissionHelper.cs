using Android.OS;

namespace FokusApp.Platforms.Android;

public static class NotificationPermissionHelper
{
    public static async Task<bool> EnsureTimerNotificationPermissionAsync()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
        {
            return true;
        }

        var currentStatus = await Permissions.CheckStatusAsync<Permissions.PostNotifications>().ConfigureAwait(false);
        if (currentStatus == PermissionStatus.Granted)
        {
            return true;
        }

        var requestedStatus = await MainThread
            .InvokeOnMainThreadAsync(Permissions.RequestAsync<Permissions.PostNotifications>)
            .ConfigureAwait(false);

        return requestedStatus == PermissionStatus.Granted;
    }
}
