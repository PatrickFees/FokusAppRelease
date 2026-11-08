using FokusApp.Components.Calender;
using FokusApp.Components.PopUps.Calendar;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FocusAppTests.ViewModels;

public class CalendarPopupViewModelTests
{
    private readonly FakeTimeProvider _clock = TestTasks.CreateClock();
    private readonly ITaskCacheService _taskCache = Substitute.For<ITaskCacheService>();
    private readonly CalendarPopupViewModel _viewModel;

    public CalendarPopupViewModelTests()
    {
        SetTasks(normal: [], done: []);
        _viewModel = new CalendarPopupViewModel(_clock, _taskCache);
    }

    [Fact]
    public void Open_Date_ShowsMonthOfDateWithSelectedDayWithoutTime()
    {
        var initial = new DateTime(2026, 3, 18, 14, 30, 0);

        _viewModel.Open(initial);

        var selectedCells = Cells(_viewModel).Where(cell => cell.IsSelected).ToList();
        var selectedCell = Assert.Single(selectedCells);
        Assert.Equal(new DateTime(2026, 3, 18), _viewModel.SelectedDate);
        Assert.Equal(new DateTime(2026, 3, 1), _viewModel.VisibleMonth);
        Assert.Equal(new DateTime(2026, 3, 18), selectedCell.Date);
    }

    [Fact]
    public void Open_Date_BuildsGridOf42DaysWithMonthDaysMarked()
    {
        var initial = new DateTime(2026, 3, 18);

        _viewModel.Open(initial);

        var cells = Cells(_viewModel);
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

        var firstCell = Cells(_viewModel)[0];
        Assert.Equal(expectedStart, firstCell.Date);
        Assert.Equal(DayOfWeek.Monday, firstCell.Date.DayOfWeek);
    }

    [Fact]
    public void Open_Today_MarksTodayCell()
    {
        var today = TestTasks.Today;

        _viewModel.Open(today);

        var todayCells = Cells(_viewModel).Where(cell => cell.IsToday).ToList();
        var todayCell = Assert.Single(todayCells);
        Assert.Equal(today, todayCell.Date);
        Assert.True(todayCell.IsSelected);
    }

    [Fact]
    public void Constructor_NoOpenCall_ShowsMonthOfTodayWithTodayMarkedAndSelected()
    {
        var todayCells = Cells(_viewModel).Where(cell => cell.IsToday).ToList();
        var todayCell = Assert.Single(todayCells);
        Assert.Equal(TestTasks.Today, _viewModel.SelectedDate);
        Assert.Equal(new DateTime(2026, 3, 1), _viewModel.VisibleMonth);
        Assert.Equal(TestTasks.Today, todayCell.Date);
        Assert.True(todayCell.IsSelected);
    }

    [Fact]
    public void GoToNextMonth_ClockAdvancedPastMonthEnd_MarksNewTodayInFollowingMonth()
    {
        _viewModel.Open(TestTasks.Today);
        _clock.Advance(TimeSpan.FromDays(20));

        _viewModel.GoToNextMonth();

        var todayCells = Cells(_viewModel).Where(cell => cell.IsToday).ToList();
        var todayCell = Assert.Single(todayCells);
        Assert.Equal(new DateTime(2026, 4, 4), todayCell.Date);
        Assert.Equal(new DateTime(2026, 4, 1), _viewModel.VisibleMonth);
    }

    [Fact]
    public void Open_TodayOnYearBoundary_ShowsDecemberAndMarksLastDayAsToday()
    {
        _clock.SetUtcNow(new DateTimeOffset(2026, 12, 31, 23, 0, 0, TimeSpan.Zero));
        var viewModel = new CalendarPopupViewModel(_clock, _taskCache);

        var todayCells = Cells(viewModel).Where(cell => cell.IsToday).ToList();
        var todayCell = Assert.Single(todayCells);
        Assert.Equal(new DateTime(2026, 12, 1), viewModel.VisibleMonth);
        Assert.Equal(new DateTime(2026, 12, 31), todayCell.Date);
    }

    [Fact]
    public void GoToNextMonth_Open_ShowsFollowingMonthAndKeepsSelection()
    {
        var initial = new DateTime(2026, 3, 18);
        _viewModel.Open(initial);

        _viewModel.GoToNextMonth();

        Assert.Equal(new DateTime(2026, 4, 1), _viewModel.VisibleMonth);
        Assert.Equal(initial, _viewModel.SelectedDate);
    }

    [Fact]
    public void GoToPreviousMonth_January_ShowsDecemberOfPreviousYear()
    {
        var initial = new DateTime(2026, 1, 15);
        _viewModel.Open(initial);

        _viewModel.GoToPreviousMonth();

        Assert.Equal(new DateTime(2025, 12, 1), _viewModel.VisibleMonth);
    }

    [Fact]
    public void GoToNextMonth_December_ShowsJanuaryOfNextYear()
    {
        var initial = new DateTime(2026, 12, 15);
        _viewModel.Open(initial);

        _viewModel.GoToNextMonth();

        Assert.Equal(new DateTime(2027, 1, 1), _viewModel.VisibleMonth);
    }

    [Fact]
    public void GoToMonth_DifferentYear_MovesVisibleMonthAndCarriesSelectionToIt()
    {
        var initial = new DateTime(2026, 3, 15);
        _viewModel.Open(initial);

        _viewModel.GoToMonth(2036, 3);

        Assert.Equal(new DateTime(2036, 3, 1), _viewModel.VisibleMonth);
        Assert.Equal(new DateTime(2036, 3, 15), _viewModel.SelectedDate);
    }

    [Fact]
    public void GoToMonth_DayDoesNotExistInTargetMonth_ClampsToItsLastDay()
    {
        var initial = new DateTime(2026, 1, 31);
        _viewModel.Open(initial);

        _viewModel.GoToMonth(2026, 2);

        Assert.Equal(new DateTime(2026, 2, 28), _viewModel.SelectedDate);
    }

    [Fact]
    public void GoToMonth_SameMonthAndYear_DoesNotRebuildWeeks()
    {
        var initial = new DateTime(2026, 3, 18);
        _viewModel.Open(initial);
        var weeksBefore = _viewModel.Weeks;

        _viewModel.GoToMonth(2026, 3);

        Assert.Same(weeksBefore, _viewModel.Weeks);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void GoToMonth_InvalidMonth_IsIgnored(int month)
    {
        var initial = new DateTime(2026, 3, 18);
        _viewModel.Open(initial);

        _viewModel.GoToMonth(2036, month);

        Assert.Equal(new DateTime(2026, 3, 1), _viewModel.VisibleMonth);
        Assert.Equal(initial, _viewModel.SelectedDate);
    }

    [Fact]
    public void SelectDate_DayInOtherMonth_ChangesSelectionAndVisibleMonth()
    {
        var initial = new DateTime(2026, 3, 18);
        _viewModel.Open(initial);
        var selected = new DateTime(2026, 5, 2, 9, 0, 0);

        _viewModel.SelectDate(selected);

        Assert.Equal(selected.Date, _viewModel.SelectedDate);
        Assert.Equal(new DateTime(2026, 5, 1), _viewModel.VisibleMonth);
    }

    [Fact]
    public void SelectDate_DayInVisibleMonth_MovesSelectionMark()
    {
        var initial = new DateTime(2026, 3, 18);
        _viewModel.Open(initial);
        var selected = new DateTime(2026, 3, 20);

        _viewModel.SelectDate(selected);

        var selectedCells = Cells(_viewModel).Where(cell => cell.IsSelected).ToList();
        var selectedCell = Assert.Single(selectedCells);
        Assert.Equal(selected, selectedCell.Date);
    }

    [Fact]
    public void SelectDate_SameDayAgain_DoesNotRebuildWeeks()
    {
        var initial = new DateTime(2026, 3, 18);
        _viewModel.Open(initial);
        var weeksBefore = _viewModel.Weeks;

        _viewModel.SelectDate(initial);

        Assert.Same(weeksBefore, _viewModel.Weeks);
    }

    [Theory]
    [InlineData(-45)]
    [InlineData(-120)]
    public void HandleHorizontalSwipe_LeftBeyondThreshold_ShowsNextMonth(double delta)
    {
        var initial = new DateTime(2026, 3, 18);
        _viewModel.Open(initial);

        _viewModel.HandleHorizontalSwipe(delta);

        Assert.Equal(new DateTime(2026, 4, 1), _viewModel.VisibleMonth);
    }

    [Theory]
    [InlineData(45)]
    [InlineData(120)]
    public void HandleHorizontalSwipe_RightBeyondThreshold_ShowsPreviousMonth(double delta)
    {
        var initial = new DateTime(2026, 3, 18);
        _viewModel.Open(initial);

        _viewModel.HandleHorizontalSwipe(delta);

        Assert.Equal(new DateTime(2026, 2, 1), _viewModel.VisibleMonth);
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

        Assert.Equal(new DateTime(2026, 3, 1), _viewModel.VisibleMonth);
    }

    [Fact]
    public void HandleHorizontalSwipe_Swipe_DoesNotConfirmOrChangeSelection()
    {
        var initial = new DateTime(2026, 3, 18);
        _viewModel.Open(initial);

        _viewModel.HandleHorizontalSwipe(-100);

        Assert.Equal(initial, _viewModel.SelectedDate);
    }

    [Fact]
    public void Open_TasksOnDays_ShowsPriorityDotsOnThoseDays()
    {
        var day = new DateTime(2026, 3, 18);
        SetTasks(
            normal:
            [
                TestTasks.Create("A", taskDate: day, priority: TaskPriority.Hoch),
                TestTasks.Create("B", taskDate: day, priority: TaskPriority.Wenig)
            ],
            done: [TestTasks.Create("C", taskDate: day, isDone: true, priority: TaskPriority.Dringend)]);

        _viewModel.Open(day);

        var cell = Cells(_viewModel).Single(c => c.Date == day);
        Assert.Equal([TaskPriority.Hoch, TaskPriority.Wenig, TaskPriority.Dringend], cell.Dots.Select(dot => dot.Priority));
        Assert.True(cell.Dots[2].IsDone);
        Assert.Empty(Cells(_viewModel).Single(c => c.Date == day.AddDays(1)).Dots);
    }

    [Fact]
    public void Open_TasksOfSelectedDay_ListsForYouAndToDoSeparately()
    {
        var day = new DateTime(2026, 3, 18);
        var forYou = TestTasks.Create("Für mich", isPositive: true, taskDate: day);
        var toDo = TestTasks.Create("Erledigen", isPositive: false, taskDate: day);
        var otherDay = TestTasks.Create("Anderer Tag", isPositive: false, taskDate: day.AddDays(1));
        SetTasks(normal: [forYou, toDo, otherDay], done: []);

        _viewModel.Open(day);

        Assert.Equal([forYou], _viewModel.ForYouTasks);
        Assert.Equal([toDo], _viewModel.ToDoTasks);
    }

    [Fact]
    public void SelectDate_OtherDay_ShowsTasksOfThatDay()
    {
        var first = new DateTime(2026, 3, 18);
        var second = new DateTime(2026, 3, 20);
        var task = TestTasks.Create("Später", isPositive: true, taskDate: second);
        SetTasks(normal: [task], done: []);
        _viewModel.Open(first);
        Assert.Empty(_viewModel.ForYouTasks);

        _viewModel.SelectDate(second);

        Assert.Equal([task], _viewModel.ForYouTasks);
    }

    [Fact]
    public void Open_Always_ReadsTheScopeFilteredTasksFromTheCache()
    {
        _taskCache.ClearReceivedCalls();

        _viewModel.Open(TestTasks.Today);

        _taskCache.Received().GetNormalTasks(null);
        _taskCache.Received().GetDoneTasks(null);
    }

    private void SetTasks(IReadOnlyList<TaskDto> normal, IReadOnlyList<TaskDto> done)
    {
        IReadOnlyList<TaskDto> empty = [];
        _taskCache.GetNormalTasks(Arg.Any<DateTime?>()).Returns(normal);
        _taskCache.GetTemplates(Arg.Any<DateTime?>()).Returns(empty);
        _taskCache.GetDoneTasks(Arg.Any<DateTime?>()).Returns(done);
    }

    private static List<CalenderDayCellModel> Cells(ICalendarPopupViewModel viewModel)
    {
        return viewModel.Weeks.SelectMany(week => week.Days).ToList();
    }
}
