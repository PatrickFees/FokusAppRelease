using Bunit;
using FokusApp.Components.WhatsNew;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FocusAppTests.Components;

public class WhatsNewOverlayTests : ComponentTestContext
{
    private readonly IWhatsNewViewModel _viewModel = Substitute.For<IWhatsNewViewModel>();

    public WhatsNewOverlayTests()
    {
        _viewModel.IsOpen.Returns(true);
        Services.AddSingleton(_viewModel);
    }

    [Fact]
    public void Render_InitializesViewModel()
    {
        Render<WhatsNewOverlay>();

        _viewModel.Received(1).InitializeAsync();
    }

    [Fact]
    public void Render_Closed_ShowsNothing()
    {
        _viewModel.IsOpen.Returns(false);

        var cut = Render<WhatsNewOverlay>();

        Assert.Empty(cut.FindAll("[data-testid='whats-new-dialog']"));
        Assert.Empty(cut.FindAll("[data-testid='whats-new-backdrop']"));
    }

    [Fact]
    public void Render_Open_IsAnAccessibleModal()
    {
        var cut = Render<WhatsNewOverlay>();

        var dialog = cut.Find("[data-testid='whats-new-dialog']");
        Assert.Equal("dialog", dialog.GetAttribute("role"));
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
    }

    [Fact]
    public void Render_Open_BoldsSoundVibrationImportExportAndSync()
    {
        var cut = Render<WhatsNewOverlay>();

        var boldWords = cut.FindAll("[data-testid='whats-new-text'] strong").Select(element => element.TextContent).ToList();
        Assert.Equal(["Sound", "Vibration", "Import", "Export", "Synchronisation"], boldWords);
    }

    [Fact]
    public void Render_Open_RendersNestedBulletLists()
    {
        var cut = Render<WhatsNewOverlay>();

        var topLevelItems = cut.Find("[data-testid='whats-new-text'] > ul").Children;
        Assert.True(topLevelItems.Length > 1);
        Assert.NotEmpty(cut.FindAll("[data-testid='whats-new-text'] li ul"));
    }

    [Fact]
    public void Click_Continue_CallsTheViewModel()
    {
        var cut = Render<WhatsNewOverlay>();

        cut.Find("[data-testid='whats-new-continue']").Click();

        _viewModel.Received(1).Dismiss();
    }
}
