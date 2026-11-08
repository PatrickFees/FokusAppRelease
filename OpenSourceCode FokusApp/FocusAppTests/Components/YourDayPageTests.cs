using Bunit;
using FokusApp.Components.Calender;
using FokusApp.Components.YourDay;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Components.PopUps.TaskSeriesChoice;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FocusAppTests.Components;

public class YourDayPageTests : ComponentTestContext
{
    private static readonly DateTime FirstVisibleDay = new(2026, 2, 23);
    private static readonly DateTime SelectedDay = new(2026, 3, 15);

    private readonly IYourDayViewModel _viewModel = Substitute.For<IYourDayViewModel>();

    public YourDayPageTests()
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
        Render<YourDayPage>();

        _viewModel.Received(1).InitializeAsync();
    }

    [Fact]
    public void Render_JustCompletedTask_HasSingleDoneAnimationClass()
    {
        var justDone = new TaskDto { Id = Guid.NewGuid(), Note = "Gerade fertig", IsDone = true };
        var earlierDone = new TaskDto { Id = Guid.NewGuid(), Note = "Schon lange fertig", IsDone = true };
        IReadOnlyList<TaskDto> toDo = [justDone, earlierDone];
        _viewModel.ToDoTasks.Returns(toDo);
        _viewModel.RecentlyCompletedTaskId.Returns(justDone.Id);

        var cut = Render<YourDayPage>();

        var animated = cut.FindAll(".todo-book-task-item.just-done");
        var item = Assert.Single(animated);
        Assert.Contains("Gerade fertig", item.TextContent);
        Assert.Equal(2, cut.FindAll(".todo-book-task-item.is-done").Count);
    }

    [Fact]
    public void Render_JustCompletedTaskInForYou_GetsTheSameAnimationAsToDo()
    {
        var done = new TaskDto { Id = Guid.NewGuid(), Note = "Für mich", IsDone = true, IsPositive = true };
        IReadOnlyList<TaskDto> forYou = [done];
        _viewModel.ForYouTasks.Returns(forYou);
        _viewModel.RecentlyCompletedTaskId.Returns(done.Id);

        var cut = Render<YourDayPage>();

        Assert.Single(cut.FindAll(".todo-book-task-item.just-done"));
    }

    [Fact]
    public void Render_NoRecentCompletion_HasNoDoneAnimation()
    {
        var done = new TaskDto { Id = Guid.NewGuid(), Note = "Alt", IsDone = true };
        IReadOnlyList<TaskDto> toDo = [done];
        _viewModel.ToDoTasks.Returns(toDo);

        var cut = Render<YourDayPage>();

        Assert.Empty(cut.FindAll(".just-done"));
        Assert.Single(cut.FindAll(".todo-book-task-item.is-done"));
    }

    [Fact]
    public void Render_OpenTaskWithRecentCompletedId_HasNoDoneAnimation()
    {
        var open = new TaskDto { Id = Guid.NewGuid(), Note = "Offen" };
        IReadOnlyList<TaskDto> toDo = [open];
        _viewModel.ToDoTasks.Returns(toDo);
        _viewModel.RecentlyCompletedTaskId.Returns(open.Id);

        var cut = Render<YourDayPage>();

        Assert.Empty(cut.FindAll(".just-done"));
    }

    [Fact]
    public void Render_NoPastTasks_ShowsNoPastTasksHint()
    {
        var cut = Render<YourDayPage>();

        Assert.Empty(cut.FindAll(".your-day-past-hint"));
    }

    [Fact]
    public void Render_OnePastTask_ShowsSingularHint()
    {
        _viewModel.PastTaskCount.Returns(1);

        var cut = Render<YourDayPage>();

        Assert.Equal(Text("PastTasksHintOne"), cut.Find(".your-day-past-hint").TextContent.Trim());
    }

    [Fact]
    public void Render_ThreePastTasks_ShowsPluralHintWithCount()
    {
        _viewModel.PastTaskCount.Returns(3);

        var cut = Render<YourDayPage>();

        Assert.Equal(Text("PastTasksHintMany", 3), cut.Find(".your-day-past-hint").TextContent.Trim());
    }

    [Fact]
    public void Render_PastTasks_HintTextAvoidsGuiltWording()
    {
        _viewModel.PastTaskCount.Returns(2);

        var cut = Render<YourDayPage>();

        var text = cut.Find(".your-day-past-hint").TextContent.ToLowerInvariant();
        Assert.DoesNotContain("versäumt", text);
        Assert.DoesNotContain("überfällig", text);
    }

    [Fact]
    public void ClickPastTasksHint_OpensPastTasks()
    {
        _viewModel.PastTaskCount.Returns(2);
        var cut = Render<YourDayPage>();

        cut.Find(".your-day-past-hint").Click();

        _viewModel.Received(1).OpenPastTasks();
    }
    [Fact]
    public void Render_Month_ShowsSixWeeksWithSevenDaysEach()
    {
        var cut = Render<YourDayPage>();

        Assert.Equal(6, cut.FindAll(".calender-week-row").Count);
        Assert.Equal(42, cut.FindAll(".calender-day").Count);
        Assert.Equal(7, cut.FindAll(".calender-weekday-cell").Count);
    }

    [Fact]
    public void Render_Weeks_ShowsCalendarWeekNumbers()
    {
        var cut = Render<YourDayPage>();

        var weekNumbers = cut.FindAll(".calender-week-number").Select(element => element.TextContent).ToArray();

        Assert.Equal(["9", "10", "11", "12", "13", "14"], weekNumbers);
    }

    [Fact]
    public void Render_Month_MarksTodayAndSelectedDay()
    {
        var cut = Render<YourDayPage>();

        Assert.Single(cut.FindAll(".calender-day.is-today"));
        Assert.Single(cut.FindAll(".calender-day.is-selected"));
        Assert.Equal(SelectedDay.Day.ToString(), cut.Find(".calender-day.is-selected .calender-day-number").TextContent);
    }

    [Fact]
    public void Render_DaysOutsideMonth_AreMarkedAsOutside()
    {
        var cut = Render<YourDayPage>();

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

        var cut = Render<YourDayPage>();

        var selectedCell = cut.Find(".calender-day.is-selected");
        Assert.Equal(2, selectedCell.QuerySelectorAll(".calender-dot").Length);
        Assert.Single(selectedCell.QuerySelectorAll(".calender-dot.is-done"));
        Assert.Single(selectedCell.QuerySelectorAll(".calender-dot-more"));
    }

    [Fact]
    public void DoubleClickDay_OpensTheEditorForThatDay()
    {
        var cut = Render<YourDayPage>();

        cut.Find(".calender-day.is-selected").DoubleClick();

        _viewModel.Received(1).OpenTaskEditorForNewTask(SelectedDay);
    }

    [Fact]
    public void Render_NoTasksForDay_ShowsEmptyHint()
    {
        var cut = Render<YourDayPage>();

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

        var cut = Render<YourDayPage>();

        var items = cut.FindAll(".todo-book-task-item");
        Assert.Equal(2, items.Count);
        Assert.DoesNotContain(Text("Done"), items[0].TextContent);
        Assert.Contains(Text("Done"), items[1].TextContent);
        Assert.Equal(2, cut.FindAll(".todo-book-section").Count);
        Assert.Single(cut.FindAll(".todo-book-empty"));
    }

    [Fact]
    public void ClickTask_OpensActionPopupForTask()
    {
        var task = new TaskDto { Id = Guid.NewGuid(), Note = "Aufgabe" };
        IReadOnlyList<TaskDto> toDo = [task];
        _viewModel.ToDoTasks.Returns(toDo);
        var cut = Render<YourDayPage>();

        cut.Find(".todo-book-task-item").Click();

        _viewModel.Received(1).OpenTaskActionPopup(task.Id!.Value);
    }

    [Fact]
    public void ClickCollapseToggle_TogglesCalendarCollapsed()
    {
        var cut = Render<YourDayPage>();

        cut.Find($"button[aria-label='{Text("CollapseCalendar")}']").Click();

        _viewModel.Received(1).ToggleCalendarCollapsed();
    }

    [Fact]
    public void Render_Collapsed_HidesOtherWeeksOffersExpandAndDoesNotDimDays()
    {
        _viewModel.IsCalendarCollapsed.Returns(true);

        var cut = Render<YourDayPage>();

        Assert.Equal(5, cut.FindAll(".calender-week-row.is-collapsed").Count);
        Assert.Single(cut.FindAll(".calender-week-row:not(.is-collapsed)"));
        Assert.Empty(cut.FindAll(".calender-day.outside-month"));
        Assert.NotNull(cut.Find($"button[aria-label='{Text("ExpandCalendar")}']"));
    }

    [Fact]
    public void ClickArrows_Collapsed_MoveSelectedDayInsteadOfMonth()
    {
        _viewModel.IsCalendarCollapsed.Returns(true);
        var cut = Render<YourDayPage>();

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
        var cut = Render<YourDayPage>();
        var list = cut.Find(".your-day-day-list");

        list.PointerDown(new PointerEventArgs { ClientX = startX, ClientY = 300 });
        list.PointerUp(new PointerEventArgs { ClientX = endX, ClientY = 305 });

        _viewModel.Received(1).MoveSelectedDay(forwards);
    }

    [Fact]
    public void SwipeOnNotepads_Expanded_DoesNotMoveSelectedDay()
    {
        var cut = Render<YourDayPage>();
        var list = cut.Find(".your-day-day-list");

        list.PointerDown(new PointerEventArgs { ClientX = 250, ClientY = 300 });
        list.PointerUp(new PointerEventArgs { ClientX = 50, ClientY = 300 });

        _viewModel.DidNotReceive().MoveSelectedDay(Arg.Any<bool>());
    }

    [Fact]
    public void ShortOrVerticalMoveOnNotepads_Collapsed_DoesNotMoveSelectedDay()
    {
        _viewModel.IsCalendarCollapsed.Returns(true);
        var cut = Render<YourDayPage>();
        var list = cut.Find(".your-day-day-list");

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
        var cut = Render<YourDayPage>();
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
        var cut = Render<YourDayPage>();

        cut.FindAll(".calender-day")[0].Click();

        _viewModel.Received(1).SelectDay(FirstVisibleDay);
    }

    [Fact]
    public void ClickNextMonth_NavigatesToNextMonth()
    {
        var cut = Render<YourDayPage>();

        cut.Find($"button[aria-label='{Text("NextMonth")}']").Click();

        _viewModel.Received(1).GoToNextMonth();
    }

    [Fact]
    public void ClickPreviousMonth_NavigatesToPreviousMonth()
    {
        var cut = Render<YourDayPage>();

        cut.Find($"button[aria-label='{Text("PreviousMonth")}']").Click();

        _viewModel.Received(1).GoToPreviousMonth();
    }

    [Fact]
    public void ClickToday_JumpsToToday()
    {
        var cut = Render<YourDayPage>();

        cut.Find(".your-day-today-button").Click();

        _viewModel.Received(1).GoToToday();
    }

    [Fact]
    public void ClickAddForYou_OpensEditorForPositiveTask()
    {
        var cut = Render<YourDayPage>();
        var addButton = cut.Find($"button[aria-label='{Text("AddForYouTask")}']");

        addButton.Click();

        _viewModel.Received(1).OpenTaskEditorForNewTask(true);
        _viewModel.DidNotReceive().OpenTaskEditorForNewTask(false);
    }

    [Fact]
    public void ClickAddToDo_OpensEditorForNormalTask()
    {
        var cut = Render<YourDayPage>();
        var addButton = cut.Find($"button[aria-label='{Text("AddToDoTask")}']");

        addButton.Click();

        _viewModel.Received(1).OpenTaskEditorForNewTask(false);
        _viewModel.DidNotReceive().OpenTaskEditorForNewTask(true);
    }

    [Fact]
    public void ActionPopup_ActiveTask_ShowsTimerRunning()
    {
        OpenActionPopup(isSelectedTaskActive: true);

        var cut = Render<YourDayPage>();

        var startButton = cut.Find(".todo-action-button.start");
        Assert.Equal(Text("TimerRunning"), startButton.TextContent.Trim());
    }

    [Fact]
    public void ActionPopup_InactiveTask_ShowsStartTask()
    {
        OpenActionPopup(isSelectedTaskActive: false);

        var cut = Render<YourDayPage>();

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

        var cut = Render<YourDayPage>();

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
        var cut = Render<YourDayPage>();
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
        var cut = Render<YourDayPage>();
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
        var cut = Render<YourDayPage>();
        var startButton = cut.Find(".todo-action-button.start");

        startButton.Click();

        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/focus-timer", navigation.Uri);
    }

    [Fact]
    public void ClickStart_ActiveTask_NavigatesToFocusTimerWithoutPausing()
    {
        OpenActionPopup(isSelectedTaskActive: true);
        _viewModel.CanStartSelectedTask.Returns(true);
        var cut = Render<YourDayPage>();
        var startButton = cut.Find(".todo-action-button.start");

        startButton.Click();

        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/focus-timer", navigation.Uri);
        _viewModel.Received(1).CloseTaskActionPopup();
        _viewModel.DidNotReceive().ExecutePrimarySelectedTaskActionAsync();
    }

    [Fact]
    public void ClickComplete_MarksTaskAsDone()
    {
        OpenActionPopup(isSelectedTaskActive: false);
        _viewModel.CanCompleteSelectedTask.Returns(true);
        var cut = Render<YourDayPage>();
        var doneButton = cut.Find(".todo-action-button.done");

        doneButton.Click();

        _viewModel.Received(1).MarkSelectedTaskAsDoneAsync();
    }

    [Fact]
    public void ClickDelete_DeletesTask()
    {
        OpenActionPopup(isSelectedTaskActive: false);
        _viewModel.CanDeleteSelectedTask.Returns(true);
        var cut = Render<YourDayPage>();
        var deleteButton = cut.Find(".todo-action-button.delete");

        deleteButton.Click();

        _viewModel.Received(1).DeleteSelectedTaskAsync();
    }

    [Fact]
    public void ClickOverlay_ClosesActionPopup()
    {
        OpenActionPopup(isSelectedTaskActive: false);
        var cut = Render<YourDayPage>();
        var overlay = cut.Find(".todo-action-overlay");

        overlay.Click();

        _viewModel.Received(1).CloseTaskActionPopup();
    }

    [Fact]
    public void Render_SeriesChoiceClosed_ShowsNoSeriesPopup()
    {
        var cut = Render<YourDayPage>();

        Assert.Empty(cut.FindAll(".series-choice-popup"));
    }

    [Fact]
    public void Render_SeriesChoiceOpenForDelete_AsksAboutDeletingTheSeries()
    {
        _viewModel.IsSeriesChoiceOpen.Returns(true);
        _viewModel.PendingSeriesAction.Returns(TaskSeriesAction.Delete);

        var cut = Render<YourDayPage>();

        var text = cut.Find(".series-choice-text");
        Assert.Equal(Text("SeriesChoiceDeleteText"), text.TextContent);
        Assert.Contains(Text("SeriesChoiceTitle"), cut.Find(".series-choice-popup h3").TextContent);
    }

    [Fact]
    public void Render_SeriesChoiceOpenForEdit_AsksAboutEditingTheSeries()
    {
        _viewModel.IsSeriesChoiceOpen.Returns(true);
        _viewModel.PendingSeriesAction.Returns(TaskSeriesAction.Edit);

        var cut = Render<YourDayPage>();

        var text = cut.Find(".series-choice-text");
        Assert.Equal(Text("SeriesChoiceEditText"), text.TextContent);
    }

    [Fact]
    public void ClickThisTaskOnly_ChoosesTheSingleTask()
    {
        _viewModel.IsSeriesChoiceOpen.Returns(true);
        var cut = Render<YourDayPage>();
        var button = cut.Find(".series-choice-button.this-task");

        button.Click();

        _viewModel.Received(1).ChooseSeriesScopeAsync(TaskSeriesScope.ThisTaskOnly);
    }

    [Fact]
    public void ClickWholeSeries_ChoosesTheWholeSeries()
    {
        _viewModel.IsSeriesChoiceOpen.Returns(true);
        var cut = Render<YourDayPage>();
        var button = cut.Find(".series-choice-button.whole-series");

        button.Click();

        _viewModel.Received(1).ChooseSeriesScopeAsync(TaskSeriesScope.WholeSeries);
    }

    [Fact]
    public void ClickCancelInSeriesChoice_CancelsTheChoice()
    {
        _viewModel.IsSeriesChoiceOpen.Returns(true);
        var cut = Render<YourDayPage>();
        var button = cut.Find(".series-choice-button.cancel");

        button.Click();

        _viewModel.Received(1).CancelSeriesChoice();
    }

    [Fact]
    public void ClickOverlayOfSeriesChoice_CancelsTheChoice()
    {
        _viewModel.IsSeriesChoiceOpen.Returns(true);
        var cut = Render<YourDayPage>();
        var overlay = cut.Find(".series-choice-overlay");

        overlay.Click();

        _viewModel.Received(1).CancelSeriesChoice();
    }

    [Fact]
    public void Render_SeriesChoiceOpenForDelete_MarksTheWholeSeriesButtonAsDestructive()
    {
        _viewModel.IsSeriesChoiceOpen.Returns(true);
        _viewModel.PendingSeriesAction.Returns(TaskSeriesAction.Delete);

        var cut = Render<YourDayPage>();

        Assert.Single(cut.FindAll(".series-choice-button.whole-series.is-delete"));
    }

    [Fact]
    public void Render_Page_SetsThePageTitle()
    {
        var head = Render<HeadOutlet>();

        Render<YourDayPage>();

        head.WaitForAssertion(() => Assert.Contains(Text("YourDay"), head.Markup));
    }

    [Fact]
    public void SwipeOnDay_Expanded_ChangesMonthAndSuppressesTheClick()
    {
        var cut = Render<YourDayPage>();
        AngleSharp.Dom.IElement Day() => FindDayButton(cut, new DateTime(2026, 3, 10));

        Day().PointerDown(new PointerEventArgs { ClientX = 250, ClientY = 300 });
        Day().PointerUp(new PointerEventArgs { ClientX = 100, ClientY = 305 });
        Day().Click();

        _viewModel.Received(1).GoToNextMonth();
        _viewModel.DidNotReceive().SelectDay(Arg.Any<DateTime>());
    }

    [Fact]
    public void SwipeOnDayToTheRight_Expanded_ShowsPreviousMonth()
    {
        var cut = Render<YourDayPage>();
        AngleSharp.Dom.IElement Day() => FindDayButton(cut, new DateTime(2026, 3, 10));

        Day().PointerDown(new PointerEventArgs { ClientX = 100, ClientY = 300 });
        Day().PointerUp(new PointerEventArgs { ClientX = 250, ClientY = 300 });

        _viewModel.Received(1).GoToPreviousMonth();
    }

    [Theory]
    [InlineData(250, 100, true)]
    [InlineData(100, 250, false)]
    public void SwipeOnDay_Collapsed_MovesSelectedDay(double startX, double endX, bool forwards)
    {
        _viewModel.IsCalendarCollapsed.Returns(true);
        var cut = Render<YourDayPage>();
        AngleSharp.Dom.IElement Day() => FindDayButton(cut, SelectedDay);

        Day().PointerDown(new PointerEventArgs { ClientX = startX, ClientY = 300 });
        Day().PointerUp(new PointerEventArgs { ClientX = endX, ClientY = 300 });

        _viewModel.Received(1).MoveSelectedDay(forwards);
    }

    [Fact]
    public void TapOnDay_ShortPress_SelectsTheDayWithoutOpeningTheEditor()
    {
        var cut = Render<YourDayPage>();
        AngleSharp.Dom.IElement Day() => FindDayButton(cut, new DateTime(2026, 3, 10));

        Day().PointerDown(new PointerEventArgs { ClientX = 100, ClientY = 100 });
        Day().PointerUp(new PointerEventArgs { ClientX = 102, ClientY = 101 });
        Day().Click();

        _viewModel.Received(1).SelectDay(new DateTime(2026, 3, 10));
        _viewModel.DidNotReceive().OpenTaskEditorForNewTask(Arg.Any<DateTime>());
    }

    [Fact]
    public async Task LongPressOnDay_WithoutMoving_OpensTheEditorForThatDayAndSuppressesTheClick()
    {
        var cut = Render<YourDayPage>();
        AngleSharp.Dom.IElement Day() => FindDayButton(cut, new DateTime(2026, 3, 10));

        Day().PointerDown(new PointerEventArgs { ClientX = 100, ClientY = 100 });
        await Task.Delay(520);
        Day().PointerUp(new PointerEventArgs { ClientX = 101, ClientY = 100 });
        Day().Click();

        _viewModel.Received(1).OpenTaskEditorForNewTask(new DateTime(2026, 3, 10));
        _viewModel.DidNotReceive().SelectDay(Arg.Any<DateTime>());
    }

    [Fact]
    public async Task LongPressOnDay_MovedTooFar_OpensNoEditor()
    {
        var cut = Render<YourDayPage>();
        AngleSharp.Dom.IElement Day() => FindDayButton(cut, new DateTime(2026, 3, 10));

        Day().PointerDown(new PointerEventArgs { ClientX = 100, ClientY = 100 });
        await Task.Delay(520);
        Day().PointerUp(new PointerEventArgs { ClientX = 100, ClientY = 130 });

        _viewModel.DidNotReceive().OpenTaskEditorForNewTask(Arg.Any<DateTime>());
        _viewModel.DidNotReceive().GoToNextMonth();
        _viewModel.DidNotReceive().GoToPreviousMonth();
    }

    [Fact]
    public void PointerUp_OnOtherDayThanPressed_IgnoresTheGesture()
    {
        var cut = Render<YourDayPage>();
        AngleSharp.Dom.IElement Pressed() => FindDayButton(cut, new DateTime(2026, 3, 10));
        AngleSharp.Dom.IElement Released() => FindDayButton(cut, new DateTime(2026, 3, 11));

        Pressed().PointerDown(new PointerEventArgs { ClientX = 250, ClientY = 300 });
        Released().PointerUp(new PointerEventArgs { ClientX = 100, ClientY = 300 });

        _viewModel.DidNotReceive().GoToNextMonth();
    }

    [Fact]
    public void PointerUp_WithoutPointerDown_IgnoresTheGesture()
    {
        var cut = Render<YourDayPage>();
        AngleSharp.Dom.IElement Day() => FindDayButton(cut, new DateTime(2026, 3, 10));

        Day().PointerUp(new PointerEventArgs { ClientX = 100, ClientY = 300 });

        _viewModel.DidNotReceive().GoToNextMonth();
        _viewModel.DidNotReceive().OpenTaskEditorForNewTask(Arg.Any<DateTime>());
    }

    [Theory]
    [InlineData("onpointerleave")]
    [InlineData("onpointercancel")]
    public void PointerLeaveOrCancel_DuringGesture_ResetsTheGesture(string resetEvent)
    {
        var cut = Render<YourDayPage>();
        AngleSharp.Dom.IElement Day() => FindDayButton(cut, new DateTime(2026, 3, 10));
        Day().PointerDown(new PointerEventArgs { ClientX = 250, ClientY = 300 });

        Day().TriggerEvent(resetEvent, new PointerEventArgs());
        Day().PointerUp(new PointerEventArgs { ClientX = 100, ClientY = 300 });

        _viewModel.DidNotReceive().GoToNextMonth();
    }

    [Fact]
    public void PointerDown_ActionPopupOpen_IgnoresTheGesture()
    {
        _viewModel.IsTaskActionPopupOpen.Returns(true);
        var cut = Render<YourDayPage>();
        AngleSharp.Dom.IElement Day() => FindDayButton(cut, new DateTime(2026, 3, 10));

        Day().PointerDown(new PointerEventArgs { ClientX = 250, ClientY = 300 });
        Day().PointerUp(new PointerEventArgs { ClientX = 100, ClientY = 300 });

        _viewModel.DidNotReceive().GoToNextMonth();
    }

    [Fact]
    public void PointerUp_ActionPopupOpenedMeanwhile_IgnoresTheGesture()
    {
        var cut = Render<YourDayPage>();
        AngleSharp.Dom.IElement Day() => FindDayButton(cut, new DateTime(2026, 3, 10));
        Day().PointerDown(new PointerEventArgs { ClientX = 250, ClientY = 300 });
        _viewModel.IsTaskActionPopupOpen.Returns(true);

        Day().PointerUp(new PointerEventArgs { ClientX = 100, ClientY = 300 });

        _viewModel.DidNotReceive().GoToNextMonth();
    }

    [Fact]
    public void SwipeOnNotepads_ActionPopupOpen_DoesNotMoveSelectedDay()
    {
        _viewModel.IsCalendarCollapsed.Returns(true);
        _viewModel.IsTaskActionPopupOpen.Returns(true);
        var cut = Render<YourDayPage>();
        var list = cut.Find(".your-day-day-list");

        list.PointerDown(new PointerEventArgs { ClientX = 250, ClientY = 300 });
        list.PointerUp(new PointerEventArgs { ClientX = 50, ClientY = 300 });

        _viewModel.DidNotReceive().MoveSelectedDay(Arg.Any<bool>());
    }

    [Fact]
    public void PointerUpOnNotepads_WithoutPointerDown_DoesNotMoveSelectedDay()
    {
        _viewModel.IsCalendarCollapsed.Returns(true);
        var cut = Render<YourDayPage>();
        var list = cut.Find(".your-day-day-list");

        list.PointerUp(new PointerEventArgs { ClientX = 50, ClientY = 300 });

        _viewModel.DidNotReceive().MoveSelectedDay(Arg.Any<bool>());
    }

    [Fact]
    public void PointerCancelOnNotepads_DuringSwipe_ResetsTheGesture()
    {
        _viewModel.IsCalendarCollapsed.Returns(true);
        var cut = Render<YourDayPage>();
        var list = cut.Find(".your-day-day-list");
        list.PointerDown(new PointerEventArgs { ClientX = 250, ClientY = 300 });

        list.TriggerEvent("onpointercancel", new PointerEventArgs());
        list.PointerUp(new PointerEventArgs { ClientX = 50, ClientY = 300 });

        _viewModel.DidNotReceive().MoveSelectedDay(Arg.Any<bool>());
    }

    [Fact]
    public void ClickTask_AfterNotepadSwipeSuppressedIt_OpensActionPopupOnNextClick()
    {
        var task = new TaskDto { Id = Guid.NewGuid(), Note = "Aufgabe" };
        IReadOnlyList<TaskDto> toDo = [task];
        _viewModel.ToDoTasks.Returns(toDo);
        _viewModel.IsCalendarCollapsed.Returns(true);
        var cut = Render<YourDayPage>();
        var list = cut.Find(".your-day-day-list");
        list.PointerDown(new PointerEventArgs { ClientX = 250, ClientY = 300 });
        list.PointerUp(new PointerEventArgs { ClientX = 50, ClientY = 300 });
        var item = cut.Find(".todo-book-task-item");

        item.Click();
        item.Click();

        _viewModel.Received(1).OpenTaskActionPopup(task.Id.Value);
    }

    private static AngleSharp.Dom.IElement FindDayButton(IRenderedComponent<YourDayPage> cut, DateTime date)
    {
        var index = (int)(date - FirstVisibleDay).TotalDays;
        return cut.FindAll(".calender-day")[index];
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
