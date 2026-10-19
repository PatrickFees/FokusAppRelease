using Microsoft.AspNetCore.Components;

namespace FocusAppTests.Fakes;

/// <summary>
/// Minimaler <see cref="NavigationManager"/> für ViewModel-Tests: merkt sich Navigationsziele
/// und löst <c>LocationChanged</c> aus, ohne Blazor-Host.
/// </summary>
public sealed class FakeNavigationManager : NavigationManager
{
    public FakeNavigationManager(string uri)
    {
        Initialize("http://localhost/", uri);
    }

    public List<string> NavigatedTo { get; } = [];

    public void SimulateNavigation(string absoluteUri)
    {
        Uri = absoluteUri;
        NotifyLocationChanged(false);
    }

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        NavigatedTo.Add(uri);
        var absoluteUri = ToAbsoluteUri(uri);
        Uri = absoluteUri.ToString();
        NotifyLocationChanged(false);
    }
}
