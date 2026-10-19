using FokusApp.Components.ErrorComponent;
using FokusApp.Core.DataBase.ErrorLogging.Entities;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FocusAppTests.ViewModels;

public class ErrorPageViewModelTests
{
    private readonly IAppErrorLogService _errorLogService = Substitute.For<IAppErrorLogService>();
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly IUiTextProvider _textProvider = TestTexts.Create();
    private readonly ErrorPageViewModel _viewModel;

    public ErrorPageViewModelTests()
    {
        var noLogs = new List<AppErrorLogEntity>();
        _errorLogService.GetLatestAsync().Returns(noLogs);
        _viewModel = new ErrorPageViewModel(_errorLogService, _errorCapture, _textProvider);
    }

    [Fact]
    public async Task InitializeAsync_NoLogs_ShowsNoItemsAndNoMessage()
    {
        await _viewModel.InitializeAsync();

        Assert.Empty(_viewModel.Items);
        Assert.Equal(string.Empty, _viewModel.LoadErrorMessage);
    }

    [Fact]
    public async Task ReloadAsync_Log_BecomesCardWithUtcTitleAndFlags()
    {
        var entry = CreateEntry();
        SetLogs(entry);

        await _viewModel.ReloadAsync();

        var item = Assert.Single(_viewModel.Items);
        Assert.Equal(entry.Id, item.Id);
        Assert.Equal("2026-03-10 08:15:30 UTC | System.InvalidOperationException", item.Title);
        Assert.Equal(entry.OccurredAtUtc, item.OccurredAtUtc);
        Assert.True(item.IsFatal);
        Assert.True(item.IsUiFailure);
    }

    [Fact]
    public async Task ReloadAsync_FullLog_ErrorTextContainsAllDetails()
    {
        var entry = CreateEntry();
        SetLogs(entry);

        await _viewModel.ReloadAsync();

        var item = Assert.Single(_viewModel.Items);
        var text = item.ErrorText;
        Assert.Contains("OccurredAtUtc: 2026-03-10 08:15:30 UTC", text);
        Assert.Contains("Source: Quelle", text);
        Assert.Contains("ExceptionType: System.InvalidOperationException", text);
        Assert.Contains("Message: Fehlertext", text);
        Assert.Contains("IsFatal: True", text);
        Assert.Contains("IsUiFailure: True", text);
        Assert.Contains("ThreadId: 4", text);
        Assert.Contains("Route: /route", text);
        Assert.Contains("Platform: Windows", text);
        Assert.Contains("OsVersion: 11", text);
        Assert.Contains("AppVersion: 1.0", text);
        Assert.Contains("InnerException:", text);
        Assert.Contains("StackTrace:", text);
        Assert.Contains("PayloadJson:", text);
    }

    [Fact]
    public async Task ReloadAsync_OptionalFieldsEmpty_ErrorTextOmitsThem()
    {
        var entry = CreateEntry();
        entry.Route = null;
        entry.Platform = "";
        entry.OsVersion = "  ";
        entry.AppVersion = null;
        entry.InnerException = null;
        entry.StackTrace = null;
        entry.PayloadJson = "";
        SetLogs(entry);

        await _viewModel.ReloadAsync();

        var item = Assert.Single(_viewModel.Items);
        var text = item.ErrorText;
        Assert.DoesNotContain("Route:", text);
        Assert.DoesNotContain("Platform:", text);
        Assert.DoesNotContain("OsVersion:", text);
        Assert.DoesNotContain("AppVersion:", text);
        Assert.DoesNotContain("InnerException:", text);
        Assert.DoesNotContain("StackTrace:", text);
        Assert.DoesNotContain("PayloadJson:", text);
    }

    [Fact]
    public async Task ReloadAsync_SeveralLogs_KeepsOrderOfService()
    {
        var first = CreateEntry();
        var second = CreateEntry();
        SetLogs(first, second);

        await _viewModel.ReloadAsync();

        Assert.Equal(2, _viewModel.Items.Count);
        Assert.Equal(first.Id, _viewModel.Items[0].Id);
        Assert.Equal(second.Id, _viewModel.Items[1].Id);
    }

    [Fact]
    public async Task ReloadAsync_LoadingFails_ShowsMessageEmptyListAndCapturesError()
    {
        var failure = new InvalidOperationException("Datenbank nicht erreichbar");
        _errorLogService.GetLatestAsync().ThrowsAsync(failure);

        await _viewModel.ReloadAsync();

        Assert.Empty(_viewModel.Items);
        Assert.Equal("ErrorsLoadFailed", _viewModel.LoadErrorMessage);
        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "ErrorPage.Reload" &&
                options.IsUiFailure &&
                options.Route == "/errors"));
    }

    [Fact]
    public async Task ReloadAsync_AfterFailedLoad_ClearsMessageAndShowsLogs()
    {
        var failure = new InvalidOperationException("Fehler");
        _errorLogService.GetLatestAsync().ThrowsAsync(failure);
        await _viewModel.ReloadAsync();
        var entry = CreateEntry();
        SetLogs(entry);

        await _viewModel.ReloadAsync();

        Assert.Equal(string.Empty, _viewModel.LoadErrorMessage);
        Assert.Single(_viewModel.Items);
    }

    private void SetLogs(params AppErrorLogEntity[] entries)
    {
        var logs = entries.ToList();
        _errorLogService.GetLatestAsync().Returns(logs);
    }

    private static AppErrorLogEntity CreateEntry()
    {
        var occurredAt = new DateTime(2026, 3, 10, 8, 15, 30, DateTimeKind.Utc);

        return new AppErrorLogEntity
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = occurredAt,
            Source = "Quelle",
            ExceptionType = "System.InvalidOperationException",
            Message = "Fehlertext",
            StackTrace = "Stack",
            InnerException = "Inner",
            Route = "/route",
            Platform = "Windows",
            OsVersion = "11",
            AppVersion = "1.0",
            IsFatal = true,
            IsUiFailure = true,
            ThreadId = 4,
            PayloadJson = "{}"
        };
    }
}
