using FocusAppTests.MauiShims;
using FokusApp.Services;

namespace FocusAppTests.Services;

[Collection(SerialMauiShimsCollection.Name)]
public class NotificationNavigationBridgeTests : IDisposable
{
    public NotificationNavigationBridgeTests()
    {
        NotificationNavigationBridge.TryConsumePendingRoute(out _);
    }

    public void Dispose()
    {
        NotificationNavigationBridge.TryConsumePendingRoute(out _);
    }

    [Fact]
    public void Contract_Values_AreTheAgreedOnes()
    {
        Assert.Equal("fokusapp.notification.route", NotificationNavigationContract.RouteExtraKey);
        Assert.Equal("/focus-timer", NotificationNavigationContract.FocusTimerRoute);
    }

    [Fact]
    public void TryConsumePendingRoute_NothingRequested_ReturnsFalseAndEmptyRoute()
    {
        var consumed = NotificationNavigationBridge.TryConsumePendingRoute(out var route);

        Assert.False(consumed);
        Assert.Equal(string.Empty, route);
    }

    [Fact]
    public void RequestRoute_Route_CanBeConsumedOnce()
    {
        NotificationNavigationBridge.RequestRoute("/focus-timer");

        var first = NotificationNavigationBridge.TryConsumePendingRoute(out var firstRoute);
        var second = NotificationNavigationBridge.TryConsumePendingRoute(out var secondRoute);

        Assert.True(first);
        Assert.Equal("/focus-timer", firstRoute);
        Assert.False(second);
        Assert.Equal(string.Empty, secondRoute);
    }

    [Fact]
    public void RequestRoute_Route_RaisesRouteRequested()
    {
        var received = new List<string>();
        EventHandler<string> handler = (_, route) => received.Add(route);
        NotificationNavigationBridge.RouteRequested += handler;

        try
        {
            NotificationNavigationBridge.RequestRoute("/einstellungen");
        }
        finally
        {
            NotificationNavigationBridge.RouteRequested -= handler;
        }

        var raised = Assert.Single(received);
        Assert.Equal("/einstellungen", raised);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RequestRoute_BlankRoute_IsIgnored(string? route)
    {
        var raised = 0;
        EventHandler<string> handler = (_, _) => raised++;
        NotificationNavigationBridge.RouteRequested += handler;

        try
        {
            NotificationNavigationBridge.RequestRoute(route!);
        }
        finally
        {
            NotificationNavigationBridge.RouteRequested -= handler;
        }

        var consumed = NotificationNavigationBridge.TryConsumePendingRoute(out _);
        Assert.Equal(0, raised);
        Assert.False(consumed);
    }

    [Fact]
    public void RequestRoute_SecondRouteBeforeConsuming_ReplacesThePendingOne()
    {
        NotificationNavigationBridge.RequestRoute("/eins");
        NotificationNavigationBridge.RequestRoute("/zwei");

        NotificationNavigationBridge.TryConsumePendingRoute(out var route);

        Assert.Equal("/zwei", route);
    }

    [Fact]
    public void RequestRoute_WithoutSubscribers_StillKeepsTheRoutePending()
    {
        NotificationNavigationBridge.RequestRoute("/aufgaben");

        var consumed = NotificationNavigationBridge.TryConsumePendingRoute(out var route);

        Assert.True(consumed);
        Assert.Equal("/aufgaben", route);
    }
}
