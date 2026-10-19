using FocusAppTests.Fakes;
using FokusApp.Components.Layout;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.ViewModels;

public class NavMenuViewModelTests
{
    private readonly IScopeService _scopeService = Substitute.For<IScopeService>();

    [Theory]
    [InlineData("http://localhost/", "/")]
    [InlineData("http://localhost/focus-timer", "/focus-timer")]
    [InlineData("http://localhost/aufgaben", "/aufgaben")]
    public void Constructor_Route_SetsCurrentRoute(string uri, string expectedRoute)
    {
        var navigation = new FakeNavigationManager(uri);

        var viewModel = new NavMenuViewModel(_scopeService, navigation);

        Assert.Equal(expectedRoute, viewModel.CurrentRoute);
    }

    [Theory]
    [InlineData("http://localhost/Aufgaben/")]
    [InlineData("http://localhost/AUFGABEN")]
    [InlineData("http://localhost/aufgaben?day=2026-03-10")]
    public void Constructor_RouteWithCaseTrailingSlashOrQuery_IsNormalized(string uri)
    {
        var navigation = new FakeNavigationManager(uri);

        var viewModel = new NavMenuViewModel(_scopeService, navigation);

        Assert.Equal("/aufgaben", viewModel.CurrentRoute);
    }

    [Fact]
    public void LocationChanged_OtherRoute_UpdatesRouteStateAndRaisesChanges()
    {
        var navigation = new FakeNavigationManager("http://localhost/focus-timer");
        var viewModel = new NavMenuViewModel(_scopeService, navigation);
        var changed = TrackChanges(viewModel);

        navigation.SimulateNavigation("http://localhost/calender");

        Assert.Equal("/calender", viewModel.CurrentRoute);
        Assert.Contains(nameof(NavMenuViewModel.CurrentRoute), changed);
    }

    [Fact]
    public void LocationChanged_SameRoute_RaisesNoChange()
    {
        var navigation = new FakeNavigationManager("http://localhost/focus-timer");
        var viewModel = new NavMenuViewModel(_scopeService, navigation);
        var changed = TrackChanges(viewModel);

        navigation.SimulateNavigation("http://localhost/focus-timer/");

        Assert.Empty(changed);
    }

    [Fact]
    public void NavigateToCommand_Route_NavigatesAndCollapsesMenu()
    {
        var navigation = new FakeNavigationManager("http://localhost/focus-timer");
        var viewModel = new NavMenuViewModel(_scopeService, navigation);
        viewModel.ToggleNavigationMenuCommand.Execute(null);

        viewModel.NavigateToCommand.Execute("/calender");

        var target = Assert.Single(navigation.NavigatedTo);
        Assert.Equal("/calender", target);
        Assert.False(viewModel.IsNavigationExpanded);
        Assert.Equal("/calender", viewModel.CurrentRoute);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NavigateToCommand_NoRoute_OnlyCollapsesMenu(string? route)
    {
        var navigation = new FakeNavigationManager("http://localhost/");
        var viewModel = new NavMenuViewModel(_scopeService, navigation);
        viewModel.ToggleNavigationMenuCommand.Execute(null);

        viewModel.NavigateToCommand.Execute(route);

        Assert.Empty(navigation.NavigatedTo);
        Assert.False(viewModel.IsNavigationExpanded);
    }

    [Fact]
    public void ExpandNavigationMenuCommand_Twice_KeepsMenuExpanded()
    {
        var navigation = new FakeNavigationManager("http://localhost/");
        var viewModel = new NavMenuViewModel(_scopeService, navigation);

        viewModel.ExpandNavigationMenuCommand.Execute(null);
        viewModel.ExpandNavigationMenuCommand.Execute(null);

        Assert.True(viewModel.IsNavigationExpanded);
    }

    [Fact]
    public void ToggleNavigationMenuCommand_Twice_ExpandsAndCollapses()
    {
        var navigation = new FakeNavigationManager("http://localhost/");
        var viewModel = new NavMenuViewModel(_scopeService, navigation);

        viewModel.ToggleNavigationMenuCommand.Execute(null);
        Assert.True(viewModel.IsNavigationExpanded);
        viewModel.ToggleNavigationMenuCommand.Execute(null);

        Assert.False(viewModel.IsNavigationExpanded);
    }

    [Fact]
    public void CollapseNavigationMenuCommand_ExpandedMenu_CollapsesIt()
    {
        var navigation = new FakeNavigationManager("http://localhost/");
        var viewModel = new NavMenuViewModel(_scopeService, navigation);
        viewModel.ToggleNavigationMenuCommand.Execute(null);

        viewModel.CollapseNavigationMenuCommand.Execute(null);

        Assert.False(viewModel.IsNavigationExpanded);
    }

    [Fact]
    public void ToggleTaskScopeCommand_Executed_TogglesScopeInService()
    {
        var navigation = new FakeNavigationManager("http://localhost/");
        var viewModel = new NavMenuViewModel(_scopeService, navigation);

        viewModel.ToggleTaskScopeCommand.Execute(null);

        _scopeService.Received(1).ToggleScope();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ScopeChanged_Event_RaisesIsWorkScopeWithServiceValue(bool isWorkScope)
    {
        var navigation = new FakeNavigationManager("http://localhost/");
        var viewModel = new NavMenuViewModel(_scopeService, navigation);
        var changed = TrackChanges(viewModel);
        _scopeService.IsWorkScope.Returns(isWorkScope);

        _scopeService.ScopeChanged += Raise.Event();

        Assert.Contains(nameof(NavMenuViewModel.IsWorkScope), changed);
        Assert.Equal(isWorkScope, viewModel.IsWorkScope);
    }

    [Fact]
    public void Dispose_AfterwardsEvents_AreIgnored()
    {
        var navigation = new FakeNavigationManager("http://localhost/focus-timer");
        var viewModel = new NavMenuViewModel(_scopeService, navigation);
        var changed = TrackChanges(viewModel);
        viewModel.Dispose();

        navigation.SimulateNavigation("http://localhost/calender");
        _scopeService.ScopeChanged += Raise.Event();

        Assert.Empty(changed);
        Assert.Equal("/focus-timer", viewModel.CurrentRoute);
    }

    private static List<string?> TrackChanges(NavMenuViewModel viewModel)
    {
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        return changed;
    }
}
