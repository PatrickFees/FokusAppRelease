using Bunit;
using FokusApp.Components.PopUps.Calendar;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Components.PopUps.TaskSchedule;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FocusAppTests.Components;

public class TaskEditPopUpTests : ComponentTestContext
{
    private static readonly DateTime Day = new(2026, 3, 10);

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 3, 15, 9, 30, 0, TimeSpan.Zero));
    private readonly ICalendarPopupViewModel _calendar = Substitute.For<ICalendarPopupViewModel>();
    private readonly List<TaskEditPopUpClosePayload> _payloads = [];

    public TaskEditPopUpTests()
    {
        _calendar.VisibleMonth.Returns(new DateTime(2026, 3, 1));
        _calendar.SelectedDate.Returns(new DateTime(2026, 3, 20));
        Services.AddSingleton<TimeProvider>(_clock);
        Services.AddSingleton(_calendar);
        Services.AddTransient<ITaskEditPopUpViewModel, TaskEditPopUpViewModel>();
        Services.AddTransient<ITaskSchedulePopUpViewModel, TaskSchedulePopUpViewModel>();
    }

    [Fact]
    public void Render_Closed_ShowsNothing()
    {
        var cut = RenderPopup(isOpen: false, input: NewInput());

        Assert.Equal(string.Empty, cut.Markup.Trim());
    }

    [Fact]
    public void Render_OpenNewTask_ShowsNewTaskTitleAndSaveButton()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());

        var title = cut.Find("#task-edit-title");
        var save = cut.Find(".popup-action-button--primary");
        Assert.Equal(Text("NewTask"), title.TextContent);
        Assert.Equal(Text("Save"), save.TextContent.Trim());
        Assert.True(save.HasAttribute("disabled"));
    }

    [Fact]
    public void Render_OpenExistingTask_ShowsEditTitleAndUpdateButton()
    {
        var task = ExistingTask("Bericht", isPositive: false);

        var cut = RenderPopup(isOpen: true, input: ExistingInput(task));

        var title = cut.Find("#task-edit-title");
        var save = cut.Find(".popup-action-button--primary");
        var note = cut.Find("#task-note");
        Assert.Equal(Text("EditTask"), title.TextContent);
        Assert.Equal(Text("Update"), save.TextContent.Trim());
        Assert.False(save.HasAttribute("disabled"));
        Assert.Equal("Bericht", note.GetAttribute("value"));
    }

    [Fact]
    public void Render_Open_IsAnAccessibleModalDialogThatReceivesFocus()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());

        var dialog = cut.Find(".modal-content");
        Assert.Equal("dialog", dialog.GetAttribute("role"));
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
        Assert.Equal("task-edit-title", dialog.GetAttribute("aria-labelledby"));
        cut.WaitForAssertion(() => JSInterop.VerifyFocusAsyncInvoke());
    }

    [Fact]
    public void ChangeNoteAndChoosePositivity_NewTask_EnablesSave()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());

        cut.Find("#task-note").Change("Neue Aufgabe");
        cut.FindAll(".positivity-button")[1].Click();
        cut.FindComponents<TaskTimerSelector>()[0].FindAll(".timer-step-button")[1].Click();

        var save = cut.Find(".popup-action-button--primary");
        Assert.False(save.HasAttribute("disabled"));
    }

    [Fact]
    public void ChoosePositivity_ForYou_MarksOnlyThatButtonAsSelected()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());

        cut.FindAll(".positivity-button")[0].Click();

        var buttons = cut.FindAll(".positivity-button");
        Assert.True(buttons[0].ClassList.Contains("selected"));
        Assert.True(buttons[0].HasAttribute("aria-pressed"));
        Assert.False(buttons[1].ClassList.Contains("selected"));
        Assert.False(buttons[1].HasAttribute("aria-pressed"));
    }

    [Fact]
    public void Render_OpenNewTaskWithoutChoice_MarksNeitherPositivityButton()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());

        Assert.Empty(cut.FindAll(".positivity-button.selected"));
    }

    [Fact]
    public void ChangeTemplateFlag_CheckedAndUnchecked_HidesAndShowsTheDayButton()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());
        Assert.NotEmpty(cut.FindAll(".task-day-button"));

        cut.Find("#task-templateFlag").Change(true);
        Assert.Empty(cut.FindAll(".task-day-button"));

        cut.Find("#task-templateFlag").Change(false);
        Assert.NotEmpty(cut.FindAll(".task-day-button"));
    }

    [Fact]
    public void Render_ExistingTemplate_LocksTheTemplateFlagCheckbox()
    {
        var template = ExistingTask("Vorlage", isPositive: true);
        template.IsTemplate = true;
        template.TaskDate = null;

        var cut = RenderPopup(isOpen: true, input: ExistingInput(template));

        var checkbox = cut.Find("#task-templateFlag");
        Assert.True(checkbox.HasAttribute("disabled"));
        Assert.True(checkbox.HasAttribute("checked"));
    }

    [Fact]
    public void InputPrioritySlider_Value_ChangesThePriorityLabel()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());
        var before = cut.Find(".priority-badge").TextContent;

        cut.Find("#task-priority").Input("3");

        var after = cut.Find(".priority-badge").TextContent;
        Assert.NotEqual(before, after);
        Assert.Equal("3", cut.Find("#task-priority").GetAttribute("value"));
    }

    [Fact]
    public void InputPrioritySlider_NoNumber_FallsBackToTheFirstPriority()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());
        cut.Find("#task-priority").Input("3");

        cut.Find("#task-priority").Input("abc");

        Assert.Equal("0", cut.Find("#task-priority").GetAttribute("value"));
    }

    [Fact]
    public void ClickTimerButtons_FocusAndBreak_AdjustTheTimesByTheStep()
    {
        var task = ExistingTask("Bericht", isPositive: true);
        task.TimerMinutes = 25;
        task.BreakMinutes = 10;
        var cut = RenderPopup(isOpen: true, input: ExistingInput(task));

        var selectors = cut.FindComponents<TaskTimerSelector>();
        selectors[0].FindAll(".timer-step-button")[1].Click();
        selectors[1].FindAll(".timer-step-button")[0].Click();

        Assert.Equal("00:30", cut.FindComponents<TaskTimerSelector>()[0].Find(".time-display").TextContent.Trim());
        Assert.Equal("00:05", cut.FindComponents<TaskTimerSelector>()[1].Find(".time-display").TextContent.Trim());
    }

    [Fact]
    public void ClickTimerButtons_DecreaseFocusTime_NeverGoesBelowOneMinute()
    {
        var task = ExistingTask("Bericht", isPositive: true);
        task.TimerMinutes = 3;
        var cut = RenderPopup(isOpen: true, input: ExistingInput(task));

        cut.FindComponents<TaskTimerSelector>()[0].FindAll(".timer-step-button")[0].Click();

        Assert.Equal("00:01", cut.FindComponents<TaskTimerSelector>()[0].Find(".time-display").TextContent.Trim());
    }

    [Theory]
    [InlineData(0, "SetFocusTime", "25")]
    [InlineData(1, "SetBreakTime", "10")]
    public void ClickTimerDisplay_FocusOrBreak_OpensTheTimeDialogForThatTimer(int selectorIndex, string titleKey, string expectedMinutes)
    {
        var task = ExistingTask("Bericht", isPositive: true);
        task.TimerMinutes = 25;
        task.BreakMinutes = 10;
        var cut = RenderPopup(isOpen: true, input: ExistingInput(task));

        cut.FindComponents<TaskTimerSelector>()[selectorIndex].Find(".time-display").Click();

        Assert.Equal(Text(titleKey), cut.Find(".timer-input-title").TextContent);
        Assert.Equal(expectedMinutes, cut.Find(".timer-input-field").GetAttribute("value"));
    }

    [Fact]
    public void ConfirmTimeDialog_NewMinutes_AreAppliedToTheChosenTimerAndTheDialogCloses()
    {
        var task = ExistingTask("Bericht", isPositive: true);
        task.TimerMinutes = 25;
        task.BreakMinutes = 10;
        var cut = RenderPopup(isOpen: true, input: ExistingInput(task));
        cut.FindComponents<TaskTimerSelector>()[1].Find(".time-display").Click();

        cut.Find(".timer-input-field").Input("20");
        cut.Find(".timer-input-form").Submit();

        Assert.Empty(cut.FindAll(".timer-input-dialog"));
        Assert.Equal("00:20", cut.FindComponents<TaskTimerSelector>()[1].Find(".time-display").TextContent.Trim());
        Assert.Equal("00:25", cut.FindComponents<TaskTimerSelector>()[0].Find(".time-display").TextContent.Trim());
    }

    [Fact]
    public void CancelTimeDialog_Open_ClosesItWithoutChangingTheTime()
    {
        var task = ExistingTask("Bericht", isPositive: true);
        task.TimerMinutes = 25;
        var cut = RenderPopup(isOpen: true, input: ExistingInput(task));
        cut.FindComponents<TaskTimerSelector>()[0].Find(".time-display").Click();

        cut.Find(".timer-input-actions .btn-secondary").Click();

        Assert.Empty(cut.FindAll(".timer-input-dialog"));
        Assert.Equal("00:25", cut.FindComponents<TaskTimerSelector>()[0].Find(".time-display").TextContent.Trim());
    }

    [Fact]
    public void ClickDayButton_NormalTask_OpensTheCalendarForTheTaskDay()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());

        cut.Find(".task-day-button").Click();

        var calendar = cut.FindComponent<CalendarPopup>();
        Assert.True(calendar.Instance.IsOpen);
        Assert.Equal(Day, calendar.Instance.InitialDate);
    }

    [Fact]
    public void ConfirmCalendar_OtherDay_SetsTheTaskDayAndClosesTheCalendar()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());
        cut.Find(".task-day-button").Click();

        cut.Find(".calendar-popup-actions .btn-primary").Click();

        var calendar = cut.FindComponent<CalendarPopup>();
        Assert.False(calendar.Instance.IsOpen);
        var expected = new DateTime(2026, 3, 20).ToString("D", System.Globalization.CultureInfo.CurrentCulture);
        Assert.Equal(expected, cut.Find(".task-day-button").TextContent.Trim());
    }

    [Fact]
    public void CancelCalendar_Open_ClosesItWithoutChangingTheDay()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());
        var before = cut.Find(".task-day-button").TextContent;
        cut.Find(".task-day-button").Click();

        cut.Find(".calendar-popup-actions .btn-secondary").Click();

        var calendar = cut.FindComponent<CalendarPopup>();
        Assert.False(calendar.Instance.IsOpen);
        Assert.Equal(before, cut.Find(".task-day-button").TextContent);
    }

    [Fact]
    public void Render_LockedTaskDate_DisablesTheDayButton()
    {
        var input = NewInput(isTaskDateLocked: true);

        var cut = RenderPopup(isOpen: true, input: input);

        Assert.True(cut.Find(".task-day-button").HasAttribute("disabled"));
    }

    [Fact]
    public void Render_NoTemplates_DisablesTheTemplateListButton()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());

        var button = cut.Find(".template-list-button");
        Assert.True(button.HasAttribute("disabled"));
    }

    [Fact]
    public void ClickTemplateListButton_WithoutOptions_OpensNoList()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());

        var button = cut.Find(".template-list-button");
        button.Click();

        Assert.Empty(cut.FindAll(".template-list-popup"));
    }

    [Fact]
    public void ClickTemplateListButton_WithOptions_OpensTheListWithAllTemplates()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput(templates: [Template("Sport"), Template("Lesen")]));

        cut.Find(".template-list-button").Click();

        var items = cut.FindAll(".template-list-item");
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void Render_TemplatesButNoTypeSelected_DisablesTheTemplateListButton()
    {
        var input = new TaskEditPopUpInputModel { ContextualTaskDate = Day, TemplateOptions = [Template("Sport")] };

        var cut = RenderPopup(isOpen: true, input: input);

        Assert.True(cut.Find(".template-list-button").HasAttribute("disabled"));
    }

    [Fact]
    public void ClickTemplateListButton_NoTypeSelected_HighlightsBothTypeButtonsBriefly()
    {
        var input = new TaskEditPopUpInputModel { ContextualTaskDate = Day, TemplateOptions = [Template("Sport")] };
        var cut = RenderPopup(isOpen: true, input: input);

        cut.Find(".template-list-button-wrapper").Click();

        var highlighted = cut.FindAll(".positivity-button.highlight");
        Assert.Equal(2, highlighted.Count);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".positivity-button.highlight")));
    }

    [Fact]
    public void ClickTemplateListButton_TypeSelected_HighlightsNothing()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput(templates: [Template("Sport")]));

        cut.Find(".template-list-button-wrapper").Click();

        Assert.Empty(cut.FindAll(".positivity-button.highlight"));
    }

    [Fact]
    public void ClickPositivityButtons_SwitchingType_ShowsOnlyTemplatesOfThatType()
    {
        var input = new TaskEditPopUpInputModel
        {
            ContextualTaskDate = Day,
            TemplateOptions = [Template("Yoga"), Template("Bericht", isPositive: false)]
        };
        var cut = RenderPopup(isOpen: true, input: input);

        cut.FindAll(".positivity-button")[0].Click();
        cut.Find(".template-list-button").Click();
        var forYouItems = cut.FindAll(".template-list-item");
        cut.Find(".template-list-close").Click();
        cut.FindAll(".positivity-button")[1].Click();
        cut.Find(".template-list-button").Click();
        var toDoItems = cut.FindAll(".template-list-item");

        var forYouItem = Assert.Single(forYouItems);
        var toDoItem = Assert.Single(toDoItems);
        Assert.Contains("Yoga", forYouItem.TextContent);
        Assert.Contains("Bericht", toDoItem.TextContent);
    }

    [Fact]
    public void ClickCopyTemplate_InList_ClosesWithCopyActionAndTemplateId()
    {
        var template = Template("Sport");
        var cut = RenderPopup(isOpen: true, input: NewInput(templates: [template]));
        cut.Find(".template-list-button").Click();

        cut.Find(".template-icon-button.copy").Click();

        var payload = Assert.Single(_payloads);
        Assert.Equal(TaskEditPopUpCloseAction.CopyTemplate, payload.Action);
        Assert.Equal(template.TaskId, payload.TemplateTaskId);
    }

    [Fact]
    public void ClickEditTemplate_InList_ClosesWithEditActionAndTemplateId()
    {
        var template = Template("Sport");
        var cut = RenderPopup(isOpen: true, input: NewInput(templates: [template]));
        cut.Find(".template-list-button").Click();

        cut.Find(".template-icon-button.edit").Click();

        var payload = Assert.Single(_payloads);
        Assert.Equal(TaskEditPopUpCloseAction.EditTemplate, payload.Action);
        Assert.Equal(template.TaskId, payload.TemplateTaskId);
    }

    [Fact]
    public void CloseTemplateList_CloseButton_HidesTheListAndKeepsTheEditor()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput(templates: [Template("Sport")]));
        cut.Find(".template-list-button").Click();

        cut.Find(".template-list-close").Click();

        Assert.Empty(cut.FindAll(".template-list-popup"));
        Assert.NotEmpty(cut.FindAll(".modal-content"));
        Assert.Empty(_payloads);
    }

    [Fact]
    public void ClickSave_ExistingTask_ClosesWithSavePayloadOfTheEditedTask()
    {
        var task = ExistingTask("Bericht", isPositive: true);
        var cut = RenderPopup(isOpen: true, input: ExistingInput(task));
        cut.Find("#task-note").Change("Bericht fertig");

        cut.Find(".popup-action-button--primary").Click();

        var payload = Assert.Single(_payloads);
        Assert.Equal(TaskEditPopUpCloseAction.Save, payload.Action);
        Assert.Equal("Bericht fertig", payload.Task.Note);
        Assert.Equal(task.Id, payload.Task.Id);
        Assert.True(payload.IsUpdateMode);
        Assert.True(payload.HasChanges);
    }

    [Fact]
    public void ClickCancel_Open_ClosesWithCancelPayload()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());

        cut.Find(".popup-action-button--secondary").Click();

        var payload = Assert.Single(_payloads);
        Assert.Equal(TaskEditPopUpCloseAction.Cancel, payload.Action);
    }

    [Fact]
    public void KeyDown_OtherKeyThanEscape_DoesNothing()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());

        cut.Find(".modal-content").KeyDown(new KeyboardEventArgs { Key = "a" });

        Assert.Empty(_payloads);
    }

    [Fact]
    public void KeyDown_EscapeOnEditor_Cancels()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());

        cut.Find(".modal-content").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        var payload = Assert.Single(_payloads);
        Assert.Equal(TaskEditPopUpCloseAction.Cancel, payload.Action);
    }

    [Fact]
    public void KeyDown_EscapeWithTimeDialogOpen_ClosesOnlyTheDialog()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());
        cut.FindComponents<TaskTimerSelector>()[0].Find(".time-display").Click();

        cut.Find(".modal-content").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(cut.FindAll(".timer-input-dialog"));
        Assert.Empty(_payloads);
    }

    [Fact]
    public void KeyDown_EscapeWithTemplateListOpen_ClosesOnlyTheList()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput(templates: [Template("Sport")]));
        cut.Find(".template-list-button").Click();

        cut.Find(".modal-content").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(cut.FindAll(".template-list-popup"));
        Assert.Empty(_payloads);
    }

    [Fact]
    public void KeyDown_EscapeWithCalendarOpen_ClosesOnlyTheCalendar()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());
        cut.Find(".task-day-button").Click();

        cut.Find(".modal-content").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        var calendar = cut.FindComponent<CalendarPopup>();
        Assert.False(calendar.Instance.IsOpen);
        Assert.Empty(_payloads);
    }

    [Fact]
    public void Render_ClosedAndOpenedAgain_ShowsTheEditorFreshlyInitialised()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());
        cut.Find("#task-note").Change("Halbfertig");

        cut.Render(parameters => parameters.Add(popup => popup.IsOpen, false));
        cut.Render(parameters => parameters.Add(popup => popup.IsOpen, true));

        Assert.Equal(string.Empty, cut.Find("#task-note").TextContent);
    }

    [Fact]
    public void Render_ClosedWhileSubDialogOpen_ReopensWithoutSubDialogs()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput(templates: [Template("Sport")]));
        cut.Find(".template-list-button").Click();

        cut.Render(parameters => parameters.Add(popup => popup.IsOpen, false));
        cut.Render(parameters => parameters.Add(popup => popup.IsOpen, true));

        Assert.Empty(cut.FindAll(".template-list-popup"));
    }

    [Fact]
    public void ClickBreakIncrease_ExistingTask_RaisesBreakTimeByTheStep()
    {
        var task = ExistingTask("Bericht", isPositive: true);
        task.BreakMinutes = 10;
        var cut = RenderPopup(isOpen: true, input: ExistingInput(task));

        cut.FindComponents<TaskTimerSelector>()[1].FindAll(".timer-step-button")[1].Click();

        Assert.Equal("00:15", cut.FindComponents<TaskTimerSelector>()[1].Find(".time-display").TextContent.Trim());
    }

    [Fact]
    public void ChangeTemplateFlag_NoValue_TreatsItAsNotTemplate()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());
        cut.Find("#task-templateFlag").Change(true);

        cut.Find("#task-templateFlag").TriggerEvent("onchange", new ChangeEventArgs { Value = null });

        Assert.NotEmpty(cut.FindAll(".task-day-button"));
    }

    [Fact]
    public void InputPrioritySlider_NoValue_FallsBackToTheFirstPriority()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());
        cut.Find("#task-priority").Input("3");

        cut.Find("#task-priority").TriggerEvent("oninput", new ChangeEventArgs { Value = null });

        Assert.Equal("0", cut.Find("#task-priority").GetAttribute("value"));
    }

    [Fact]
    public void Render_OpenNewTask_ShowsTheClockButtonInTheHeaderWithoutScheduleMarker()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());

        var button = cut.Find(".task-edit-header .schedule-button");
        Assert.Equal(Text("ScheduleButton"), button.GetAttribute("aria-label"));
        Assert.False(button.ClassList.Contains("has-schedule"));
        Assert.Empty(cut.FindAll(".schedule-button-time"));
    }

    [Fact]
    public void Render_ExistingTaskWithTime_ShowsTheTimeOnTheClockButton()
    {
        var task = ExistingTask("Bericht", isPositive: false);
        task.TaskTime = new TimeSpan(9, 5, 0);

        var cut = RenderPopup(isOpen: true, input: ExistingInput(task));

        var button = cut.Find(".schedule-button");
        Assert.True(button.ClassList.Contains("has-schedule"));
        Assert.Equal("09:05", cut.Find(".schedule-button-time").TextContent);
    }

    [Fact]
    public void Render_ExistingRepeatingTask_ShowsTheRepeatIconOnTheClockButton()
    {
        var task = ExistingTask("Bericht", isPositive: false);
        task.RepeatUnit = TaskRepeatUnit.Day;

        var cut = RenderPopup(isOpen: true, input: ExistingInput(task));

        Assert.NotEmpty(cut.FindAll(".schedule-button-repeat"));
    }

    [Fact]
    public void ClickClockButton_Always_OpensTheSchedulePopUpWithTheTasksValues()
    {
        var task = ExistingTask("Bericht", isPositive: false);
        task.TaskTime = new TimeSpan(16, 45, 0);
        var cut = RenderPopup(isOpen: true, input: ExistingInput(task));

        cut.Find(".schedule-button").Click();

        Assert.NotEmpty(cut.FindAll(".schedule-popup"));
        var selected = cut.FindAll(".schedule-popup .time-wheel-item.selected");
        Assert.Equal(["16", "45"], selected.Select(item => item.TextContent.Trim()).ToList());
    }

    [Fact]
    public void ApplySchedule_PickedTimeAndRepetition_UpdatesTheClockButtonAndTheSavedTask()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());
        cut.Find("#task-note").Change("Neue Aufgabe");
        cut.FindAll(".positivity-button")[1].Click();
        cut.FindComponents<TaskTimerSelector>()[0].FindAll(".timer-step-button")[1].Click();
        cut.Find(".schedule-button").Click();
        cut.FindAll(".schedule-popup .time-wheel-item")[8].Click();
        cut.Find("#task-schedule-repeat").Change(((int)TaskRepeatUnit.Day).ToString());

        cut.Find(".schedule-popup .app-button--primary").Click();

        Assert.Empty(cut.FindAll(".schedule-popup"));
        Assert.Equal("08:00", cut.Find(".schedule-button-time").TextContent);
        Assert.NotEmpty(cut.FindAll(".schedule-button-repeat"));
        cut.Find(".popup-action-button--primary").Click();
        var payload = Assert.Single(_payloads);
        Assert.Equal(new TimeSpan(8, 0, 0), payload.Task.TaskTime);
        Assert.Equal(TaskRepeatUnit.Day, payload.Task.RepeatUnit);
    }

    [Fact]
    public void CancelSchedule_AfterChanges_KeepsTheTaskUnchanged()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());
        cut.Find(".schedule-button").Click();
        cut.FindAll(".schedule-popup .time-wheel-item")[8].Click();

        cut.Find(".schedule-popup .app-button--secondary").Click();

        Assert.Empty(cut.FindAll(".schedule-popup"));
        Assert.Empty(cut.FindAll(".schedule-button-time"));
    }

    [Fact]
    public void ClickClockButton_TemplateChecked_ShowsTheTemplateHintInsteadOfTheRepeatSelection()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());
        cut.Find("#task-templateFlag").Change(true);

        cut.Find(".schedule-button").Click();

        Assert.Empty(cut.FindAll("#task-schedule-repeat"));
        Assert.Contains(Text("TemplatesDoNotRepeat"), cut.Markup);
    }

    [Fact]
    public void KeyDownEscape_SchedulePopUpOpen_ClosesOnlyThePopUp()
    {
        var cut = RenderPopup(isOpen: true, input: NewInput());
        cut.Find(".schedule-button").Click();

        cut.Find(".modal-content").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(cut.FindAll(".schedule-popup"));
        Assert.Empty(_payloads);
    }

    private IRenderedComponent<TaskEditPopUp> RenderPopup(bool isOpen, TaskEditPopUpInputModel? input)
    {
        return Render<TaskEditPopUp>(parameters => parameters
            .Add(popup => popup.IsOpen, isOpen)
            .Add(popup => popup.Model, input)
            .Add(popup => popup.OnClose, EventCallback.Factory.Create<TaskEditPopUpClosePayload>(this, payload => _payloads.Add(payload))));
    }

    private static TaskEditPopUpInputModel NewInput(
        bool isTaskDateLocked = false,
        IReadOnlyList<TaskEditTemplateOptionModel>? templates = null)
    {
        return new TaskEditPopUpInputModel
        {
            ContextualTaskDate = Day,
            IsTaskDateLocked = isTaskDateLocked,
            InitialIsPositive = templates is null ? null : true,
            TemplateOptions = templates ?? []
        };
    }

    private static TaskEditPopUpInputModel ExistingInput(TaskDto task)
    {
        return new TaskEditPopUpInputModel { Task = task, ContextualTaskDate = Day };
    }

    private static TaskDto ExistingTask(string note, bool isPositive)
    {
        return new TaskDto
        {
            Id = Guid.NewGuid(),
            Note = note,
            IsPositive = isPositive,
            IsWorkTask = true,
            TaskDate = Day,
            CreationDate = Day,
            TimerMinutes = 25,
            BreakMinutes = 5,
            Priority = TaskPriority.Keine
        };
    }

    private static TaskEditTemplateOptionModel Template(string text, bool isPositive = true)
    {
        return new TaskEditTemplateOptionModel { TaskId = Guid.NewGuid(), Text = text, IsPositive = isPositive };
    }
}
