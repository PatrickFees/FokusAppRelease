using Bunit;
using FokusApp.Components.Calender;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FocusAppTests.Components;

public class CalenderPageTests : ComponentTestContext
{
    private static readonly DateTime FirstVisibleDay = new(2026, 2, 23);
    private static readonly DateTime SelectedDay = new(2026, 3, 15);

    private readonly ICalenderViewModel _viewModel = Substitute.For<ICalenderViewModel>();

    public CalenderPageTests()
    {
        IReadOnlyList<TaskDto> noTasks = [];
        _viewModel.VisibleMonth.Returns(new DateTime(2026, 3, 1));
        _viewModel.SelectedDay.Returns(SelectedDay);
        _viewModel.Weeks.Returns(BuildWeeks());
        _viewModel.ForYouTasks.Returns(noTasks);
        _viewModel.ToDoTasks.Returns(noTasks);

        Services.AddSingleton(_viewModel);
        Services.AddSingleton(Substitute.For<ITaskEditPopUpViewModel>());
    }

    [Fact]
    public void Render_InitializesViewModel()
    {
        Render<CalenderPage>();

        _viewModel.Received(1).InitializeAsync();
    }

    [Fact]
    public void Render_Month_ShowsSixWeeksWithSevenDaysEach()
    {
        var cut = Render<CalenderPage>();

        Assert.Equal(6, cut.FindAll(".calender-week-row").Count);
        Assert.Equal(42, cut.FindAll(".calender-day").Count);
        Assert.Equal(7, cut.FindAll(".calender-weekday-cell").Count);
    }

    [Fact]
    public void Render_Weeks_ShowsCalendarWeekNumbers()
    {
        var cut = Render<CalenderPage>();

        var weekNumbers = cut.FindAll(".calender-week-number").Select(element => element.TextContent).ToArray();

        Assert.Equal(["9", "10", "11", "12", "13", "14"], weekNumbers);
    }

    [Fact]
    public void Render_Month_MarksTodayAndSelectedDay()
    {
        var cut = Render<CalenderPage>();

        Assert.Single(cut.FindAll(".calender-day.is-today"));
        Assert.Single(cut.FindAll(".calender-day.is-selected"));
        Assert.Equal(SelectedDay.Day.ToString(), cut.Find(".calender-day.is-selected .calender-day-number").TextContent);
    }

    [Fact]
    public void Render_DaysOutsideMonth_AreMarkedAsOutside()
    {
        var cut = Render<CalenderPage>();

        var outsideDays = cut.FindAll(".calender-day.outside-month");

        Assert.Equal(11, outsideDays.Count);
    }

    [Fact]
    public void Render_DayWithTasks_ShowsDotsAndMoreMarker()
    {
        var day = new CalenderDayCellModel
        {
            Date = SelectedDay,
            IsInCurrentMonth = true,
            IsToday = true,
            IsSelected = true,
            Dots =
            [
                new CalenderDotModel { Priority = TaskPriority.Hoch },
                new CalenderDotModel { Priority = TaskPriority.Keine, IsDone = true }
            ],
            HasMoreTasks = true
        };
        _viewModel.Weeks.Returns(BuildWeeks(day));

        var cut = Render<CalenderPage>();

        var selectedCell = cut.Find(".calender-day.is-selected");
        Assert.Equal(2, selectedCell.QuerySelectorAll(".calender-dot").Length);
        Assert.Single(selectedCell.QuerySelectorAll(".calender-dot.is-done"));
        Assert.Single(selectedCell.QuerySelectorAll(".calender-dot-more"));
    }

    [Fact]
    public void Render_NoTasksForDay_ShowsEmptyHint()
    {
        var cut = Render<CalenderPage>();

        var hints = cut.FindAll(".todo-book-empty");
        Assert.Equal(2, hints.Count);
        Assert.All(hints, hint => Assert.Equal(Text("NoTasksForDay"), hint.TextContent));
        Assert.Empty(cut.FindAll(".todo-book-task-item"));
    }

    [Fact]
    public void Render_TasksForDay_ShowsNotepadsAndGreysOutDoneTasks()
    {
        var open = new TaskDto { Id = Guid.NewGuid(), Note = "Offen", Priority = TaskPriority.Hoch };
        var done = new TaskDto { Id = Guid.NewGuid(), Note = "Fertig", IsDone = true };
        IReadOnlyList<TaskDto> forYou = [open, done];
        _viewModel.ForYouTasks.Returns(forYou);

        var cut = Render<CalenderPage>();

        var items = cut.FindAll(".todo-book-task-item");
        Assert.Equal(2, items.Count);
        Assert.DoesNotContain("is-done", items[0].ClassList);
        Assert.Contains("is-done", items[1].ClassList);
        Assert.Equal(2, cut.FindAll(".todo-book-section").Count);
        Assert.Single(cut.FindAll(".todo-book-empty"));
    }

    [Fact]
    public void ClickTask_OpensActionPopupForTask()
    {
        var task = new TaskDto { Id = Guid.NewGuid(), Note = "Aufgabe" };
        IReadOnlyList<TaskDto> toDo = [task];
        _viewModel.ToDoTasks.Returns(toDo);
        var cut = Render<CalenderPage>();

        cut.Find(".todo-book-task-item").Click();

        _viewModel.Received(1).OpenTaskActionPopup(task.Id!.Value);
    }

    [Fact]
    public void ClickCollapseToggle_TogglesCalendarCollapsed()
    {
        var cut = Render<CalenderPage>();

        cut.Find($"button[aria-label='{Text("CollapseCalendar")}']").Click();

        _viewModel.Received(1).ToggleCalendarCollapsed();
    }

    [Fact]
    public void Render_Collapsed_HidesOtherWeeksOffersExpandAndDoesNotDimDays()
    {
        _viewModel.IsCalendarCollapsed.Returns(true);

        var cut = Render<CalenderPage>();

        Assert.Equal(5, cut.FindAll(".calender-week-row.is-collapsed").Count);
        Assert.Single(cut.FindAll(".calender-week-row:not(.is-collapsed)"));
        Assert.Empty(cut.FindAll(".calender-day.outside-month"));
        Assert.NotNull(cut.Find($"button[aria-label='{Text("ExpandCalendar")}']"));
    }

    [Fact]
    public void ClickArrows_Collapsed_MoveSelectedDayInsteadOfMonth()
    {
        _viewModel.IsCalendarCollapsed.Returns(true);
        var cut = Render<CalenderPage>();

        cut.Find($"button[aria-label='{Text("NextMonth")}']").Click();
        cut.Find($"button[aria-label='{Text("PreviousMonth")}']").Click();

        _viewModel.Received(1).MoveSelectedDay(true);
        _viewModel.Received(1).MoveSelectedDay(false);
        _viewModel.DidNotReceive().GoToNextMonth();
        _viewModel.DidNotReceive().GoToPreviousMonth();
    }

    [Theory]
    [InlineData(200, 100, true)]
    [InlineData(100, 200, false)]
    public void SwipeOnNotepads_Collapsed_MovesSelectedDay(double startX, double endX, bool forwards)
    {
        _viewModel.IsCalendarCollapsed.Returns(true);
        var cut = Render<CalenderPage>();
        var list = cut.Find(".calender-day-list");

        list.PointerDown(new PointerEventArgs { ClientX = startX, ClientY = 300 });
        list.PointerUp(new PointerEventArgs { ClientX = endX, ClientY = 305 });

        _viewModel.Received(1).MoveSelectedDay(forwards);
    }

    [Fact]
    public void SwipeOnNotepads_Expanded_DoesNotMoveSelectedDay()
    {
        var cut = Render<CalenderPage>();
        var list = cut.Find(".calender-day-list");

        list.PointerDown(new PointerEventArgs { ClientX = 250, ClientY = 300 });
        list.PointerUp(new PointerEventArgs { ClientX = 50, ClientY = 300 });

        _viewModel.DidNotReceive().MoveSelectedDay(Arg.Any<bool>());
    }

    [Fact]
    public void ShortOrVerticalMoveOnNotepads_Collapsed_DoesNotMoveSelectedDay()
    {
        _viewModel.IsCalendarCollapsed.Returns(true);
        var cut = Render<CalenderPage>();
        var list = cut.Find(".calender-day-list");

        list.PointerDown(new PointerEventArgs { ClientX = 200, ClientY = 300 });
        list.PointerUp(new PointerEventArgs { ClientX = 170, ClientY = 300 });
        list.PointerDown(new PointerEventArgs { ClientX = 200, ClientY = 300 });
        list.PointerUp(new PointerEventArgs { ClientX = 120, ClientY = 500 });

        _viewModel.DidNotReceive().MoveSelectedDay(Arg.Any<bool>());
    }

    [Fact]
    public void SwipeOverTask_Collapsed_MovesDayWithoutOpeningActionPopup()
    {
        _viewModel.IsCalendarCollapsed.Returns(true);
        var task = new TaskDto { Id = Guid.NewGuid(), Note = "Aufgabe" };
        IReadOnlyList<TaskDto> toDo = [task];
        _viewModel.ToDoTasks.Returns(toDo);
        var cut = Render<CalenderPage>();
        var item = cut.Find(".todo-book-task-item");

        item.PointerDown(new PointerEventArgs { ClientX = 250, ClientY = 300 });
        item.PointerUp(new PointerEventArgs { ClientX = 100, ClientY = 300 });
        item.Click();

        _viewModel.Received(1).MoveSelectedDay(true);
        _viewModel.DidNotReceive().OpenTaskActionPopup(Arg.Any<Guid>());
    }

    [Fact]
    public void ClickDay_SelectsThatDay()
    {
        var cut = Render<CalenderPage>();

        cut.FindAll(".calender-day")[0].Click();

        _viewModel.Received(1).SelectDay(FirstVisibleDay);
    }

    [Fact]
    public void ClickNextMonth_NavigatesToNextMonth()
    {
        var cut = Render<CalenderPage>();

        cut.Find($"button[aria-label='{Text("NextMonth")}']").Click();

        _viewModel.Received(1).GoToNextMonth();
    }

    [Fact]
    public void ClickPreviousMonth_NavigatesToPreviousMonth()
    {
        var cut = Render<CalenderPage>();

        cut.Find($"button[aria-label='{Text("PreviousMonth")}']").Click();

        _viewModel.Received(1).GoToPreviousMonth();
    }

    [Fact]
    public void ClickToday_JumpsToToday()
    {
        var cut = Render<CalenderPage>();

        cut.Find(".calender-today-button").Click();

        _viewModel.Received(1).GoToToday();
    }

    [Fact]
    public void ClickAddForYou_OpensEditorForPositiveTask()
    {
        var cut = Render<CalenderPage>();
        var addButton = cut.Find($"button[aria-label='{Text("AddForYouTask")}']");

        addButton.Click();

        _viewModel.Received(1).OpenTaskEditorForNewTask(true);
        _viewModel.DidNotReceive().OpenTaskEditorForNewTask(false);
    }

    [Fact]
    public void ClickAddToDo_OpensEditorForNormalTask()
    {
        var cut = Render<CalenderPage>();
        var addButton = cut.Find($"button[aria-label='{Text("AddToDoTask")}']");

        addButton.Click();

        _viewModel.Received(1).OpenTaskEditorForNewTask(false);
        _viewModel.DidNotReceive().OpenTaskEditorForNewTask(true);
    }

    [Fact]
    public void ActionPopup_ActiveTask_ShowsStopTask()
    {
        OpenActionPopup(isSelectedTaskActive: true);

        var cut = Render<CalenderPage>();

        var startButton = cut.Find(".todo-action-button.start");
        Assert.Equal(Text("StopTask"), startButton.TextContent.Trim());
    }

    [Fact]
    public void ActionPopup_InactiveTask_ShowsStartTask()
    {
        OpenActionPopup(isSelectedTaskActive: false);

        var cut = Render<CalenderPage>();

        var startButton = cut.Find(".todo-action-button.start");
        Assert.Equal(Text("StartTask"), startButton.TextContent.Trim());
    }

    [Fact]
    public void ActionPopup_ActionsFollowViewModelPermissions()
    {
        OpenActionPopup(isSelectedTaskActive: true);
        _viewModel.CanStartSelectedTask.Returns(true);
        _viewModel.CanCompleteSelectedTask.Returns(false);
        _viewModel.CanDeleteSelectedTask.Returns(false);

        var cut = Render<CalenderPage>();

        var startButton = cut.Find(".todo-action-button.start");
        var doneButton = cut.Find(".todo-action-button.done");
        var deleteButton = cut.Find(".todo-action-button.delete");
        var editButton = cut.Find(".todo-action-button.edit");
        Assert.False(startButton.HasAttribute("disabled"));
        Assert.True(doneButton.HasAttribute("disabled"));
        Assert.True(deleteButton.HasAttribute("disabled"));
        Assert.False(editButton.HasAttribute("disabled"));
    }

    [Fact]
    public void ClickEdit_ActiveTask_NavigatesToFocusTimerWithoutOpeningEditor()
    {
        OpenActionPopup(isSelectedTaskActive: true);
        var cut = Render<CalenderPage>();
        var editButton = cut.Find(".todo-action-button.edit");

        editButton.Click();

        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/focus-timer", navigation.Uri);
        _viewModel.Received(1).CloseTaskActionPopup();
        _viewModel.DidNotReceive().StartEditSelectedTask();
    }

    [Fact]
    public void ClickEdit_InactiveTask_OpensEditorWithoutNavigation()
    {
        OpenActionPopup(isSelectedTaskActive: false);
        var cut = Render<CalenderPage>();
        var navigation = Services.GetRequiredService<NavigationManager>();
        var uriBefore = navigation.Uri;
        var editButton = cut.Find(".todo-action-button.edit");

        editButton.Click();

        _viewModel.Received(1).StartEditSelectedTask();
        Assert.Equal(uriBefore, navigation.Uri);
    }

    [Fact]
    public void ClickStart_ActionSucceeds_NavigatesToFocusTimer()
    {
        OpenActionPopup(isSelectedTaskActive: false);
        _viewModel.CanStartSelectedTask.Returns(true);
        _viewModel.ExecutePrimarySelectedTaskActionAsync().Returns(true);
        var cut = Render<CalenderPage>();
        var startButton = cut.Find(".todo-action-button.start");

        startButton.Click();

        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/focus-timer", navigation.Uri);
    }

    [Fact]
    public void ClickStart_ActionDoesNothing_StaysOnPage()
    {
        OpenActionPopup(isSelectedTaskActive: true);
        _viewModel.CanStartSelectedTask.Returns(true);
        _viewModel.ExecutePrimarySelectedTaskActionAsync().Returns(false);
        var cut = Render<CalenderPage>();
        var navigation = Services.GetRequiredService<NavigationManager>();
        var uriBefore = navigation.Uri;
        var startButton = cut.Find(".todo-action-button.start");

        startButton.Click();

        _viewModel.Received(1).ExecutePrimarySelectedTaskActionAsync();
        Assert.Equal(uriBefore, navigation.Uri);
    }

    [Fact]
    public void ClickComplete_MarksTaskAsDone()
    {
        OpenActionPopup(isSelectedTaskActive: false);
        _viewModel.CanCompleteSelectedTask.Returns(true);
        var cut = Render<CalenderPage>();
        var doneButton = cut.Find(".todo-action-button.done");

        doneButton.Click();

        _viewModel.Received(1).MarkSelectedTaskAsDoneAsync();
    }

    [Fact]
    public void ClickDelete_DeletesTask()
    {
        OpenActionPopup(isSelectedTaskActive: false);
        _viewModel.CanDeleteSelectedTask.Returns(true);
        var cut = Render<CalenderPage>();
        var deleteButton = cut.Find(".todo-action-button.delete");

        deleteButton.Click();

        _viewModel.Received(1).DeleteSelectedTaskAsync();
    }

    [Fact]
    public void ClickOverlay_ClosesActionPopup()
    {
        OpenActionPopup(isSelectedTaskActive: false);
        var cut = Render<CalenderPage>();
        var overlay = cut.Find(".todo-action-overlay");

        overlay.Click();

        _viewModel.Received(1).CloseTaskActionPopup();
    }

    private void OpenActionPopup(bool isSelectedTaskActive)
    {
        _viewModel.IsTaskActionPopupOpen.Returns(true);
        _viewModel.IsSelectedActionTaskActive.Returns(isSelectedTaskActive);
    }

    private static IReadOnlyList<CalenderWeekModel> BuildWeeks(CalenderDayCellModel? override15March = null)
    {
        var weeks = new List<CalenderWeekModel>();
        for (var weekIndex = 0; weekIndex < 6; weekIndex++)
        {
            var days = new List<CalenderDayCellModel>();
            for (var dayIndex = 0; dayIndex < 7; dayIndex++)
            {
                var date = FirstVisibleDay.AddDays(weekIndex * 7 + dayIndex);
                days.Add(override15March is not null && date == override15March.Date
                    ? override15March
                    : CreateDay(date));
            }

            weeks.Add(new CalenderWeekModel { WeekNumber = 9 + weekIndex, Days = days });
        }

        return weeks;
    }

    private static CalenderDayCellModel CreateDay(DateTime date)
    {
        return new CalenderDayCellModel
        {
            Date = date,
            IsInCurrentMonth = date.Month == 3,
            IsToday = date == SelectedDay,
            IsSelected = date == SelectedDay
        };
    }
}
