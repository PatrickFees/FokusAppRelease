using Bunit;
using FokusApp.Components.SharedUI;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FocusAppTests.Components;

public class ConfettiTests : ComponentTestContext
{
    private readonly IConfettiViewModel _viewModel = Substitute.For<IConfettiViewModel>();

    public ConfettiTests()
    {
        Services.AddSingleton(_viewModel);
    }

    [Fact]
    public void Render_NotVisible_RendersNothing()
    {
        _viewModel.IsVisible.Returns(false);

        var cut = Render<Confetti>();

        Assert.Empty(cut.FindAll(".confetti"));
        Assert.Empty(cut.FindAll(".confetti-piece"));
    }

    [Fact]
    public void Render_Visible_ShowsPiecesHiddenFromScreenReaders()
    {
        _viewModel.IsVisible.Returns(true);
        _viewModel.BurstNumber.Returns(1);

        var cut = Render<Confetti>();

        var container = cut.Find(".confetti");
        Assert.Equal("true", container.GetAttribute("aria-hidden"));
        Assert.Equal(28, cut.FindAll(".confetti-piece").Count);
    }

    [Fact]
    public void Render_Visible_UsesSeveralColorsWithoutAnyTaskTypeClass()
    {
        _viewModel.IsVisible.Returns(true);
        _viewModel.BurstNumber.Returns(3);

        var cut = Render<Confetti>();

        var colors = cut.FindAll(".confetti-piece")
            .SelectMany(piece => piece.ClassList)
            .Where(name => name.StartsWith("confetti-color-", StringComparison.Ordinal))
            .Distinct()
            .ToList();
        Assert.True(colors.Count > 1);
        Assert.DoesNotContain("for-you", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("todo", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_SameBurstNumber_ProducesTheSamePieces()
    {
        _viewModel.IsVisible.Returns(true);
        _viewModel.BurstNumber.Returns(5);

        var first = Render<Confetti>().Markup;
        var second = Render<Confetti>().Markup;

        Assert.Equal(first, second);
    }

    [Fact]
    public void Render_DifferentBurstNumbers_ProduceDifferentPieces()
    {
        _viewModel.IsVisible.Returns(true);
        _viewModel.BurstNumber.Returns(1);
        var first = Render<Confetti>().Markup;
        _viewModel.BurstNumber.Returns(2);

        var second = Render<Confetti>().Markup;

        Assert.NotEqual(first, second);
    }
}
