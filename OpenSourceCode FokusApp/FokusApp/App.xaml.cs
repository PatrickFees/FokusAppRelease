using FokusApp.Core.Interfaces;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using System.Globalization;

namespace FokusApp;
public partial class App : Application
{
    private const double DesktopWindowWidth = 620;
    private const double DesktopWindowHeight = 940;
    private const double DesktopWindowMinimumWidth = 380;
    private const double DesktopWindowMinimumHeight = 600;

    private readonly MainPage _mainPage;
    private readonly IStartupDataChecker _startupDataChecker;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly IAppLocalizationService _appLocalizationService;
    private readonly IAppStartupStateService _appStartupStateService;
    private readonly IAppDataCoordinator _appDataCoordinator;
    private readonly IDayChangeService _dayChangeService;
    private readonly IAppThemeService _appThemeService;
    private readonly IServiceScopeFactory _scopeFactory;

    public App(
        MainPage mainPage,
        IStartupDataChecker startupDataChecker,
        IAppErrorCaptureService appErrorCaptureService,
        IAppLocalizationService appLocalizationService,
        IAppStartupStateService appStartupStateService,
        IAppDataCoordinator appDataCoordinator,
        IDayChangeService dayChangeService,
        IAppThemeService appThemeService,
        IServiceScopeFactory scopeFactory)
    {
        InitializeComponent();
        _mainPage = mainPage;
        _startupDataChecker = startupDataChecker;
        _appErrorCaptureService = appErrorCaptureService;
        _appLocalizationService = appLocalizationService;
        _appStartupStateService = appStartupStateService;
        _appDataCoordinator = appDataCoordinator;
        _dayChangeService = dayChangeService;
        _appThemeService = appThemeService;
        _scopeFactory = scopeFactory;
        RegisterGlobalErrorHandlers();
        _ = RunStartupInitAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_mainPage) { Title = "FokusApp" };

        if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
        {
            window.Width = DesktopWindowWidth;
            window.Height = DesktopWindowHeight;
            window.MinimumWidth = DesktopWindowMinimumWidth;
            window.MinimumHeight = DesktopWindowMinimumHeight;
        }

        window.Resumed += (_, _) =>
        {
            _dayChangeService.CheckNow();
            _ = RunCalendarAutoSyncAsync();
        };
        return window;
    }

    private async Task RunCalendarAutoSyncAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<ICalendarAutoSyncRunner>();
            await runner.RunIfDueAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureAsync(
                ex,
                new AppErrorCaptureOptions
                {
                    Source = "App.CalendarAutoSync",
                    IsFatal = false
                }).ConfigureAwait(false);
        }
    }

    private void RestoreCalendarAutoSyncBackgroundJob()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var settingsService = scope.ServiceProvider.GetRequiredService<ICalendarAutoSyncSettingsService>();
            if (!settingsService.Enabled)
            {
                return;
            }

            var backgroundScheduler = scope.ServiceProvider.GetRequiredService<ICalendarAutoSyncBackgroundScheduler>();
            backgroundScheduler.Enable();
        }
        catch (Exception ex)
        {
            _ = _appErrorCaptureService.CaptureAsync(
                ex,
                new AppErrorCaptureOptions
                {
                    Source = "App.CalendarAutoSyncBackgroundJobRestore",
                    IsFatal = false
                });
        }
    }

    private async Task RunStartupInitAsync()
    {
        try
        {
            await _startupDataChecker.EnsureFirstStartDataAsync();
            await _appDataCoordinator.InitializeAsync().ConfigureAwait(false);
            await _appLocalizationService.InitializeAsync();
            await _appThemeService.InitializeAsync();
            _appStartupStateService.MarkReady();
            RestoreCalendarAutoSyncBackgroundJob();
            _ = RunCalendarAutoSyncAsync();
        }
        catch (Exception ex)
        {
            _appStartupStateService.MarkFailed(ex);

            try
            {
                await _appErrorCaptureService.CaptureAsync(
                    ex,
                    new AppErrorCaptureOptions
                    {
                        Source = "App.StartupInitialization",
                        IsFatal = true,
                        IsUiFailure = true
                    });
            }
            catch
            {

            }
        }
    }

    private void RegisterGlobalErrorHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
        TaskScheduler.UnobservedTaskException += HandleUnobservedTaskException;
    }

    private void HandleUnhandledException(object? sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is not Exception exception)
        {
            return;
        }

        var metadata = new Dictionary<string, string>
        {
            ["IsTerminating"] = args.IsTerminating.ToString(CultureInfo.InvariantCulture)
        };

        _ = _appErrorCaptureService.CaptureAsync(
            exception,
            new AppErrorCaptureOptions
            {
                Source = "AppDomain.UnhandledException",
                IsFatal = args.IsTerminating,
                IsUiFailure = true,
                Metadata = metadata
            });
    }

    private void HandleUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        var metadata = new Dictionary<string, string>
        {
            ["InnerExceptionCount"] = args.Exception.InnerExceptions.Count.ToString(CultureInfo.InvariantCulture)
        };

        _ = _appErrorCaptureService.CaptureAsync(
            args.Exception,
            new AppErrorCaptureOptions
            {
                Source = "TaskScheduler.UnobservedTaskException",
                IsFatal = false,
                IsUiFailure = true,
                Metadata = metadata
            });

        args.SetObserved();
    }
}
