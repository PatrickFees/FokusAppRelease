using System.Reflection;
using Bunit;
using FokusApp.Components.WeeklyReviewComponent;
using FokusApp.Core.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FocusAppTests.Components;

public class WeeklyReviewPageTests : ComponentTestContext
{
    private static readonly DateTime WeekStart = new(2026, 3, 9);

    private readonly IWeeklyReviewViewModel _viewModel = Substitute.For<IWeeklyReviewViewModel>();

    public WeeklyReviewPageTests()
    {
        _viewModel.IsReviewEnabled.Returns(true);
        _viewModel.IsLoaded.Returns(true);
        _viewModel.WeekStart.Returns(WeekStart);
        _viewModel.WeekEnd.Returns(WeekStart.AddDays(6));
        _viewModel.IsEmpty.Returns(true);
        _viewModel.GoalForYou.Returns(1);
        _viewModel.GoalToDo.Returns(1);
        _viewModel.Days.Returns(BuildDays());
        Services.AddSingleton(_viewModel);
    }

    [Fact]
    public void Page_Route_IsTheOneOfTheMenuEntry()
    {
        var routes = typeof(WeeklyReviewPage).GetCustomAttributes<RouteAttribute>().Select(route => route.Template);

        Assert.Contains("/wochenrueckblick", routes);
    }

    [Fact]
    public void Render_InitializesViewModel()
    {
        Render<WeeklyReviewPage>();

        _viewModel.Received(1).InitializeAsync();
    }

    [Fact]
    public void Render_ReviewSwitchedOff_ShowsOnlyAFriendlyNote()
    {
        _viewModel.IsReviewEnabled.Returns(false);

        var cut = Render<WeeklyReviewPage>();

        Assert.Equal(Text("WeeklyReviewDisabled"), cut.Find(".weekly-disabled").TextContent.Trim());
        Assert.Empty(cut.FindAll(".weekly-card"));
    }

    [Fact]
    public void Render_NotLoadedYet_ShowsNoCardAndNoWrongNumbers()
    {
        _viewModel.IsLoaded.Returns(false);

        var cut = Render<WeeklyReviewPage>();

        Assert.Empty(cut.FindAll(".weekly-card"));
        Assert.Empty(cut.FindAll(".weekly-disabled"));
    }

    [Fact]
    public void Render_EmptyWeek_ShowsAFriendlySentenceAndTheGoalButNoProgress()
    {
        var cut = Render<WeeklyReviewPage>();

        Assert.Equal(Text("WeeklyReviewEmpty"), cut.Find(".weekly-summary").TextContent.Trim());
        Assert.Empty(cut.FindAll(".weekly-progress"));
        Assert.Empty(cut.FindAll(".weekly-peak"));
        Assert.Equal(7, cut.FindAll(".weekly-day").Count);
    }

    [Fact]
    public void Render_Week_ShowsGoalCreatedAndDoneAsThreeEntries()
    {
        _viewModel.IsEmpty.Returns(false);
        _viewModel.GoalForYou.Returns(2);
        _viewModel.GoalToDo.Returns(3);
        _viewModel.CreatedForYou.Returns(4);
        _viewModel.CreatedToDo.Returns(5);
        _viewModel.DoneForYou.Returns(2);
        _viewModel.DoneToDo.Returns(3);

        var cut = Render<WeeklyReviewPage>();

        Assert.Equal(3, cut.FindAll(".weekly-stats li").Count);
        Assert.Empty(cut.FindAll(".weekly-summary"));
        Assert.Equal(Text("WeeklyReviewCounts", 2, 3), cut.Find(".weekly-goal-value").TextContent.Trim());
        Assert.Equal(Text("WeeklyReviewCounts", 4, 5), cut.Find(".weekly-created-value").TextContent.Trim());
        Assert.Equal(Text("WeeklyReviewCounts", 2, 3), cut.Find(".weekly-done-value").TextContent.Trim());
        Assert.Equal(Text("WeeklyReviewGoal"), cut.Find(".weekly-goal .weekly-stat-label").TextContent.Trim());
        Assert.Equal(Text("WeeklyReviewCreated"), cut.Find(".weekly-created .weekly-stat-label").TextContent.Trim());
        Assert.Equal(Text("WeeklyReviewDone"), cut.Find(".weekly-done .weekly-stat-label").TextContent.Trim());
    }

    [Theory]
    [InlineData(GoalProgress.Open, "WeeklyReviewProgressOpen")]
    [InlineData(GoalProgress.Reached, "WeeklyReviewProgressReached")]
    [InlineData(GoalProgress.Exceeded, "WeeklyReviewProgressExceeded")]
    public void Render_Progress_SaysHowCreatedAndDoneStandAgainstTheGoal(GoalProgress progress, string key)
    {
        _viewModel.IsEmpty.Returns(false);
        _viewModel.CreatedProgress.Returns(progress);
        _viewModel.DoneProgress.Returns(progress);

        var cut = Render<WeeklyReviewPage>();

        Assert.Equal(Text(key), cut.Find(".weekly-created-progress").TextContent.Trim());
        Assert.Equal(Text(key), cut.Find(".weekly-done-progress").TextContent.Trim());
    }

    [Fact]
    public void Render_ProgressOfOnlyOneEntry_ShowsOnlyThatOne()
    {
        _viewModel.IsEmpty.Returns(false);
        _viewModel.CreatedProgress.Returns((GoalProgress?)null);
        _viewModel.DoneProgress.Returns(GoalProgress.Reached);

        var cut = Render<WeeklyReviewPage>();

        Assert.Empty(cut.FindAll(".weekly-created-progress"));
        Assert.Single(cut.FindAll(".weekly-done-progress"));
    }

    [Fact]
    public void Render_MostForYouOnOneDay_NamesTheDayAndTheCount()
    {
        _viewModel.MostForYouDays.Returns(new WeeklyReviewPeak(3, [WeekStart.AddDays(2)]));

        var cut = Render<WeeklyReviewPage>();

        Assert.Equal(Text("WeeklyReviewMostForYou", "Mittwoch", 3), cut.Find(".weekly-peak-for-you").TextContent.Trim());
        Assert.Empty(cut.FindAll(".weekly-peak-todo"));
    }

    [Fact]
    public void Render_MostToDoOnTiedDays_NamesAllOfThem()
    {
        _viewModel.MostToDoDays.Returns(new WeeklyReviewPeak(4, [WeekStart, WeekStart.AddDays(2), WeekStart.AddDays(4)]));

        var cut = Render<WeeklyReviewPage>();

        var days = $"Montag, Mittwoch {Text("WeeklyReviewAnd")} Freitag";
        Assert.Equal(Text("WeeklyReviewMostToDo", days, 4), cut.Find(".weekly-peak-todo").TextContent.Trim());
        Assert.Empty(cut.FindAll(".weekly-peak-for-you"));
    }

    [Fact]
    public void Render_MostToDoOnTwoDays_JoinsThemWithAnd()
    {
        _viewModel.MostToDoDays.Returns(new WeeklyReviewPeak(2, [WeekStart, WeekStart.AddDays(1)]));

        var cut = Render<WeeklyReviewPage>();

        var days = $"Montag {Text("WeeklyReviewAnd")} Dienstag";
        Assert.Equal(Text("WeeklyReviewMostToDo", days, 2), cut.Find(".weekly-peak-todo").TextContent.Trim());
    }

    [Fact]
    public void Render_NoPeaks_ShowsNoSentence()
    {
        var cut = Render<WeeklyReviewPage>();

        Assert.Empty(cut.FindAll(".weekly-peak"));
    }

    [Fact]
    public void Render_ReducedReview_ShowsNoStreakNoPlantNoBestDayAndNoWeekdayPatterns()
    {
        _viewModel.IsEmpty.Returns(false);

        var cut = Render<WeeklyReviewPage>();

        Assert.Empty(cut.FindAll(".weekly-streak"));
        Assert.Empty(cut.FindAll(".weekly-companion"));
        Assert.Empty(cut.FindAll(".weekly-best-day"));
        Assert.Empty(cut.FindAll(".weekly-focus"));
        Assert.Empty(cut.FindAll(".weekly-patterns"));
        Assert.Empty(cut.FindAll(".weekly-insight"));
        Assert.Empty(cut.FindAll(".weekly-fresh-start"));
    }

    [Fact]
    public void Render_CurrentWeek_ShowsTheStatusNoteAndNoCurrentWeekButton()
    {
        _viewModel.IsCurrentWeek.Returns(true);

        var cut = Render<WeeklyReviewPage>();

        Assert.Equal(Text("WeeklyReviewSoFar"), cut.Find(".weekly-so-far").TextContent.Trim());
        Assert.Empty(cut.FindAll(".weekly-current"));
    }

    [Fact]
    public void Render_PastWeek_ShowsNoStatusNoteButAWayBackToThisWeek()
    {
        _viewModel.IsCurrentWeek.Returns(false);

        var cut = Render<WeeklyReviewPage>();

        Assert.Empty(cut.FindAll(".weekly-so-far"));
        cut.Find(".weekly-current").Click();
        _viewModel.Received(1).ShowCurrentWeekAsync();
    }

    [Fact]
    public void Render_Navigation_CallsTheViewModel()
    {
        _viewModel.CanShowNextWeek.Returns(true);
        var cut = Render<WeeklyReviewPage>();

        cut.Find(".weekly-previous").Click();
        cut.Find(".weekly-next").Click();

        _viewModel.Received(1).ShowPreviousWeekAsync();
        _viewModel.Received(1).ShowNextWeekAsync();
    }

    [Fact]
    public void Render_CurrentWeekIsNewest_DisablesTheNextButton()
    {
        _viewModel.CanShowNextWeek.Returns(false);

        var cut = Render<WeeklyReviewPage>();

        Assert.True(cut.Find(".weekly-next").HasAttribute("disabled"));
    }

    [Fact]
    public void Render_Days_SetBarHeights()
    {
        _viewModel.Days.Returns(BuildDays(forYou: 50, toDo: 25));

        var cut = Render<WeeklyReviewPage>();

        var monday = cut.FindAll(".weekly-day")[0];
        Assert.Contains("height: 50%", monday.QuerySelector(".weekly-bar-for-you")!.GetAttribute("style"));
        Assert.Contains("height: 25%", monday.QuerySelector(".weekly-bar-todo")!.GetAttribute("style"));
    }

    [Fact]
    public void Render_Day_AnnouncesTheNumbersToScreenReaders()
    {
        _viewModel.Days.Returns(BuildDays(forYou: 50, toDo: 25, doneForYou: 2, doneToDo: 1));

        var cut = Render<WeeklyReviewPage>();

        var label = cut.FindAll(".weekly-day")[0].GetAttribute("aria-label");
        Assert.Equal(Text("WeeklyReviewDayLabel", "Montag", 2, 1), label);
        Assert.Equal("img", cut.FindAll(".weekly-day")[0].GetAttribute("role"));
    }

    [Fact]
    public void Render_Legend_DistinguishesForYouAndToDoByTextureAndLabelNotByColour()
    {
        var cut = Render<WeeklyReviewPage>();

        Assert.Contains("weekly-swatch-for-you", cut.Markup);
        Assert.Contains("weekly-swatch-todo", cut.Markup);
        Assert.Contains(Text("WeeklyReviewForYou"), cut.Find(".weekly-legend").TextContent);
        Assert.Contains(Text("WeeklyReviewToDo"), cut.Find(".weekly-legend").TextContent);
    }

    [Fact]
    public void Render_Page_SetsThePageTitle()
    {
        var head = Render<HeadOutlet>();

        Render<WeeklyReviewPage>();

        head.WaitForAssertion(() => Assert.Contains(Text("WeeklyReviewTitle"), head.Markup));
    }

    private static List<WeeklyReviewDay> BuildDays(
        double forYou = 0,
        double toDo = 0,
        int doneForYou = 0,
        int doneToDo = 0)
    {
        return Enumerable.Range(0, 7)
            .Select(offset => new WeeklyReviewDay(
                WeekStart.AddDays(offset),
                offset == 0 ? doneForYou : 0,
                offset == 0 ? doneToDo : 0,
                offset == 0 ? forYou : 0,
                offset == 0 ? toDo : 0))
            .ToList();
    }
}
