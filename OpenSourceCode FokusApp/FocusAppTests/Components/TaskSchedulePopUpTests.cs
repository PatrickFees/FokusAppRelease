using Bunit;
using FokusApp.Components.PopUps.Calendar;
using FokusApp.Components.PopUps.TaskSchedule;
using FokusApp.Components.SharedUI.TimeWheel;
using FokusApp.Core.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FocusAppTests.Components;

public class TaskSchedulePopUpTests : ComponentTestContext
{

    private static readonly DateTime TaskDate = new(2026, 3, 10);

    private readonly List<TaskScheduleModel> _confirmed = [];
    private int _cancelCount;

    public TaskSchedulePopUpTests()
    {
        Services.AddTransient<ITaskSchedulePopUpViewModel, TaskSchedulePopUpViewModel>();
        Services.AddSingleton(Substitute.For<ICalendarPopupViewModel>());
    }

    [Fact]
    public void Render_Closed_ShowsNothing()
    {
        var cut = RenderPopup(isOpen: false, model: null);

        Assert.Equal(string.Empty, cut.Markup.Trim());
    }

    [Fact]
    public void Render_Open_IsAnAccessibleModalDialog()
    {
        var cut = RenderPopup(isOpen: true, model: null);

        var dialog = cut.Find(".schedule-popup");
        Assert.Equal("dialog", dialog.GetAttribute("role"));
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
        Assert.Equal(Text("ScheduleTitle"), cut.Find("#task-schedule-title").TextContent);
    }

    [Fact]
    public void Render_Open_ShowsHourAndMinuteWheels()
    {
        var cut = RenderPopup(isOpen: true, model: null);

        var wheels = cut.FindComponents<TimeWheel>();
        Assert.Equal(2, wheels.Count);
        Assert.Equal(24, wheels[0].FindAll(".time-wheel-item").Count);
        Assert.Equal(60, wheels[1].FindAll(".time-wheel-item").Count);
    }

    [Fact]
    public void Render_ScheduleWithTime_PositionsTheWheelsOnIt()
    {
        var model = new TaskScheduleModel { Time = new TimeSpan(14, 35, 0) };

        var cut = RenderPopup(isOpen: true, model);

        var selected = cut.FindAll(".time-wheel-item.selected");
        Assert.Equal(["14", "35"], selected.Select(item => item.TextContent.Trim()).ToList());
    }

    [Fact]
    public void Render_NoTime_DisablesReminderAndClearTime()
    {
        var cut = RenderPopup(isOpen: true, model: null);

        Assert.True(cut.Find("#task-schedule-reminder").HasAttribute("disabled"));
        Assert.True(cut.Find(".schedule-clear-time").HasAttribute("disabled"));
        Assert.Contains(Text("ReminderNeedsTime"), cut.Markup);
    }

    [Fact]
    public void ClickHourItem_NoTimeYet_EnablesReminderAndClearTime()
    {
        var cut = RenderPopup(isOpen: true, model: null);

        cut.FindComponents<TimeWheel>()[0].FindAll(".time-wheel-item")[7].Click();

        Assert.False(cut.Find("#task-schedule-reminder").HasAttribute("disabled"));
        Assert.False(cut.Find(".schedule-clear-time").HasAttribute("disabled"));
    }

    [Fact]
    public void ClickClearTime_TimeSet_ClearsTheWheelsSelection()
    {
        var model = new TaskScheduleModel { Time = new TimeSpan(8, 0, 0), ReminderMinutesBefore = 10 };
        var cut = RenderPopup(isOpen: true, model);

        cut.Find(".schedule-clear-time").Click();

        Assert.Empty(cut.FindAll(".time-wheel-item.selected"));
        Assert.True(cut.Find("#task-schedule-reminder").HasAttribute("disabled"));
    }

    [Fact]
    public void Render_ReminderOptions_ListsNoneAndEveryStandardOptionWithGermanTexts()
    {
        var model = new TaskScheduleModel { Time = new TimeSpan(8, 0, 0) };

        var cut = RenderPopup(isOpen: true, model);

        var labels = cut.FindAll("#task-schedule-reminder option").Select(option => option.TextContent.Trim()).ToList();
        Assert.Equal(
        [
            Text("ReminderNone"),
            Text("ReminderAtTime"),
            Text("ReminderMinutesBefore", 5),
            Text("ReminderMinutesBefore", 10),
            Text("ReminderMinutesBefore", 15),
            Text("ReminderMinutesBefore", 30),
            Text("ReminderHourBefore"),
            Text("ReminderHoursBefore", 2),
            Text("ReminderDayBefore")
        ],
        labels);
    }

    [Fact]
    public void ChangeReminder_Option_IsPartOfTheConfirmedSchedule()
    {
        var model = new TaskScheduleModel { Time = new TimeSpan(8, 0, 0) };
        var cut = RenderPopup(isOpen: true, model);

        cut.Find("#task-schedule-reminder").Change("15");
        ClickApply(cut);

        var result = Assert.Single(_confirmed);
        Assert.Equal(15, result.ReminderMinutesBefore);
    }

    [Fact]
    public void ChangeReminder_NoneOption_RemovesTheReminder()
    {
        var model = new TaskScheduleModel { Time = new TimeSpan(8, 0, 0), ReminderMinutesBefore = 15 };
        var cut = RenderPopup(isOpen: true, model);

        cut.Find("#task-schedule-reminder").Change(string.Empty);
        ClickApply(cut);

        var result = Assert.Single(_confirmed);
        Assert.Null(result.ReminderMinutesBefore);
    }

    [Fact]
    public void Render_ReminderNotInTheStandardList_ShowsItAsSelectedOption()
    {
        var model = new TaskScheduleModel { Time = new TimeSpan(8, 0, 0), ReminderMinutesBefore = 45 };

        var cut = RenderPopup(isOpen: true, model);

        var selected = cut.Find("#task-schedule-reminder option[selected]");
        Assert.Equal(Text("ReminderMinutesBefore", 45), selected.TextContent.Trim());
    }

    [Fact]
    public void Render_NotRepeating_HidesIntervalAndWeekdays()
    {
        var cut = RenderPopup(isOpen: true, model: null);

        Assert.Empty(cut.FindAll(".schedule-interval"));
        Assert.Empty(cut.FindAll(".schedule-weekdays"));
    }

    [Fact]
    public void ChangeRepeatUnit_Daily_ShowsIntervalButNoWeekdays()
    {
        var cut = RenderPopup(isOpen: true, model: null);

        cut.Find("#task-schedule-repeat").Change(((int)TaskRepeatUnit.Day).ToString());

        Assert.NotEmpty(cut.FindAll(".schedule-interval"));
        Assert.Empty(cut.FindAll(".schedule-weekdays"));
        Assert.Contains(Text("RepeatUnitDay"), cut.Find(".schedule-interval").TextContent);
    }

    [Fact]
    public void ChangeRepeatUnit_Weekly_ShowsSevenWeekdaysWithTheTaskWeekdaySelected()
    {
        var cut = RenderPopup(isOpen: true, model: null);

        cut.Find("#task-schedule-repeat").Change(((int)TaskRepeatUnit.Week).ToString());

        var weekdays = cut.FindAll(".schedule-weekday");
        Assert.Equal(7, weekdays.Count);
        Assert.Equal(Text("WeekdayShortMonday"), weekdays[0].TextContent.Trim());
        Assert.Equal(Text("WeekdayShortSunday"), weekdays[6].TextContent.Trim());
        Assert.Equal([1], weekdays.Select((day, index) => day.ClassList.Contains("selected") ? index : -1).Where(index => index >= 0).ToList());
    }

    [Fact]
    public void ClickWeekday_Weekly_TogglesItAndConfirmsTheDays()
    {
        var model = new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Week };
        var cut = RenderPopup(isOpen: true, model);

        cut.FindAll(".schedule-weekday")[4].Click();
        ClickApply(cut);

        var result = Assert.Single(_confirmed);
        Assert.Equal(TaskWeekdays.Tuesday | TaskWeekdays.Friday, result.RepeatWeekdays);
    }

    [Fact]
    public void ClickIntervalButtons_Repeating_ChangesTheIntervalAndTheUnitWording()
    {
        var model = new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Week };
        var cut = RenderPopup(isOpen: true, model);

        cut.FindAll(".schedule-step-button")[1].Click();

        Assert.Equal("2", cut.Find(".schedule-interval-value").TextContent.Trim());
        Assert.Contains(Text("RepeatUnitWeeks"), cut.Find(".schedule-interval").TextContent);

        cut.FindAll(".schedule-step-button")[0].Click();

        Assert.Equal("1", cut.Find(".schedule-interval-value").TextContent.Trim());
        Assert.Contains(Text("RepeatUnitWeek"), cut.Find(".schedule-interval").TextContent);
    }

    [Fact]
    public void Render_CannotRepeat_ShowsTheTemplateHintInsteadOfTheRepeatSelection()
    {
        var cut = RenderPopup(isOpen: true, model: null, canRepeat: false);

        Assert.Empty(cut.FindAll("#task-schedule-repeat"));
        Assert.Contains(Text("TemplatesDoNotRepeat"), cut.Markup);
    }

    [Fact]
    public void ClickApply_Always_ConfirmsTheChosenSchedule()
    {
        var cut = RenderPopup(isOpen: true, model: null);
        cut.FindComponents<TimeWheel>()[0].FindAll(".time-wheel-item")[18].Click();
        cut.FindComponents<TimeWheel>()[1].FindAll(".time-wheel-item")[30].Click();
        cut.Find("#task-schedule-repeat").Change(((int)TaskRepeatUnit.Month).ToString());

        ClickApply(cut);

        var result = Assert.Single(_confirmed);
        Assert.Equal(new TimeSpan(18, 30, 0), result.Time);
        Assert.Equal(TaskRepeatUnit.Month, result.RepeatUnit);
    }

    [Fact]
    public void ClickCancel_Always_ConfirmsNothing()
    {
        var cut = RenderPopup(isOpen: true, model: null);

        cut.Find(".app-button--secondary").Click();

        Assert.Empty(_confirmed);
        Assert.Equal(1, _cancelCount);
    }

    [Fact]
    public void ClickBackdrop_Always_Cancels()
    {
        var cut = RenderPopup(isOpen: true, model: null);

        cut.Find(".schedule-backdrop").Click();

        Assert.Equal(1, _cancelCount);
    }

    [Fact]
    public void ClickInsideThePopup_DoesNotCancel()
    {
        var cut = RenderPopup(isOpen: true, model: null);

        var popup = cut.Find(".schedule-popup");

        Assert.Throws<MissingEventHandlerException>(() => popup.Click());
        Assert.Equal(0, _cancelCount);
    }

    [Fact]
    public void Render_Repeating_ShowsUncheckedEndDateCheckboxByDefault()
    {
        var model = new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Day };
        var cut = RenderPopup(isOpen: true, model);

        var checkbox = cut.Find(".schedule-checkbox input");
        Assert.False(checkbox.HasAttribute("checked"));
        Assert.Empty(cut.FindAll(".schedule-end-date .app-button--secondary"));
    }

    [Fact]
    public void Render_RepeatingWithEndDate_ChecksTheBoxAndShowsTheDate()
    {
        var model = new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Day, RepeatEndDate = TaskDate.AddDays(10) };
        var cut = RenderPopup(isOpen: true, model);

        var checkbox = cut.Find(".schedule-checkbox input");
        Assert.True(checkbox.HasAttribute("checked"));
        Assert.Contains(
            TaskDate.AddDays(10).ToString("d", System.Globalization.CultureInfo.CurrentCulture),
            cut.Find(".schedule-end-date .app-button--secondary").TextContent);
    }

    [Fact]
    public void CheckEndDateCheckbox_Repeating_ConfirmsWithAnEndDate()
    {
        var model = new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Day };
        var cut = RenderPopup(isOpen: true, model);

        cut.Find(".schedule-checkbox input").Change(true);
        ClickApply(cut);

        var result = Assert.Single(_confirmed);
        Assert.NotNull(result.RepeatEndDate);
    }

    [Fact]
    public void UncheckEndDateCheckbox_EndDateWasSet_ConfirmsWithoutAnEndDate()
    {
        var model = new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Day, RepeatEndDate = TaskDate.AddDays(10) };
        var cut = RenderPopup(isOpen: true, model);

        cut.Find(".schedule-checkbox input").Change(false);
        ClickApply(cut);

        var result = Assert.Single(_confirmed);
        Assert.Null(result.RepeatEndDate);
    }

    private IRenderedComponent<TaskSchedulePopUp> RenderPopup(bool isOpen, TaskScheduleModel? model, bool canRepeat = true)
    {
        return Render<TaskSchedulePopUp>(parameters => parameters
            .Add(popup => popup.IsOpen, isOpen)
            .Add(popup => popup.Model, model)
            .Add(popup => popup.TaskDate, TaskDate)
            .Add(popup => popup.CanRepeat, canRepeat)
            .Add(popup => popup.OnConfirm, EventCallback.Factory.Create<TaskScheduleModel>(this, schedule => _confirmed.Add(schedule)))
            .Add(popup => popup.OnCancel, EventCallback.Factory.Create(this, () => _cancelCount++)));
    }

    private static void ClickApply(IRenderedComponent<TaskSchedulePopUp> cut)
    {
        cut.Find(".app-button--primary").Click();
    }
}
