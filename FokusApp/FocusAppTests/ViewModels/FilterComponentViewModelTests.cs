using FokusApp.Components.SharedUI.FilterComponent;
using FokusApp.Core.Enums;
using FokusApp.Core.Services.Interfaces;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FocusAppTests.ViewModels;

public class FilterComponentViewModelTests
{
    private readonly FakeTimeProvider _clock = TestTasks.CreateClock();
    private readonly IDayChangeService _dayChange = Substitute.For<IDayChangeService>();
    private readonly FilterComponentViewModel _viewModel;

    public FilterComponentViewModelTests()
    {
        _viewModel = new FilterComponentViewModel(_clock, _dayChange);
    }

    [Fact]
    public void Open_WithReset_ShowsDefaultFilterForToday()
    {
        var today = TestTasks.Today;

        _viewModel.Open();

        Assert.True(_viewModel.IsOpen);
        Assert.False(_viewModel.IsDayCalendarOpen);
        Assert.Equal(string.Empty, _viewModel.Model.SearchText);
        Assert.Empty(_viewModel.Model.SelectedPriorities);
        Assert.True(_viewModel.Model.IsSortDescending);
        Assert.Equal(today, _viewModel.Model.SelectedDate);
    }

    [Fact]
    public void Constructor_NoOpenCall_StartsOnToday()
    {
        Assert.Equal(TestTasks.Today, _viewModel.Model.SelectedDate);
        Assert.Equal(TestTasks.Today, _viewModel.DisplayDate);
    }

    [Fact]
    public void DisplayDate_NoDayFilter_FallsBackToTodayOfClock()
    {
        _viewModel.Open(false);
        _clock.Advance(TimeSpan.FromDays(1));

        var displayDate = _viewModel.DisplayDate;

        Assert.Null(_viewModel.Model.SelectedDate);
        Assert.Equal(new DateTime(2026, 3, 16), displayDate);
    }

    [Fact]
    public void DisplayDate_DayFilterSet_ShowsSelectedDay()
    {
        _viewModel.SelectDate(TestTasks.Day);

        Assert.Equal(TestTasks.Day, _viewModel.DisplayDate);
    }

    [Fact]
    public void Open_WithResetAfterMidnight_UsesNewToday()
    {
        _clock.Advance(TimeSpan.FromDays(1));

        _viewModel.Open();

        Assert.Equal(new DateTime(2026, 3, 16), _viewModel.Model.SelectedDate);
    }

    [Fact]
    public void HasFilterChanged_AfterResetOfLastPayloadWithTodayFilter_IsFalse()
    {
        _viewModel.Open();

        _viewModel.ResetLastNotifiedFilterPayload();

        Assert.False(_viewModel.HasFilterChanged());
    }

    [Fact]
    public void Open_WithoutReset_TakesInitialValues()
    {
        IReadOnlyCollection<TaskPriority> priorities = [TaskPriority.Hoch, TaskPriority.Wenig];

        _viewModel.Open(false, "Sport", priorities, false, TestTasks.Day);

        Assert.Equal("Sport", _viewModel.Model.SearchText);
        Assert.Equal([TaskPriority.Wenig, TaskPriority.Hoch], _viewModel.SelectedPriorities);
        Assert.False(_viewModel.Model.IsSortDescending);
        Assert.Equal(TestTasks.Day, _viewModel.Model.SelectedDate);
    }

    [Fact]
    public void Open_WithoutResetAndWithoutValues_ClearsFilterIncludingDay()
    {
        _viewModel.Open();

        _viewModel.Open(false);

        Assert.Equal(string.Empty, _viewModel.Model.SearchText);
        Assert.Empty(_viewModel.Model.SelectedPriorities);
        Assert.Null(_viewModel.Model.SelectedDate);
    }

    [Fact]
    public void Open_CalendarWasOpen_ClosesCalendar()
    {
        _viewModel.OpenDayCalendar();

        _viewModel.Open();

        Assert.False(_viewModel.IsDayCalendarOpen);
    }

    [Fact]
    public void TogglePriorityFilterCommand_Priority_IsAddedAndRemovedAgain()
    {
        _viewModel.TogglePriorityFilterCommand.Execute(TaskPriority.Hoch);
        Assert.Equal([TaskPriority.Hoch], _viewModel.SelectedPriorities);

        _viewModel.TogglePriorityFilterCommand.Execute(TaskPriority.Hoch);

        Assert.Empty(_viewModel.SelectedPriorities);
    }

    [Fact]
    public void TogglePriorityFilterCommand_SeveralPriorities_AreKeptInAscendingOrder()
    {
        _viewModel.TogglePriorityFilterCommand.Execute(TaskPriority.Dringend);
        _viewModel.TogglePriorityFilterCommand.Execute(TaskPriority.Wenig);
        _viewModel.TogglePriorityFilterCommand.Execute(TaskPriority.Mittel);

        Assert.Equal([TaskPriority.Wenig, TaskPriority.Mittel, TaskPriority.Dringend], _viewModel.SelectedPriorities);
    }

    [Fact]
    public void TogglePriorityFilterCommand_UndefinedPriority_IsTreatedAsNone()
    {
        var undefined = (TaskPriority)99;

        _viewModel.TogglePriorityFilterCommand.Execute(undefined);

        Assert.Equal([TaskPriority.Keine], _viewModel.SelectedPriorities);
    }

    [Fact]
    public void TogglePriorityFilterCommand_Priority_RaisesSelectedPrioritiesChanged()
    {
        var changed = TrackChanges();

        _viewModel.TogglePriorityFilterCommand.Execute(TaskPriority.Mittel);

        Assert.Contains(nameof(FilterComponentViewModel.SelectedPriorities), changed);
    }

    [Fact]
    public void ToggleSortDirectionCommand_Twice_ReturnsToDescending()
    {
        _viewModel.ToggleSortDirectionCommand.Execute(null);
        Assert.False(_viewModel.Model.IsSortDescending);

        _viewModel.ToggleSortDirectionCommand.Execute(null);

        Assert.True(_viewModel.Model.IsSortDescending);
    }

    [Fact]
    public void ResetFiltersCommand_ChangedFilter_ResetsSearchPrioritySortAndDay()
    {
        _viewModel.Open(false, "Sport", [TaskPriority.Hoch], false, TestTasks.Day);
        _viewModel.OpenDayCalendar();

        _viewModel.ResetFiltersCommand.Execute(null);

        Assert.Equal(string.Empty, _viewModel.Model.SearchText);
        Assert.Empty(_viewModel.Model.SelectedPriorities);
        Assert.True(_viewModel.Model.IsSortDescending);
        Assert.Null(_viewModel.Model.SelectedDate);
        Assert.False(_viewModel.IsDayCalendarOpen);
    }

    [Fact]
    public void SelectDate_DateWithTime_StoresDayAndClosesCalendar()
    {
        _viewModel.OpenDayCalendar();
        var selected = new DateTime(2026, 4, 2, 17, 30, 0);

        _viewModel.SelectDate(selected);

        Assert.Equal(selected.Date, _viewModel.Model.SelectedDate);
        Assert.False(_viewModel.IsDayCalendarOpen);
    }

    [Fact]
    public void OpenAndCloseDayCalendar_TogglesFlag()
    {
        _viewModel.OpenDayCalendar();
        Assert.True(_viewModel.IsDayCalendarOpen);

        _viewModel.CloseDayCalendar();

        Assert.False(_viewModel.IsDayCalendarOpen);
    }

    [Fact]
    public void CloseCommand_OpenFilter_ClosesFilter()
    {
        _viewModel.Open();

        _viewModel.CloseCommand.Execute(null);

        Assert.False(_viewModel.IsOpen);
    }

    [Fact]
    public void HasFilterChanged_RightAfterOpen_IsFalse()
    {
        _viewModel.Open();

        Assert.False(_viewModel.HasFilterChanged());
    }

    [Fact]
    public void HasFilterChanged_SearchTextEdited_IsTrueUntilNotified()
    {
        _viewModel.Open();
        _viewModel.Model.SearchText = "Sport";
        Assert.True(_viewModel.HasFilterChanged());

        _viewModel.MarkFilterChangesNotified();

        Assert.False(_viewModel.HasFilterChanged());
    }

    [Fact]
    public void HasFilterChanged_AfterResetOfLastPayload_ComparesAgainstDefaults()
    {
        _viewModel.Open(false, "Sport", [], true, TestTasks.Day);
        Assert.False(_viewModel.HasFilterChanged());

        _viewModel.ResetLastNotifiedFilterPayload();

        Assert.True(_viewModel.HasFilterChanged());
    }

    [Fact]
    public void CreateFilterPayload_LaterModelChange_DoesNotAffectPayload()
    {
        _viewModel.Open();
        _viewModel.Model.SearchText = "Sport";

        var payload = _viewModel.CreateFilterPayload();
        _viewModel.Model.SearchText = "Lesen";

        Assert.Equal("Sport", payload.SearchText);
        Assert.NotSame(_viewModel.Model, payload);
    }

    [Fact]
    public void Dispose_AfterwardsModelChanges_DoNotRaiseSelectedPriorities()
    {
        var changed = TrackChanges();
        _viewModel.Dispose();

        _viewModel.Model.SelectedPriorities = [TaskPriority.Hoch];

        Assert.DoesNotContain(nameof(FilterComponentViewModel.SelectedPriorities), changed);
    }

    [Fact]
    public void DayChanged_TodayWasFiltered_FollowsToNewDay()
    {
        var tomorrow = TestTasks.Today.AddDays(1);

        RaiseDayChanged(TestTasks.Today, tomorrow);

        Assert.Equal(tomorrow, _viewModel.Model.SelectedDate);
        Assert.Equal(tomorrow, _viewModel.DisplayDate);
    }

    [Fact]
    public void DayChanged_OtherDayWasFiltered_KeepsFilterDay()
    {
        _viewModel.SelectDate(TestTasks.Day);

        RaiseDayChanged(TestTasks.Today, TestTasks.Today.AddDays(1));

        Assert.Equal(TestTasks.Day, _viewModel.Model.SelectedDate);
    }

    [Fact]
    public void DayChanged_AfterDispose_IsIgnored()
    {
        _viewModel.Dispose();

        RaiseDayChanged(TestTasks.Today, TestTasks.Today.AddDays(1));

        Assert.Equal(TestTasks.Today, _viewModel.Model.SelectedDate);
    }

    private void RaiseDayChanged(DateTime previousDay, DateTime currentDay)
    {
        var args = new DayChangedEventArgs(previousDay, currentDay);
        _dayChange.DayChanged += Raise.EventWith(args);
    }

    private List<string?> TrackChanges()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        return changed;
    }
}
