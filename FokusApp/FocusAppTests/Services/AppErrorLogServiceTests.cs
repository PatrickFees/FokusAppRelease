using System.Text.Json;
using FokusApp.Core.DataBase.ErrorLogging.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Services;
using NSubstitute;

namespace FocusAppTests.Services;

public class AppErrorLogServiceTests
{
    private readonly IAppErrorLogRepository _repository = Substitute.For<IAppErrorLogRepository>();
    private readonly AppErrorLogService _service;
    private AppErrorLogEntity? _savedEntry;

    public AppErrorLogServiceTests()
    {
        _repository.AddAsync(Arg.Do<AppErrorLogEntity>(entry => _savedEntry = entry));
        _service = new AppErrorLogService(_repository);
    }

    [Fact]
    public async Task LogExceptionAsync_Exception_StoresEntryOnce()
    {
        var exception = new InvalidOperationException("Fehler");
        var context = new AppErrorCaptureContext();

        await _service.LogExceptionAsync(exception, context);

        await _repository.Received(1).AddAsync(Arg.Any<AppErrorLogEntity>());
    }

    [Fact]
    public async Task LogExceptionAsync_Exception_SetsIdTimestampAndExceptionData()
    {
        var exception = new InvalidOperationException("Fehler");
        var context = new AppErrorCaptureContext();
        var expectedExceptionType = typeof(InvalidOperationException).FullName;
        var before = DateTime.UtcNow;

        await _service.LogExceptionAsync(exception, context);

        var after = DateTime.UtcNow;
        Assert.NotNull(_savedEntry);
        Assert.NotEqual(Guid.Empty, _savedEntry.Id);
        Assert.InRange(_savedEntry.OccurredAtUtc, before, after);
        Assert.Equal(expectedExceptionType, _savedEntry.ExceptionType);
        Assert.Equal("Fehler", _savedEntry.Message);
    }

    [Fact]
    public async Task LogExceptionAsync_ThrownException_StoresStackTrace()
    {
        var exception = CreateThrownException();
        var context = new AppErrorCaptureContext();

        await _service.LogExceptionAsync(exception, context);

        var stackTrace = _savedEntry?.StackTrace;
        var hasStackTrace = !string.IsNullOrWhiteSpace(stackTrace);
        Assert.True(hasStackTrace);
    }

    [Fact]
    public async Task LogExceptionAsync_ExceptionNeverThrown_StoresNullStackTrace()
    {
        var exception = new Exception("Fehler");
        var context = new AppErrorCaptureContext();

        await _service.LogExceptionAsync(exception, context);

        Assert.Null(_savedEntry?.StackTrace);
    }

    [Fact]
    public async Task LogExceptionAsync_ContextValues_AreCopiedToEntry()
    {
        var exception = new Exception("Fehler");
        var context = new AppErrorCaptureContext
        {
            Source = "FocusTimer",
            IsFatal = true,
            IsUiFailure = true,
            Route = "/timer",
            Platform = "Android",
            OsVersion = "15",
            AppVersion = "1.2.3 (45)",
            ThreadId = 42
        };

        await _service.LogExceptionAsync(exception, context);

        Assert.NotNull(_savedEntry);
        Assert.Equal("FocusTimer", _savedEntry.Source);
        Assert.True(_savedEntry.IsFatal);
        Assert.True(_savedEntry.IsUiFailure);
        Assert.Equal("/timer", _savedEntry.Route);
        Assert.Equal("Android", _savedEntry.Platform);
        Assert.Equal("15", _savedEntry.OsVersion);
        Assert.Equal("1.2.3 (45)", _savedEntry.AppVersion);
        Assert.Equal(42, _savedEntry.ThreadId);
    }

    [Fact]
    public async Task LogExceptionAsync_NullContext_UsesDefaults()
    {
        var exception = new Exception("Fehler");

        await _service.LogExceptionAsync(exception, null!);

        Assert.NotNull(_savedEntry);
        Assert.Equal("Unknown", _savedEntry.Source);
        Assert.False(_savedEntry.IsFatal);
        Assert.False(_savedEntry.IsUiFailure);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LogExceptionAsync_BlankSource_FallsBackToUnknown(string? source)
    {
        var exception = new Exception("Fehler");
        var context = new AppErrorCaptureContext { Source = source! };

        await _service.LogExceptionAsync(exception, context);

        Assert.Equal("Unknown", _savedEntry?.Source);
    }

    [Fact]
    public async Task LogExceptionAsync_SourceWithWhitespace_IsTrimmed()
    {
        var exception = new Exception("Fehler");
        var context = new AppErrorCaptureContext { Source = "  Settings  " };

        await _service.LogExceptionAsync(exception, context);

        Assert.Equal("Settings", _savedEntry?.Source);
    }

    [Fact]
    public async Task LogExceptionAsync_BlankMessage_FallsBackToExceptionTypeName()
    {
        var exception = new BlankMessageException();
        var context = new AppErrorCaptureContext();

        await _service.LogExceptionAsync(exception, context);

        Assert.Equal(nameof(BlankMessageException), _savedEntry?.Message);
    }

    [Fact]
    public async Task LogExceptionAsync_BlankOptionalValues_AreStoredAsNull()
    {
        var exception = new Exception("Fehler");
        var context = new AppErrorCaptureContext
        {
            Route = "  ",
            Platform = "",
            OsVersion = "   ",
            AppVersion = null
        };

        await _service.LogExceptionAsync(exception, context);

        Assert.NotNull(_savedEntry);
        Assert.Null(_savedEntry.Route);
        Assert.Null(_savedEntry.Platform);
        Assert.Null(_savedEntry.OsVersion);
        Assert.Null(_savedEntry.AppVersion);
    }

    [Fact]
    public async Task LogExceptionAsync_ThreadIdZero_UsesCurrentManagedThreadId()
    {
        var exception = new Exception("Fehler");
        var context = new AppErrorCaptureContext { ThreadId = 0 };

        await _service.LogExceptionAsync(exception, context);

        Assert.Equal(Environment.CurrentManagedThreadId, _savedEntry?.ThreadId);
    }

    [Fact]
    public async Task LogExceptionAsync_WithoutInnerException_StoresNullInnerException()
    {
        var exception = new Exception("Fehler");
        var context = new AppErrorCaptureContext();

        await _service.LogExceptionAsync(exception, context);

        Assert.Null(_savedEntry?.InnerException);
    }

    [Fact]
    public async Task LogExceptionAsync_NestedInnerExceptions_StoresWholeChainOnePerLine()
    {
        var innermost = new ArgumentException("innen");
        var middle = new InvalidOperationException("mitte", innermost);
        var exception = new Exception("außen", middle);
        var context = new AppErrorCaptureContext();

        await _service.LogExceptionAsync(exception, context);

        var innerExceptionText = _savedEntry?.InnerException;
        Assert.NotNull(innerExceptionText);
        var lines = innerExceptionText.Split(Environment.NewLine);
        Assert.Equal(2, lines.Length);
        Assert.Equal("System.InvalidOperationException: mitte", lines[0]);
        Assert.Equal("System.ArgumentException: innen", lines[1]);
    }

    [Fact]
    public async Task LogExceptionAsync_MetadataWithSensitiveKeys_StoresOnlySanitizedPayload()
    {
        var exception = new Exception("Fehler");
        var context = new AppErrorCaptureContext
        {
            Metadata = new Dictionary<string, string>
            {
                ["Page"] = "Home",
                ["Password"] = "geheim"
            }
        };

        await _service.LogExceptionAsync(exception, context);

        var payloadJson = _savedEntry?.PayloadJson;
        Assert.NotNull(payloadJson);
        var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(payloadJson);
        Assert.NotNull(payload);
        var entry = Assert.Single(payload);
        Assert.Equal("Home", entry.Value);
        Assert.DoesNotContain("geheim", payloadJson);
    }

    [Fact]
    public async Task LogExceptionAsync_WithoutMetadata_StoresNullPayload()
    {
        var exception = new Exception("Fehler");
        var context = new AppErrorCaptureContext();

        await _service.LogExceptionAsync(exception, context);

        Assert.Null(_savedEntry?.PayloadJson);
    }

    [Fact]
    public async Task GetLatestAsync_Repository_ReturnsRepositoryEntries()
    {
        var entry = new AppErrorLogEntity { Id = Guid.NewGuid() };
        var entries = new List<AppErrorLogEntity> { entry };
        _repository.GetLatestAsync().Returns(entries);

        var result = await _service.GetLatestAsync();

        Assert.Same(entries, result);
    }

    private static Exception CreateThrownException()
    {
        try
        {
            throw new InvalidOperationException("Fehler");
        }
        catch (InvalidOperationException exception)
        {
            return exception;
        }
    }

    private sealed class BlankMessageException : Exception
    {
        public override string Message => " ";
    }
}
