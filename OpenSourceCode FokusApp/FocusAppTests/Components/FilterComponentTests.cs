using System.Globalization;
using Bunit;
using FokusApp.Components.PopUps.Calendar;
using FokusApp.Components.SharedUI.FilterComponent;
using FokusApp.Core.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FocusAppTests.Components;

public class FilterComponentTests : ComponentTestContext
{
    private static readonly DateTime Today = new(2026, 10, 13);

    private readonly List<FilterModel> _payloads = [];
    private readonly ICalendarPopupViewModel _calendar;

    public FilterComponentTests()
    {
        AddFilterComponentServices();
        _calendar = Services.GetRequiredService<ICalendarPopupViewModel>();
        _calendar.VisibleMonth.Returns(new DateTime(2026, 10, 1));
        _calendar.SelectedDate.Returns(new DateTime(2026, 10, 20));
    }

    [Fact]
    public void Render_Closed_ShowsNothing()
    {
        var cut = RenderFilter(isOpen: false);

        Assert.Equal(string.Empty, cut.Markup.Trim());
    }

    [Fact]
    public void Render_Open_ShowsSearchFieldPriorityButtonsAndResetButton()
    {
        var cut = RenderFilter(isOpen: true);

        var search = cut.Find(".timer-task-search");
        var priorityButtons = cut.FindAll(".priority-button");
        var reset = cut.Find(".timer-task-reset-button");
        Assert.Equal(Text("SearchTask"), search.GetAttribute("placeholder"));
        Assert.Equal(Enum.GetValues<TaskPriority>().Length, priorityButtons.Count);
        Assert.Equal(Text("ResetFilters"), reset.TextContent.Trim());
    }

    [Fact]
    public void Render_OpenWithResetOnOpen_StartsWithTodayAndClearedFilters()
    {
        var cut = RenderFilter(isOpen: true, resetFiltersOnOpen: true, initialSearchText: "Alt");

        var dayButton = cut.Find(".task-day-button");
        Assert.Equal(Today.ToString("D", CultureInfo.CurrentCulture), dayButton.TextContent.Trim());
        Assert.Equal(string.Empty, cut.Find(".timer-task-search").GetAttribute("value"));
    }

    [Fact]
    public void Render_OpenWithInitialValues_TakesTheValuesOver()
    {
        var cut = RenderFilter(
            isOpen: true,
            resetFiltersOnOpen: false,
            initialSearchText: "Bericht",
            initialPriorities: [TaskPriority.Hoch],
            initialDate: new DateTime(2026, 10, 5));

        Assert.Equal("Bericht", cut.Find(".timer-task-search").GetAttribute("value"));
        var selected = Assert.Single(cut.FindAll(".priority-button.selected"));
        Assert.Contains(nameof(TaskPriority.Hoch), selected.TextContent);
        Assert.Equal(new DateTime(2026, 10, 5).ToString("D", CultureInfo.CurrentCulture), cut.Find(".task-day-button").TextContent.Trim());
    }

    [Fact]
    public void Render_NoDayFilter_ShowsAllDaysOnTheDayButtonAndNoClearButton()
    {
        var cut = RenderFilter(isOpen: true, resetFiltersOnOpen: false, initialDate: null);

        Assert.Equal(Text("AllDays"), cut.Find(".task-day-button").TextContent.Trim());
        Assert.Empty(cut.FindAll(".timer-task-filter-actions .timer-task-sort-button"));
    }

    [Fact]
    public void ClickAllDays_DayFilterActive_ClearsTheDayAndReportsIt()
    {
        var cut = RenderFilter(isOpen: true, resetFiltersOnOpen: false, initialDate: new DateTime(2026, 10, 5));

        cut.Find(".timer-task-filter-actions .timer-task-sort-button").Click();

        cut.WaitForAssertion(() => Assert.Contains(_payloads, payload => payload.SelectedDate is null));
        Assert.Equal(Text("AllDays"), cut.Find(".task-day-button").TextContent.Trim());
    }

    [Fact]
    public void Render_ShowDateSortToggle_OffersSortOrderToggle()
    {
        var cut = RenderFilter(isOpen: true, showDateSortToggle: true, resetFiltersOnOpen: false, initialDate: null);

        var toggle = cut.Find(".timer-task-filter-actions .timer-task-sort-button");
        Assert.Equal(Text("DateNewestFirst"), toggle.TextContent.Trim());

        toggle.Click();

        cut.WaitForAssertion(() => Assert.Equal(Text("DateOldestFirst"), cut.Find(".timer-task-filter-actions .timer-task-sort-button").TextContent.Trim()));
        Assert.Contains(_payloads, payload => !payload.IsSortDescending);
    }

    [Fact]
    public void EnterSearchText_Text_ReportsTheFilter()
    {
        var cut = RenderFilter(isOpen: true);

        cut.Find(".timer-task-search").Input("Bericht");

        cut.WaitForAssertion(() => Assert.Contains(_payloads, payload => payload.SearchText == "Bericht"));
    }

    [Fact]
    public void EnterSearchText_NoValue_ClearsTheSearchText()
    {
        var cut = RenderFilter(isOpen: true, resetFiltersOnOpen: false, initialSearchText: "Alt");

        cut.Find(".timer-task-search").TriggerEvent("oninput", new ChangeEventArgs { Value = null });

        cut.WaitForAssertion(() => Assert.Contains(_payloads, payload => payload.SearchText == string.Empty));
    }

    [Fact]
    public void ClickPriority_Twice_SelectsAndDeselectsIt()
    {
        var cut = RenderFilter(isOpen: true);

        cut.FindAll(".priority-button")[1].Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".priority-button.selected")));
        cut.FindAll(".priority-button")[1].Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".priority-button.selected")));
    }

    [Fact]
    public void ClickReset_ActiveFilters_ClearsEverythingAndReportsIt()
    {
        var cut = RenderFilter(
            isOpen: true,
            resetFiltersOnOpen: false,
            initialSearchText: "Bericht",
            initialPriorities: [TaskPriority.Hoch],
            initialDate: new DateTime(2026, 10, 5));

        cut.Find(".timer-task-reset-button").Click();

        cut.WaitForAssertion(() => Assert.Contains(_payloads, payload =>
            payload.SearchText == string.Empty && payload.SelectedPriorities.Count == 0 && payload.SelectedDate is null));
    }

    [Fact]
    public void ClickDayButton_Closed_OpensTheCalendarForTheDisplayedDay()
    {
        var cut = RenderFilter(isOpen: true);

        cut.Find(".task-day-button").Click();

        var calendar = cut.FindComponent<CalendarPopup>();
        Assert.True(calendar.Instance.IsOpen);
        Assert.Equal(Today, calendar.Instance.InitialDate);
    }

    [Fact]
    public void ConfirmCalendar_Date_SelectsThatDayAndClosesTheCalendar()
    {
        var cut = RenderFilter(isOpen: true);
        cut.Find(".task-day-button").Click();

        cut.Find(".calendar-popup-actions .btn-primary").Click();

        var calendar = cut.FindComponent<CalendarPopup>();
        Assert.False(calendar.Instance.IsOpen);
        Assert.Equal(new DateTime(2026, 10, 20).ToString("D", CultureInfo.CurrentCulture), cut.Find(".task-day-button").TextContent.Trim());
    }

    [Fact]
    public void CancelCalendar_Open_ClosesTheCalendarWithoutChangingTheDay()
    {
        var cut = RenderFilter(isOpen: true);
        cut.Find(".task-day-button").Click();

        cut.Find(".calendar-popup-actions .btn-secondary").Click();

        var calendar = cut.FindComponent<CalendarPopup>();
        Assert.False(calendar.Instance.IsOpen);
        Assert.Equal(Today.ToString("D", CultureInfo.CurrentCulture), cut.Find(".task-day-button").TextContent.Trim());
    }

    [Fact]
    public void EnterSearchText_WithoutFilterCallback_DoesNotFail()
    {
        var cut = Render<FilterComponent>(parameters => parameters.Add(filter => filter.IsOpen, true));

        var exception = Record.Exception(() => cut.Find(".timer-task-search").Input("Bericht"));

        Assert.Null(exception);
    }

    [Fact]
    public void Render_ReopenedAfterClose_ShowsTheFilterControlsAgain()
    {
        var cut = RenderFilter(isOpen: true);
        cut.Render(parameters => parameters.Add(filter => filter.IsOpen, false));
        Assert.Empty(cut.FindAll(".timer-task-picker-filters"));

        cut.Render(parameters => parameters.Add(filter => filter.IsOpen, true));

        Assert.NotEmpty(cut.FindAll(".timer-task-picker-filters"));
    }

    [Fact]
    public void Render_ReopenedWithResetOnOpen_RotatesTheSearchFieldName()
    {
        var cut = RenderFilter(isOpen: true);
        var firstName = cut.Find(".timer-task-search").GetAttribute("name");

        cut.Render(parameters => parameters.Add(filter => filter.IsOpen, false));
        cut.Render(parameters => parameters.Add(filter => filter.IsOpen, true));

        var secondName = cut.Find(".timer-task-search").GetAttribute("name");
        Assert.NotEqual(firstName, secondName);
    }

[Fact]    public void Render_Disabled_GreysOutTheFiltersAndBlocksInput()    {        var cut = RenderFilter(isOpen: true, isDisabled: true);        var filter = cut.Find(".timer-task-inline-filter");        Assert.Contains("timer-task-inline-filter-disabled", filter.ClassName);        Assert.True(filter.HasAttribute("inert"));        Assert.Equal("true", filter.GetAttribute("aria-disabled"));    }    [Fact]    public void Render_Enabled_HasNoDisabledMarkers()    {        var cut = RenderFilter(isOpen: true);        var filter = cut.Find(".timer-task-inline-filter");        Assert.DoesNotContain("timer-task-inline-filter-disabled", filter.ClassName);        Assert.False(filter.HasAttribute("inert"));    }
    private IRenderedComponent<FilterComponent> RenderFilter(
        bool isOpen,
        bool showDateSortToggle = false,
        bool resetFiltersOnOpen = true,
        string? initialSearchText = null,
        IReadOnlyCollection<TaskPriority>? initialPriorities = null,
        DateTime? initialDate = null,
        bool isDisabled = false)
    {
        return Render<FilterComponent>(parameters => parameters
            .Add(filter => filter.IsOpen, isOpen)
            .Add(filter => filter.IsDisabled, isDisabled)
            .Add(filter => filter.ShowDateSortToggle, showDateSortToggle)
            .Add(filter => filter.ResetFiltersOnOpen, resetFiltersOnOpen)
            .Add(filter => filter.InitialSearchText, initialSearchText)
            .Add(filter => filter.InitialSelectedPriorities, initialPriorities ?? [])
            .Add(filter => filter.InitialSelectedDate, initialDate)
            .Add(filter => filter.OnFiltersChanged, EventCallback.Factory.Create<FilterModel>(this, payload => _payloads.Add(payload))));
    }
}
