using Bunit;
using CommunityToolkit.Mvvm.Input;
using FokusApp.Components.ForYouSuggestions;
using FokusApp.Components.Layout;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;

namespace FocusAppTests.Components;

public class NavMenuTests : ComponentTestContext
{
    private readonly INavMenuViewModel _viewModel = Substitute.For<INavMenuViewModel>();
    private readonly IRelayCommand _toggleScope = Substitute.For<IRelayCommand>();
    private readonly IRelayCommand _toggleMenu = Substitute.For<IRelayCommand>();
    private readonly IRelayCommand _expandMenu = Substitute.For<IRelayCommand>();
    private readonly IRelayCommand _collapseMenu = Substitute.For<IRelayCommand>();
    private readonly IRelayCommand<string> _navigateTo = Substitute.For<IRelayCommand<string>>();

    public NavMenuTests()
    {
        _viewModel.ToggleTaskScopeCommand.Returns(_toggleScope);
        _viewModel.ToggleNavigationMenuCommand.Returns(_toggleMenu);
        _viewModel.ExpandNavigationMenuCommand.Returns(_expandMenu);
        _viewModel.CollapseNavigationMenuCommand.Returns(_collapseMenu);
        _viewModel.NavigateToCommand.Returns(_navigateTo);
        _viewModel.CanSwitchScope.Returns(true);
        _viewModel.IsWorkScope.Returns(true);
        _viewModel.IsReviewAvailable.Returns(true);
        _viewModel.CurrentRoute.Returns("/");

        Services.AddSingleton(_viewModel);
        Services.AddSingleton(Substitute.For<IPlantHelperViewModel>());
        Services.AddSingleton(Substitute.For<ITaskEditPopUpViewModel>());
    }

    [Theory]
    [InlineData(true, "active", "inactive", "true")]
    [InlineData(false, "inactive", "active", "false")]
    public void Render_Scope_MarksTheActiveSideOfTheSwitch(bool isWorkScope, string workState, string privateState, string ariaChecked)
    {
        _viewModel.IsWorkScope.Returns(isWorkScope);

        var cut = Render<NavMenu>();

        var labels = cut.FindAll(".scope-label");
        var toggle = cut.Find(".scope-toggle");
        Assert.True(labels[0].ClassList.Contains(workState));
        Assert.True(labels[1].ClassList.Contains(privateState));
        Assert.Equal(Text("Work"), labels[0].TextContent);
        Assert.Equal(Text("Private"), labels[1].TextContent);
        Assert.Equal(ariaChecked, toggle.GetAttribute("aria-checked"));
    }

    [Theory]
    [InlineData(true, "is-work")]
    [InlineData(false, "is-private")]
    public void Render_Scope_PlacesTheSwitchThumb(bool isWorkScope, string expectedClass)
    {
        _viewModel.IsWorkScope.Returns(isWorkScope);

        var cut = Render<NavMenu>();

        var thumb = cut.Find(".scope-toggle-thumb");
        Assert.True(thumb.ClassList.Contains(expectedClass));
    }

    [Fact]
    public void ClickScopeSwitch_Always_TogglesTheTaskScope()
    {
        var cut = Render<NavMenu>();

        cut.Find(".scope-toggle").Click();

        _toggleScope.Received(1).Execute(null);
    }

    [Fact]
    public void ClickMenuButton_Always_TogglesTheNavigationMenu()
    {
        var cut = Render<NavMenu>();

        cut.Find(".navbar-toggler").Click();

        _toggleMenu.Received(1).Execute(null);
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void Render_Menu_ExposesTheExpandedStateToAssistiveTechnology(bool expanded, string ariaExpanded)
    {
        _viewModel.IsNavigationExpanded.Returns(expanded);

        var cut = Render<NavMenu>();

        var button = cut.Find(".navbar-toggler");
        Assert.Equal(ariaExpanded, button.GetAttribute("aria-expanded"));
        Assert.Equal(expanded, button.ClassList.Contains("expanded"));
        Assert.Equal(Text("NavigationMenu"), button.GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_MenuCollapsed_ShowsNoNavigationEntries()
    {
        var cut = Render<NavMenu>();

        Assert.Empty(cut.FindAll(".nav-fan"));
        Assert.Empty(cut.FindAll(".nav-backdrop"));
    }

    [Fact]
    public void Render_MenuExpanded_ShowsAllEntriesInOrder()
    {
        _viewModel.IsNavigationExpanded.Returns(true);

        var cut = Render<NavMenu>();

        var routes = cut.FindAll(".nav-fan-item").Select(item => item.GetAttribute("data-nav-route")).ToList();
        Assert.Equal(["/focus-timer", "/", "/tagesplaner", "/wochenrueckblick", "/einstellungen", "/aufgaben", "/errors", "/contact-developer", "/app-webseite"], routes);
    }

    [Fact]
    public void Render_ReviewsSwitchedOff_HidesTheWeeklyReviewEntry()
    {
        _viewModel.IsNavigationExpanded.Returns(true);
        _viewModel.IsReviewAvailable.Returns(false);

        var cut = Render<NavMenu>();

        var routes = cut.FindAll(".nav-fan-item").Select(item => item.GetAttribute("data-nav-route")).ToList();
        Assert.DoesNotContain("/wochenrueckblick", routes);
        Assert.Equal(8, routes.Count);
    }

    [Theory]
    [InlineData("/focus-timer", "/focus-timer")]
    [InlineData("/", "/")]
    [InlineData("/calender", "/")]
    [InlineData("/tagesplaner", "/tagesplaner")]
    [InlineData("/wochenrueckblick", "/wochenrueckblick")]
    [InlineData("/einstellungen", "/einstellungen")]
    [InlineData("/settings", "/einstellungen")]
    [InlineData("/aufgaben", "/aufgaben")]
    [InlineData("/errors", "/errors")]
    [InlineData("/fehler", "/errors")]
    [InlineData("/contact-developer", "/contact-developer")]
    [InlineData("/kontaktiere-entwickler", "/contact-developer")]
    [InlineData("/app-webseite", "/app-webseite")]
    [InlineData("/app-website", "/app-webseite")]
    public void Render_CurrentRoute_MarksExactlyTheMatchingEntry(string currentRoute, string expectedEntryRoute)
    {
        _viewModel.IsNavigationExpanded.Returns(true);
        _viewModel.CurrentRoute.Returns(currentRoute);

        var cut = Render<NavMenu>();

        var current = Assert.Single(cut.FindAll(".nav-fan-item.current"));
        Assert.Equal(expectedEntryRoute, current.GetAttribute("data-nav-route"));
        Assert.Equal("page", current.GetAttribute("aria-current"));
    }

    [Fact]
    public void Render_UnknownRoute_MarksNoEntry()
    {
        _viewModel.IsNavigationExpanded.Returns(true);
        _viewModel.CurrentRoute.Returns("/unbekannt");

        var cut = Render<NavMenu>();

        Assert.Empty(cut.FindAll(".nav-fan-item.current"));
        Assert.Empty(cut.FindAll("[aria-current]"));
    }

    [Fact]
    public void ClickEntry_Route_NavigatesThere()
    {
        _viewModel.IsNavigationExpanded.Returns(true);
        var cut = Render<NavMenu>();

        cut.Find("[data-nav-route=\"/aufgaben\"]").Click();

        _navigateTo.Received(1).Execute("/aufgaben");
    }

    [Fact]
    public void ClickBackdrop_MenuExpanded_CollapsesTheMenu()
    {
        _viewModel.IsNavigationExpanded.Returns(true);
        var cut = Render<NavMenu>();

        cut.Find(".nav-backdrop").Click();

        _collapseMenu.Received(1).Execute(null);
    }

    [Fact]
    public void BackPress_MenuExpanded_CollapsesMenuAndIsHandled()
    {
        _viewModel.IsNavigationExpanded.Returns(true);
        var handler = RenderNavMenuAndGetBackPressHandler();

        var handled = handler();

        Assert.True(handled);
        _collapseMenu.Received(1).Execute(null);
    }

    [Fact]
    public void BackPress_MenuCollapsed_IsNotHandled()
    {
        var handler = RenderNavMenuAndGetBackPressHandler();

        var handled = handler();

        Assert.False(handled);
    }

    private Func<bool> RenderNavMenuAndGetBackPressHandler()
    {
        var captured = new List<Func<bool>>();
        var backPress = Services.GetRequiredService<IUiBackPressService>();
        backPress.RegisterHandler(Arg.Do<Func<bool>>(captured.Add));

        Render<NavMenu>();

        return captured.Single(handler => handler.Target is NavMenu);
    }

    [Fact]
    public void PropertyChanged_ViewModel_RendersTheNewState()
    {
        var cut = Render<NavMenu>();
        _viewModel.IsWorkScope.Returns(false);

        RaiseViewModelChanged(nameof(INavMenuViewModel.IsWorkScope));

        cut.WaitForAssertion(() =>
        {
            var thumb = cut.Find(".scope-toggle-thumb");
            Assert.True(thumb.ClassList.Contains("is-private"));
        });
    }

    [Fact]
    public void OnHoldStarted_FromJavaScript_ExpandsTheMenu()
    {
        var cut = Render<NavMenu>();

        cut.Instance.OnHoldStarted();

        _expandMenu.Received(1).Execute(null);
    }

    [Theory]
    [InlineData("/aufgaben")]
    [InlineData(null)]
    public void OnHoldReleased_FromJavaScript_NavigatesToTheReleasedRoute(string? route)
    {
        var cut = Render<NavMenu>();

        cut.Instance.OnHoldReleased(route);

        _navigateTo.Received(1).Execute(route);
    }

    [Fact]
    public void Render_FirstRender_AttachesTheHoldGestureModuleToTheMenuButton()
    {
        var module = JSInterop.SetupModule("./js/navFan.js");
        module.SetupModule(invocation => invocation.Identifier == "attach");

        Render<NavMenu>();

        var invocation = module.VerifyInvoke("attach");
        Assert.Equal(2, invocation.Arguments.Count);
    }

    [Fact]
    public async Task Dispose_AfterAttaching_DisposesTheAttachmentThroughTheModule()
    {
        var module = JSInterop.SetupModule("./js/navFan.js");
        var attachment = module.SetupModule(invocation => invocation.Identifier == "attach");
        attachment.SetupVoid("dispose");
        Render<NavMenu>();

        await DisposeComponentsAsync();

        attachment.VerifyInvoke("dispose");
    }

    [Fact]
    public async Task Dispose_AfterDispose_IgnoresFurtherViewModelChanges()
    {
        Render<NavMenu>();

        await DisposeComponentsAsync();

        var exception = Record.Exception(() => RaiseViewModelChanged(nameof(INavMenuViewModel.IsWorkScope)));
        Assert.Null(exception);
    }

    [Fact]
    public async Task Dispose_ConnectionAlreadyClosed_DoesNotThrow()
    {
        var failure = new JSDisconnectedException("getrennt");
        var module = JSInterop.SetupModule("./js/navFan.js");
        var attachment = module.SetupModule(invocation => invocation.Identifier == "attach");
        attachment.SetupVoid("dispose").SetException(failure);
        Render<NavMenu>();

        var exception = await Record.ExceptionAsync(() => DisposeComponentsAsync());

        Assert.Null(exception);
    }
    private void RaiseViewModelChanged(string propertyName)
    {
        var args = new System.ComponentModel.PropertyChangedEventArgs(propertyName);
        _viewModel.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(_viewModel, args);
    }

    [Fact]
    public void Render_ScopeFixedByDefaultScope_ShowsOnlyTheActiveLabelWithoutSwitch()
    {
        _viewModel.CanSwitchScope.Returns(false);
        _viewModel.IsWorkScope.Returns(false);

        var cut = Render<NavMenu>();

        var label = Assert.Single(cut.FindAll(".scope-label"));
        Assert.Equal(Text("Private"), label.TextContent);
        Assert.True(label.ClassList.Contains("active"));
        Assert.Empty(cut.FindAll(".scope-toggle"));
    }
}
