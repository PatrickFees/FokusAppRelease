using Bunit;
using FokusApp.Components.UpdateNotice;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FocusAppTests.Components;

public class UpdateNoticeOverlayTests : ComponentTestContext
{
    private readonly IUpdateNoticeViewModel _viewModel = Substitute.For<IUpdateNoticeViewModel>();

    public UpdateNoticeOverlayTests()
    {
        _viewModel.IsOpen.Returns(true);
        _viewModel.LatestVersion.Returns("1.5.0");
        Services.AddSingleton(_viewModel);
    }

    [Fact]
    public void Render_InitializesViewModel()
    {
        Render<UpdateNoticeOverlay>();

        _viewModel.Received(1).InitializeAsync();
    }

    [Fact]
    public void Render_Closed_ShowsNothing()
    {
        _viewModel.IsOpen.Returns(false);

        var cut = Render<UpdateNoticeOverlay>();

        Assert.Empty(cut.FindAll(".update-notice-dialog"));
        Assert.Empty(cut.FindAll(".update-notice-backdrop"));
    }

    [Fact]
    public void Render_Open_IsAnAccessibleModalWithTheVersion()
    {
        var cut = Render<UpdateNoticeOverlay>();

        var dialog = cut.Find(".update-notice-dialog");
        Assert.Equal("dialog", dialog.GetAttribute("role"));
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
        Assert.Contains("1.5.0", cut.Find(".update-notice-text").TextContent);
    }

    [Fact]
    public void Click_Dismiss_CallsTheViewModel()
    {
        var cut = Render<UpdateNoticeOverlay>();

        cut.Find(".update-notice-dismiss").Click();

        _viewModel.Received(1).DismissAsync();
    }

    [Fact]
    public void Click_Download_CallsTheViewModel()
    {
        var cut = Render<UpdateNoticeOverlay>();

        cut.Find(".update-notice-download").Click();

        _viewModel.Received(1).DownloadAsync();
    }
}
