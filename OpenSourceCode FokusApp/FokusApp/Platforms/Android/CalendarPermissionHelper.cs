namespace FokusApp.Platforms.Android;

public static class CalendarPermissionHelper
{
    public static async Task<bool> EnsureReadCalendarPermissionAsync()
    {
        var currentStatus = await Permissions.CheckStatusAsync<Permissions.CalendarRead>().ConfigureAwait(false);
        if (currentStatus == PermissionStatus.Granted)
        {
            return true;
        }

        var requestedStatus = await MainThread
            .InvokeOnMainThreadAsync(Permissions.RequestAsync<Permissions.CalendarRead>)
            .ConfigureAwait(false);

        return requestedStatus == PermissionStatus.Granted;
    }

    public static async Task<bool> CheckReadCalendarPermissionAsync()
    {
        var currentStatus = await Permissions.CheckStatusAsync<Permissions.CalendarRead>().ConfigureAwait(false);
        return currentStatus == PermissionStatus.Granted;
    }
}
