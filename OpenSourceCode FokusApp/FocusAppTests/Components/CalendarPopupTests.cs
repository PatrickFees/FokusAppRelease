using System.Globalization;
using Bunit;
using FokusApp.Components.Calender;
using FokusApp.Components.PopUps.Calendar;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FocusAppTests.Components;

public class CalendarPopupTests : ComponentTestContext
{
    private static readonly DateTime MonthStart = new(2026, 3, 1);
    private static readonly DateTime FirstGridDay = new(2026, 2, 23);

    private readonly ICalendarPopupViewModel _viewModel = Substitute.For<ICalendarPopupViewModel>();
    private readonly List<DateTime> _confirmedDates = [];
    private int _cancelCount;

    public CalendarPopupTests()
    {
        _viewModel.VisibleMonth.Returns(MonthStart);
        _viewModel.Weeks.Returns(BuildWeeks(selected: new DateTime(2026, 3, 15), today: new DateTime(2026, 3, 12)));
        _viewModel.ForYouTasks.Returns([]);
        _viewModel.ToDoTasks.Returns([]);
        _viewModel.SelectedDate.Returns(new DateTime(2026, 3, 15));
        Services.AddSingleton(_viewModel);
    }

    [Fact]
    public void Render_Closed_ShowsNothingAndDoesNotOpenTheViewModel()
    {
        var cut = RenderPopup(isOpen: false);

        Assert.Equal(string.Empty, cut.Markup.Trim());
        _viewModel.DidNotReceive().Open(Arg.Any<DateTime>());
    }

    [Fact]
    public void Render_Open_OpensTheViewModelWithTheInitialDay()
    {
        RenderPopup(isOpen: true, initialDate: new DateTime(2026, 3, 15, 13, 45, 0));

        _viewModel.Received(1).Open(new DateTime(2026, 3, 15));
    }

    [Fact]
    public void Render_Open_ShowsMonthTitleWeekdaysAndSixWeeksOfDays()
    {
        var cut = RenderPopup(isOpen: true);

        var weekdays = cut.FindAll(".calender-weekday-cell");
        var days = cut.FindAll(".calender-day");
        Assert.Equal(MonthStart.ToString("MMMM", CultureInfo.CurrentCulture), GetSelectedOptionText(cut, "select.calendar-popup-month-select"));
        Assert.Equal(MonthStart.Year.ToString(CultureInfo.CurrentCulture), GetSelectedOptionText(cut, "select.calendar-popup-year-select"));
        Assert.Equal(7, weekdays.Count);
        Assert.Equal(AbbreviatedDayName(DayOfWeek.Monday), weekdays[0].TextContent);
        Assert.Equal(AbbreviatedDayName(DayOfWeek.Sunday), weekdays[6].TextContent);
        Assert.Equal(42, days.Count);
        Assert.Equal("23", days[0].TextContent.Trim());
    }

    [Fact]
    public void Render_Open_MarksOutsideTodayAndSelectedDays()
    {
        var cut = RenderPopup(isOpen: true);

        Assert.Equal(11, cut.FindAll(".calender-day.outside-month").Count);
        var today = Assert.Single(cut.FindAll(".calender-day.is-today"));
        var selected = Assert.Single(cut.FindAll(".calender-day.is-selected"));
        Assert.Equal("12", today.TextContent.Trim());
        Assert.Equal("15", selected.TextContent.Trim());
    }

    [Fact]
    public void Render_Open_IsAnAccessibleModalDialog()
    {
        var cut = RenderPopup(isOpen: true);

        var dialog = cut.Find(".calendar-popup-shell");
        Assert.Equal("dialog", dialog.GetAttribute("role"));
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
    }

    [Fact]
    public void ClickDay_Day_SelectsThatDayAtTheViewModel()
    {
        var cut = RenderPopup(isOpen: true);

        cut.FindAll(".calender-day")[17].Click();

        _viewModel.Received(1).SelectDate(new DateTime(2026, 3, 12));
    }

    [Fact]
    public void ClickMonthArrows_Always_NavigatesBetweenMonthsAtTheViewModel()
    {
        var cut = RenderPopup(isOpen: true);

        cut.Find($"button[aria-label=\"{Text("NextMonth")}\"]").Click();
        cut.Find($"button[aria-label=\"{Text("PreviousMonth")}\"]").Click();

        _viewModel.Received(1).GoToNextMonth();
        _viewModel.Received(1).GoToPreviousMonth();
    }

    [Fact]
    public void ClickOk_Open_ConfirmsTheSelectedDate()
    {
        var cut = RenderPopup(isOpen: true);

        cut.Find(".calendar-popup-actions .btn-primary").Click();

        var confirmed = Assert.Single(_confirmedDates);
        Assert.Equal(new DateTime(2026, 3, 15), confirmed);
    }

    [Fact]
    public void ClickCancel_Open_CancelsWithoutConfirming()
    {
        var cut = RenderPopup(isOpen: true);

        cut.Find(".calendar-popup-actions .btn-secondary").Click();

        Assert.Equal(1, _cancelCount);
        Assert.Empty(_confirmedDates);
    }

    [Theory]
    [InlineData(300, 100, -200)]
    [InlineData(100, 300, 200)]
    public void Swipe_HorizontalMove_ReportsTheDistanceToTheViewModel(double startX, double endX, double expectedDelta)
    {
        var cut = RenderPopup(isOpen: true);
        var shell = cut.Find(".calendar-popup-shell");

        shell.PointerDown(new PointerEventArgs { ClientX = startX });
        Shell(cut).PointerUp(new PointerEventArgs { ClientX = endX });

        _viewModel.Received(1).HandleHorizontalSwipe(expectedDelta);
    }

    [Fact]
    public void Swipe_PointerUpWithoutPointerDown_ReportsNothing()
    {
        var cut = RenderPopup(isOpen: true);

        Shell(cut).PointerUp(new PointerEventArgs { ClientX = 100 });

        _viewModel.DidNotReceive().HandleHorizontalSwipe(Arg.Any<double>());
    }

    [Theory]
    [InlineData("onpointerleave")]
    [InlineData("onpointercancel")]
    public void Swipe_PointerLeavesOrIsCancelled_ForgetsTheGesture(string resetEvent)
    {
        var cut = RenderPopup(isOpen: true);
        Shell(cut).PointerDown(new PointerEventArgs { ClientX = 300 });

        Shell(cut).TriggerEvent(resetEvent, new PointerEventArgs());
        Shell(cut).PointerUp(new PointerEventArgs { ClientX = 100 });

        _viewModel.DidNotReceive().HandleHorizontalSwipe(Arg.Any<double>());
    }

    [Fact]
    public void Swipe_SecondPointerUpAfterGesture_ReportsOnlyOnce()
    {
        var cut = RenderPopup(isOpen: true);
        Shell(cut).PointerDown(new PointerEventArgs { ClientX = 300 });
        Shell(cut).PointerUp(new PointerEventArgs { ClientX = 100 });

        Shell(cut).PointerUp(new PointerEventArgs { ClientX = 50 });

        _viewModel.Received(1).HandleHorizontalSwipe(Arg.Any<double>());
    }

    [Fact]
    public void Render_ClosedAndOpenedAgain_OpensTheViewModelForEachOpening()
    {
        var cut = RenderPopup(isOpen: true);

        cut.Render(parameters => parameters.Add(popup => popup.IsOpen, false));
        cut.Render(parameters => parameters.Add(popup => popup.IsOpen, true));

        _viewModel.Received(2).Open(Arg.Any<DateTime>());
    }

    [Fact]
    public void Render_StillOpenAfterParameterUpdate_DoesNotOpenTheViewModelAgain()
    {
        var cut = RenderPopup(isOpen: true);

        cut.Render(parameters => parameters.Add(popup => popup.InitialDate, new DateTime(2026, 3, 20)));

        _viewModel.Received(1).Open(Arg.Any<DateTime>());
    }

    [Fact]
    public void PropertyChanged_ViewModel_RendersTheNewMonth()
    {
        var cut = RenderPopup(isOpen: true);
        var april = new DateTime(2026, 4, 1);
        _viewModel.VisibleMonth.Returns(april);
        _viewModel.Weeks.Returns(BuildWeeks(new DateTime(2026, 4, 2), new DateTime(2026, 4, 1)));

        var args = new System.ComponentModel.PropertyChangedEventArgs(nameof(ICalendarPopupViewModel.Weeks));
        _viewModel.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(_viewModel, args);

        cut.WaitForAssertion(() => Assert.Equal(
            april.ToString("MMMM", CultureInfo.CurrentCulture), GetSelectedOptionText(cut, "select.calendar-popup-month-select")));
    }

    [Fact]
    public void Render_DayWithTasks_ShowsPriorityDotsLikeTheCalendarPage()
    {
        var day = new DateTime(2026, 3, 18);
        _viewModel.Weeks.Returns(BuildWeeks(
            new DateTime(2026, 3, 15),
            new DateTime(2026, 3, 12),
            date => date == day ? [new CalenderDotModel { Priority = TaskPriority.Hoch }, new CalenderDotModel { IsDone = true }] : []));

        var cut = RenderPopup(isOpen: true);

        var dots = cut.FindAll(".calender-dot");
        Assert.Equal(2, dots.Count);
        Assert.Contains("is-done", dots[1].ClassList);
    }

    [Fact]
    public void Render_TasksOfSelectedDay_ListsForYouAndToDoWithoutActions()
    {
        _viewModel.ForYouTasks.Returns([TestTask("Sport", isDone: false), TestTask("Lesen", isDone: true)]);
        _viewModel.ToDoTasks.Returns([TestTask("Steuer", isDone: false)]);

        var cut = RenderPopup(isOpen: true);

        var sections = cut.FindAll(".calendar-popup-task-section");
        Assert.Equal(2, sections.Count);
        Assert.Equal(Text("TaskTypeForYou"), sections[0].QuerySelector("h4")!.TextContent);
        Assert.Equal(["Sport", "Lesen"], sections[0].QuerySelectorAll(".calendar-popup-task-note").Select(note => note.TextContent));
        Assert.Contains("is-done", sections[0].QuerySelectorAll(".calendar-popup-task")[1].ClassList);
        Assert.Equal(Text("TaskTypeToDo"), sections[1].QuerySelector("h4")!.TextContent);
        Assert.Empty(cut.FindAll(".calendar-popup-day-list button"));
    }

    [Fact]
    public void Render_DayWithoutTasks_ShowsEmptyHintInBothSections()
    {
        var cut = RenderPopup(isOpen: true);

        var hints = cut.FindAll(".calendar-popup-task-empty");
        Assert.Equal(2, hints.Count);
        Assert.All(hints, hint => Assert.Equal(Text("NoTasksForDay"), hint.TextContent));
    }

    [Fact]
    public void ChangeYearSelect_FarAwayYear_SelectsExactlyThatYearEvenAsTheOptionWindowShifts()
    {
        var (cut, realViewModel) = RenderPopupWithRealViewModel();

        cut.Find("select.calendar-popup-year-select").Change("2036");
        cut.Find("select.calendar-popup-year-select").Change("2020");
        cut.Find("select.calendar-popup-year-select").Change("2036");

        Assert.Equal(2036, realViewModel.VisibleMonth.Year);
        Assert.Equal("2036", GetSelectedOptionText(cut, "select.calendar-popup-year-select"));
    }

    [Fact]
    public void ChangeYearSelect_WithoutTappingADay_ConfirmsADateInTheNewYearAnyway()
    {
        var (cut, _) = RenderPopupWithRealViewModel();

        cut.Find("select.calendar-popup-year-select").Change("2036");
        cut.Find(".btn-primary").Click();

        var confirmed = Assert.Single(_confirmedDates);
        Assert.Equal(new DateTime(2036, 3, 15), confirmed);
    }

    [Fact]
    public void ChangeYearThenClickDayThenConfirm_ConfirmsTheTappedDayInTheNewYear()
    {
        var (cut, _) = RenderPopupWithRealViewModel();

        cut.Find("select.calendar-popup-year-select").Change("2036");
        var dayInsideMonth = cut.FindAll(".calender-day").First(day => !day.ClassList.Contains("outside-month"));
        dayInsideMonth.Click();
        cut.Find(".btn-primary").Click();

        var confirmed = Assert.Single(_confirmedDates);
        Assert.Equal(2036, confirmed.Year);
    }

    private static TaskDto TestTask(string note, bool isDone)
    {
        return new TaskDto { Id = Guid.NewGuid(), Note = note, IsDone = isDone };
    }

    private IRenderedComponent<CalendarPopup> RenderPopup(bool isOpen, DateTime? initialDate = null)
    {
        return Render<CalendarPopup>(parameters => parameters
            .Add(popup => popup.IsOpen, isOpen)
            .Add(popup => popup.InitialDate, initialDate ?? new DateTime(2026, 3, 15))
            .Add(popup => popup.OnConfirm, EventCallback.Factory.Create<DateTime>(this, date => _confirmedDates.Add(date)))
            .Add(popup => popup.OnCancel, EventCallback.Factory.Create(this, () => _cancelCount++)));
    }

    private (IRenderedComponent<CalendarPopup> Cut, CalendarPopupViewModel ViewModel) RenderPopupWithRealViewModel()
    {
        var viewModel = new CalendarPopupViewModel(
            new FakeTimeProvider(new DateTimeOffset(2026, 3, 10, 9, 0, 0, TimeSpan.Zero)),
            Substitute.For<ITaskCacheService>());
        Services.AddSingleton<ICalendarPopupViewModel>(viewModel);
        var cut = Render<CalendarPopup>(parameters => parameters
            .Add(popup => popup.IsOpen, true)
            .Add(popup => popup.InitialDate, new DateTime(2026, 3, 15))
            .Add(popup => popup.OnConfirm, EventCallback.Factory.Create<DateTime>(this, date => _confirmedDates.Add(date)))
            .Add(popup => popup.OnCancel, EventCallback.Factory.Create(this, () => _cancelCount++)));
        return (cut, viewModel);
    }

    private static AngleSharp.Dom.IElement Shell(IRenderedComponent<CalendarPopup> cut)
    {
        return cut.Find(".calendar-popup-shell");
    }

    private static string AbbreviatedDayName(DayOfWeek day)
    {
        return CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames[(int)day];
    }

    private static string GetSelectedOptionText(IRenderedComponent<CalendarPopup> cut, string selectSelector)
    {
        var select = cut.Find(selectSelector);
        var selectedOption = select.Children.First(option => option.HasAttribute("selected"));
        return selectedOption.TextContent;
    }

    private static List<CalenderWeekModel> BuildWeeks(DateTime selected, DateTime today, Func<DateTime, IReadOnlyList<CalenderDotModel>>? dots = null)
    {
        var monthStart = new DateTime(selected.Year, selected.Month, 1);
        var gridStart = monthStart.AddDays(-(((int)monthStart.DayOfWeek + 6) % 7));
        return Enumerable.Range(0, 6)
            .Select(week => new CalenderWeekModel
            {
                WeekNumber = ISOWeek.GetWeekOfYear(gridStart.AddDays(week * 7)),
                Days = Enumerable.Range(0, 7)
                    .Select(day => gridStart.AddDays((week * 7) + day))
                    .Select(date => new CalenderDayCellModel
                    {
                        Date = date,
                        IsInCurrentMonth = date.Month == monthStart.Month,
                        IsToday = date == today,
                        IsSelected = date == selected,
                        Dots = dots?.Invoke(date) ?? []
                    })
                    .ToList()
            })
            .ToList();
    }
}
