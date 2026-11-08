using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Components.PopUps.TaskSchedule;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using Microsoft.Extensions.Time.Testing;

namespace FocusAppTests.ViewModels;

public class TaskEditPopUpViewModelTests
{
    private readonly FakeTimeProvider _clock = TestTasks.CreateClock();
    private readonly TaskEditPopUpViewModel _viewModel;

    public TaskEditPopUpViewModelTests()
    {
        _viewModel = new TaskEditPopUpViewModel(_clock);
    }

    [Fact]
    public void Open_NoInput_CreatesDefaultTaskThatCannotBeSaved()
    {
        _viewModel.Open(null);

        Assert.False(_viewModel.IsUpdateMode);
        Assert.False(_viewModel.HasSelectedTaskPositivity);
        Assert.False(_viewModel.CanSave);
        Assert.Equal(string.Empty, _viewModel.Model.Note);
    }

    [Fact]
    public void Constructor_NoOpenCall_StartsWithDefaultTaskForToday()
    {
        Assert.Equal(TestTasks.Today, _viewModel.Model.TaskDate);
        Assert.Equal(TestTasks.Today, _viewModel.CalendarDate);
        Assert.False(_viewModel.CanSave);
    }

    [Fact]
    public void Open_NoInput_UsesTodayOfClockAsDay()
    {
        _clock.Advance(TimeSpan.FromDays(2));

        _viewModel.Open(null);

        Assert.Equal(new DateTime(2026, 3, 17), _viewModel.Model.TaskDate);
        Assert.Equal(new DateTime(2026, 3, 17), _viewModel.CalendarDate);
    }

    [Fact]
    public void CreateClosePayload_NewTaskAtLaterTime_UsesCurrentTimeOfClockAsCreationDate()
    {
        var input = CreateNewInput(initialIsPositive: true);
        _viewModel.Open(input);
        _clock.Advance(TimeSpan.FromHours(3));

        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.Equal(new DateTime(2026, 3, 15, 12, 30, 0), payload.Task.CreationDate);
    }

    [Fact]
    public void Open_NewTask_UsesContextualDayAndScope()
    {
        var input = CreateNewInput(isWorkScope: false);

        _viewModel.Open(input);

        Assert.Equal(TestTasks.Day, _viewModel.Model.TaskDate);
        Assert.Equal(TestTasks.Day, _viewModel.CalendarDate);
        Assert.False(_viewModel.Model.IsWorkTask);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Open_NewTaskWithInitialPositivity_PreselectsForYouOrToDo(bool isPositive)
    {
        var input = CreateNewInput(initialIsPositive: isPositive);

        _viewModel.Open(input);

        Assert.True(_viewModel.HasSelectedTaskPositivity);
        Assert.Equal(isPositive, _viewModel.Model.IsPositive);
    }

    [Fact]
    public void Open_ExistingTask_IsUpdateModeAndWorksOnCopy()
    {
        var task = TestTasks.Create("Original", priority: TaskPriority.Mittel);
        var input = CreateExistingInput(task);

        _viewModel.Open(input);
        _viewModel.SetPriorityIndex(4);

        Assert.True(_viewModel.IsUpdateMode);
        Assert.True(_viewModel.HasSelectedTaskPositivity);
        Assert.Equal(TaskPriority.Dringend, _viewModel.Model.Priority);
        Assert.Equal(TaskPriority.Mittel, task.Priority);
    }

    [Fact]
    public void Open_TaskWithoutId_IsCreateModeButPositivityIsKnown()
    {
        var task = TestTasks.Create("Kopie");
        task.Id = null;
        var input = CreateExistingInput(task);

        _viewModel.Open(input);

        Assert.False(_viewModel.IsUpdateMode);
        Assert.True(_viewModel.HasSelectedTaskPositivity);
        Assert.Equal("Kopie", _viewModel.Model.Note);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void Open_ExistingTask_LocksTemplateFlagOnlyForTemplates(bool isTemplate, bool expectedLocked)
    {
        var task = TestTasks.Create(isTemplate: isTemplate);
        var input = CreateExistingInput(task);

        _viewModel.Open(input);

        Assert.Equal(expectedLocked, _viewModel.IsTemplateLocked);
    }

    [Fact]
    public void Open_TaskDurationsAboveMaximum_AreLimitedTo120Minutes()
    {
        var task = TestTasks.Create(timerMinutes: 200, breakMinutes: 150);
        var input = CreateExistingInput(task);

        _viewModel.Open(input);

        Assert.Equal(120, _viewModel.Model.TimerMinutes);
        Assert.Equal(120, _viewModel.Model.BreakMinutes);
    }

    [Fact]
    public void Open_AfterCalendarWasShown_HidesCalendar()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        _viewModel.RequestOpeningCalendarPopUp();

        _viewModel.Open(input);

        Assert.False(_viewModel.ShowCalendarPopup);
    }

    [Fact]
    public void Open_TemplateOptions_AreExposed()
    {
        var option = new TaskEditTemplateOptionModel
        {
            TaskId = Guid.NewGuid(),
            Text = "Sport",
            IsPositive = true
        };
        var input = new TaskEditPopUpInputModel
        {
            ContextualTaskDate = TestTasks.Day,
            InitialIsPositive = true,
            TemplateOptions = [option]
        };

        _viewModel.Open(input);

        Assert.True(_viewModel.HasTemplateOptions);
        Assert.Single(_viewModel.TemplateOptions);
    }

    [Fact]
    public void Open_NoTaskTypeSelected_ExposesNoTemplateOptions()
    {
        var input = CreateInputWithTemplates(initialIsPositive: null);

        _viewModel.Open(input);

        Assert.Empty(_viewModel.TemplateOptions);
        Assert.False(_viewModel.HasTemplateOptions);
    }

    [Theory]
    [InlineData(true, "Yoga")]
    [InlineData(false, "Bericht")]
    public void SelectTaskPositivity_NoTypeSelectedBefore_ExposesOnlyTemplatesOfThatType(bool isPositive, string expectedText)
    {
        var input = CreateInputWithTemplates(initialIsPositive: null);
        _viewModel.Open(input);

        _viewModel.SelectTaskPositivity(isPositive);

        var option = Assert.Single(_viewModel.TemplateOptions);
        Assert.Equal(expectedText, option.Text);
    }

    [Theory]
    [InlineData(true, "Yoga")]
    [InlineData(false, "Bericht")]
    public void Open_InitialTaskType_ExposesOnlyMatchingTemplateOptions(bool initialIsPositive, string expectedText)
    {
        var input = CreateInputWithTemplates(initialIsPositive);

        _viewModel.Open(input);

        var option = Assert.Single(_viewModel.TemplateOptions);
        Assert.Equal(expectedText, option.Text);
    }

    [Fact]
    public void SelectTaskPositivity_SwitchesTemplateOptionsAndNotifies()
    {
        var input = CreateInputWithTemplates(initialIsPositive: true);
        _viewModel.Open(input);
        var changed = TrackChanges();

        _viewModel.SelectTaskPositivity(false);

        var option = Assert.Single(_viewModel.TemplateOptions);
        Assert.Equal("Bericht", option.Text);
        Assert.Contains(nameof(ITaskEditPopUpViewModel.TemplateOptions), changed);
        Assert.Contains(nameof(ITaskEditPopUpViewModel.HasTemplateOptions), changed);
    }

    [Fact]
    public void Open_OnlyOtherTaskTypeHasTemplates_HasNoTemplateOptions()
    {
        var forYouOnly = new TaskEditTemplateOptionModel
        {
            TaskId = Guid.NewGuid(),
            Text = "Yoga",
            IsPositive = true
        };
        var input = new TaskEditPopUpInputModel
        {
            ContextualTaskDate = TestTasks.Day,
            InitialIsPositive = false,
            TemplateOptions = [forYouOnly]
        };

        _viewModel.Open(input);

        Assert.False(_viewModel.HasTemplateOptions);
    }

    [Fact]
    public void CanSave_NewTaskNeedsPositivityNoteAndTime()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        _viewModel.Model.Note = "Notiz";
        _viewModel.SetTimerMinutes(TimerType.FocusTimer, 25);
        Assert.False(_viewModel.CanSave);

        _viewModel.SelectTaskPositivity(true);

        Assert.True(_viewModel.CanSave);
    }

    [Theory]
    [InlineData("Notiz", 25, true)]
    [InlineData("", 25, false)]
    [InlineData("   ", 25, false)]
    [InlineData("Notiz", 0, false)]
    public void CanSave_ExistingTask_RequiresNoteAndFocusTime(string note, int minutes, bool expected)
    {
        var task = TestTasks.Create(note, timerMinutes: minutes);
        var input = CreateExistingInput(task);

        _viewModel.Open(input);

        Assert.Equal(expected, _viewModel.CanSave);
    }

    [Fact]
    public void SelectTaskPositivity_RaisesCanSaveChanged()
    {
        var changed = TrackChanges();
        var input = CreateNewInput();
        _viewModel.Open(input);

        _viewModel.SelectTaskPositivity(false);

        Assert.Contains(nameof(TaskEditPopUpViewModel.CanSave), changed);
        Assert.False(_viewModel.Model.IsPositive);
    }

    [Theory]
    [InlineData(0, TaskPriority.Keine)]
    [InlineData(1, TaskPriority.Wenig)]
    [InlineData(2, TaskPriority.Mittel)]
    [InlineData(3, TaskPriority.Hoch)]
    [InlineData(4, TaskPriority.Dringend)]
    [InlineData(9, TaskPriority.Dringend)]
    [InlineData(-3, TaskPriority.Keine)]
    public void SetPriorityIndex_Index_SelectsPriorityWithinRange(int index, TaskPriority expected)
    {
        var input = CreateNewInput();
        _viewModel.Open(input);

        _viewModel.SetPriorityIndex(index);

        Assert.Equal(expected, _viewModel.Model.Priority);
        Assert.Equal(4, _viewModel.MaximumPriorityIndex);
    }

    [Theory]
    [InlineData(TimerType.FocusTimer, 150, 120)]
    [InlineData(TimerType.FocusTimer, 120, 120)]
    [InlineData(TimerType.FocusTimer, 30, 30)]
    [InlineData(TimerType.BreakTimer, 200, 120)]
    [InlineData(TimerType.BreakTimer, 10, 10)]
    public void SetTimerMinutes_Minutes_AreLimitedToMaximum(TimerType type, int minutes, int expected)
    {
        var input = CreateNewInput();
        _viewModel.Open(input);

        _viewModel.SetTimerMinutes(type, minutes);

        var actual = _viewModel.GetTimerMinutes(type);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(TimerType.FocusTimer, 0)]
    [InlineData(TimerType.FocusTimer, -5)]
    [InlineData(TimerType.BreakTimer, 0)]
    [InlineData(TimerType.BreakTimer, -5)]
    public void SetTimerMinutes_BelowOneMinute_KeepsPreviousValue(TimerType type, int invalidMinutes)
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        _viewModel.SetTimerMinutes(type, 25);

        _viewModel.SetTimerMinutes(type, invalidMinutes);

        var actual = _viewModel.GetTimerMinutes(type);
        Assert.Equal(25, actual);
    }

    [Fact]
    public void SetTimerMinutes_FocusTime_RaisesDisplayAndCanSaveChanged()
    {
        var changed = TrackChanges();
        var input = CreateNewInput();
        _viewModel.Open(input);

        _viewModel.SetTimerMinutes(TimerType.FocusTimer, 25);

        Assert.Contains(nameof(TaskEditPopUpViewModel.FocusTimerDisplay), changed);
        Assert.Contains(nameof(TaskEditPopUpViewModel.CanSave), changed);
    }

    [Fact]
    public void SetTimerMinutes_BreakTime_RaisesBreakDisplayChanged()
    {
        var changed = TrackChanges();
        var input = CreateNewInput();
        _viewModel.Open(input);

        _viewModel.SetTimerMinutes(TimerType.BreakTimer, 10);

        Assert.Contains(nameof(TaskEditPopUpViewModel.BreakTimerDisplay), changed);
    }

    [Fact]
    public void SetTimerMinutes_UnknownTimerType_Throws()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        var unknownType = (TimerType)99;

        Assert.Throws<ArgumentOutOfRangeException>(() => _viewModel.SetTimerMinutes(unknownType, 10));
    }

    [Theory]
    [InlineData(TimerType.FocusTimer, 25, 5, 30)]
    [InlineData(TimerType.FocusTimer, 25, 100, 120)]
    [InlineData(TimerType.BreakTimer, 10, -5, 5)]
    [InlineData(TimerType.FocusTimer, 5, -5, 1)]
    [InlineData(TimerType.FocusTimer, 3, -100, 1)]
    [InlineData(TimerType.BreakTimer, 1, -5, 1)]
    [InlineData(TimerType.BreakTimer, 120, 5, 120)]
    public void AdjustTimer_Delta_ChangesMinutesWithinRange(TimerType type, int start, int delta, int expected)
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        _viewModel.SetTimerMinutes(type, start);

        _viewModel.AdjustTimer(type, delta);

        var actual = _viewModel.GetTimerMinutes(type);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(5, "00:05")]
    [InlineData(90, "01:30")]
    [InlineData(120, "02:00")]
    public void FocusTimerDisplay_Minutes_AreFormattedAsHoursAndMinutes(int minutes, string expected)
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        _viewModel.SetTimerMinutes(TimerType.FocusTimer, minutes);

        Assert.Equal(expected, _viewModel.FocusTimerDisplay);
    }

    [Fact]
    public void FocusTimerDisplay_NewTask_IsZeroUntilTimeIsSet()
    {
        var input = CreateNewInput();

        _viewModel.Open(input);

        Assert.Equal("00:00", _viewModel.FocusTimerDisplay);
        Assert.False(_viewModel.CanSave);
    }

    [Fact]
    public void AdjustTimer_NewTaskWithoutTime_StartsAtStepAndNeverBelowOneMinute()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);

        _viewModel.AdjustTimer(TimerType.FocusTimer, -_viewModel.TimerStepMinutes);
        var afterDecrease = _viewModel.GetTimerMinutes(TimerType.FocusTimer);
        _viewModel.AdjustTimer(TimerType.FocusTimer, _viewModel.TimerStepMinutes);
        var afterIncrease = _viewModel.GetTimerMinutes(TimerType.FocusTimer);

        Assert.Equal(1, afterDecrease);
        Assert.Equal(6, afterIncrease);
    }

    [Fact]
    public void SetDate_NormalTask_StoresDayWithoutTimeAndClosesCalendar()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        _viewModel.RequestOpeningCalendarPopUp();
        var selected = new DateTime(2026, 4, 2, 15, 0, 0);

        _viewModel.SetDate(selected);

        Assert.Equal(selected.Date, _viewModel.Model.TaskDate);
        Assert.Equal(selected.Date, _viewModel.CalendarDate);
        Assert.False(_viewModel.ShowCalendarPopup);
    }

    [Fact]
    public void SetDate_Template_IsIgnored()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        _viewModel.SetTemplateFlag(true);
        var before = _viewModel.Model.TaskDate;
        var selected = new DateTime(2026, 4, 2);

        _viewModel.SetDate(selected);

        Assert.Equal(before, _viewModel.Model.TaskDate);
    }

    [Fact]
    public void SetDate_LockedDate_IsIgnored()
    {
        var input = CreateNewInput(isTaskDateLocked: true);
        _viewModel.Open(input);
        var before = _viewModel.Model.TaskDate;

        _viewModel.SetDate(new DateTime(2026, 4, 2));

        Assert.Equal(before, _viewModel.Model.TaskDate);
    }

    [Fact]
    public void RequestOpeningCalendarPopUp_NormalTask_ShowsCalendar()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);

        _viewModel.RequestOpeningCalendarPopUp();

        Assert.True(_viewModel.ShowCalendarPopup);
    }

    [Fact]
    public void RequestOpeningCalendarPopUp_Template_DoesNotShowCalendar()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        _viewModel.SetTemplateFlag(true);

        _viewModel.RequestOpeningCalendarPopUp();

        Assert.False(_viewModel.ShowCalendarPopup);
    }

    [Fact]
    public void CloseCalendarPopup_VisibleCalendar_HidesIt()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        _viewModel.RequestOpeningCalendarPopUp();

        _viewModel.CloseCalendarPopup();

        Assert.False(_viewModel.ShowCalendarPopup);
    }

    [Fact]
    public void SetTemplateFlag_True_SavesTaskWithoutDate()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);

        _viewModel.SetTemplateFlag(true);
        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.False(_viewModel.IsTaskDateAllowed);
        Assert.True(payload.Task.IsTemplate);
        Assert.Null(payload.Task.TaskDate);
    }

    [Fact]
    public void SetTemplateFlag_RemovedAgain_RestoresPreviouslySelectedDate()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        var chosenDay = new DateTime(2026, 5, 20);
        _viewModel.SetDate(chosenDay);

        _viewModel.SetTemplateFlag(true);
        _viewModel.SetTemplateFlag(false);
        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.True(_viewModel.IsTaskDateAllowed);
        Assert.Equal(chosenDay, _viewModel.CalendarDate);
        Assert.Equal(chosenDay, payload.Task.TaskDate);
        Assert.False(payload.Task.IsTemplate);
    }

    [Fact]
    public void SetTemplateFlag_RemovedWithoutChosenDate_FallsBackToContextualDay()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);

        _viewModel.SetTemplateFlag(true);
        _viewModel.SetTemplateFlag(false);

        Assert.Equal(TestTasks.Day, _viewModel.Model.TaskDate);
    }

    [Fact]
    public void SetTemplateFlag_ExistingTemplate_CannotBeUnmarked()
    {
        var template = TestTasks.Create(isTemplate: true);
        var input = CreateExistingInput(template);
        _viewModel.Open(input);

        _viewModel.SetTemplateFlag(false);

        Assert.True(_viewModel.Model.IsTemplate);
        Assert.True(_viewModel.IsTemplateLocked);
    }

    [Fact]
    public void SetTemplateFlag_Changed_RaisesDateRelatedProperties()
    {
        var changed = TrackChanges();
        var input = CreateNewInput();
        _viewModel.Open(input);

        _viewModel.SetTemplateFlag(true);

        Assert.Contains(nameof(TaskEditPopUpViewModel.IsTaskDateAllowed), changed);
        Assert.Contains(nameof(TaskEditPopUpViewModel.CalendarDate), changed);
        Assert.Contains(nameof(TaskEditPopUpViewModel.TaskDayButtonModel), changed);
    }

    [Fact]
    public void TaskDayButtonModel_Template_IsDisabled()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);

        _viewModel.SetTemplateFlag(true);
        var buttonModel = _viewModel.TaskDayButtonModel;

        Assert.False(buttonModel.IsEnabled);
    }

    [Fact]
    public void TaskDayButtonModel_LockedDate_IsDisabledButShowsDate()
    {
        var input = CreateNewInput(isTaskDateLocked: true);
        _viewModel.Open(input);

        var buttonModel = _viewModel.TaskDayButtonModel;

        Assert.False(buttonModel.IsEnabled);
        Assert.True(_viewModel.IsTaskDateAllowed);
        Assert.Equal(TestTasks.Day, buttonModel.SelectedDate);
    }

    [Fact]
    public void CreateClosePayload_NewTask_CompletesTaskForSaving()
    {
        var input = CreateNewInput(isWorkScope: false, initialIsPositive: true);
        _viewModel.Open(input);
        _viewModel.Model.Note = "  Neue Aufgabe  ";
        _viewModel.SetTimerMinutes(TimerType.FocusTimer, 25);
        var templateId = Guid.NewGuid();
        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.CopyTemplate, templateId);

        var task = payload.Task;
        Assert.Equal(TaskEditPopUpCloseAction.CopyTemplate, payload.Action);
        Assert.Equal(templateId, payload.TemplateTaskId);
        Assert.False(payload.IsUpdateMode);
        Assert.True(payload.HasChanges);
        Assert.NotNull(task.Id);
        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal("Neue Aufgabe", task.Note);
        Assert.False(task.IsDone);
        Assert.False(task.IsWorkTask);
        Assert.True(task.IsPositive);
        Assert.Equal(TestTasks.Day, task.TaskDate);
        Assert.Equal(_clock.GetLocalNow().DateTime, task.CreationDate);
    }

    [Fact]
    public void CreateClosePayload_UpdateWithoutEdits_HasNoChanges()
    {
        var task = TestTasks.Create("Notiz", priority: TaskPriority.Hoch);
        var input = CreateExistingInput(task);
        _viewModel.Open(input);

        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.True(payload.IsUpdateMode);
        Assert.False(payload.HasChanges);
    }

    [Fact]
    public void CreateClosePayload_UpdateWithOnlyTrailingWhitespace_HasNoChanges()
    {
        var task = TestTasks.Create("Notiz");
        var input = CreateExistingInput(task);
        _viewModel.Open(input);
        _viewModel.Model.Note = "Notiz   ";

        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.False(payload.HasChanges);
    }

    [Fact]
    public void CreateClosePayload_UpdateWithChangedNote_HasChanges()
    {
        var task = TestTasks.Create("Notiz");
        var input = CreateExistingInput(task);
        _viewModel.Open(input);
        _viewModel.Model.Note = "Anderer Text";

        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.True(payload.HasChanges);
    }

    [Fact]
    public void CreateClosePayload_UpdateWithChangedDuration_HasChanges()
    {
        var task = TestTasks.Create(timerMinutes: 25);
        var input = CreateExistingInput(task);
        _viewModel.Open(input);

        _viewModel.SetTimerMinutes(TimerType.FocusTimer, 30);
        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.True(payload.HasChanges);
    }

    [Fact]
    public void CreateClosePayload_Update_KeepsIdentityOfOriginalTask()
    {
        var task = TestTasks.Create("Notiz", isDone: true, isWork: false);
        var input = CreateExistingInput(task);
        _viewModel.Open(input);
        _viewModel.Model.Note = "Geändert";

        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        var result = payload.Task;
        Assert.Equal(task.Id, result.Id);
        Assert.True(result.IsDone);
        Assert.False(result.IsWorkTask);
        Assert.Equal(task.CreationDate, result.CreationDate);
    }

    [Fact]
    public void Close_VisibleCalendar_HidesIt()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        _viewModel.RequestOpeningCalendarPopUp();

        _viewModel.Close();

        Assert.False(_viewModel.ShowCalendarPopup);
    }

    [Fact]
    public void BreakTimerDisplay_BreakMinutes_AreFormattedAsHoursAndMinutes()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);

        _viewModel.SetTimerMinutes(TimerType.BreakTimer, 75);

        Assert.Equal("01:15", _viewModel.BreakTimerDisplay);
    }

    [Fact]
    public void SelectedPriorityIndex_ChosenPriority_FollowsThePriorityOrder()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        var maximumIndex = _viewModel.MaximumPriorityIndex;

        _viewModel.SetPriorityIndex(maximumIndex);

        Assert.Equal(maximumIndex, _viewModel.SelectedPriorityIndex);
        Assert.Equal(0, new TaskEditPopUpViewModel(_clock).SelectedPriorityIndex);
    }

    [Fact]
    public void GetTimerMinutes_UnknownTimerType_Throws()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        var unknownType = (TimerType)99;

        Assert.Throws<ArgumentOutOfRangeException>(() => _viewModel.GetTimerMinutes(unknownType));
    }

    [Fact]
    public void GetTimerMinutes_KnownTimerTypes_ReturnFocusAndBreakMinutes()
    {
        var task = TestTasks.Create(timerMinutes: 30, breakMinutes: 10);
        var input = CreateExistingInput(task);
        _viewModel.Open(input);

        Assert.Equal(30, _viewModel.GetTimerMinutes(TimerType.FocusTimer));
        Assert.Equal(10, _viewModel.GetTimerMinutes(TimerType.BreakTimer));
    }

    [Fact]
    public void CreateClosePayload_DateRemoved_FallsBackToContextualDay()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        _viewModel.SetDate(null);

        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.Equal(input.ContextualTaskDate.Date, payload.Task.TaskDate);
    }

    [Fact]
    public void SetTemplateFlag_DateRemovedBefore_FallsBackToContextualDay()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        _viewModel.SetDate(null);

        _viewModel.SetTemplateFlag(false);

        Assert.Equal(input.ContextualTaskDate.Date, _viewModel.Model.TaskDate);
    }

    [Fact]
    public void SetTemplateFlag_DateRemovedBeforeThenTemplateThenNormal_UsesContextualDay()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        _viewModel.SetDate(null);

        _viewModel.SetTemplateFlag(true);
        _viewModel.SetTemplateFlag(false);

        Assert.Equal(input.ContextualTaskDate.Date, _viewModel.Model.TaskDate);
        Assert.False(_viewModel.Model.IsTemplate);
    }

    [Fact]
    public void Open_TemplateOptionsMissing_AreTreatedAsEmpty()
    {
        var input = new TaskEditPopUpInputModel
        {
            ContextualTaskDate = TestTasks.Day,
            TemplateOptions = null!
        };

        _viewModel.Open(input);

        Assert.Empty(_viewModel.TemplateOptions);
        Assert.False(_viewModel.HasTemplateOptions);
    }

    [Fact]
    public void CreateClosePayload_UpdateWithChangedPriority_HasChanges()
    {
        var task = TestTasks.Create(priority: TaskPriority.Keine);
        var input = CreateExistingInput(task);
        _viewModel.Open(input);

        _viewModel.SetPriorityIndex(_viewModel.MaximumPriorityIndex);
        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.True(payload.HasChanges);
    }

    [Fact]
    public void CreateClosePayload_UpdateWithChangedBreakTime_HasChanges()
    {
        var task = TestTasks.Create(breakMinutes: 7);
        var input = CreateExistingInput(task);
        _viewModel.Open(input);

        _viewModel.SetTimerMinutes(TimerType.BreakTimer, 15);
        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.True(payload.HasChanges);
    }

    [Fact]
    public void CreateClosePayload_UpdateWithChangedPositivity_HasChanges()
    {
        var task = TestTasks.Create(isPositive: false);
        var input = CreateExistingInput(task);
        _viewModel.Open(input);

        _viewModel.SelectTaskPositivity(true);
        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.True(payload.HasChanges);
    }

    [Fact]
    public void CreateClosePayload_UpdateWithChangedTemplateFlag_HasChanges()
    {
        var task = TestTasks.Create();
        var input = CreateExistingInput(task);
        _viewModel.Open(input);

        _viewModel.SetTemplateFlag(true);
        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.True(payload.HasChanges);
    }

    [Fact]
    public void CreateClosePayload_UpdateWithChangedDate_HasChanges()
    {
        var task = TestTasks.Create(taskDate: TestTasks.Day);
        var input = CreateExistingInput(task);
        _viewModel.Open(input);

        _viewModel.SetDate(TestTasks.Day.AddDays(2));
        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.True(payload.HasChanges);
    }

    [Fact]
    public void CalendarDate_DateRemoved_FallsBackToContextualDay()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);

        _viewModel.SetDate(null);

        Assert.Equal(input.ContextualTaskDate.Date, _viewModel.CalendarDate);
    }

    [Fact]
    public void CalendarDate_Template_ShowsTheRememberedDay()
    {
        var template = TestTasks.Create("Vorlage", isTemplate: true);
        var input = CreateExistingInput(template);
        _viewModel.Open(input);

        Assert.Null(_viewModel.Model.TaskDate);
        Assert.Equal(TestTasks.Day, _viewModel.CalendarDate);
    }

    [Fact]
    public void SetTemplateFlag_TemplateMarkedTemplateAgain_KeepsTheRememberedDay()
    {
        var template = TestTasks.Create("Vorlage", isTemplate: true);
        var input = CreateExistingInput(template);
        _viewModel.Open(input);

        _viewModel.SetTemplateFlag(true);

        Assert.True(_viewModel.Model.IsTemplate);
        Assert.Equal(TestTasks.Day, _viewModel.CalendarDate);
    }

    [Fact]
    public void CreateClosePayload_ExistingTaskWithoutNote_HasNoChangesWhenNothingWasEdited()
    {
        var task = TestTasks.Create();
        task.Note = null;
        var input = CreateExistingInput(task);
        _viewModel.Open(input);

        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.False(payload.HasChanges);
        Assert.Equal(string.Empty, payload.Task.Note);
    }

    [Fact]
    public void CreateClosePayload_NoteRemovedFromModel_SavesEmptyNote()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        _viewModel.Model.Note = null!;

        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.Equal(string.Empty, payload.Task.Note);
    }

    [Fact]
    public void ApplySchedule_TimeReminderAndRepetition_AreStoredOnTheTask()
    {
        _viewModel.Open(CreateNewInput(initialIsPositive: true));
        var schedule = new TaskScheduleModel
        {
            Time = new TimeSpan(18, 30, 0),
            ReminderMinutesBefore = 15,
            RepeatUnit = TaskRepeatUnit.Week,
            RepeatInterval = 2,
            RepeatWeekdays = TaskWeekdays.Monday | TaskWeekdays.Friday
        };

        _viewModel.ApplySchedule(schedule);

        Assert.Equal(new TimeSpan(18, 30, 0), _viewModel.Model.TaskTime);
        Assert.Equal(15, _viewModel.Model.ReminderMinutesBefore);
        Assert.Equal(TaskRepeatUnit.Week, _viewModel.Model.RepeatUnit);
        Assert.Equal(2, _viewModel.Model.RepeatInterval);
        Assert.Equal(TaskWeekdays.Monday | TaskWeekdays.Friday, _viewModel.Model.RepeatWeekdays);
    }

    [Fact]
    public void ApplySchedule_Changed_RaisesTheScheduleProperties()
    {
        _viewModel.Open(CreateNewInput(initialIsPositive: true));
        var changed = TrackChanges();

        _viewModel.ApplySchedule(new TaskScheduleModel { Time = new TimeSpan(9, 0, 0) });

        Assert.Contains(nameof(ITaskEditPopUpViewModel.HasSchedule), changed);
        Assert.Contains(nameof(ITaskEditPopUpViewModel.ScheduleTimeDisplay), changed);
        Assert.Contains(nameof(ITaskEditPopUpViewModel.IsRepeating), changed);
    }

    [Fact]
    public void ApplySchedule_ReminderWithoutTime_DropsTheReminder()
    {
        _viewModel.Open(CreateNewInput(initialIsPositive: true));

        _viewModel.ApplySchedule(new TaskScheduleModel { ReminderMinutesBefore = 15 });

        Assert.Null(_viewModel.Model.ReminderMinutesBefore);
    }

    [Fact]
    public void ApplySchedule_OnTemplate_DoesNotRepeat()
    {
        _viewModel.Open(CreateNewInput(initialIsPositive: true));
        _viewModel.SetTemplateFlag(true);

        _viewModel.ApplySchedule(new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Day });

        Assert.Equal(TaskRepeatUnit.None, _viewModel.Model.RepeatUnit);
    }

    [Fact]
    public void HasSchedule_TimeOrRepetition_IsTrue()
    {
        _viewModel.Open(CreateNewInput(initialIsPositive: true));
        Assert.False(_viewModel.HasSchedule);

        _viewModel.ApplySchedule(new TaskScheduleModel { Time = new TimeSpan(9, 0, 0) });
        Assert.True(_viewModel.HasSchedule);

        _viewModel.ApplySchedule(new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Month });
        Assert.True(_viewModel.HasSchedule);
        Assert.True(_viewModel.IsRepeating);
    }

    [Fact]
    public void ScheduleTimeDisplay_WithoutTime_IsNull()
    {
        _viewModel.Open(CreateNewInput(initialIsPositive: true));

        Assert.Null(_viewModel.ScheduleTimeDisplay);
    }

    [Fact]
    public void ScheduleTimeDisplay_WithTime_IsFormattedAsHoursAndMinutes()
    {
        _viewModel.Open(CreateNewInput(initialIsPositive: true));

        _viewModel.ApplySchedule(new TaskScheduleModel { Time = new TimeSpan(7, 5, 0) });

        Assert.Equal("07:05", _viewModel.ScheduleTimeDisplay);
    }

    [Fact]
    public void CreateScheduleModel_ExistingTaskWithSchedule_ReturnsItsValues()
    {
        var task = TestTasks.Create();
        task.TaskTime = new TimeSpan(12, 0, 0);
        task.ReminderMinutesBefore = 5;
        task.RepeatUnit = TaskRepeatUnit.Day;
        task.RepeatInterval = 3;
        _viewModel.Open(CreateExistingInput(task));

        var schedule = _viewModel.CreateScheduleModel();

        Assert.Equal(new TimeSpan(12, 0, 0), schedule.Time);
        Assert.Equal(5, schedule.ReminderMinutesBefore);
        Assert.Equal(TaskRepeatUnit.Day, schedule.RepeatUnit);
        Assert.Equal(3, schedule.RepeatInterval);
    }

    [Fact]
    public void CanRepeat_Template_IsFalse()
    {
        _viewModel.Open(CreateNewInput(initialIsPositive: true));
        Assert.True(_viewModel.CanRepeat);

        _viewModel.SetTemplateFlag(true);

        Assert.False(_viewModel.CanRepeat);
    }

    [Fact]
    public void SetTemplateFlag_True_RemovesTheRepetitionButKeepsTheTime()
    {
        _viewModel.Open(CreateNewInput(initialIsPositive: true));
        _viewModel.ApplySchedule(new TaskScheduleModel
        {
            Time = new TimeSpan(9, 0, 0),
            RepeatUnit = TaskRepeatUnit.Week,
            RepeatWeekdays = TaskWeekdays.Monday
        });

        _viewModel.SetTemplateFlag(true);

        Assert.Equal(TaskRepeatUnit.None, _viewModel.Model.RepeatUnit);
        Assert.Equal(new TimeSpan(9, 0, 0), _viewModel.Model.TaskTime);
        Assert.False(_viewModel.IsRepeating);
    }

    [Fact]
    public void CreateClosePayload_UpdateWithChangedTime_HasChanges()
    {
        var task = TestTasks.Create();
        _viewModel.Open(CreateExistingInput(task));
        _viewModel.ApplySchedule(new TaskScheduleModel { Time = new TimeSpan(10, 0, 0) });

        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.True(payload.HasChanges);
        Assert.Equal(new TimeSpan(10, 0, 0), payload.Task.TaskTime);
    }

    [Fact]
    public void CreateClosePayload_UpdateWithChangedReminder_HasChanges()
    {
        var task = TestTasks.Create();
        task.TaskTime = new TimeSpan(10, 0, 0);
        _viewModel.Open(CreateExistingInput(task));
        _viewModel.ApplySchedule(new TaskScheduleModel { Time = new TimeSpan(10, 0, 0), ReminderMinutesBefore = 30 });

        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.True(payload.HasChanges);
    }

    [Fact]
    public void CreateClosePayload_UpdateWithChangedRepetition_HasChanges()
    {
        var task = TestTasks.Create();
        _viewModel.Open(CreateExistingInput(task));
        _viewModel.ApplySchedule(new TaskScheduleModel { RepeatUnit = TaskRepeatUnit.Day });

        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.True(payload.HasChanges);
        Assert.Equal(TaskRepeatUnit.Day, payload.Task.RepeatUnit);
    }

    [Fact]
    public void CreateClosePayload_UpdateWithUnchangedSchedule_HasNoChanges()
    {
        var task = TestTasks.Create();
        task.TaskTime = new TimeSpan(10, 0, 0);
        task.ReminderMinutesBefore = 5;
        task.RepeatUnit = TaskRepeatUnit.Week;
        task.RepeatWeekdays = TaskWeekdays.Tuesday;
        _viewModel.Open(CreateExistingInput(task));

        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.False(payload.HasChanges);
    }

    [Fact]
    public void CreateClosePayload_ExistingSeriesTask_KeepsItsSeries()
    {
        var seriesId = Guid.NewGuid();
        var task = TestTasks.Create();
        task.SeriesId = seriesId;
        task.RepeatUnit = TaskRepeatUnit.Day;
        _viewModel.Open(CreateExistingInput(task));

        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.Equal(seriesId, payload.Task.SeriesId);
    }

    private List<string?> TrackChanges()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        return changed;
    }

    private static TaskEditPopUpInputModel CreateNewInput(
        bool isWorkScope = true,
        bool? initialIsPositive = null,
        bool isTaskDateLocked = false)
    {
        return new TaskEditPopUpInputModel
        {
            ContextualTaskDate = TestTasks.Day,
            IsWorkScope = isWorkScope,
            InitialIsPositive = initialIsPositive,
            IsTaskDateLocked = isTaskDateLocked
        };
    }

    private static TaskEditPopUpInputModel CreateInputWithTemplates(bool? initialIsPositive)
    {
        var forYouTemplate = new TaskEditTemplateOptionModel
        {
            TaskId = Guid.NewGuid(),
            Text = "Yoga",
            IsPositive = true
        };
        var toDoTemplate = new TaskEditTemplateOptionModel
        {
            TaskId = Guid.NewGuid(),
            Text = "Bericht",
            IsPositive = false
        };

        return new TaskEditPopUpInputModel
        {
            ContextualTaskDate = TestTasks.Day,
            InitialIsPositive = initialIsPositive,
            TemplateOptions = [forYouTemplate, toDoTemplate]
        };
    }

    private static TaskEditPopUpInputModel CreateExistingInput(TaskDto task)
    {
        return new TaskEditPopUpInputModel
        {
            Task = task,
            ContextualTaskDate = TestTasks.Day
        };
    }
}
