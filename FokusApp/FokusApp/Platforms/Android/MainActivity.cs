using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using FokusApp.Services;

namespace FokusApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ResizeableActivity = true, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleNavigationIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        HandleNavigationIntent(intent);
    }

    private static void HandleNavigationIntent(Intent? intent)
    {
        if (intent is null)
        {
            return;
        }

        var requestedRoute = intent.GetStringExtra(NotificationNavigationContract.RouteExtraKey);
        if (string.IsNullOrWhiteSpace(requestedRoute))
        {
            return;
        }

        NotificationNavigationBridge.RequestRoute(requestedRoute);
        intent.RemoveExtra(NotificationNavigationContract.RouteExtraKey);
    }
}
