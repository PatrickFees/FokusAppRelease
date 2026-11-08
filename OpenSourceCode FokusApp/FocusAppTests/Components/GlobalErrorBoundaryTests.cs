using Bunit;
using FokusApp.Components.Layout;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FocusAppTests.Components;

public class GlobalErrorBoundaryTests : ComponentTestContext
{
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly FailureSwitch _failure = new() { ShouldFail = true };

    public GlobalErrorBoundaryTests()
    {
        Services.AddSingleton(_errorCapture);
    }

    [Fact]
    public void Render_ChildWorks_ShowsTheChildContent()
    {
        _failure.ShouldFail = false;

        var cut = RenderBoundary();

        Assert.Contains("Alles gut", cut.Markup);
        Assert.Empty(cut.FindAll(".global-error-boundary"));
    }

    [Fact]
    public void Render_ChildThrows_ShowsTheFriendlyErrorMessage()
    {
        var cut = RenderBoundary();

        var section = cut.Find(".global-error-boundary");
        Assert.Equal("alert", section.GetAttribute("role"));
        Assert.Equal(Text("UnexpectedErrorTitle"), cut.Find("h1").TextContent);
        Assert.Equal(Text("UnexpectedErrorDescription"), cut.Find("p").TextContent);
        Assert.Equal(Text("ReloadView"), cut.Find(".global-error-recover-button").TextContent);
        Assert.DoesNotContain("Alles gut", cut.Markup);
    }

    [Fact]
    public void Render_ChildThrows_CapturesTheErrorAsNonFatalUiFailureWithRoute()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/aufgaben");

        RenderBoundary();

        _errorCapture.Received(1).CaptureAsync(
            Arg.Is<Exception>(exception => exception.Message == "kaputt"),
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "Blazor.GlobalErrorBoundary" &&
                !options.IsFatal &&
                options.IsUiFailure &&
                options.Route == "/aufgaben"));
    }

    [Fact]
    public void Render_ChildThrowsOnStartPage_CapturesTheRootRoute()
    {
        RenderBoundary();

        _errorCapture.Received(1).CaptureAsync(
            Arg.Any<Exception>(),
            Arg.Is<AppErrorCaptureOptions?>(options => options != null && options.Route == "/"));
    }

    [Fact]
    public void Render_ChildThrowsOnPathWithLeadingSlash_KeepsThePathAsIs()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("http://localhost//doppelt");

        RenderBoundary();

        _errorCapture.Received(1).CaptureAsync(
            Arg.Any<Exception>(),
            Arg.Is<AppErrorCaptureOptions?>(options => options != null && options.Route == "/doppelt"));
    }

    [Fact]
    public void ClickReloadView_AfterError_ShowsTheChildAgain()
    {
        var cut = RenderBoundary();
        _failure.ShouldFail = false;

        cut.Find(".global-error-recover-button").Click();

        Assert.Contains("Alles gut", cut.Markup);
        Assert.Empty(cut.FindAll(".global-error-boundary"));
    }

    [Fact]
    public void LocationChanged_AfterError_RecoversTheView()
    {
        var cut = RenderBoundary();
        _failure.ShouldFail = false;
        var navigation = Services.GetRequiredService<NavigationManager>();

        navigation.NavigateTo("/einstellungen");

        cut.WaitForAssertion(() => Assert.Contains("Alles gut", cut.Markup));
    }

    [Fact]
    public void LocationChanged_NoError_DoesNotRemountTheChildContent()
    {
        var cut = Render<GlobalErrorBoundary>(parameters => parameters
            .AddChildContent<StatefulComponent>());
        var idBefore = cut.Find(".stateful-marker").TextContent;
        var navigation = Services.GetRequiredService<NavigationManager>();

        navigation.NavigateTo("/einstellungen");

        Assert.Equal(idBefore, cut.Find(".stateful-marker").TextContent);
    }

    [Fact]
    public async Task Dispose_AfterDispose_IgnoresLocationChanges()
    {
        RenderBoundary();
        _failure.ShouldFail = false;
        var navigation = Services.GetRequiredService<NavigationManager>();
        await DisposeComponentsAsync();

        var exception = Record.Exception(() => navigation.NavigateTo("/einstellungen"));

        Assert.Null(exception);
    }

    private IRenderedComponent<GlobalErrorBoundary> RenderBoundary()
    {
        return Render<GlobalErrorBoundary>(parameters => parameters
            .AddChildContent<FailingComponent>(child => child.Add(component => component.Switch, _failure)));
    }

    private sealed class FailureSwitch
    {
        public bool ShouldFail { get; set; }
    }

    private sealed class FailingComponent : ComponentBase
    {
        [Parameter]
        public FailureSwitch Switch { get; set; } = new();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (Switch.ShouldFail)
            {
                throw new InvalidOperationException("kaputt");
            }

            builder.AddContent(0, "Alles gut");
        }
    }

    private sealed class StatefulComponent : ComponentBase
    {
        private readonly Guid _instanceId = Guid.NewGuid();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "stateful-marker");
            builder.AddContent(2, _instanceId.ToString());
            builder.CloseElement();
        }
    }
}
