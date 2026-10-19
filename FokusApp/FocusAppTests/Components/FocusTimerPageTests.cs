using Bunit;
using FokusApp.Components.FocusTimerComponent;
using FokusApp.Components.SharedUI.FilterComponent;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using FilterComponent = FokusApp.Components.SharedUI.FilterComponent.FilterComponent;

namespace FocusAppTests.Components;

public class FocusTimerPageTests : ComponentTestContext
{
    private const string PickerTriggerSelector = "#timer-task-picker-trigger";

    private readonly IFocusTimerViewModel _viewModel = Substitute.For<IFocusTimerViewModel>();
    private readonly FilterModel _taskFilter = new();

    public FocusTimerPageTests()
    {
        IReadOnlyList<TaskDto> noTasks = [];
        _viewModel.NormalTasks.Returns(noTasks);
        _viewModel.RegularTasks.Returns(noTasks);
        _viewModel.SelectedTask.Returns(new TaskDto());
        _viewModel.TaskFilter.Returns(_taskFilter);
        _viewModel.FormattedTime.Returns("25:00");
        _viewModel.InputMinutes.Returns(25);

        Services.AddSingleton(_viewModel);
        Services.AddSingleton(Substitute.For<IUiBackPressService>());
        AddFilterComponentServices();
    }

    [Fact]
    public void Render_InitializesViewModel()
    {
        Render<FocusTimerPage>();

        _viewModel.Received(1).InitializeAsync();
    }

    [Fact]
    public void Render_PickerClosed_ShowsNoFilter()
    {
        var cut = Render<FocusTimerPage>();

        var filters = cut.FindComponents<FilterComponent>();
        Assert.Empty(filters);
    }

    [Fact]
    public void ClickPickerTrigger_PreparesPickerAndShowsFilterComponent()
    {
        var cut = Render<FocusTimerPage>();
        var trigger = cut.Find(PickerTriggerSelector);

        trigger.Click();

        var filter = cut.FindComponent<FilterComponent>();
        _viewModel.Received(1).PrepareTaskPicker();
        Assert.True(filter.Instance.IsOpen);
        Assert.NotEmpty(cut.FindAll(".timer-task-search"));
    }

    [Fact]
    public void ClickPickerTrigger_ShowsFilterWithoutDateSortToggleAndKeepsFilters()
    {
        var cut = Render<FocusTimerPage>();
        var trigger = cut.Find(PickerTriggerSelector);

        trigger.Click();

        var filter = cut.FindComponent<FilterComponent>();
        Assert.False(filter.Instance.ShowDateSortToggle);
        Assert.False(filter.Instance.ResetFiltersOnOpen);
        Assert.Empty(cut.FindAll(".timer-task-sort-button"));
    }

    [Fact]
    public void ClickPickerTrigger_ShowsFilterWithCurrentFilterOfViewModel()
    {
        _taskFilter.SearchText = "Bericht";
        _taskFilter.SelectedPriorities = [TaskPriority.Hoch];
        _taskFilter.IsSortDescending = false;
        var cut = Render<FocusTimerPage>();
        var trigger = cut.Find(PickerTriggerSelector);

        trigger.Click();

        var filter = cut.FindComponent<FilterComponent>();
        var searchInput = cut.Find(".timer-task-search");
        Assert.Equal("Bericht", filter.Instance.InitialSearchText);
        Assert.Equal([TaskPriority.Hoch], filter.Instance.InitialSelectedPriorities);
        Assert.False(filter.Instance.InitialIsSortDescending);
        Assert.Equal("Bericht", searchInput.GetAttribute("value"));
    }

    [Fact]
    public void ClickPickerTriggerTwice_ClosesFilterComponent()
    {
        var cut = Render<FocusTimerPage>();
        var trigger = cut.Find(PickerTriggerSelector);
        trigger.Click();

        trigger.Click();

        var filters = cut.FindComponents<FilterComponent>();
        Assert.Empty(filters);
    }

    [Fact]
    public void EnterSearchText_InFilter_AppliesFilterChangesAtViewModel()
    {
        var cut = Render<FocusTimerPage>();
        var trigger = cut.Find(PickerTriggerSelector);
        trigger.Click();
        var searchInput = cut.Find(".timer-task-search");

        searchInput.Input("Bericht");

        cut.WaitForAssertion(() => _viewModel.Received().ApplyFilterChangesAsync(
            Arg.Is<FilterModel>(payload => payload.SearchText == "Bericht")));
    }

    [Fact]
    public void ClickPriority_InFilter_AppliesFilterChangesAtViewModel()
    {
        var cut = Render<FocusTimerPage>();
        var trigger = cut.Find(PickerTriggerSelector);
        trigger.Click();
        var priorityButtons = cut.FindAll(".priority-button");

        priorityButtons[0].Click();

        cut.WaitForAssertion(() => _viewModel.Received().ApplyFilterChangesAsync(
            Arg.Is<FilterModel>(payload => payload.SelectedPriorities.Count == 1)));
    }

    [Fact]
    public void Render_BreakMode_ShowsNoTaskPicker()
    {
        _viewModel.IsBreakMode.Returns(true);

        var cut = Render<FocusTimerPage>();

        var triggers = cut.FindAll(PickerTriggerSelector);
        var filters = cut.FindComponents<FilterComponent>();
        Assert.Empty(triggers);
        Assert.Empty(filters);
    }
}
