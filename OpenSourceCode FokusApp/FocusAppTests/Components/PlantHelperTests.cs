using Bunit;
using FokusApp.Components.ForYouSuggestions;
using FokusApp.Components.Layout;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FocusAppTests.Components;

public class PlantHelperTests : ComponentTestContext
{
    private readonly IPlantHelperViewModel _viewModel = Substitute.For<IPlantHelperViewModel>();

    public PlantHelperTests()
    {
        _viewModel.IsPlantVisible.Returns(true);
        _viewModel.Suggestions.Returns(
        [
            new ForYouSuggestionItem("Gewohnheit", false, null, Guid.NewGuid()),
            new ForYouSuggestionItem("Mein Vorschlag", true, null),
            new ForYouSuggestionItem("Spaziergang an der frischen Luft", false, ForYouSuggestionCategory.Movement)
        ]);
        Services.AddSingleton(_viewModel);
        Services.AddSingleton(Substitute.For<ITaskEditPopUpViewModel>());
    }

    [Fact]
    public void Render_InitializesViewModel()
    {
        Render<PlantHelper>();

        _viewModel.Received(1).InitializeAsync();
    }

    [Fact]
    public void Render_CompanionSwitchedOff_ShowsNoPlantAndNoBubble()
    {
        _viewModel.IsPlantVisible.Returns(false);
        _viewModel.IsBubbleVisible.Returns(true);

        var cut = Render<PlantHelper>();

        Assert.Empty(cut.FindAll(".plant-helper"));
        Assert.Empty(cut.FindAll(".plant-bubble"));
    }

    [Fact]
    public void Render_Plant_IsAButtonWithAnAccessibleName()
    {
        var cut = Render<PlantHelper>();

        var button = cut.Find(".plant-button");
        Assert.Equal(Text("PlantTap"), button.GetAttribute("aria-label"));
        Assert.NotNull(cut.Find(".plant-button svg"));
    }

    [Theory]
    [InlineData(CompanionMood.Resting, "resting")]
    [InlineData(CompanionMood.Awake, "awake")]
    [InlineData(CompanionMood.Budding, "budding")]
    [InlineData(CompanionMood.Blooming, "blooming")]
    public void Render_Plant_ShowsTheFigureInItsCurrentMood(CompanionMood mood, string cssName)
    {
        _viewModel.Mood.Returns(mood);

        var cut = Render<PlantHelper>();

        Assert.Single(cut.FindAll($".plant-button svg.companion-figure--{cssName}"));
    }

    [Fact]
    public void Render_RestingPlant_HasClosedEyesAndNoWhiteOfTheEyes()
    {
        _viewModel.Mood.Returns(CompanionMood.Resting);

        var cut = Render<PlantHelper>();

        Assert.Equal(2, cut.FindAll(".companion-eye-closed").Count);
        Assert.Empty(cut.FindAll(".plant-eye-white"));
    }

    [Fact]
    public void Render_AwakePlant_HasOpenEyesAndNoBudOrFlower()
    {
        _viewModel.Mood.Returns(CompanionMood.Awake);

        var cut = Render<PlantHelper>();

        Assert.Equal(2, cut.FindAll(".plant-eye-white").Count);
        Assert.Empty(cut.FindAll(".companion-eye-closed"));
        Assert.Empty(cut.FindAll(".companion-bud"));
        Assert.Empty(cut.FindAll(".companion-flower"));
    }

    [Fact]
    public void Render_BuddingAndBloomingPlant_ShowABudOrAFlower()
    {
        _viewModel.Mood.Returns(CompanionMood.Budding);
        var budding = Render<PlantHelper>();
        Assert.Single(budding.FindAll(".companion-bud"));
        Assert.Empty(budding.FindAll(".companion-flower"));

        _viewModel.Mood.Returns(CompanionMood.Blooming);
        var blooming = Render<PlantHelper>();
        Assert.Single(blooming.FindAll(".companion-flower"));
        Assert.Empty(blooming.FindAll(".companion-bud"));
    }

    [Fact]
    public void Render_NoBubbleYet_ShowsOnlyThePlant()
    {
        var cut = Render<PlantHelper>();

        Assert.Single(cut.FindAll(".plant-helper"));
        Assert.Empty(cut.FindAll(".plant-bubble"));
    }

    [Fact]
    public void Render_Bubble_ShowsTheQuestion()
    {
        _viewModel.IsBubbleVisible.Returns(true);
        _viewModel.BubbleKind.Returns(PlantBubbleKind.ForYou);

        var cut = Render<PlantHelper>();

        Assert.Equal(Text("PlantBubble"), cut.Find(".plant-bubble-text").TextContent.Trim());
        Assert.Equal("Brauchst du einen ForYou?", cut.Find(".plant-bubble-text").TextContent.Trim());
    }

    [Fact]
    public void Render_PlanDayBubble_AsksToPlanTheDay()
    {
        _viewModel.IsBubbleVisible.Returns(true);
        _viewModel.BubbleKind.Returns(PlantBubbleKind.PlanDay);

        var cut = Render<PlantHelper>();

        Assert.Equal(Text("PlantBubblePlanDay"), cut.Find(".plant-bubble-text").TextContent.Trim());
        Assert.Equal("Tag planen?", cut.Find(".plant-bubble-text").TextContent.Trim());
    }

    [Fact]
    public void Plant_OffersNoWayToPushItAway()
    {
        _viewModel.IsSuggestionListOpen.Returns(true);
        var cut = Render<PlantHelper>();

        var button = cut.Find(".plant-button");
        Assert.Null(button.GetAttribute("aria-keyshortcuts"));
        Assert.Throws<MissingEventHandlerException>(() => button.PointerUp(new PointerEventArgs { ClientY = 40 }));
        Assert.Throws<MissingEventHandlerException>(() => button.KeyDown(new KeyboardEventArgs { Key = "ArrowUp" }));
        Assert.Empty(cut.FindAll(".plant-hide-button"));
    }

    [Fact]
    public void Click_Plant_TapsThePlantAndStaysOnThePage()
    {
        var cut = Render<PlantHelper>();

        cut.Find(".plant-button").Click();

        _viewModel.Received(1).TapPlant();
        Assert.DoesNotContain("/tagesplaner", Services.GetRequiredService<NavigationManager>().Uri);
        _viewModel.DidNotReceive().OpenSuggestions();
    }

    [Theory]
    [InlineData(PlantBubbleKind.GoalReached, "PlantBubbleGoalReached")]
    [InlineData(PlantBubbleKind.GoalExceeded, "PlantBubbleGoalExceeded")]
    [InlineData(PlantBubbleKind.AskForYou, "PlantBubbleAskForYou")]
    public void Render_TapBubble_ShowsTheTextOfItsKind(PlantBubbleKind kind, string key)
    {
        _viewModel.IsBubbleVisible.Returns(true);
        _viewModel.BubbleKind.Returns(kind);

        var cut = Render<PlantHelper>();

        Assert.Equal(Text(key), cut.Find(".plant-bubble-text").TextContent.Trim());
    }

    [Theory]
    [InlineData(PlantBubbleKind.GoalReached)]
    [InlineData(PlantBubbleKind.GoalExceeded)]
    public void Click_GoalBubble_ClosesItWithoutOpeningTheSuggestions(PlantBubbleKind kind)
    {
        _viewModel.IsBubbleVisible.Returns(true);
        _viewModel.BubbleKind.Returns(kind);
        var cut = Render<PlantHelper>();

        cut.Find(".plant-bubble-text").Click();

        _viewModel.Received(1).DismissBubble();
        _viewModel.DidNotReceive().OpenSuggestions();
    }

    [Fact]
    public void Click_AskForYouBubble_OpensTheSuggestions()
    {
        _viewModel.IsBubbleVisible.Returns(true);
        _viewModel.BubbleKind.Returns(PlantBubbleKind.AskForYou);
        var cut = Render<PlantHelper>();

        cut.Find(".plant-bubble-text").Click();

        _viewModel.Received(1).OpenSuggestions();
    }

    [Fact]
    public void Click_PlanDayBubble_OpensTheDayPlanner()
    {
        _viewModel.IsBubbleVisible.Returns(true);
        _viewModel.BubbleKind.Returns(PlantBubbleKind.PlanDay);
        var cut = Render<PlantHelper>();

        cut.Find(".plant-bubble-text").Click();

        Assert.EndsWith("/tagesplaner", Services.GetRequiredService<NavigationManager>().Uri);
        _viewModel.DidNotReceive().OpenSuggestions();
    }

    [Fact]
    public void Click_ForYouBubble_OpensTheSuggestionsDirectly()
    {
        _viewModel.IsBubbleVisible.Returns(true);
        _viewModel.BubbleKind.Returns(PlantBubbleKind.ForYou);
        var cut = Render<PlantHelper>();

        cut.Find(".plant-bubble-text").Click();

        _viewModel.Received(1).OpenSuggestions();
        Assert.DoesNotContain("/tagesplaner", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void Click_BubbleText_OpensTheSuggestions()
    {
        _viewModel.IsBubbleVisible.Returns(true);
        var cut = Render<PlantHelper>();

        cut.Find(".plant-bubble-text").Click();

        _viewModel.Received(1).OpenSuggestions();
    }

    [Fact]
    public void Click_BubbleClose_DismissesTheBubble()
    {
        _viewModel.IsBubbleVisible.Returns(true);
        var cut = Render<PlantHelper>();

        cut.Find(".plant-bubble-close").Click();

        _viewModel.Received(1).DismissBubble();
        Assert.Equal(Text("PlantDismiss"), cut.Find(".plant-bubble-close").GetAttribute("aria-label"));
    }

    [Fact]
    public void Render_ListClosed_ShowsNoDialog()
    {
        var cut = Render<PlantHelper>();

        Assert.Empty(cut.FindAll(".plant-dialog"));
    }

    [Fact]
    public void Render_ListOpen_ShowsOwnFirstAndTheCategoryOfPoolEntries()
    {
        _viewModel.IsSuggestionListOpen.Returns(true);

        var cut = Render<PlantHelper>();

        var items = cut.FindAll(".plant-suggestion");
        Assert.Equal(3, items.Count);
        Assert.Equal("Gewohnheit", items[0].QuerySelector(".plant-suggestion-text")!.TextContent.Trim());
        Assert.Equal("Mein Vorschlag", items[1].QuerySelector(".plant-suggestion-text")!.TextContent.Trim());
        Assert.Contains("plant-suggestion-own", items[1].ClassName);
        var titles = cut.FindAll(".plant-group-title > span:first-child").Select(title => title.TextContent.Trim()).ToList();
        Assert.Equal(
            [Text("ForYouCategoryTemplate"), Text("ForYouCategoryOwn"), Text("ForYouCategoryMovement")],
            titles);
        Assert.Equal("dialog", cut.Find(".plant-dialog").GetAttribute("role"));
    }

    [Fact]
    public void Click_Suggestion_PicksItsText()
    {
        _viewModel.IsSuggestionListOpen.Returns(true);
        var cut = Render<PlantHelper>();

        cut.FindAll(".plant-suggestion")[2].Click();

        _viewModel.Received(1).PickSuggestion(Arg.Is<ForYouSuggestionItem>(item => item.Text == "Spaziergang an der frischen Luft"));
    }

    [Fact]
    public void Click_BackdropOrCloseButton_ClosesTheList()
    {
        _viewModel.IsSuggestionListOpen.Returns(true);
        var cut = Render<PlantHelper>();

        cut.Find(".plant-backdrop").Click();
        cut.Find(".plant-dialog-close").Click();

        _viewModel.Received(2).CloseSuggestions();
    }

    [Fact]
    public void Texts_EveryPoolSuggestionAndCategory_AreLocalized()
    {
        foreach (var entry in ForYouSuggestionPool.Entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(Text(entry.TextKey)));
        }

        foreach (var category in Enum.GetValues<ForYouSuggestionCategory>())
        {
            Assert.False(string.IsNullOrWhiteSpace(Text($"ForYouCategory{category}")));
        }

        Assert.False(string.IsNullOrWhiteSpace(Text("ForYouCategoryTemplate")));
    }
}
