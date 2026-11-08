using FokusApp.Components.WhatsNew;
using FokusApp.Core.Enums;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FocusAppTests.ViewModels;

public class WhatsNewViewModelTests
{
    private readonly IChangelogSettingsService _changelogSettings = Substitute.For<IChangelogSettingsService>();
    private readonly IOnboardingSettingsService _onboardingSettings = Substitute.For<IOnboardingSettingsService>();
    private readonly IAppEnvironmentInfo _environmentInfo = Substitute.For<IAppEnvironmentInfo>();
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly WhatsNewViewModel _viewModel;

    public WhatsNewViewModelTests()
    {
        _onboardingSettings.OnboardingState.Returns(OnboardingState.Completed);
        _environmentInfo.AppVersionNumber.Returns("1.5.0");
        _viewModel = new WhatsNewViewModel(_changelogSettings, _onboardingSettings, _environmentInfo, _errorCapture);
    }

    [Fact]
    public async Task InitializeAsync_NewVersionAfterOnboardingIsCompleted_OpensAndStoresTheVersionAtOnce()
    {
        _changelogSettings.LastSeenChangelogVersion.Returns("1.4.0");

        await _viewModel.InitializeAsync();

        Assert.True(_viewModel.IsOpen);
        await _changelogSettings.Received(1).SetLastSeenChangelogVersionAsync("1.5.0");
    }

    [Fact]
    public async Task InitializeAsync_VersionAlreadySeen_StaysClosed()
    {
        _changelogSettings.LastSeenChangelogVersion.Returns("1.5.0");

        await _viewModel.InitializeAsync();

        Assert.False(_viewModel.IsOpen);
    }

    [Fact]
    public async Task InitializeAsync_OnboardingNotCompletedYet_StaysClosed()
    {
        _onboardingSettings.OnboardingState.Returns(OnboardingState.NotSeen);
        _changelogSettings.LastSeenChangelogVersion.Returns((string?)null);

        await _viewModel.InitializeAsync();

        Assert.False(_viewModel.IsOpen);
    }

    [Fact]
    public async Task InitializeAsync_InvitePending_StaysClosed()
    {
        _onboardingSettings.OnboardingState.Returns(OnboardingState.InvitePending);
        _changelogSettings.LastSeenChangelogVersion.Returns((string?)null);

        await _viewModel.InitializeAsync();

        Assert.False(_viewModel.IsOpen);
    }

    [Fact]
    public async Task InitializeAsync_StoringTheVersionFails_CapturesTheErrorAndStaysClosed()
    {
        _changelogSettings.LastSeenChangelogVersion.Returns("1.4.0");
        var failure = new InvalidOperationException("Datenbank gesperrt");
        _changelogSettings.SetLastSeenChangelogVersionAsync(Arg.Any<string>()).ThrowsAsync(failure);

        await _viewModel.InitializeAsync();

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null && options.Source == "WhatsNewViewModel.InitializeAsync" && options.IsUiFailure));
        Assert.False(_viewModel.IsOpen);
    }

    [Fact]
    public async Task InitializeAsync_OnANewStartAfterTheVersionWasAlreadyStored_StaysClosed()
    {
        _changelogSettings.LastSeenChangelogVersion.Returns("1.4.0");
        await _viewModel.InitializeAsync();
        Assert.True(_viewModel.IsOpen);

        _changelogSettings.LastSeenChangelogVersion.Returns("1.5.0");
        var secondStart = new WhatsNewViewModel(_changelogSettings, _onboardingSettings, _environmentInfo, _errorCapture);

        await secondStart.InitializeAsync();

        Assert.False(secondStart.IsOpen);
    }

    [Fact]
    public void Dismiss_OpenNotice_Closes()
    {
        _viewModel.IsOpen = true;

        _viewModel.Dismiss();

        Assert.False(_viewModel.IsOpen);
    }
}
