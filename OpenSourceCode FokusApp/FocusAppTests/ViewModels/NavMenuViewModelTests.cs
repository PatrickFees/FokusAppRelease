using FocusAppTests.Fakes;
using FokusApp.Components.Layout;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;
using NSubstitute;

namespace FocusAppTests.ViewModels;

public class NavMenuViewModelTests
{
    private readonly IScopeService _scopeService = Substitute.For<IScopeService>();
    private readonly IRewardingSettingsService _userSettings = TestSettings.Create();
    private readonly IDefaultScopeSettingsService _defaultScopeSettings = Substitute.For<IDefaultScopeSettingsService>();

    [Fact]
    public void IsReviewAvailable_ReviewOn_IsTrue()
    {
        var viewModel = new NavMenuViewModel(_scopeService, new FakeNavigationManager("http://localhost/"), _userSettings, _defaultScopeSettings);

        Assert.True(viewModel.IsReviewAvailable);
    }

    [Fact]
    public void IsReviewAvailable_ReviewOff_IsFalse()
    {
        _userSettings.Rewarding.Returns(AppRewardingSettings.Default with { ReviewsEnabled = false });
        var viewModel = new NavMenuViewModel(_scopeService, new FakeNavigationManager("http://localhost/"), _userSettings, _defaultScopeSettings);

        Assert.False(viewModel.IsReviewAvailable);
    }

    [Fact]
    public void RewardingChanged_ReviewSwitchedOff_NotifiesAndHidesTheEntry()
    {
        var viewModel = new NavMenuViewModel(_scopeService, new FakeNavigationManager("http://localhost/"), _userSettings, _defaultScopeSettings);
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        _userSettings.Rewarding.Returns(AppRewardingSettings.Default with { ReviewsEnabled = false });

        _userSettings.RewardingChanged += Raise.EventWith(EventArgs.Empty);

        Assert.Contains(nameof(INavMenuViewModel.IsReviewAvailable), changed);
        Assert.False(viewModel.IsReviewAvailable);
    }

    [Fact]
    public void Dispose_AfterDispose_IgnoresSettingChanges()
    {
        var viewModel = new NavMenuViewModel(_scopeService, new FakeNavigationManager("http://localhost/"), _userSettings, _defaultScopeSettings);
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        viewModel.Dispose();

        _userSettings.RewardingChanged += Raise.EventWith(EventArgs.Empty);

        Assert.DoesNotContain(nameof(INavMenuViewModel.IsReviewAvailable), changed);
    }

    [Theory]
    [InlineData("http://localhost/", "/")]
    [InlineData("http://localhost/focus-timer", "/focus-timer")]
    [InlineData("http://localhost/aufgaben", "/aufgaben")]
    public void Constructor_Route_SetsCurrentRoute(string uri, string expectedRoute)
    {
        var navigation = new FakeNavigationManager(uri);

        var viewModel = new NavMenuViewModel(_scopeService, navigation, _userSettings, _defaultScopeSettings);

        Assert.Equal(expectedRoute, viewModel.CurrentRoute);
    }

    [Theory]
    [InlineData("http://localhost/Aufgaben/")]
    [InlineData("http://localhost/AUFGABEN")]
    [InlineData("http://localhost/aufgaben?day=2026-03-10")]
    public void Constructor_RouteWithCaseTrailingSlashOrQuery_IsNormalized(string uri)
    {
        var navigation = new FakeNavigationManager(uri);

        var viewModel = new NavMenuViewModel(_scopeService, navigation, _userSettings, _defaultScopeSettings);

        Assert.Equal("/aufgaben", viewModel.CurrentRoute);
    }

    [Fact]
    public void LocationChanged_OtherRoute_UpdatesRouteStateAndRaisesChanges()
    {
        var navigation = new FakeNavigationManager("http://localhost/focus-timer");
        var viewModel = new NavMenuViewModel(_scopeService, navigation, _userSettings, _defaultScopeSettings);
        var changed = TrackChanges(viewModel);

        navigation.SimulateNavigation("http://localhost/calender");

        Assert.Equal("/calender", viewModel.CurrentRoute);
        Assert.Contains(nameof(NavMenuViewModel.CurrentRoute), changed);
    }

    [Fact]
    public void LocationChanged_SameRoute_RaisesNoChange()
    {
        var navigation = new FakeNavigationManager("http://localhost/focus-timer");
        var viewModel = new NavMenuViewModel(_scopeService, navigation, _userSettings, _defaultScopeSettings);
        var changed = TrackChanges(viewModel);

        navigation.SimulateNavigation("http://localhost/focus-timer/");

        Assert.Empty(changed);
    }

    [Fact]
    public void NavigateToCommand_Route_NavigatesAndCollapsesMenu()
    {
        var navigation = new FakeNavigationManager("http://localhost/focus-timer");
        var viewModel = new NavMenuViewModel(_scopeService, navigation, _userSettings, _defaultScopeSettings);
        viewModel.ToggleNavigationMenuCommand.Execute(null);

        viewModel.NavigateToCommand.Execute("/calender");

        var target = Assert.Single(navigation.NavigatedTo);
        Assert.Equal("/calender", target);
        Assert.False(viewModel.IsNavigationExpanded);
        Assert.Equal("/calender", viewModel.CurrentRoute);
    }

    [Fact]
    public void NavigateToCommand_Route_NavigatesBeforeCollapsingTheMenu()
    {
        var navigation = new FakeNavigationManager("http://localhost/focus-timer");
        var viewModel = new NavMenuViewModel(_scopeService, navigation, _userSettings, _defaultScopeSettings);
        viewModel.ToggleNavigationMenuCommand.Execute(null);
        var order = TrackChanges(viewModel);

        viewModel.NavigateToCommand.Execute("/calender");

        Assert.Equal([nameof(NavMenuViewModel.CurrentRoute), nameof(NavMenuViewModel.IsNavigationExpanded)], order);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NavigateToCommand_NoRoute_OnlyCollapsesMenu(string? route)
    {
        var navigation = new FakeNavigationManager("http://localhost/");
        var viewModel = new NavMenuViewModel(_scopeService, navigation, _userSettings, _defaultScopeSettings);
        viewModel.ToggleNavigationMenuCommand.Execute(null);

        viewModel.NavigateToCommand.Execute(route);

        Assert.Empty(navigation.NavigatedTo);
        Assert.False(viewModel.IsNavigationExpanded);
    }

    [Fact]
    public void ExpandNavigationMenuCommand_Twice_KeepsMenuExpanded()
    {
        var navigation = new FakeNavigationManager("http://localhost/");
        var viewModel = new NavMenuViewModel(_scopeService, navigation, _userSettings, _defaultScopeSettings);

        viewModel.ExpandNavigationMenuCommand.Execute(null);
        viewModel.ExpandNavigationMenuCommand.Execute(null);

        Assert.True(viewModel.IsNavigationExpanded);
    }

    [Fact]
    public void ToggleNavigationMenuCommand_Twice_ExpandsAndCollapses()
    {
        var navigation = new FakeNavigationManager("http://localhost/");
        var viewModel = new NavMenuViewModel(_scopeService, navigation, _userSettings, _defaultScopeSettings);

        viewModel.ToggleNavigationMenuCommand.Execute(null);
        Assert.True(viewModel.IsNavigationExpanded);
        viewModel.ToggleNavigationMenuCommand.Execute(null);

        Assert.False(viewModel.IsNavigationExpanded);
    }

    [Fact]
    public void CollapseNavigationMenuCommand_ExpandedMenu_CollapsesIt()
    {
        var navigation = new FakeNavigationManager("http://localhost/");
        var viewModel = new NavMenuViewModel(_scopeService, navigation, _userSettings, _defaultScopeSettings);
        viewModel.ToggleNavigationMenuCommand.Execute(null);

        viewModel.CollapseNavigationMenuCommand.Execute(null);

        Assert.False(viewModel.IsNavigationExpanded);
    }

    [Fact]
    public void ToggleTaskScopeCommand_Executed_TogglesScopeInService()
    {
        var navigation = new FakeNavigationManager("http://localhost/");
        var viewModel = new NavMenuViewModel(_scopeService, navigation, _userSettings, _defaultScopeSettings);

        viewModel.ToggleTaskScopeCommand.Execute(null);

        _scopeService.Received(1).ToggleScope();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ScopeChanged_Event_RaisesIsWorkScopeWithServiceValue(bool isWorkScope)
    {
        var navigation = new FakeNavigationManager("http://localhost/");
        var viewModel = new NavMenuViewModel(_scopeService, navigation, _userSettings, _defaultScopeSettings);
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
        var viewModel = new NavMenuViewModel(_scopeService, navigation, _userSettings, _defaultScopeSettings);
        var changed = TrackChanges(viewModel);
        viewModel.Dispose();

        navigation.SimulateNavigation("http://localhost/calender");
        _scopeService.ScopeChanged += Raise.Event();

        Assert.Empty(changed);
        Assert.Equal("/focus-timer", viewModel.CurrentRoute);
    }

    [Fact]
    public void Dispose_AfterDispose_IgnoresLocationChanges()
    {
        var navigation = new FakeNavigationManager("http://localhost/focus-timer");
        var viewModel = new NavMenuViewModel(_scopeService, navigation, _userSettings, _defaultScopeSettings);
        viewModel.Dispose();

        navigation.SimulateNavigation("http://localhost/calender");

        Assert.Equal("/focus-timer", viewModel.CurrentRoute);
    }

    private static List<string?> TrackChanges(NavMenuViewModel viewModel)
    {
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        return changed;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanSwitchScope_Always_FollowsTheScopeService(bool canSwitch)
    {
        _scopeService.CanSwitch.Returns(canSwitch);
        var viewModel = new NavMenuViewModel(_scopeService, new FakeNavigationManager("http://localhost/"), _userSettings, _defaultScopeSettings);

        Assert.Equal(canSwitch, viewModel.CanSwitchScope);
    }

    [Fact]
    public void DefaultScopeChanged_Raised_NotifiesSwitchAndActiveScope()
    {
        var viewModel = new NavMenuViewModel(_scopeService, new FakeNavigationManager("http://localhost/"), _userSettings, _defaultScopeSettings);
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        _defaultScopeSettings.DefaultScopeChanged += Raise.Event();

        Assert.Contains(nameof(INavMenuViewModel.CanSwitchScope), changed);
        Assert.Contains(nameof(INavMenuViewModel.IsWorkScope), changed);
    }
}
