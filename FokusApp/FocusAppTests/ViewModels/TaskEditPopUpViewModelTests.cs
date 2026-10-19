using FokusApp.Components.PopUps.TaskEdit;
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
    public void Open_ExistingTask_LocksRegularFlagOnlyForTemplates(bool isRegular, bool expectedLocked)
    {
        var task = TestTasks.Create(isRegular: isRegular);
        var input = CreateExistingInput(task);

        _viewModel.Open(input);

        Assert.Equal(expectedLocked, _viewModel.IsRegularTaskLocked);
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
    public void Open_RegularTemplateOptions_AreExposed()
    {
        var option = new TaskEditRegularTemplateOptionModel
        {
            TaskId = Guid.NewGuid(),
            Text = "Sport"
        };
        var input = new TaskEditPopUpInputModel
        {
            ContextualTaskDate = TestTasks.Day,
            RegularTemplateOptions = [option]
        };

        _viewModel.Open(input);

        Assert.True(_viewModel.HasRegularTemplateOptions);
        Assert.Single(_viewModel.RegularTemplateOptions);
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
    public void SetDate_RegularTask_IsIgnored()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        _viewModel.SetRegularity(true);
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
    public void RequestOpeningCalendarPopUp_RegularTask_DoesNotShowCalendar()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        _viewModel.SetRegularity(true);

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
    public void SetRegularity_True_SavesTaskWithoutDate()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);

        _viewModel.SetRegularity(true);
        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.False(_viewModel.IsTaskDateAllowed);
        Assert.True(payload.Task.IsRegularTask);
        Assert.Null(payload.Task.TaskDate);
    }

    [Fact]
    public void SetRegularity_RemovedAgain_RestoresPreviouslySelectedDate()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);
        var chosenDay = new DateTime(2026, 5, 20);
        _viewModel.SetDate(chosenDay);

        _viewModel.SetRegularity(true);
        _viewModel.SetRegularity(false);
        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.Save, null);

        Assert.True(_viewModel.IsTaskDateAllowed);
        Assert.Equal(chosenDay, _viewModel.CalendarDate);
        Assert.Equal(chosenDay, payload.Task.TaskDate);
        Assert.False(payload.Task.IsRegularTask);
    }

    [Fact]
    public void SetRegularity_RemovedWithoutChosenDate_FallsBackToContextualDay()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);

        _viewModel.SetRegularity(true);
        _viewModel.SetRegularity(false);

        Assert.Equal(TestTasks.Day, _viewModel.Model.TaskDate);
    }

    [Fact]
    public void SetRegularity_ExistingTemplate_CannotBeUnmarked()
    {
        var template = TestTasks.Create(isRegular: true);
        var input = CreateExistingInput(template);
        _viewModel.Open(input);

        _viewModel.SetRegularity(false);

        Assert.True(_viewModel.Model.IsRegularTask);
        Assert.True(_viewModel.IsRegularTaskLocked);
    }

    [Fact]
    public void SetRegularity_Changed_RaisesDateRelatedProperties()
    {
        var changed = TrackChanges();
        var input = CreateNewInput();
        _viewModel.Open(input);

        _viewModel.SetRegularity(true);

        Assert.Contains(nameof(TaskEditPopUpViewModel.IsTaskDateAllowed), changed);
        Assert.Contains(nameof(TaskEditPopUpViewModel.CalendarDate), changed);
        Assert.Contains(nameof(TaskEditPopUpViewModel.TaskDayButtonModel), changed);
    }

    [Fact]
    public void TaskDayButtonModel_RegularTask_IsDisabled()
    {
        var input = CreateNewInput();
        _viewModel.Open(input);

        _viewModel.SetRegularity(true);
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
        var payload = _viewModel.CreateClosePayload(TaskEditPopUpCloseAction.CopyRegularTemplate, templateId);

        var task = payload.Task;
        Assert.Equal(TaskEditPopUpCloseAction.CopyRegularTemplate, payload.Action);
        Assert.Equal(templateId, payload.RegularTemplateTaskId);
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

    private static TaskEditPopUpInputModel CreateExistingInput(TaskDto task)
    {
        return new TaskEditPopUpInputModel
        {
            Task = task,
            ContextualTaskDate = TestTasks.Day
        };
    }
}
