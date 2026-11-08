using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Components.UpdateNotice;

public sealed partial class UpdateNoticeViewModel : ObservableObject, IUpdateNoticeViewModel
{
    private readonly IUpdateCheckService _updateCheckService;
    private readonly IUpdateSettingsService _updateSettingsService;
    private readonly IExternalLinkOpener _externalLinkOpener;
    private readonly IAppErrorCaptureService _appErrorCaptureService;

    private string? _downloadUrl;

    [ObservableProperty]
    private bool isOpen;

    [ObservableProperty]
    private string? latestVersion;

    public UpdateNoticeViewModel(
        IUpdateCheckService updateCheckService,
        IUpdateSettingsService updateSettingsService,
        IExternalLinkOpener externalLinkOpener,
        IAppErrorCaptureService appErrorCaptureService)
    {
        _updateCheckService = updateCheckService;
        _updateSettingsService = updateSettingsService;
        _externalLinkOpener = externalLinkOpener;
        _appErrorCaptureService = appErrorCaptureService;
    }

    public async Task InitializeAsync()
    {
        var update = await _updateCheckService.GetAvailableUpdateAsync();
        if (update is null || update.Version == _updateSettingsService.DismissedUpdateVersion)
        {
            return;
        }

        _downloadUrl = update.DownloadUrl;
        LatestVersion = update.Version;
        IsOpen = true;
    }

    public async Task DismissAsync()
    {
        if (LatestVersion is not null)
        {
            await _updateSettingsService.SetDismissedUpdateVersionAsync(LatestVersion);
        }

        IsOpen = false;
    }

    public async Task DownloadAsync()
    {
        if (_downloadUrl is null)
        {
            return;
        }

        try
        {
            await _externalLinkOpener.OpenAsync(_downloadUrl);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureAsync(
                ex,
                new AppErrorCaptureOptions { Source = "UpdateNoticeViewModel.DownloadAsync" }).ConfigureAwait(false);
        }
    }
}
