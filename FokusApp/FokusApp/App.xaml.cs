using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using System.Globalization;

namespace FokusApp; 
public partial class App : Application
{
    private readonly MainPage _mainPage;
    private readonly IStartupDataChecker _startupDataChecker;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly IAppLocalizationService _appLocalizationService;
    private readonly IAppStartupStateService _appStartupStateService;
    private readonly IAppDataCoordinator _appDataCoordinator;
    private readonly IDayChangeService _dayChangeService;

    public App(
        MainPage mainPage,
        IStartupDataChecker startupDataChecker,
        IAppErrorCaptureService appErrorCaptureService,
        IAppLocalizationService appLocalizationService,
        IAppStartupStateService appStartupStateService,
        IAppDataCoordinator appDataCoordinator,
        IDayChangeService dayChangeService)
    {
        InitializeComponent();
        _mainPage = mainPage;
        _startupDataChecker = startupDataChecker;
        _appErrorCaptureService = appErrorCaptureService;
        _appLocalizationService = appLocalizationService;
        _appStartupStateService = appStartupStateService;
        _appDataCoordinator = appDataCoordinator;
        _dayChangeService = dayChangeService;
        RegisterGlobalErrorHandlers();
        _ = RunStartupInitAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_mainPage) { Title = "FokusApp" };
        window.Resumed += (_, _) => _dayChangeService.CheckNow();
        return window;
    }

    private async Task RunStartupInitAsync()
    {
        try
        {
            await _startupDataChecker.EnsureFirstStartDataAsync();
            await _appDataCoordinator.InitializeAsync().ConfigureAwait(false);
            await _appLocalizationService.InitializeAsync();
            _appStartupStateService.MarkReady();
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
                // The startup failure is already available to the UI. A logging
                // failure must not turn this observed task into another fault.
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
