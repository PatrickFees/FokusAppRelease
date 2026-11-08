using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using FokusApp.Services;

namespace FokusApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ResizeableActivity = true, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {

        base.OnCreate(null);

        WindowCompat.SetDecorFitsSystemWindows(Window!, true);

        var contentView = (ViewGroup)Window!.DecorView.FindViewById(Android.Resource.Id.Content)!;
        ViewCompat.SetOnApplyWindowInsetsListener(contentView, new SystemBarsInsetsListener(Resources!.DisplayMetrics!.Density));
        ViewCompat.RequestApplyInsets(contentView);

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

    private sealed class SystemBarsInsetsListener(float density) : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        public WindowInsetsCompat OnApplyWindowInsets(Android.Views.View view, WindowInsetsCompat insets)
        {
            var systemBars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars())!;
            var minBottomPx = (int)(8 * density);
            view.SetPadding(view.PaddingLeft, view.PaddingTop, view.PaddingRight, Math.Max(systemBars.Bottom, minBottomPx));
            return insets;
        }
    }
}
