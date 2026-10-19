using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FokusApp.Core.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;


namespace FokusApp.Components.Layout;

public sealed partial class NavMenuViewModel : ObservableObject, INavMenuViewModel, IDisposable
{
    private const string RootRoute = "/";

    private readonly IScopeService _scopeService;
    private readonly NavigationManager _navigationManager;

    [ObservableProperty]
    private bool isNavigationExpanded;
    private string _currentRoute = RootRoute;

    public NavMenuViewModel(IScopeService scopeService, NavigationManager navigationManager)
    {
        _scopeService = scopeService;
        _navigationManager = navigationManager;

        ToggleTaskScopeCommand = new RelayCommand(ToggleTaskScope);
        ToggleNavigationMenuCommand = new RelayCommand(ToggleNavigationMenu);
        ExpandNavigationMenuCommand = new RelayCommand(ExpandNavigationMenu);
        CollapseNavigationMenuCommand = new RelayCommand(CollapseNavigationMenu);
        NavigateToCommand = new RelayCommand<string>(NavigateTo);

        _currentRoute = NormalizeRoutePath(_navigationManager.Uri);
        _scopeService.ScopeChanged += OnScopeChanged;
        _navigationManager.LocationChanged += OnLocationChanged;
    }

    public IRelayCommand ToggleTaskScopeCommand { get; }
    public IRelayCommand ToggleNavigationMenuCommand { get; }
    public IRelayCommand ExpandNavigationMenuCommand { get; }
    public IRelayCommand CollapseNavigationMenuCommand { get; }
    public IRelayCommand<string> NavigateToCommand { get; }

    public bool IsWorkScope => _scopeService.IsWorkScope;

    public string CurrentRoute => _currentRoute;

    private void ToggleTaskScope()
    {
        _scopeService.ToggleScope();
    }

    private void ToggleNavigationMenu()
    {
        IsNavigationExpanded = !IsNavigationExpanded;
    }

    private void ExpandNavigationMenu()
    {
        IsNavigationExpanded = true;
    }

    private void CollapseNavigationMenu()
    {
        IsNavigationExpanded = false;
    }

    private void NavigateTo(string? route)
    {
        CollapseNavigationMenu();

        if (string.IsNullOrWhiteSpace(route))
        {
            return;
        }

        _navigationManager.NavigateTo(route);
    }

    private void OnScopeChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(IsWorkScope));
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var route = NormalizeRoutePath(e.Location);

        if (string.Equals(_currentRoute, route, StringComparison.Ordinal))
        {
            return;
        }

        _currentRoute = route;
        OnPropertyChanged(nameof(CurrentRoute));
    }

    private static string NormalizeRoutePath(string absoluteUri)
    {
        if (string.IsNullOrWhiteSpace(absoluteUri))
        {
            return RootRoute;
        }

        var uri = new Uri(absoluteUri, UriKind.Absolute);
        var path = uri.AbsolutePath;

        if (string.IsNullOrWhiteSpace(path))
        {
            return RootRoute;
        }

        if (path.Length > 1)
        {
            path = path.TrimEnd('/');
        }

        return path.ToLowerInvariant();
    }

    public void Dispose()
    {
        _scopeService.ScopeChanged -= OnScopeChanged;
        _navigationManager.LocationChanged -= OnLocationChanged;
    }
}
