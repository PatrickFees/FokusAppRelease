using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Core.Enums;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Components.WhatsNew;

public sealed partial class WhatsNewViewModel : ObservableObject, IWhatsNewViewModel
{
    private readonly IChangelogSettingsService _changelogSettingsService;
    private readonly IOnboardingSettingsService _onboardingSettingsService;
    private readonly IAppEnvironmentInfo _environmentInfo;
    private readonly IAppErrorCaptureService _appErrorCaptureService;

    [ObservableProperty]
    private bool isOpen;

    public WhatsNewViewModel(
        IChangelogSettingsService changelogSettingsService,
        IOnboardingSettingsService onboardingSettingsService,
        IAppEnvironmentInfo environmentInfo,
        IAppErrorCaptureService appErrorCaptureService)
    {
        _changelogSettingsService = changelogSettingsService;
        _onboardingSettingsService = onboardingSettingsService;
        _environmentInfo = environmentInfo;
        _appErrorCaptureService = appErrorCaptureService;
    }

    public async Task InitializeAsync()
    {

        if (_onboardingSettingsService.OnboardingState != OnboardingState.Completed)
        {
            return;
        }

        var currentVersion = _environmentInfo.AppVersionNumber;
        if (string.IsNullOrWhiteSpace(currentVersion) || currentVersion == _changelogSettingsService.LastSeenChangelogVersion)
        {
            return;
        }

        try
        {
            await _changelogSettingsService.SetLastSeenChangelogVersionAsync(currentVersion);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(WhatsNewViewModel)}.{nameof(InitializeAsync)}");
            return;
        }

        IsOpen = true;
    }

    public void Dismiss()
    {
        IsOpen = false;
    }
}
