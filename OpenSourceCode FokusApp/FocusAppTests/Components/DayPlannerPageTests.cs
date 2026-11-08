using System.Reflection;
using Bunit;
using FokusApp.Components.DayPlanner;
using FokusApp.Components.ForYouSuggestions;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Components.TaskOverViewComponent;
using FokusApp.Core.Enums;
using FokusApp.Core.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FocusAppTests.Components;

public class DayPlannerPageTests : ComponentTestContext
{
    private readonly IDayPlannerViewModel _viewModel = Substitute.For<IDayPlannerViewModel>();
    private readonly Guid _pastId = Guid.NewGuid();

    public DayPlannerPageTests()
    {
        _viewModel.IsLoaded.Returns(true);
        _viewModel.Step.Returns(DayPlannerStep.Overview);
        _viewModel.OverviewText.Returns("Für heute geplant.");
        _viewModel.TodayTasks.Returns([]);
        _viewModel.PastTasks.Returns([]);
        _viewModel.Suggestions.Returns([]);
        _viewModel.DutyTemplates.Returns([]);
        Services.AddSingleton(_viewModel);
        Services.AddSingleton(Substitute.For<ITaskEditPopUpViewModel>());
        Services.AddSingleton(Substitute.For<FokusApp.Components.PopUps.Calendar.ICalendarPopupViewModel>());
    }

    [Fact]
    public void Page_Route_IsTheOneOfTheMenuAndThePlant()
    {
        var routes = typeof(DayPlannerPage).GetCustomAttributes<RouteAttribute>().Select(route => route.Template);

        Assert.Contains(DayPlannerRoutes.Planner, routes);
        Assert.Equal("/tagesplaner", DayPlannerRoutes.Planner);
    }

    [Fact]
    public void Render_InitializesViewModel()
    {
        Render<DayPlannerPage>();

        _viewModel.Received(1).InitializeAsync();
    }

    [Fact]
    public void Render_NotLoadedYet_ShowsNoCard()
    {
        _viewModel.IsLoaded.Returns(false);

        var cut = Render<DayPlannerPage>();

        Assert.Empty(cut.FindAll(".planner-card"));
    }

    [Fact]
    public void Render_Overview_ShowsBarsWithCountsAndTheSentence()
    {
        _viewModel.PlannedForYou.Returns(2);
        _viewModel.PlannedToDo.Returns(3);
        _viewModel.DoneForYou.Returns(1);
        _viewModel.DoneToDo.Returns(1);
        _viewModel.GoalForYou.Returns(2);
        _viewModel.GoalToDo.Returns(3);
        _viewModel.RequiredForYou.Returns(2);
        _viewModel.AllowedToDo.Returns(3);
        _viewModel.ForYouDonePercent.Returns(33.3);
        _viewModel.ForYouExistingPercent.Returns(66.7);
        _viewModel.ToDoDonePercent.Returns(33.3);
        _viewModel.ToDoExistingPercent.Returns(100);

        var cut = Render<DayPlannerPage>();

        Assert.Equal("2", cut.Find(".planner-count-for-you").TextContent.Trim());
        Assert.Equal("3", cut.Find(".planner-count-todo").TextContent.Trim());
        Assert.Equal(Text("PlannerGoalLine", 2, 3), cut.Find(".planner-goal-line").TextContent.Trim());
        Assert.Equal("Für heute geplant.", cut.Find(".planner-overview-text").TextContent.Trim());
        Assert.Equal(Text("PlannerChartLabel", 2, 3, 1, 1, 2, 3), cut.Find(".planner-chart").GetAttribute("aria-label"));
        Assert.Equal("img", cut.Find(".planner-chart").GetAttribute("role"));
    }

    [Fact]
    public void Render_Overview_DoneTasksFillTheBarAndNewlyPlannedOnesSitDashedOnTop()
    {
        _viewModel.ForYouDonePercent.Returns(25);
        _viewModel.ForYouExistingPercent.Returns(50);
        _viewModel.ForYouPlannedStartPercent.Returns(50);
        _viewModel.ForYouPlannedPercent.Returns(25);
        _viewModel.ToDoDonePercent.Returns(50);
        _viewModel.ToDoExistingPercent.Returns(75);
        _viewModel.ToDoPlannedStartPercent.Returns(75);
        _viewModel.ToDoPlannedPercent.Returns(33.3);

        var cut = Render<DayPlannerPage>();

        Assert.Equal("height: 25%", cut.Find(".planner-bar-for-you-done").GetAttribute("style"));
        Assert.Equal("bottom: 50%; height: 25%", cut.Find(".planner-bar-for-you-planned").GetAttribute("style"));
        Assert.Equal("height: 50%", cut.Find(".planner-bar-todo-done").GetAttribute("style"));
        Assert.Equal("bottom: 75%; height: 33.3%", cut.Find(".planner-bar-todo-planned").GetAttribute("style"));
        var doneBars = cut.FindAll("[data-testid='planner-bar-done']");
        var plannedBars = cut.FindAll("[data-testid='planner-bar-planned']");
        Assert.Equal(2, doneBars.Count);
        Assert.Equal(2, plannedBars.Count);
        Assert.DoesNotContain(doneBars, bar => plannedBars.Contains(bar));
    }

    [Fact]
    public void Render_Overview_ExistingTasksStandGreyBehindTheDoneOnesInTheSameBar()
    {
        _viewModel.ForYouDonePercent.Returns(25);
        _viewModel.ForYouExistingPercent.Returns(75);
        _viewModel.ToDoDonePercent.Returns(50);
        _viewModel.ToDoExistingPercent.Returns(100);

        var cut = Render<DayPlannerPage>();

        var forYouTrack = cut.FindAll(".planner-bar-track")[0];
        var toDoTrack = cut.FindAll(".planner-bar-track")[1];
        Assert.Equal("height: 75%", forYouTrack.QuerySelector(".planner-bar-existing")!.GetAttribute("style"));
        Assert.Equal("height: 100%", toDoTrack.QuerySelector(".planner-bar-existing")!.GetAttribute("style"));
        Assert.Equal(["planner-bar-existing", "planner-bar-done"], BarKinds(forYouTrack));
        Assert.Equal(["planner-bar-existing", "planner-bar-done"], BarKinds(toDoTrack));
    }

    [Fact]
    public void Render_Overview_NothingNewlyPlanned_ShowsNoDashedPart()
    {
        _viewModel.ForYouDonePercent.Returns(100);
        _viewModel.ToDoDonePercent.Returns(100);

        var cut = Render<DayPlannerPage>();

        Assert.Empty(cut.FindAll(".planner-bar-planned"));
        Assert.Equal(2, cut.FindAll(".planner-bar-done").Count);
        Assert.Equal(2, cut.FindAll(".planner-bar-existing").Count);
    }

    [Fact]
    public void Render_Overview_GoalIsMarkedLeftOfTheForYouBarAndRightOfTheToDoBar()
    {
        _viewModel.RequiredForYou.Returns(2);
        _viewModel.AllowedToDo.Returns(3);
        _viewModel.ForYouGoalPercent.Returns(66.7);
        _viewModel.ToDoGoalPercent.Returns(100);

        var cut = Render<DayPlannerPage>();

        var forYouGoal = cut.Find(".planner-goal-for-you");
        var toDoGoal = cut.Find(".planner-goal-todo");
        Assert.Equal("bottom: 66.7%", forYouGoal.GetAttribute("style"));
        Assert.Equal("bottom: 100%", toDoGoal.GetAttribute("style"));
        Assert.Equal("planner-goal-tick-left", forYouGoal.GetAttribute("data-testid"));
        Assert.Equal("planner-goal-tick-right", toDoGoal.GetAttribute("data-testid"));
        Assert.Equal(Text("PlannerGoalMark"), forYouGoal.QuerySelector(".planner-goal-label")!.TextContent.Trim());
        Assert.Equal(Text("PlannerGoalMark"), toDoGoal.QuerySelector(".planner-goal-label")!.TextContent.Trim());
        Assert.Equal(["planner-goal-label", "planner-goal-mark"], forYouGoal.Children.Select(child => child.ClassName));
        Assert.Equal(["planner-goal-mark", "planner-goal-label"], toDoGoal.Children.Select(child => child.ClassName));
    }

    [Fact]
    public void Render_Overview_NoToDoFitsYet_ShowsNoGoalMarkAtTheToDoBar()
    {
        _viewModel.AllowedToDo.Returns(0);

        var cut = Render<DayPlannerPage>();

        Assert.Empty(cut.FindAll(".planner-goal-todo"));
        Assert.Single(cut.FindAll(".planner-goal-for-you"));
    }

    [Fact]
    public void Render_Overview_HasNoMarkerLineInTheBarsAndNoExplanationOfIt()
    {
        var cut = Render<DayPlannerPage>();

        Assert.Empty(cut.FindAll(".planner-goal-marker"));
        Assert.Empty(cut.FindAll(".planner-bar .planner-goal-tick"));
        Assert.DoesNotContain("Strich", cut.Find(".planner-goal-line").TextContent);
    }

    [Fact]
    public void Render_Overview_HasNoRingAndNoCentralTotal()
    {
        var cut = Render<DayPlannerPage>();

        Assert.Empty(cut.FindAll(".planner-ring"));
        Assert.Empty(cut.FindAll(".planner-total"));
        Assert.Empty(cut.FindAll("svg"));
    }

    [Fact]
    public void Render_Overview_DistinguishesForYouAndToDoByLabelNotByColour()
    {
        var cut = Render<DayPlannerPage>();

        Assert.Single(cut.FindAll(".planner-bar-for-you-done"));
        Assert.Single(cut.FindAll(".planner-bar-todo-done"));
        var names = cut.FindAll(".planner-chart-name").Select(name => name.TextContent.Trim());
        Assert.Equal([Text("TaskTypeForYou"), Text("TaskTypeToDo")], names);
    }

    [Fact]
    public void Render_OverviewNothingPlanned_ShowsEmptyBarsAndNoTaskList()
    {
        var cut = Render<DayPlannerPage>();

        Assert.Equal(2, cut.FindAll(".planner-bar-track").Count);
        Assert.Contains("height: 0%", cut.Find(".planner-bar-for-you-done").GetAttribute("style"));
        Assert.Empty(cut.FindAll(".planner-tasks"));
    }

    [Fact]
    public void Render_OverviewWithTasks_ListsThemWithTheirKind()
    {
        _viewModel.TodayTasks.Returns(
        [
            new DayPlannerTask(Guid.NewGuid(), "Spazieren", true),
            new DayPlannerTask(Guid.NewGuid(), "Steuer", false)
        ]);

        var cut = Render<DayPlannerPage>();

        var items = cut.FindAll(".planner-task");
        Assert.Equal(2, items.Count);
        Assert.Equal("Spazieren", items[0].QuerySelector(".planner-task-text")!.TextContent.Trim());
        Assert.Equal(Text("TaskTypeForYou"), items[0].QuerySelector(".planner-task-kind")!.TextContent.Trim());
        Assert.Equal(Text("TaskTypeToDo"), items[1].QuerySelector(".planner-task-kind")!.TextContent.Trim());
    }

    [Fact]
    public void Render_Overview_ShowsNextButOffersNeitherSkipNorBack()
    {
        var cut = Render<DayPlannerPage>();

        Assert.Single(cut.FindAll(".planner-next"));
        Assert.Empty(cut.FindAll(".planner-skip"));
        Assert.Empty(cut.FindAll(".planner-back"));
        Assert.Empty(cut.FindAll(".planner-finish"));
    }

    [Fact]
    public void Click_NextAndBack_CallTheViewModel()
    {
        _viewModel.Step.Returns(DayPlannerStep.Assign);
        _viewModel.CanGoBack.Returns(true);
        var cut = Render<DayPlannerPage>();

        cut.Find(".planner-next").Click();
        cut.Find(".planner-back").Click();

        _viewModel.Received(1).Next();
        _viewModel.Received(1).Back();
    }

    [Theory]
    [InlineData(DayPlannerStep.Overview)]
    [InlineData(DayPlannerStep.Assign)]
    [InlineData(DayPlannerStep.ForYou)]
    [InlineData(DayPlannerStep.Duties)]
    [InlineData(DayPlannerStep.Done)]
    public void Render_AnyStep_OffersNoSkipButton(DayPlannerStep step)
    {
        _viewModel.Step.Returns(step);
        _viewModel.CanGoBack.Returns(true);

        var cut = Render<DayPlannerPage>();

        Assert.Empty(cut.FindAll(".planner-skip"));
        Assert.DoesNotContain("Überspringen", cut.Markup);
    }

    [Fact]
    public void Render_Assign_ListsPastTasksWithTheFourChipsAndTheBulkButton()
    {
        _viewModel.Step.Returns(DayPlannerStep.Assign);
        _viewModel.PastTasks.Returns([new DayPlannerTask(_pastId, "Alt", false)]);

        var cut = Render<DayPlannerPage>();

        var item = cut.Find(".planner-past-task");
        Assert.Equal("Alt", item.QuerySelector(".planner-task-text")!.TextContent.Trim());
        Assert.Equal(Text("AssignToday"), item.QuerySelector(".planner-chip-today")!.TextContent.Trim());
        Assert.Equal(Text("AssignTomorrow"), item.QuerySelector(".planner-chip-tomorrow")!.TextContent.Trim());
        Assert.Equal(Text("AssignOtherDay"), item.QuerySelector(".planner-chip-other-day")!.TextContent.Trim());
        Assert.Equal(Text("AssignDiscard"), item.QuerySelector(".planner-chip-discard")!.TextContent.Trim());
        Assert.Equal(Text("AssignAllTomorrow"), cut.Find(".planner-all-tomorrow").TextContent.Trim());
    }

    [Fact]
    public void Click_Chips_AssignTheTaskToTheRightTarget()
    {
        _viewModel.Step.Returns(DayPlannerStep.Assign);
        _viewModel.PastTasks.Returns([new DayPlannerTask(_pastId, "Alt", false)]);
        var cut = Render<DayPlannerPage>();

        cut.Find(".planner-chip-today").Click();
        cut.Find(".planner-chip-tomorrow").Click();
        cut.Find(".planner-chip-other-day").Click();
        cut.Find(".planner-chip-discard").Click();
        cut.Find(".planner-all-tomorrow").Click();

        _viewModel.Received(1).AssignPastTaskAsync(_pastId, PastTaskTarget.Today);
        _viewModel.Received(1).AssignPastTaskAsync(_pastId, PastTaskTarget.Tomorrow);
        _viewModel.Received(1).OpenReschedulePopup(_pastId);
        _viewModel.Received(1).DiscardPastTaskAsync(_pastId);
        _viewModel.Received(1).AssignAllPastTasksToTomorrowAsync();
    }

    [Fact]
    public void Render_ReschedulePopupOpen_RendersTheCalendarPopup()
    {
        _viewModel.Step.Returns(DayPlannerStep.Assign);
        _viewModel.IsReschedulePopupOpen.Returns(true);

        var cut = Render<DayPlannerPage>();

        Assert.NotEmpty(cut.FindAll(".calendar-popup-shell"));
    }

    [Fact]
    public void Render_CurrentUndoSet_ShowsTheUndoNoticeWithAButton()
    {
        _viewModel.CurrentUndo.Returns(new UndoNotice(UndoNoticeKind.TaskDiscarded, 1));

        var cut = Render<DayPlannerPage>();
        cut.Find(".planner-undo-button").Click();

        _viewModel.Received(1).UndoAsync();
    }

    [Fact]
    public void Render_AssignEverythingAssigned_ShowsAFriendlyNoteInsteadOfTheList()
    {
        _viewModel.Step.Returns(DayPlannerStep.Assign);

        var cut = Render<DayPlannerPage>();

        Assert.Equal(Text("PlannerAssignDone"), cut.Find(".planner-assign-done").TextContent.Trim());
        Assert.Empty(cut.FindAll(".planner-all-tomorrow"));
    }

    [Fact]
    public void Render_ForYou_ShowsAllSuggestionsInCollapsedCategoryGroups()
    {
        _viewModel.Step.Returns(DayPlannerStep.ForYou);
        _viewModel.Suggestions.Returns(
        [
            new ForYouSuggestionItem("Yoga", false, null, Guid.NewGuid()),
            new ForYouSuggestionItem("Mein Vorschlag", true, null),
            new ForYouSuggestionItem("Spaziergang", false, ForYouSuggestionCategory.Movement),
            new ForYouSuggestionItem("Dehnen", false, ForYouSuggestionCategory.Movement),
            new ForYouSuggestionItem("Lesen", false, ForYouSuggestionCategory.Calm)
        ]);

        var cut = Render<DayPlannerPage>();

        var groups = cut.FindAll("details.planner-group");
        Assert.Equal(
            [Text("ForYouCategoryTemplate"), Text("ForYouCategoryOwn"), Text("ForYouCategoryMovement"), Text("ForYouCategoryCalm")],
            groups.Select(group => group.QuerySelector(".planner-group-title")!.TextContent.Trim()));
        Assert.Equal(["1", "1", "2", "1"], groups.Select(group => group.QuerySelector(".planner-group-count")!.TextContent.Trim()));
        Assert.All(groups, group => Assert.False(group.HasAttribute("open")));
        Assert.Equal(
            ["Spaziergang", "Dehnen"],
            groups[2].QuerySelectorAll(".planner-suggestion-text").Select(item => item.TextContent.Trim()));
        Assert.Equal(5, cut.FindAll(".planner-group .planner-suggestion").Count);
    }

    [Fact]
    public void Click_Suggestion_PicksIt()
    {
        _viewModel.Step.Returns(DayPlannerStep.ForYou);
        _viewModel.Suggestions.Returns([new ForYouSuggestionItem("Spaziergang", false, ForYouSuggestionCategory.Movement)]);
        var cut = Render<DayPlannerPage>();

        cut.Find(".planner-group .planner-suggestion").Click();

        _viewModel.Received(1).PickSuggestion(Arg.Is<ForYouSuggestionItem>(item => item.Text == "Spaziergang"));
    }

    [Fact]
    public void Render_ForYouWithoutPastForYous_ShowsNoHistory()
    {
        _viewModel.Step.Returns(DayPlannerStep.ForYou);
        _viewModel.PastForYous.Returns([]);

        var cut = Render<DayPlannerPage>();

        Assert.Empty(cut.FindAll(".planner-history"));
    }

    [Fact]
    public void Render_ForYouWithPastForYous_ShowsACollapsedListWithTitleAndCountBelowTheSuggestions()
    {
        _viewModel.Step.Returns(DayPlannerStep.ForYou);
        _viewModel.Suggestions.Returns([new ForYouSuggestionItem("Spaziergang", false, ForYouSuggestionCategory.Movement)]);
        _viewModel.PastForYous.Returns(["Yoga", "Lesen"]);

        var cut = Render<DayPlannerPage>();

        var history = cut.Find("details.planner-history");
        Assert.False(history.HasAttribute("open"));
        Assert.Equal(Text("PlannerPastForYousTitle"), history.QuerySelector("summary span")!.TextContent.Trim());
        Assert.Equal("2", history.QuerySelector(".planner-history-count")!.TextContent.Trim());
        var historySuggestionTexts = history.QuerySelectorAll(".planner-suggestion-text").Select(item => item.TextContent.Trim());
        Assert.Equal(["Yoga", "Lesen"], historySuggestionTexts);
        Assert.Equal("planner-group", history.PreviousElementSibling!.GetAttribute("data-testid"));
    }

    [Fact]
    public void Click_PastForYou_PicksItAsAnOwnSuggestionWithoutCategory()
    {
        _viewModel.Step.Returns(DayPlannerStep.ForYou);
        _viewModel.PastForYous.Returns(["Yoga"]);
        var cut = Render<DayPlannerPage>();

        cut.Find(".planner-history .planner-suggestion").Click();

        _viewModel.Received(1).PickSuggestion(Arg.Is<ForYouSuggestionItem>(item =>
            item.Text == "Yoga" && item.Category == null && item.TemplateId == null));
    }

    [Fact]
    public void Render_ForYouWithoutInsight_ShowsNoHintCard()
    {
        _viewModel.Step.Returns(DayPlannerStep.ForYou);

        var cut = Render<DayPlannerPage>();

        Assert.Empty(cut.FindAll(".planner-insight"));
    }

    [Fact]
    public void Render_ForYouWithInsight_ShowsTheHintAndAButtonForANewForYouTask()
    {
        _viewModel.Step.Returns(DayPlannerStep.ForYou);
        _viewModel.Insight.Returns(new DayPlannerInsight("Sonntags planst du gern etwas für dich."));
        var cut = Render<DayPlannerPage>();

        Assert.Equal("Sonntags planst du gern etwas für dich.", cut.Find(".planner-insight-text").TextContent.Trim());
        cut.Find(".planner-insight-action").Click();

        _viewModel.Received(1).OpenTaskEditorForForYou();
        Assert.Equal(Text("WeekdayInsightAction"), cut.Find(".planner-insight-action").TextContent.Trim());
    }

    [Fact]
    public void Render_ForYou_ShowsTheTextOfTheViewModel()
    {
        _viewModel.Step.Returns(DayPlannerStep.ForYou);
        _viewModel.ForYouText.Returns("Hey, toll, dass du weiter planst.");

        var cut = Render<DayPlannerPage>();

        Assert.Equal("Hey, toll, dass du weiter planst.", cut.Find(".planner-foryou-text").TextContent.Trim());
    }

    [Fact]
    public void Render_ForYou_OffersAnOwnForYouTaskAndOpensTheEditorForIt()
    {
        _viewModel.Step.Returns(DayPlannerStep.ForYou);
        var cut = Render<DayPlannerPage>();

        Assert.Equal(Text("PlannerForYouCreate"), cut.Find(".planner-foryou-create").TextContent.Trim());
        cut.Find(".planner-foryou-create").Click();

        _viewModel.Received(1).OpenTaskEditorForForYou();
    }

    [Fact]
    public void Render_Duties_ShowsTitleTextAndAButtonForANewToDo()
    {
        _viewModel.Step.Returns(DayPlannerStep.Duties);
        var cut = Render<DayPlannerPage>();

        Assert.Equal(Text("PlannerDutiesTitle"), cut.Find("h2").TextContent.Trim());
        Assert.Equal(Text("PlannerDutyCreate"), cut.Find(".planner-duty-create").TextContent.Trim());
        cut.Find(".planner-duty-create").Click();

        _viewModel.Received(1).OpenTaskEditorForToDo();
        Assert.Single(cut.FindAll(".planner-next"));
        Assert.Empty(cut.FindAll(".planner-finish"));
    }

    [Fact]
    public void Render_DutiesWithoutTemplates_ShowsNoTemplateList()
    {
        _viewModel.Step.Returns(DayPlannerStep.Duties);

        var cut = Render<DayPlannerPage>();

        Assert.Empty(cut.FindAll(".planner-duty-templates"));
    }

    [Fact]
    public void Render_DutiesWithTemplates_ShowsACollapsedListWithCountAndPicksOne()
    {
        var id = Guid.NewGuid();
        _viewModel.Step.Returns(DayPlannerStep.Duties);
        _viewModel.DutyTemplates.Returns([new DayPlannerTask(id, "Steuer", false), new DayPlannerTask(Guid.NewGuid(), "  ", false)]);
        var cut = Render<DayPlannerPage>();

        var list = cut.Find("details.planner-duty-templates");
        Assert.False(list.HasAttribute("open"));
        Assert.Equal(Text("PlannerDutyTemplatesTitle"), list.QuerySelector(".planner-group-title")!.TextContent.Trim());
        Assert.Equal("2", list.QuerySelector(".planner-group-count")!.TextContent.Trim());
        Assert.Equal(
            ["Steuer", Text("Untitled")],
            list.QuerySelectorAll(".planner-suggestion-text").Select(item => item.TextContent.Trim()));

        cut.Find(".planner-duty-template").Click();

        _viewModel.Received(1).PickDutyTemplate(id);
    }

    [Fact]
    public void Render_Done_ShowsTheConfirmationAndAFinishButtonThatLeaves()
    {
        _viewModel.Step.Returns(DayPlannerStep.Done);
        _viewModel.DoneText.Returns("Dein Tag ist geplant.");
        var cut = Render<DayPlannerPage>();

        Assert.Equal("Dein Tag ist geplant.", cut.Find(".planner-done-text").TextContent.Trim());
        Assert.Empty(cut.FindAll(".planner-next"));

        cut.Find(".planner-finish").Click();

        _viewModel.Received(1).Finish();
    }

    [Fact]
    public void Texts_AllPlannerKeys_ExistAndPoolKeysAreStillThere()
    {
        string[] keys =
        [
            "NavDayPlanner", "PlannerTitle", "PlannerOverviewTitle", "PlannerOverviewEmpty",
            "PlannerAssignTitle", "PlannerAssignText", "PlannerForYouTitle", "PlannerForYouText", "PlannerForYouTextReached", "PlannerForYouCreate",
            "PlannerDutiesTitle", "PlannerDutiesText", "PlannerDutyCreate", "PlannerDutyTemplatesTitle", "PlannerChartLabel",
            "PlannerGoalLine", "PlannerGoalMark", "PlannerDoneTitle",
            "PlannerDoneNothing", "PlannerDonePlanned", "PlannerNext", "PlannerBack", "PlannerFinish",
            "PlannerPastForYousTitle"
        ];

        foreach (var key in keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(Text(key)));
        }

        Assert.NotEmpty(ForYouSuggestionPool.Entries);
    }

    [Fact]
    public void Render_Page_SetsThePageTitle()
    {
        var head = Render<HeadOutlet>();

        Render<DayPlannerPage>();

        head.WaitForAssertion(() => Assert.Contains(Text("PlannerTitle"), head.Markup));
    }

    [Fact]
    public void Render_TodayTaskWithoutNote_ShowsTheUntitledText()
    {
        _viewModel.TodayTasks.Returns([new DayPlannerTask(Guid.NewGuid(), "  ", false)]);

        var cut = Render<DayPlannerPage>();

        Assert.Contains(Text("Untitled"), cut.Markup);
    }

    private static List<string> BarKinds(AngleSharp.Dom.IElement track) =>
        track.Children
            .Where(child => child.ClassList.Contains("planner-bar"))
            .Select(child => child.ClassList.First(name => name is "planner-bar-existing" or "planner-bar-done" or "planner-bar-planned"))
            .ToList();
}
