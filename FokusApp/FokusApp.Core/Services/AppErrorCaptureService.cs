using FokusApp.Core.Interfaces;
using FokusApp.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace FokusApp.Core.Services;

public class AppErrorCaptureService : IAppErrorCaptureService
{
    private readonly IAppErrorLogService _errorLogService;
    private readonly IAppEnvironmentInfo _environmentInfo;
    private readonly ILogger<AppErrorCaptureService> _logger;

    public AppErrorCaptureService(
        IAppErrorLogService errorLogService,
        IAppEnvironmentInfo environmentInfo,
        ILogger<AppErrorCaptureService> logger)
    {
        _errorLogService = errorLogService;
        _environmentInfo = environmentInfo;
        _logger = logger;
    }

    // Must never throw: called fire-and-forget from global exception handlers
    // (AppDomain.UnhandledException, TaskScheduler.UnobservedTaskException). A failure
    // here becoming another unobserved task exception would re-trigger those handlers.
    public async Task CaptureAsync(Exception exception, AppErrorCaptureOptions? options = null)
    {
        exception ??= new Exception("Unknown Exception");
        options ??= new AppErrorCaptureOptions();

        try
        {
            var metadata = BuildMetadata(exception, options.Metadata);
            var context = new AppErrorCaptureContext
            {
                Source = options.Source,
                IsFatal = options.IsFatal,
                IsUiFailure = options.IsUiFailure,
                Route = options.Route,
                Platform = TryRead(() => _environmentInfo.Platform),
                OsVersion = TryRead(() => _environmentInfo.OsVersion),
                AppVersion = TryRead(() => _environmentInfo.AppVersion),
                ThreadId = Environment.CurrentManagedThreadId,
                Metadata = metadata
            };

            await _errorLogService.LogExceptionAsync(exception, context).ConfigureAwait(false);
        }
        catch (Exception captureFailure)
        {
            _logger.LogError(captureFailure, "Failed to persist captured exception from {Source}: {OriginalMessage}", options.Source, exception.Message);
        }
    }

    private static Dictionary<string, string> BuildMetadata(
        Exception exception,
        IReadOnlyDictionary<string, string>? requestedMetadata)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ExceptionSource"] = exception.Source ?? string.Empty,
            ["HResult"] = exception.HResult.ToString(CultureInfo.InvariantCulture),
            ["ManagedThreadId"] = Environment.CurrentManagedThreadId.ToString(CultureInfo.InvariantCulture),
            ["ProcessId"] = Environment.ProcessId.ToString(CultureInfo.InvariantCulture)
        };

        if (requestedMetadata is null)
        {
            return metadata;
        }

        foreach (var pair in requestedMetadata)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
            {
                continue;
            }

            metadata[pair.Key] = pair.Value;
        }

        return metadata;
    }

    // Platform APIs may throw before the app is fully initialized; a missing value must not lose the log entry.
    private static string? TryRead(Func<string?> readValue)
    {
        try
        {
            return readValue();
        }
        catch
        {
            return null;
        }
    }
}
