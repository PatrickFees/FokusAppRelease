using Bunit;
using FokusApp.Components.Layout;
using FokusApp.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FocusAppTests.Components;

public class DesktopInputBridgeTests : ComponentTestContext
{
    private readonly IUiBackPressService _backPress = Substitute.For<IUiBackPressService>();

    public DesktopInputBridgeTests()
    {
        Services.AddSingleton(_backPress);
    }

    [Fact]
    public void Render_Bridge_LoadsTheDesktopInputModule()
    {
        Render<DesktopInputBridge>();

        JSInterop.VerifyInvoke("import");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TryHandleBackRequest_ReturnsTheResultOfTheBackPressService(bool handled)
    {
        _backPress.TryHandleBackPress().Returns(handled);
        var cut = Render<DesktopInputBridge>();

        var result = cut.Instance.TryHandleBackRequest();

        Assert.Equal(handled, result);
    }
}
