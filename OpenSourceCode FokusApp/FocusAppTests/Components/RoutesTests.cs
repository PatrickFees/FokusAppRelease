using Bunit;
using FocusAppTests.MauiShims;
using CommunityToolkit.Mvvm.Input;
using FokusApp.Components;
using FokusApp.Components.ErrorComponent;
using FokusApp.Components.ForYouSuggestions;
using FokusApp.Components.Layout;
using FokusApp.Components.Onboarding;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Components.SharedUI;
using FokusApp.Components.UpdateNotice;
using FokusApp.Components.WhatsNew;
using FokusApp.Core.Enums;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;

namespace FocusAppTests.Components;

[Collection(SerialMauiShimsCollection.Name)]
public class RoutesTests : ComponentTestContext
{
    private readonly IAppLocalizationService _localization = Substitute.For<IAppLocalizationService>();
    private readonly IAppStartupStateService _startupState = Substitute.For<IAppStartupStateService>();
    private readonly IAppThemeService _themeService = Substitute.For<IAppThemeService>();
    private readonly IOnboardingViewModel _onboarding = Substitute.For<IOnboardingViewModel>();
    private readonly IUpdateNoticeViewModel _updateNotice = Substitute.For<IUpdateNoticeViewModel>();
    private readonly IWhatsNewViewModel _whatsNew = Substitute.For<IWhatsNewViewModel>();
    private readonly IErrorPageViewModel _errorPage = Substitute.For<IErrorPageViewModel>();
    private readonly IDeviceInfo _deviceInfo = Substitute.For<IDeviceInfo>();

    public RoutesTests()
    {
        NotificationNavigationBridge.TryConsumePendingRoute(out _);
        _startupState.Status.Returns(AppStartupStatus.Ready);
        _themeService.SelectedTheme.Returns(AppColorTheme.Green);
        _themeService.SelectedMode.Returns(AppColorMode.Dark);
        _errorPage.Items.Returns([]);
        _errorPage.LoadErrorMessage.Returns(string.Empty);
        _deviceInfo.Platform.Returns(DevicePlatform.WinUI);
        DeviceInfo.Current = _deviceInfo;

        var navMenu = Substitute.For<INavMenuViewModel>();
        navMenu.ToggleTaskScopeCommand.Returns(Substitute.For<IRelayCommand>());
        navMenu.ToggleNavigationMenuCommand.Returns(Substitute.For<IRelayCommand>());
        navMenu.CurrentRoute.Returns("/errors");

        Services.AddSingleton(_localization);
        Services.AddSingleton(_startupState);
        Services.AddSingleton(_themeService);
        Services.AddSingleton(_onboarding);
        Services.AddSingleton(_updateNotice);
        Services.AddSingleton(_whatsNew);
        Services.AddSingleton(_errorPage);
        Services.AddSingleton(navMenu);
        Services.AddSingleton(Substitute.For<IPlantHelperViewModel>());
        Services.AddSingleton(Substitute.For<ITaskEditPopUpViewModel>());
        Services.AddSingleton(Substitute.For<IConfettiViewModel>());
        Services.AddSingleton(Substitute.For<IAppErrorCaptureService>());
        Services.AddSingleton(Substitute.For<IExternalLinkOpener>());
        Services.GetRequiredService<NavigationManager>().NavigateTo("/errors");
    }

    [Fact]
    public void Render_StartupLoading_ShowsTheLoadingScreenOnly()
    {
        _startupState.Status.Returns(AppStartupStatus.Loading);

        var cut = Render<Routes>();

        var screen = cut.Find("main.startup-state");
        Assert.Equal("status", screen.GetAttribute("role"));
        Assert.Equal(Text("StartupLoadingTitle"), screen.QuerySelector("h1")!.TextContent);
        Assert.Equal(Text("StartupLoadingDescription"), screen.QuerySelector("p")!.TextContent);
        Assert.Empty(cut.FindAll(".errors-page"));
    }

    [Fact]
    public void Render_StartupFailedWithReason_ShowsTheErrorAndTechnicalDetails()
    {
        _startupState.Status.Returns(AppStartupStatus.Failed);
        _startupState.Failure.Returns(new InvalidOperationException("Datenbank defekt"));

        var cut = Render<Routes>();

        var screen = cut.Find("main.startup-state-failed");
        Assert.Equal("alert", screen.GetAttribute("role"));
        Assert.Equal(Text("StartupFailedTitle"), screen.QuerySelector("h1")!.TextContent);
        Assert.Equal(Text("StartupFailedDescription"), screen.QuerySelector("p")!.TextContent);
        Assert.Equal(Text("TechnicalDetails"), screen.QuerySelector("summary")!.TextContent);
        Assert.Equal("Datenbank defekt", cut.Find(".startup-failure-message").TextContent);
    }

    [Fact]
    public void Render_StartupFailedWithoutReason_ShowsNoTechnicalDetails()
    {
        _startupState.Status.Returns(AppStartupStatus.Failed);
        _startupState.Failure.Returns((Exception?)null);

        var cut = Render<Routes>();

        Assert.NotEmpty(cut.FindAll("main.startup-state-failed"));
        Assert.Empty(cut.FindAll("details"));
    }

    [Fact]
    public void Render_StartupReady_ShowsThePageOfTheCurrentRouteInsideTheLayout()
    {
        var cut = Render<Routes>();

        Assert.NotEmpty(cut.FindAll(".errors-page"));
        Assert.NotEmpty(cut.FindAll(".page .nav-root"));
        _onboarding.Received().InitializeAsync();
    }

    [Fact]
    public void StateChanged_LoadingFinished_ShowsThePage()
    {
        _startupState.Status.Returns(AppStartupStatus.Loading);
        var cut = Render<Routes>();
        Assert.Empty(cut.FindAll(".errors-page"));
        _startupState.Status.Returns(AppStartupStatus.Ready);

        _startupState.StateChanged += Raise.Event();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".errors-page")));
    }

    [Fact]
    public void LanguageChanged_WhileShowingThePage_KeepsThePageRendered()
    {
        var cut = Render<Routes>();

        _localization.LanguageChanged += Raise.Event();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".errors-page")));
    }

    [Fact]
    public void Render_FirstRender_AppliesTheSelectedThemeToTheWebView()
    {
        var invocation = JSInterop.SetupVoid("fokusTheme.apply", _ => true);

        Render<Routes>();

        invocation.VerifyInvoke("fokusTheme.apply");
        var arguments = Assert.Single(invocation.Invocations).Arguments;
        Assert.Equal("green", arguments[0]);
        Assert.Equal("dark", arguments[1]);
    }

    [Fact]
    public void ThemeChanged_NewTheme_AppliesItToTheWebView()
    {
        var invocation = JSInterop.SetupVoid("fokusTheme.apply", _ => true);
        Render<Routes>();
        _themeService.SelectedTheme.Returns(AppColorTheme.Pink);

        _themeService.ThemeChanged += Raise.Event();

        invocation.VerifyInvoke("fokusTheme.apply", 2);
        Assert.Equal("pink", invocation.Invocations.Last().Arguments[0]);
        Assert.Equal("dark", invocation.Invocations.Last().Arguments[1]);
    }

    [Fact]
    public void Render_WebViewNotReadyForTheTheme_DoesNotFail()
    {
        var invocation = JSInterop.SetupVoid("fokusTheme.apply", _ => true);
        invocation.SetException(new JSException("noch nicht bereit"));

        var exception = Record.Exception(() => Render<Routes>());

        Assert.Null(exception);
    }

    [Fact]
    public void Render_PendingNotificationRouteForOtherPage_NavigatesThere()
    {
        NotificationNavigationBridge.RequestRoute("/fehler");
        var navigation = Services.GetRequiredService<NavigationManager>();

        Render<Routes>();

        Assert.EndsWith("/fehler", navigation.Uri);
        Assert.False(NotificationNavigationBridge.TryConsumePendingRoute(out _));
    }

    [Fact]
    public void Render_PendingNotificationRouteForCurrentPage_StaysThere()
    {
        NotificationNavigationBridge.RequestRoute("/errors");
        var navigation = Services.GetRequiredService<NavigationManager>();

        Render<Routes>();

        Assert.EndsWith("/errors", navigation.Uri);
    }

    [Fact]
    public void RouteRequested_WhileRunning_NavigatesToTheRoute()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        Render<Routes>();

        NotificationNavigationBridge.RequestRoute("/fehler");

        Assert.EndsWith("/fehler", navigation.Uri);
    }

    [Fact]
    public void RouteRequested_RouteOfTheCurrentPage_DoesNotNavigateAgain()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        Render<Routes>();
        var locationChanges = 0;
        navigation.LocationChanged += (_, _) => locationChanges++;

        NotificationNavigationBridge.RequestRoute("/errors");

        Assert.Equal(0, locationChanges);
    }

    [Fact]
    public void BackPress_NothingElseHandledIt_NavigatesBackInHistory()
    {
        var invocation = JSInterop.SetupVoid("history.back", _ => true);
        var captured = new List<Func<bool>>();
        var backPress = Services.GetRequiredService<IUiBackPressService>();
        backPress.RegisterHandler(Arg.Do<Func<bool>>(captured.Add));
        var cut = Render<Routes>();
        var handler = captured.Single(h => h.Target is Routes);

        var handled = handler();

        Assert.True(handled);
        cut.WaitForAssertion(() => invocation.VerifyInvoke("history.back"));
    }

    [Fact]
    public async Task Dispose_AfterDispose_IgnoresEventsOfTheServices()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        Render<Routes>();

        await DisposeComponentsAsync();

        var exception = Record.Exception(() =>
        {
            NotificationNavigationBridge.RequestRoute("/fehler");
            _localization.LanguageChanged += Raise.Event();
            _startupState.StateChanged += Raise.Event();
            _themeService.ThemeChanged += Raise.Event();
        });
        Assert.Null(exception);
        Assert.EndsWith("/errors", navigation.Uri);
    }

    [Fact]
    public void RouteRequested_OnStartPageForTheStartPage_DoesNotNavigate()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/");
        _startupState.Status.Returns(AppStartupStatus.Loading);
        Render<Routes>();
        var locationChanges = 0;
        navigation.LocationChanged += (_, _) => locationChanges++;

        NotificationNavigationBridge.RequestRoute("/");

        Assert.Equal(0, locationChanges);
    }
}
