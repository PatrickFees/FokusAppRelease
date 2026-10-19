using Bunit;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Components.SharedUI.FilterComponent;
using FokusApp.Components.TaskOverViewComponent;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
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
        _viewModel.RegularTasks.Returns(noTasks);
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
    public void ClickAddTask_OpensAddNoteModal()
    {
        var cut = Render<TaskOverViewPage>();
        var addButton = cut.Find(".task-header button");

        addButton.Click();

        _viewModel.Received(1).OpenAddNoteModal();
    }
}
