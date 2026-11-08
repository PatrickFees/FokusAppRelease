using Microsoft.AspNetCore.Components;

namespace FocusAppTests.Fakes;

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
