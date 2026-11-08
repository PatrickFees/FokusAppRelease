using Bunit;
using FocusAppTests.ViewModels;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Components.SharedUI.FilterComponent;
using FokusApp.Components.TaskOverViewComponent;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using FilterComponent = FokusApp.Components.SharedUI.FilterComponent.FilterComponent;

namespace FocusAppTests.Components;

public class TaskOverViewPageTests : ComponentTestContext
{
    private readonly ITaskOverViewViewModel _viewModel = Substitute.For<ITaskOverViewViewModel>();
    private readonly FilterModel _filter = new();

    public TaskOverViewPageTests()
    {
        IReadOnlyList<TaskDto> noTasks = [];
        _viewModel.ActiveTasks.Returns(noTasks);
        _viewModel.Templates.Returns(noTasks);
        _viewModel.CompletedTasks.Returns(noTasks);
        _viewModel.Filter.Returns(_filter);

        Services.AddSingleton(_viewModel);
        Services.AddSingleton(Substitute.For<ITaskEditPopUpViewModel>());
        AddFilterComponentServices();
    }

    [Fact]
    public void Render_InitializesViewModel()
    {
        Render<TaskOverViewPage>();

        _viewModel.Received(1).InitializeAsync();
    }

    [Fact]
    public void Render_FilterPanelClosed_ShowsNoFilterAndShowFiltersText()
    {
        var cut = Render<TaskOverViewPage>();

        var toggle = cut.Find(".task-filter-toggle");
        var filters = cut.FindComponents<FilterComponent>();
        Assert.Equal(Text("ShowFilters"), toggle.TextContent.Trim());
        Assert.Empty(filters);
    }

    [Fact]
    public void ClickFilterToggle_TogglesFilterPanel()
    {
        var cut = Render<TaskOverViewPage>();
        var toggle = cut.Find(".task-filter-toggle");

        toggle.Click();

        _viewModel.Received(1).ToggleFilterPanel();
    }

    [Fact]
    public void Render_FilterPanelOpen_ShowsFilterComponentWithDateSortToggle()
    {
        _viewModel.IsFilterPanelOpen.Returns(true);

        var cut = Render<TaskOverViewPage>();

        var filter = cut.FindComponent<FilterComponent>();
        var toggle = cut.Find(".task-filter-toggle");
        var sortButtons = cut.FindAll(".timer-task-sort-button");
        Assert.True(filter.Instance.IsOpen);
        Assert.True(filter.Instance.ShowDateSortToggle);
        Assert.False(filter.Instance.ResetFiltersOnOpen);
        Assert.Single(sortButtons);
        Assert.Equal(Text("HideFilters"), toggle.TextContent.Trim());
    }

    [Fact]
    public void Render_FilterPanelOpen_ShowsFilterWithCurrentFilterOfViewModel()
    {
        _viewModel.IsFilterPanelOpen.Returns(true);
        _filter.SearchText = "Bericht";
        _filter.SelectedPriorities = [TaskPriority.Dringend];
        _filter.IsSortDescending = false;

        var cut = Render<TaskOverViewPage>();

        var filter = cut.FindComponent<FilterComponent>();
        var searchInput = cut.Find(".timer-task-search");
        Assert.Equal("Bericht", filter.Instance.InitialSearchText);
        Assert.Equal([TaskPriority.Dringend], filter.Instance.InitialSelectedPriorities);
        Assert.False(filter.Instance.InitialIsSortDescending);
        Assert.Equal("Bericht", searchInput.GetAttribute("value"));
    }

    [Fact]
    public void ClickBackdrop_ClosesFilterPanel()
    {
        _viewModel.IsFilterPanelOpen.Returns(true);
        var cut = Render<TaskOverViewPage>();
        var backdrop = cut.Find(".task-filter-backdrop");

        backdrop.Click();

        _viewModel.Received(1).CloseFilterPanel();
    }

    [Fact]
    public void EnterSearchText_InFilter_AppliesOverviewFiltersAtViewModel()
    {
        _viewModel.IsFilterPanelOpen.Returns(true);
        var cut = Render<TaskOverViewPage>();
        var searchInput = cut.Find(".timer-task-search");

        searchInput.Input("Bericht");

        cut.WaitForAssertion(() => _viewModel.Received().ApplyOverviewFilters(
            Arg.Is<FilterModel>(payload => payload.SearchText == "Bericht")));
    }

    [Fact]
    public void ClickSortToggle_InFilter_AppliesOverviewFiltersWithChangedSortOrder()
    {
        _viewModel.IsFilterPanelOpen.Returns(true);
        var cut = Render<TaskOverViewPage>();
        var sortButton = cut.Find(".timer-task-sort-button");

        sortButton.Click();

        cut.WaitForAssertion(() => _viewModel.Received().ApplyOverviewFilters(
            Arg.Is<FilterModel>(payload => !payload.IsSortDescending)));
    }

    [Fact]
    public void Render_PastTask_ShowsChipsPermanently()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Today.AddDays(-1));
        IReadOnlyList<TaskDto> overdue = [past];
        _viewModel.OverdueTasks.Returns(overdue);
        var cut = Render<TaskOverViewPage>();

        var chips = cut.FindAll(".task-assign-chip").Select(chip => chip.TextContent.Trim()).ToArray();
        Assert.Equal(
            [Text("AssignToday"), Text("AssignTomorrow"), Text("AssignOtherDay"), Text("AssignDiscard")],
            chips);
    }

    [Fact]
    public void Render_RunningPastTask_ShowsNoChips()
    {
        var past = TestTasks.Create("Läuft", taskDate: TestTasks.Today.AddDays(-1));
        IReadOnlyList<TaskDto> overdue = [past];
        _viewModel.OverdueTasks.Returns(overdue);
        _viewModel.IsTaskActive(past.Id).Returns(true);
        var cut = Render<TaskOverViewPage>();

        Assert.Empty(cut.FindAll(".task-assign-chip"));
    }

    [Fact]
    public void ClickTomorrowChip_AssignsTaskToTomorrow()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Today.AddDays(-1));
        IReadOnlyList<TaskDto> overdue = [past];
        _viewModel.OverdueTasks.Returns(overdue);
        var cut = Render<TaskOverViewPage>();

        cut.FindAll(".task-assign-chip")[1].Click();

        cut.WaitForAssertion(() => _viewModel.Received(1).AssignPastTaskAsync(past.Id!.Value, PastTaskTarget.Tomorrow));
    }

    [Fact]
    public void ClickOtherDayChip_OpensReschedulePopup()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Today.AddDays(-1));
        IReadOnlyList<TaskDto> overdue = [past];
        _viewModel.OverdueTasks.Returns(overdue);
        var cut = Render<TaskOverViewPage>();

        cut.FindAll(".task-assign-chip")[2].Click();

        _viewModel.Received(1).OpenReschedulePopup(past.Id!.Value);
    }

    [Fact]
    public void ClickDiscardChip_DiscardsTaskWithoutDialog()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Today.AddDays(-1));
        IReadOnlyList<TaskDto> overdue = [past];
        _viewModel.OverdueTasks.Returns(overdue);
        var cut = Render<TaskOverViewPage>();

        cut.FindAll(".task-assign-chip")[3].Click();

        cut.WaitForAssertion(() => _viewModel.Received(1).DiscardPastTaskAsync(past.Id!.Value));
    }

    [Fact]
    public void Render_PastTask_HasPastStyleWithoutOverdueLabel()
    {
        var past = TestTasks.Create("Gestern", taskDate: TestTasks.Today.AddDays(-1));
        IReadOnlyList<TaskDto> overdue = [past];
        _viewModel.OverdueTasks.Returns(overdue);

        var cut = Render<TaskOverViewPage>();

        Assert.Single(cut.FindAll(".task-item.is-past"));
        Assert.DoesNotContain("überfällig", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("versäumt", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClickAssignAll_AssignsAllPastTasksToTomorrow()
    {
        IReadOnlyList<TaskDto> overdue = [TestTasks.Create("Gestern", taskDate: TestTasks.Today.AddDays(-1))];
        _viewModel.OverdueTasks.Returns(overdue);
        var cut = Render<TaskOverViewPage>();

        cut.Find(".task-assign-all").Click();

        _viewModel.Received(1).AssignAllPastTasksToTomorrowAsync();
    }

    [Fact]
    public void Render_NoPastTasks_ShowsNoAssignAllButton()
    {
        var cut = Render<TaskOverViewPage>();

        Assert.Empty(cut.FindAll(".task-assign-all"));
    }

    [Fact]
    public void Render_UndoNotice_ShowsTextAndUndoButton()
    {
        _viewModel.CurrentUndo.Returns(new UndoNotice(UndoNoticeKind.TaskDiscarded, 1));
        var cut = Render<TaskOverViewPage>();

        var notice = cut.Find(".task-undo-notice");
        Assert.Contains(Text("UndoTaskDiscarded"), notice.TextContent);

        cut.Find(".task-undo-button").Click();

        _viewModel.Received(1).UndoAsync();
    }

    [Fact]
    public void Render_NoNoticeState_ShowsNoUndo()
    {
        var cut = Render<TaskOverViewPage>();

        Assert.Empty(cut.FindAll(".task-undo-notice"));
    }

    [Fact]
    public void Render_NoQuery_ShowsAllSectionsOpen()
    {
        var cut = Render<TaskOverViewPage>();

        Assert.All(cut.FindAll("details"), details => Assert.True(details.HasAttribute("open")));
    }

    [Fact]
    public void Render_PastSectionQuery_OpensOnlyPastSection()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/aufgaben?bereich=vergangenheit");

        var cut = Render<TaskOverViewPage>();

        var open = cut.FindAll("details").Where(details => details.HasAttribute("open")).ToArray();
        var single = Assert.Single(open);
        Assert.Contains(Text("OverdueTasksCount", 0), single.QuerySelector("summary")!.TextContent);
    }
    [Fact]
    public void ClickAddTask_OpensAddNoteModal()
    {
        var cut = Render<TaskOverViewPage>();
        var addButton = cut.Find(".task-header button");

        addButton.Click();

        _viewModel.Received(1).OpenAddNoteModal();
    }

    [Fact]
    public void Render_Page_SetsThePageTitle()
    {
        var head = Render<HeadOutlet>();

        Render<TaskOverViewPage>();

        head.WaitForAssertion(() => Assert.Contains(Text("Tasks"), head.Markup));
    }

    [Fact]
    public void Render_TasksInEverySection_ListsThemInTheirOwnSection()
    {
        IReadOnlyList<TaskDto> active = [TestTasks.Create("Aktiv"), TestTasks.Create("Aktiv zwei")];
        IReadOnlyList<TaskDto> template = [TestTasks.Create("Vorlage", isTemplate: true)];
        IReadOnlyList<TaskDto> completed = [TestTasks.Create("Fertig", isDone: true)];
        _viewModel.ActiveTasks.Returns(active);
        _viewModel.Templates.Returns(template);
        _viewModel.CompletedTasks.Returns(completed);

        var cut = Render<TaskOverViewPage>();

        var sections = cut.FindAll(".task-section");
        Assert.Equal(2, sections[0].QuerySelectorAll(".task-item").Length);
        Assert.Single(sections[1].QuerySelectorAll(".task-item"));
        Assert.Empty(sections[2].QuerySelectorAll(".task-item"));
        Assert.Single(sections[3].QuerySelectorAll(".task-item"));
        Assert.Contains(Text("CompletedTasksCount", 1), sections[3].QuerySelector("summary")!.TextContent);
    }

    [Fact]
    public void Render_RunningTask_IsMarkedAsRunningInItsSection()
    {
        var running = TestTasks.Create("Läuft");
        IReadOnlyList<TaskDto> active = [running];
        _viewModel.ActiveTasks.Returns(active);
        _viewModel.IsTaskActive(running.Id).Returns(true);

        var cut = Render<TaskOverViewPage>();

        Assert.Single(cut.FindAll(".task-action.running"));
    }

    [Fact]
    public void ClickEditAndDelete_TaskInSection_ForwardsToTheViewModel()
    {
        var task = TestTasks.Create("Aktiv");
        IReadOnlyList<TaskDto> active = [task];
        _viewModel.ActiveTasks.Returns(active);
        var cut = Render<TaskOverViewPage>();

        cut.Find(".task-action.edit").Click();
        cut.Find(".task-action.delete").Click();

        _viewModel.Received(1).StartEditTask(task.Id!.Value);
        _viewModel.Received(1).DeleteTaskAsync(task.Id!.Value);
    }

    [Fact]
    public void Render_UndoNoticeForMovedTasks_ShowsTheCountInTheText()
    {
        _viewModel.CurrentUndo.Returns(new UndoNotice(UndoNoticeKind.TasksMovedToTomorrow, 3));

        var cut = Render<TaskOverViewPage>();

        var notice = cut.Find(".task-undo-notice span");
        Assert.Equal(Text("UndoTasksMovedToTomorrow", 3), notice.TextContent);
    }

    [Fact]
    public void Render_UndoNoticeOfUnknownKind_ShowsNoText()
    {
        _viewModel.CurrentUndo.Returns(new UndoNotice((UndoNoticeKind)99, 1));

        var cut = Render<TaskOverViewPage>();

        var notice = cut.Find(".task-undo-notice span");
        Assert.Equal(string.Empty, notice.TextContent);
    }

    [Fact]
    public void Render_RescheduleOpen_ShowsTheCalendarForTheInitialDate()
    {
        _viewModel.IsReschedulePopupOpen.Returns(true);
        _viewModel.RescheduleInitialDate.Returns(new DateTime(2026, 10, 20));
        var calendar = Services.GetRequiredService<FokusApp.Components.PopUps.Calendar.ICalendarPopupViewModel>();
        calendar.VisibleMonth.Returns(new DateTime(2026, 10, 1));

        var cut = Render<TaskOverViewPage>();

        var popup = cut.FindComponent<FokusApp.Components.PopUps.Calendar.CalendarPopup>();
        Assert.True(popup.Instance.IsOpen);
        Assert.Equal(new DateTime(2026, 10, 20), popup.Instance.InitialDate);
    }
}
