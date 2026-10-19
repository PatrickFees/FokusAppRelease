using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Core.DataBase.ErrorLogging.Entities;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace FokusApp.Components.ErrorComponent; 
public sealed record ErrorPageItem(
    Guid Id,
    string Title,
    DateTime OccurredAtUtc,
    bool IsFatal,
    bool IsUiFailure,
    string ErrorText);


public partial class ErrorPageViewModel : ObservableObject, IErrorPageViewModel
{
    private readonly IAppErrorLogService _appErrorLogService;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly IUiTextProvider _textProvider;

    public ObservableCollection<ErrorPageItem> Items { get; private set; } = [];

    [ObservableProperty]
    private string loadErrorMessage = string.Empty;


    public ErrorPageViewModel(
        IAppErrorLogService appErrorLogService,
        IAppErrorCaptureService appErrorCaptureService,
        IUiTextProvider textProvider)
    {
        _appErrorLogService = appErrorLogService;
        _appErrorCaptureService = appErrorCaptureService;
        _textProvider = textProvider;
    }

    public async Task InitializeAsync()
    {
        await ReloadAsync();
    }

    public async Task ReloadAsync()
    {
        try
        {
            LoadErrorMessage = string.Empty;
            var errorLogs = await _appErrorLogService.GetLatestAsync();
            var errorPageItems = CreateListOfErrorPageItems(errorLogs);
            Items = new ObservableCollection<ErrorPageItem>(errorPageItems);
        }
        catch (Exception ex)
        {
            Items = [];
            LoadErrorMessage = _textProvider.GetText("ErrorsLoadFailed");

            await _appErrorCaptureService.CaptureAsync(
                ex,
                new AppErrorCaptureOptions
                {
                    Source = "ErrorPage.Reload",
                    IsUiFailure = true,
                    Route = "/errors"
                }).ConfigureAwait(false);
        }
    }

    private List<ErrorPageItem> CreateListOfErrorPageItems(List<AppErrorLogEntity> entities)
    {
        var errorPageItems = new List<ErrorPageItem>();

        foreach (var entity in entities)
        {
            var pageItem = CreateListItem(entity);
            errorPageItems.Add(pageItem);
        }

        return errorPageItems;
    }

    private ErrorPageItem CreateListItem(AppErrorLogEntity entry)
    {
        var utcText = entry.OccurredAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
        var title = $"{utcText} | {entry.ExceptionType}";

        return new ErrorPageItem(
            entry.Id,
            title,
            entry.OccurredAtUtc,
            entry.IsFatal,
            entry.IsUiFailure,
            BuildErrorText(entry));
    }

    private static string BuildErrorText(AppErrorLogEntity entry)
    {
        var lines = new StringBuilder();
        lines.AppendLine($"OccurredAtUtc: {entry.OccurredAtUtc:yyyy-MM-dd HH:mm:ss 'UTC'}");
        lines.AppendLine($"Source: {entry.Source}");
        lines.AppendLine($"ExceptionType: {entry.ExceptionType}");
        lines.AppendLine($"Message: {entry.Message}");
        lines.AppendLine($"IsFatal: {entry.IsFatal}");
        lines.AppendLine($"IsUiFailure: {entry.IsUiFailure}");
        lines.AppendLine($"ThreadId: {entry.ThreadId}");

        if (!string.IsNullOrWhiteSpace(entry.Route))
        {
            lines.AppendLine($"Route: {entry.Route}");
        }

        if (!string.IsNullOrWhiteSpace(entry.Platform))
        {
            lines.AppendLine($"Platform: {entry.Platform}");
        }

        if (!string.IsNullOrWhiteSpace(entry.OsVersion))
        {
            lines.AppendLine($"OsVersion: {entry.OsVersion}");
        }

        if (!string.IsNullOrWhiteSpace(entry.AppVersion))
        {
            lines.AppendLine($"AppVersion: {entry.AppVersion}");
        }

        if (!string.IsNullOrWhiteSpace(entry.InnerException))
        {
            lines.AppendLine("InnerException:");
            lines.AppendLine(entry.InnerException);
        }

        if (!string.IsNullOrWhiteSpace(entry.StackTrace))
        {
            lines.AppendLine("StackTrace:");
            lines.AppendLine(entry.StackTrace);
        }

        if (!string.IsNullOrWhiteSpace(entry.PayloadJson))
        {
            lines.AppendLine("PayloadJson:");
            lines.AppendLine(entry.PayloadJson);
        }

        return lines.ToString();
    }

}
