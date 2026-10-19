using System.Globalization;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FocusAppTests.Services;

public class AppErrorCaptureServiceTests
{
    private readonly IAppErrorLogService _errorLogService = Substitute.For<IAppErrorLogService>();
    private readonly IAppEnvironmentInfo _environmentInfo = Substitute.For<IAppEnvironmentInfo>();
    private readonly ILogger<AppErrorCaptureService> _logger = Substitute.For<ILogger<AppErrorCaptureService>>();
    private readonly AppErrorCaptureService _service;
    private Exception? _loggedException;
    private AppErrorCaptureContext? _loggedContext;

    public AppErrorCaptureServiceTests()
    {
        _errorLogService.LogExceptionAsync(
            Arg.Do<Exception>(exception => _loggedException = exception),
            Arg.Do<AppErrorCaptureContext>(context => _loggedContext = context));
        _environmentInfo.Platform.Returns("Android");
        _environmentInfo.OsVersion.Returns("15");
        _environmentInfo.AppVersion.Returns("1.2.3 (45)");
        _service = new AppErrorCaptureService(_errorLogService, _environmentInfo, _logger);
    }

    [Fact]
    public async Task CaptureAsync_Exception_PassesExceptionToErrorLog()
    {
        var exception = new InvalidOperationException("Fehler");

        await _service.CaptureAsync(exception);

        Assert.Same(exception, _loggedException);
    }

    [Fact]
    public async Task CaptureAsync_WithoutOptions_UsesDefaultOptions()
    {
        var exception = new Exception("Fehler");

        await _service.CaptureAsync(exception);

        Assert.NotNull(_loggedContext);
        Assert.Equal("App", _loggedContext.Source);
        Assert.False(_loggedContext.IsFatal);
        Assert.False(_loggedContext.IsUiFailure);
        Assert.Null(_loggedContext.Route);
    }

    [Fact]
    public async Task CaptureAsync_Options_AreCopiedToContext()
    {
        var exception = new Exception("Fehler");
        var options = new AppErrorCaptureOptions
        {
            Source = "FocusTimer",
            IsFatal = true,
            IsUiFailure = true,
            Route = "/timer"
        };

        await _service.CaptureAsync(exception, options);

        Assert.NotNull(_loggedContext);
        Assert.Equal("FocusTimer", _loggedContext.Source);
        Assert.True(_loggedContext.IsFatal);
        Assert.True(_loggedContext.IsUiFailure);
        Assert.Equal("/timer", _loggedContext.Route);
    }

    [Fact]
    public async Task CaptureAsync_NullException_LogsPlaceholderException()
    {
        await _service.CaptureAsync(null!);

        Assert.NotNull(_loggedException);
        Assert.Equal("Unknown Exception", _loggedException.Message);
    }

    [Fact]
    public async Task CaptureAsync_EnvironmentInfo_IsCopiedToContext()
    {
        var exception = new Exception("Fehler");

        await _service.CaptureAsync(exception);

        Assert.NotNull(_loggedContext);
        Assert.Equal("Android", _loggedContext.Platform);
        Assert.Equal("15", _loggedContext.OsVersion);
        Assert.Equal("1.2.3 (45)", _loggedContext.AppVersion);
    }

    [Fact]
    public async Task CaptureAsync_EnvironmentInfoThrows_StillLogsWithMissingValueAsNull()
    {
        var exception = new Exception("Fehler");
        _environmentInfo.Platform.Throws(new InvalidOperationException("Nicht initialisiert"));

        await _service.CaptureAsync(exception);

        Assert.NotNull(_loggedContext);
        Assert.Null(_loggedContext.Platform);
        Assert.Equal("15", _loggedContext.OsVersion);
        Assert.Equal("1.2.3 (45)", _loggedContext.AppVersion);
    }

    [Fact]
    public async Task CaptureAsync_Call_UsesCurrentManagedThreadId()
    {
        var exception = new Exception("Fehler");

        await _service.CaptureAsync(exception);

        Assert.Equal(Environment.CurrentManagedThreadId, _loggedContext?.ThreadId);
    }

    [Fact]
    public async Task CaptureAsync_Exception_AddsExceptionAndProcessMetadata()
    {
        var exception = new Exception("Fehler") { Source = "MeineQuelle" };
        var expectedHResult = exception.HResult.ToString(CultureInfo.InvariantCulture);
        var expectedThreadId = Environment.CurrentManagedThreadId.ToString(CultureInfo.InvariantCulture);
        var expectedProcessId = Environment.ProcessId.ToString(CultureInfo.InvariantCulture);

        await _service.CaptureAsync(exception);

        var metadata = _loggedContext?.Metadata;
        Assert.NotNull(metadata);
        Assert.Equal("MeineQuelle", metadata["ExceptionSource"]);
        Assert.Equal(expectedHResult, metadata["HResult"]);
        Assert.Equal(expectedThreadId, metadata["ManagedThreadId"]);
        Assert.Equal(expectedProcessId, metadata["ProcessId"]);
    }

    [Fact]
    public async Task CaptureAsync_ExceptionWithoutSource_UsesEmptyExceptionSource()
    {
        var exception = new Exception("Fehler");

        await _service.CaptureAsync(exception);

        var metadata = _loggedContext?.Metadata;
        Assert.NotNull(metadata);
        Assert.Equal(string.Empty, metadata["ExceptionSource"]);
    }

    [Fact]
    public async Task CaptureAsync_RequestedMetadata_IsMergedAndOverridesDefaults()
    {
        var exception = new Exception("Fehler");
        var options = new AppErrorCaptureOptions
        {
            Metadata = new Dictionary<string, string>
            {
                ["Page"] = "Home",
                ["processid"] = "überschrieben"
            }
        };

        await _service.CaptureAsync(exception, options);

        var metadata = _loggedContext?.Metadata;
        Assert.NotNull(metadata);
        Assert.Equal("Home", metadata["Page"]);
        Assert.Equal("überschrieben", metadata["ProcessId"]);
    }

    [Fact]
    public async Task CaptureAsync_RequestedMetadataWithBlankKeyOrValue_IsSkipped()
    {
        var exception = new Exception("Fehler");
        var options = new AppErrorCaptureOptions
        {
            Metadata = new Dictionary<string, string>
            {
                [" "] = "Wert",
                ["LeererWert"] = " "
            }
        };

        await _service.CaptureAsync(exception, options);

        var metadata = _loggedContext?.Metadata;
        Assert.NotNull(metadata);
        Assert.False(metadata.ContainsKey(" "));
        Assert.False(metadata.ContainsKey("LeererWert"));
    }

    [Fact]
    public async Task CaptureAsync_ErrorLogThrowsAsynchronously_DoesNotThrowAndLogsError()
    {
        var exception = new Exception("Fehler");
        var failure = new InvalidOperationException("Datenbank gesperrt");
        _errorLogService
            .LogExceptionAsync(Arg.Any<Exception>(), Arg.Any<AppErrorCaptureContext>())
            .ThrowsAsync(failure);

        var captureTask = _service.CaptureAsync(exception);
        await captureTask;

        Assert.True(captureTask.IsCompletedSuccessfully);
        Assert.Equal(1, CountLoggedErrors());
    }

    [Fact]
    public async Task CaptureAsync_ErrorLogThrowsSynchronously_DoesNotThrowAndLogsError()
    {
        var exception = new Exception("Fehler");
        var failure = new InvalidOperationException("Datenbank gesperrt");
        _errorLogService
            .LogExceptionAsync(Arg.Any<Exception>(), Arg.Any<AppErrorCaptureContext>())
            .Throws(failure);

        var captureTask = _service.CaptureAsync(exception);
        await captureTask;

        Assert.True(captureTask.IsCompletedSuccessfully);
        Assert.Equal(1, CountLoggedErrors());
    }

    [Fact]
    public async Task CaptureAsync_ErrorLogSucceeds_LogsNoError()
    {
        var exception = new Exception("Fehler");

        await _service.CaptureAsync(exception);

        Assert.Equal(0, CountLoggedErrors());
    }

    [Fact]
    public async Task CaptureUiErrorAsync_Exception_CapturesNonFatalUiFailureWithSource()
    {
        var captureService = Substitute.For<IAppErrorCaptureService>();
        var exception = new Exception("Fehler");

        await captureService.CaptureUiErrorAsync(exception, "SettingsViewModel.Save");

        await captureService.Received(1).CaptureAsync(
            exception,
            Arg.Is<AppErrorCaptureOptions>(options =>
                options.Source == "SettingsViewModel.Save"
                && !options.IsFatal
                && options.IsUiFailure));
    }

    private int CountLoggedErrors()
    {
        var loggedErrors = _logger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
            .Where(call => call.GetArguments()[0] is LogLevel.Error);

        return loggedErrors.Count();
    }
}
