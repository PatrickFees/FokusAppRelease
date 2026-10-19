using FokusApp.Components.PopUps.Calendar;
using Microsoft.Extensions.Time.Testing;

namespace FocusAppTests.ViewModels;

public class CalendarPopupViewModelTests
{
    private readonly FakeTimeProvider _clock = TestTasks.CreateClock();
    private readonly CalendarPopupViewModel _viewModel;

    public CalendarPopupViewModelTests()
    {
        _viewModel = new CalendarPopupViewModel(_clock);
    }

    [Fact]
    public void Open_Date_ShowsMonthOfDateWithSelectedDayWithoutTime()
    {
        var initial = new DateTime(2026, 3, 18, 14, 30, 0);

        _viewModel.Open(initial);

        var month = _viewModel.CurrentMonth;
        var selectedCells = month.DayCells.Where(cell => cell.IsSelected).ToList();
        var selectedCell = Assert.Single(selectedCells);
        Assert.Equal(new DateTime(2026, 3, 18), _viewModel.SelectedDate);
        Assert.Equal(new DateTime(2026, 3, 1), month.MonthStart);
        Assert.Equal(new DateTime(2026, 3, 18), selectedCell.Date);
    }

    [Fact]
    public void Open_Date_BuildsGridOf42DaysWithMonthDaysMarked()
    {
        var initial = new DateTime(2026, 3, 18);

        _viewModel.Open(initial);

        var cells = _viewModel.CurrentMonth.DayCells;
        var daysInMonth = cells.Count(cell => cell.IsInCurrentMonth);
        Assert.Equal(42, cells.Count);
        Assert.Equal(31, daysInMonth);
    }

    [Theory]
    [InlineData(2026, 3, 18, 2026, 2, 23)]
    [InlineData(2027, 2, 10, 2027, 2, 1)]
    [InlineData(2026, 2, 28, 2026, 1, 26)]
    [InlineData(2026, 11, 5, 2026, 10, 26)]
    public void Open_Date_GridStartsOnMondayOfFirstWeek(
        int year,
        int month,
        int day,
        int expectedYear,
        int expectedMonth,
        int expectedDay)
    {
        var initial = new DateTime(year, month, day);
        var expectedStart = new DateTime(expectedYear, expectedMonth, expectedDay);

        _viewModel.Open(initial);

        var firstCell = _viewModel.CurrentMonth.DayCells[0];
        Assert.Equal(expectedStart, firstCell.Date);
        Assert.Equal(DayOfWeek.Monday, firstCell.Date.DayOfWeek);
    }

    [Fact]
    public void Open_Today_MarksTodayCell()
    {
        var today = TestTasks.Today;

        _viewModel.Open(today);

        var todayCells = _viewModel.CurrentMonth.DayCells.Where(cell => cell.IsToday).ToList();
        var todayCell = Assert.Single(todayCells);
        Assert.Equal(today, todayCell.Date);
        Assert.True(todayCell.IsSelected);
    }

    [Fact]
    public void Constructor_NoOpenCall_ShowsMonthOfTodayWithTodayMarkedAndSelected()
    {
        var month = _viewModel.CurrentMonth;

        var todayCells = month.DayCells.Where(cell => cell.IsToday).ToList();
        var todayCell = Assert.Single(todayCells);
        Assert.Equal(TestTasks.Today, _viewModel.SelectedDate);
        Assert.Equal(new DateTime(2026, 3, 1), month.MonthStart);
        Assert.Equal(TestTasks.Today, todayCell.Date);
        Assert.True(todayCell.IsSelected);
    }

    [Fact]
    public void GoToNextMonth_ClockAdvancedPastMonthEnd_MarksNewTodayInFollowingMonth()
    {
        _viewModel.Open(TestTasks.Today);
        _clock.Advance(TimeSpan.FromDays(20));

        _viewModel.GoToNextMonth();

        var todayCells = _viewModel.CurrentMonth.DayCells.Where(cell => cell.IsToday).ToList();
        var todayCell = Assert.Single(todayCells);
        Assert.Equal(new DateTime(2026, 4, 4), todayCell.Date);
        Assert.Equal(new DateTime(2026, 4, 1), _viewModel.CurrentMonth.MonthStart);
    }

    [Fact]
    public void Open_TodayOnYearBoundary_ShowsDecemberAndMarksLastDayAsToday()
    {
        _clock.SetUtcNow(new DateTimeOffset(2026, 12, 31, 23, 0, 0, TimeSpan.Zero));
        var viewModel = new CalendarPopupViewModel(_clock);

        var month = viewModel.CurrentMonth;

        var todayCells = month.DayCells.Where(cell => cell.IsToday).ToList();
        var todayCell = Assert.Single(todayCells);
        Assert.Equal(new DateTime(2026, 12, 1), month.MonthStart);
        Assert.Equal(new DateTime(2026, 12, 31), todayCell.Date);
    }

    [Fact]
    public void GoToNextMonth_Open_ShowsFollowingMonthAndKeepsSelection()
    {
        var initial = new DateTime(2026, 3, 18);
        _viewModel.Open(initial);

        _viewModel.GoToNextMonth();

        Assert.Equal(new DateTime(2026, 4, 1), _viewModel.CurrentMonth.MonthStart);
        Assert.Equal(initial, _viewModel.SelectedDate);
    }

    [Fact]
    public void GoToPreviousMonth_January_ShowsDecemberOfPreviousYear()
    {
        var initial = new DateTime(2026, 1, 15);
        _viewModel.Open(initial);

        _viewModel.GoToPreviousMonth();

        Assert.Equal(new DateTime(2025, 12, 1), _viewModel.CurrentMonth.MonthStart);
    }

    [Fact]
    public void GoToNextMonth_December_ShowsJanuaryOfNextYear()
    {
        var initial = new DateTime(2026, 12, 15);
        _viewModel.Open(initial);

        _viewModel.GoToNextMonth();

        Assert.Equal(new DateTime(2027, 1, 1), _viewModel.CurrentMonth.MonthStart);
    }

    [Fact]
    public void SelectDate_DayInOtherMonth_ChangesSelectionAndVisibleMonth()
    {
        var initial = new DateTime(2026, 3, 18);
        _viewModel.Open(initial);
        var selected = new DateTime(2026, 5, 2, 9, 0, 0);

        _viewModel.SelectDate(selected);

        Assert.Equal(selected.Date, _viewModel.SelectedDate);
        Assert.Equal(new DateTime(2026, 5, 1), _viewModel.CurrentMonth.MonthStart);
    }

    [Fact]
    public void SelectDate_DayInVisibleMonth_MovesSelectionMark()
    {
        var initial = new DateTime(2026, 3, 18);
        _viewModel.Open(initial);
        var selected = new DateTime(2026, 3, 20);

        _viewModel.SelectDate(selected);

        var selectedCells = _viewModel.CurrentMonth.DayCells.Where(cell => cell.IsSelected).ToList();
        var selectedCell = Assert.Single(selectedCells);
        Assert.Equal(selected, selectedCell.Date);
    }

    [Fact]
    public void SelectDate_SameDayAgain_DoesNotRebuildMonth()
    {
        var initial = new DateTime(2026, 3, 18);
        _viewModel.Open(initial);
        var monthBefore = _viewModel.CurrentMonth;

        _viewModel.SelectDate(initial);

        Assert.Same(monthBefore, _viewModel.CurrentMonth);
    }

    [Theory]
    [InlineData(-45)]
    [InlineData(-120)]
    public void HandleHorizontalSwipe_LeftBeyondThreshold_ShowsNextMonth(double delta)
    {
        var initial = new DateTime(2026, 3, 18);
        _viewModel.Open(initial);

        _viewModel.HandleHorizontalSwipe(delta);

        Assert.Equal(new DateTime(2026, 4, 1), _viewModel.CurrentMonth.MonthStart);
    }

    [Theory]
    [InlineData(45)]
    [InlineData(120)]
    public void HandleHorizontalSwipe_RightBeyondThreshold_ShowsPreviousMonth(double delta)
    {
        var initial = new DateTime(2026, 3, 18);
        _viewModel.Open(initial);

        _viewModel.HandleHorizontalSwipe(delta);

        Assert.Equal(new DateTime(2026, 2, 1), _viewModel.CurrentMonth.MonthStart);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(44.9)]
    [InlineData(-44.9)]
    public void HandleHorizontalSwipe_BelowThreshold_KeepsMonth(double delta)
    {
        var initial = new DateTime(2026, 3, 18);
        _viewModel.Open(initial);

        _viewModel.HandleHorizontalSwipe(delta);

        Assert.Equal(new DateTime(2026, 3, 1), _viewModel.CurrentMonth.MonthStart);
    }

    [Fact]
    public void HandleHorizontalSwipe_Swipe_DoesNotConfirmOrChangeSelection()
    {
        var initial = new DateTime(2026, 3, 18);
        _viewModel.Open(initial);

        _viewModel.HandleHorizontalSwipe(-100);

        Assert.Equal(initial, _viewModel.SelectedDate);
    }
}
