using Bunit;
using CommunityToolkit.Mvvm.Input;
using FokusApp.Components.ForYouSuggestions;
using FokusApp.Components.Layout;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Components.SharedUI;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FocusAppTests.Components;

public class MainLayoutTests : ComponentTestContext
{
    public MainLayoutTests()
    {
        var navMenu = Substitute.For<INavMenuViewModel>();
        navMenu.ToggleTaskScopeCommand.Returns(Substitute.For<IRelayCommand>());
        navMenu.ToggleNavigationMenuCommand.Returns(Substitute.For<IRelayCommand>());
        navMenu.CurrentRoute.Returns("/");
        Services.AddSingleton(navMenu);
        Services.AddSingleton(Substitute.For<IPlantHelperViewModel>());
        Services.AddSingleton(Substitute.For<ITaskEditPopUpViewModel>());
        Services.AddSingleton(Substitute.For<IConfettiViewModel>());
    }

    [Fact]
    public void Render_Body_IsShownInsideTheContentArea()
    {
        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<p class=\"seite\">Inhalt</p>"))));

        var content = cut.Find("main article.content .seite");
        Assert.Equal("Inhalt", content.TextContent);
    }

    [Fact]
    public void Render_Layout_ContainsNavigationAndConfettiLayer()
    {
        var cut = Render<MainLayout>();

        Assert.NotNull(cut.FindComponent<NavMenu>());
        Assert.NotNull(cut.FindComponent<Confetti>());
    }

    [Fact]
    public void Render_Layout_AppliesThePageTransitionClassToTheContentArea()
    {
        var cut = Render<MainLayout>();

        Assert.NotEmpty(cut.FindAll("main article.content.page-transition"));
    }

    [Fact]
    public void LocationChanged_Navigates_RecreatesTheContentElement()
    {
        var cut = Render<MainLayout>(parameters => parameters
            .Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<p>Inhalt</p>"))));
        var elementBefore = cut.Find("article.content");
        var navigation = Services.GetRequiredService<NavigationManager>();

        navigation.NavigateTo("/einstellungen");
        cut.Render();

        var elementAfter = cut.Find("article.content");
        Assert.False(ReferenceEquals(elementBefore, elementAfter));
    }
}
