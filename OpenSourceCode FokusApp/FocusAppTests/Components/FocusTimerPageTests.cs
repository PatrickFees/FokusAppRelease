using Bunit;
using FokusApp.Components.FocusTimerComponent;
using FokusApp.Components.PopUps.TimeInputDialog;
using FokusApp.Components.SharedUI.FilterComponent;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using FilterComponent = FokusApp.Components.SharedUI.FilterComponent.FilterComponent;

namespace FocusAppTests.Components;

public class FocusTimerPageTests : ComponentTestContext
{
    private const string PickerTriggerSelector = "#timer-task-picker-trigger";

    private readonly IFocusTimerViewModel _viewModel = Substitute.For<IFocusTimerViewModel>();
    private readonly FilterModel _taskFilter = new();

    public FocusTimerPageTests()
    {
        IReadOnlyList<TaskDto> noTasks = [];
        _viewModel.NormalTasks.Returns(noTasks);
        _viewModel.Templates.Returns(noTasks);
        _viewModel.SelectedTask.Returns(new TaskDto());
        _viewModel.TaskFilter.Returns(_taskFilter);
        _viewModel.FormattedTime.Returns("25:00");
        _viewModel.InputMinutes.Returns(25);

        Services.AddSingleton(_viewModel);
        Services.AddSingleton(Substitute.For<IUiBackPressService>());
        AddFilterComponentServices();
    }

    [Fact]
    public void Render_InitializesViewModel()
    {
        Render<FocusTimerPage>();

        _viewModel.Received(1).InitializeAsync();
    }

    [Fact]
    public void Render_PickerClosed_ShowsNoFilter()
    {
        var cut = Render<FocusTimerPage>();

        var filters = cut.FindComponents<FilterComponent>();
        Assert.Empty(filters);
    }

    [Fact]
    public void ClickPickerTrigger_PreparesPickerAndShowsFilterComponent()
    {
        var cut = Render<FocusTimerPage>();
        var trigger = cut.Find(PickerTriggerSelector);

        trigger.Click();

        var filter = cut.FindComponent<FilterComponent>();
        _viewModel.Received(1).PrepareTaskPicker();
        Assert.True(filter.Instance.IsOpen);
        Assert.NotEmpty(cut.FindAll(".timer-task-search"));
    }

    [Fact]
    public void ClickPickerTrigger_ShowsFilterWithoutDateSortToggleAndKeepsFilters()
    {
        var cut = Render<FocusTimerPage>();
        var trigger = cut.Find(PickerTriggerSelector);

        trigger.Click();

        var filter = cut.FindComponent<FilterComponent>();
        Assert.False(filter.Instance.ShowDateSortToggle);
        Assert.False(filter.Instance.ResetFiltersOnOpen);
        Assert.Empty(cut.FindAll(".timer-task-sort-button"));
    }

    [Fact]
    public void ClickPickerTrigger_ShowsFilterWithCurrentFilterOfViewModel()
    {
        _taskFilter.SearchText = "Bericht";
        _taskFilter.SelectedPriorities = [TaskPriority.Hoch];
        _taskFilter.IsSortDescending = false;
        var cut = Render<FocusTimerPage>();
        var trigger = cut.Find(PickerTriggerSelector);

        trigger.Click();

        var filter = cut.FindComponent<FilterComponent>();
        var searchInput = cut.Find(".timer-task-search");
        Assert.Equal("Bericht", filter.Instance.InitialSearchText);
        Assert.Equal([TaskPriority.Hoch], filter.Instance.InitialSelectedPriorities);
        Assert.False(filter.Instance.InitialIsSortDescending);
        Assert.Equal("Bericht", searchInput.GetAttribute("value"));
    }

    [Fact]
    public void ClickPickerTriggerTwice_ClosesFilterComponent()
    {
        var cut = Render<FocusTimerPage>();
        var trigger = cut.Find(PickerTriggerSelector);
        trigger.Click();

        trigger.Click();

        var filters = cut.FindComponents<FilterComponent>();
        Assert.Empty(filters);
    }

    [Fact]
    public void EnterSearchText_InFilter_AppliesFilterChangesAtViewModel()
    {
        var cut = Render<FocusTimerPage>();
        var trigger = cut.Find(PickerTriggerSelector);
        trigger.Click();
        var searchInput = cut.Find(".timer-task-search");

        searchInput.Input("Bericht");

        cut.WaitForAssertion(() => _viewModel.Received().ApplyFilterChangesAsync(
            Arg.Is<FilterModel>(payload => payload.SearchText == "Bericht")));
    }

    [Fact]
    public void ClickPriority_InFilter_AppliesFilterChangesAtViewModel()
    {
        var cut = Render<FocusTimerPage>();
        var trigger = cut.Find(PickerTriggerSelector);
        trigger.Click();
        var priorityButtons = cut.FindAll(".priority-button");

        priorityButtons[0].Click();

        cut.WaitForAssertion(() => _viewModel.Received().ApplyFilterChangesAsync(
            Arg.Is<FilterModel>(payload => payload.SelectedPriorities.Count == 1)));
    }

    [Fact]
    public void Render_BreakMode_ShowsNoTaskPicker()
    {
        _viewModel.IsBreakMode.Returns(true);

        var cut = Render<FocusTimerPage>();

        var triggers = cut.FindAll(PickerTriggerSelector);
        var filters = cut.FindComponents<FilterComponent>();
        Assert.Empty(triggers);
        Assert.Empty(filters);
    }

    [Fact]
    public void Render_Always_ShowsFormattedTimeAndMinutes()
    {
        var cut = Render<FocusTimerPage>();

        var time = cut.Find(".timer-display-time");
        var minutes = cut.Find(".timer-display-minutes");
        Assert.Equal("25:00", time.TextContent);
        Assert.StartsWith("25", minutes.TextContent);
    }

    [Fact]
    public void Render_NoTasksAvailable_ShowsHintThatNoTasksAreOpen()
    {
        _viewModel.HasAvailableTasks.Returns(false);

        var cut = Render<FocusTimerPage>();

        var hint = cut.Find(".timer-task-empty");
        Assert.Equal(Text("NoOpenTasks"), hint.TextContent);
    }

    [Fact]
    public void Render_TasksAvailable_ShowsNoEmptyHint()
    {
        _viewModel.HasAvailableTasks.Returns(true);

        var cut = Render<FocusTimerPage>();

        Assert.Empty(cut.FindAll(".timer-task-empty"));
    }

    [Fact]
    public void Render_SelectedTaskWithNote_ShowsPriorityAndNote()
    {
        _viewModel.SelectedTask.Returns(new TaskDto { Note = "Bericht schreiben" });
        _viewModel.SelectedTaskNote.Returns("Bericht schreiben");
        _viewModel.SelectedTaskPriority.Returns(TaskPriority.Hoch);

        var cut = Render<FocusTimerPage>();

        var note = cut.Find(".timer-task-note");
        var priority = cut.Find(".timer-task-priority");
        Assert.Equal("Bericht schreiben", note.TextContent);
        Assert.Equal(Text("Priority", TaskPriority.Hoch.ToString()), priority.TextContent);
    }

    [Fact]
    public void Render_SelectedTaskWithBlankNote_ShowsNoTaskDetails()
    {
        _viewModel.SelectedTask.Returns(new TaskDto { Note = "   " });

        var cut = Render<FocusTimerPage>();

        Assert.Empty(cut.FindAll(".timer-task-details"));
    }

    [Fact]
    public void Render_BreakMode_ShowsBreakTitleAndEndBreakButton()
    {
        _viewModel.IsBreakMode.Returns(true);

        var cut = Render<FocusTimerPage>();

        var title = cut.Find(".timer-break-title");
        var buttons = cut.FindAll(".timer-actions button");
        Assert.Equal(Text("Break"), title.TextContent);
        Assert.Equal(Text("EndBreak"), buttons[1].TextContent);
    }

    [Fact]
    public void Render_NotificationPermissionMissing_ShowsPermissionHint()
    {
        _viewModel.IsNotificationPermissionMissing.Returns(true);

        var cut = Render<FocusTimerPage>();

        var hint = cut.Find(".timer-permission-hint");
        Assert.Equal(Text("NotificationPermissionHint"), hint.TextContent);
    }

    [Fact]
    public void Render_NotificationPermissionGranted_ShowsNoPermissionHint()
    {
        var cut = Render<FocusTimerPage>();

        Assert.Empty(cut.FindAll(".timer-permission-hint"));
    }

    [Theory]
    [InlineData(false, "Start", false)]
    [InlineData(true, "Break", true)]
    public void Render_TimerRunningOrNot_ShowsMatchingToggleButton(bool isRunning, string textKey, bool hasRunningClass)
    {
        _viewModel.IsRunning.Returns(isRunning);
        _viewModel.CanToggleTimer.Returns(true);

        var cut = Render<FocusTimerPage>();

        var toggle = cut.Find(".timer-toggle-button");
        Assert.Equal(Text(textKey), toggle.TextContent);
        Assert.Equal(hasRunningClass, toggle.ClassList.Contains("is-running"));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Render_ToggleAndResetAvailability_DisablesTheButtons(bool canToggle, bool canReset)
    {
        _viewModel.CanToggleTimer.Returns(canToggle);
        _viewModel.CanResetTimer.Returns(canReset);

        var cut = Render<FocusTimerPage>();

        var buttons = cut.FindAll(".timer-actions button");
        Assert.Equal(!canToggle, buttons[0].HasAttribute("disabled"));
        Assert.Equal(!canReset, buttons[1].HasAttribute("disabled"));
    }

    [Fact]
    public void ClickToggleButton_Enabled_SwitchesTimerAtViewModel()
    {
        _viewModel.CanToggleTimer.Returns(true);
        var cut = Render<FocusTimerPage>();

        cut.Find(".timer-toggle-button").Click();

        _viewModel.Received(1).SwitchTimerToStartOrStop();
    }

    [Fact]
    public void ClickResetButton_Enabled_ResetsTimerAtViewModel()
    {
        _viewModel.CanResetTimer.Returns(true);
        var cut = Render<FocusTimerPage>();

        cut.FindAll(".timer-actions button")[1].Click();

        _viewModel.Received(1).ResetTimer();
    }

    [Fact]
    public void PropertyChanged_FormattedTimeChanges_RendersNewTime()
    {
        var cut = Render<FocusTimerPage>();
        _viewModel.FormattedTime.Returns("24:59");

        _viewModel.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _viewModel,
            new System.ComponentModel.PropertyChangedEventArgs(nameof(IFocusTimerViewModel.FormattedTime)));

        cut.WaitForAssertion(() => Assert.Equal("24:59", cut.Find(".timer-display-time").TextContent));
    }

    [Fact]
    public void Render_ProgressAndSelection_AreDrawnAsRingStrokes()
    {
        _viewModel.SelectedMinuteRatio.Returns(0.5);
        _viewModel.CountdownProgress.Returns(0.25);

        var cut = Render<FocusTimerPage>();

        var selection = cut.Find(".timer-ring-selection");
        var progress = cut.Find(".timer-ring-progress");
        Assert.Contains("stroke-dasharray", selection.GetAttribute("style"));
        Assert.Contains("stroke-dashoffset", progress.GetAttribute("style"));
    }

    [Fact]
    public void ClickTimerDisplay_TimerIdle_OpensManualInputDialog()
    {
        var cut = Render<FocusTimerPage>();

        cut.Find(".timer-display").Click();

        var dialog = cut.FindComponent<TimeInputDialogPopUp>();
        Assert.True(dialog.Instance.IsOpen);
        Assert.Equal(25, dialog.Instance.InitialMinutes);
    }

    [Fact]
    public void ClickTimerDisplay_TimerActive_KeepsManualInputDialogClosed()
    {
        _viewModel.IsTimerActive.Returns(true);
        var cut = Render<FocusTimerPage>();

        cut.Find(".timer-display").Click();

        var dialog = cut.FindComponent<TimeInputDialogPopUp>();
        Assert.False(dialog.Instance.IsOpen);
    }

    [Theory]
    [InlineData("Enter", true)]
    [InlineData(" ", true)]
    [InlineData("Spacebar", true)]
    [InlineData("a", false)]
    [InlineData("Escape", false)]
    public void KeyDownOnTimerDisplay_Key_OpensManualInputDialogOnlyForActivationKeys(string key, bool expectedOpen)
    {
        var cut = Render<FocusTimerPage>();

        cut.Find(".timer-display").KeyDown(new KeyboardEventArgs { Key = key });

        var dialog = cut.FindComponent<TimeInputDialogPopUp>();
        Assert.Equal(expectedOpen, dialog.Instance.IsOpen);
    }

    [Fact]
    public void ConfirmManualInput_Minutes_SetsMinutesAtViewModelAndClosesDialog()
    {
        var cut = Render<FocusTimerPage>();
        cut.Find(".timer-display").Click();

        cut.Find(".timer-input-field").Input("40");
        cut.Find(".timer-input-form").Submit();

        _viewModel.Received(1).SetMinutes(40);
        var dialog = cut.FindComponent<TimeInputDialogPopUp>();
        Assert.False(dialog.Instance.IsOpen);
    }

    [Fact]
    public void CancelManualInput_OpenDialog_ClosesDialogWithoutChangingMinutes()
    {
        var cut = Render<FocusTimerPage>();
        cut.Find(".timer-display").Click();

        cut.Find(".timer-input-actions .btn-secondary").Click();

        var dialog = cut.FindComponent<TimeInputDialogPopUp>();
        Assert.False(dialog.Instance.IsOpen);
        _viewModel.DidNotReceive().SetMinutes(Arg.Any<int>());
    }

    [Fact]
    public void PointerDown_OnAdjustmentRing_SetsMinutesFromPointerPosition()
    {
        var cut = Render<FocusTimerPage>();

        cut.Find(".timer-clock").PointerDown(RingPointer(angleDegrees: 90));

        _viewModel.Received(1).SetMinutes(30);
    }

    [Fact]
    public void PointerMove_AfterPointerDownOnRing_FollowsThePointer()
    {
        var cut = Render<FocusTimerPage>();
        var clock = cut.Find(".timer-clock");
        clock.PointerDown(RingPointer(angleDegrees: 90));

        clock.PointerMove(RingPointer(angleDegrees: 180));

        _viewModel.Received(1).SetMinutes(60);
    }

    [Fact]
    public void PointerMove_WithoutPointerDown_ChangesNothing()
    {
        var cut = Render<FocusTimerPage>();

        cut.Find(".timer-clock").PointerMove(RingPointer(angleDegrees: 180));

        _viewModel.DidNotReceive().SetMinutes(Arg.Any<int>());
    }

    [Fact]
    public void PointerMove_PointerLeavesRing_StopsFollowingThePointer()
    {
        var cut = Render<FocusTimerPage>();
        var clock = cut.Find(".timer-clock");
        clock.PointerDown(RingPointer(angleDegrees: 90));
        clock.PointerMove(new PointerEventArgs { OffsetX = 130, OffsetY = 130 });
        _viewModel.ClearReceivedCalls();

        clock.PointerMove(RingPointer(angleDegrees: 180));

        _viewModel.DidNotReceive().SetMinutes(Arg.Any<int>());
    }

    [Fact]
    public void PointerDown_OutsideAdjustmentRing_ChangesNothing()
    {
        var cut = Render<FocusTimerPage>();

        cut.Find(".timer-clock").PointerDown(new PointerEventArgs { OffsetX = 130, OffsetY = 130 });

        _viewModel.DidNotReceive().SetMinutes(Arg.Any<int>());
    }

    [Fact]
    public void PointerDown_TimerActive_ChangesNothing()
    {
        _viewModel.IsTimerActive.Returns(true);
        var cut = Render<FocusTimerPage>();
        var clock = cut.Find(".timer-clock");

        clock.PointerDown(RingPointer(angleDegrees: 90));
        clock.PointerMove(RingPointer(angleDegrees: 180));

        _viewModel.DidNotReceive().SetMinutes(Arg.Any<int>());
    }

    [Theory]
    [InlineData("onpointerup")]
    [InlineData("onpointercancel")]
    [InlineData("onpointerleave")]
    public void PointerRelease_AfterPointerDown_StopsFollowingThePointer(string releaseEvent)
    {
        var cut = Render<FocusTimerPage>();
        var clock = cut.Find(".timer-clock");
        clock.PointerDown(RingPointer(angleDegrees: 90));
        _viewModel.ClearReceivedCalls();

        clock.TriggerEvent(releaseEvent, new PointerEventArgs());
        clock.PointerMove(RingPointer(angleDegrees: 180));

        _viewModel.DidNotReceive().SetMinutes(Arg.Any<int>());
    }

    [Fact]
    public void ClickPickerOption_Task_SelectsTaskAndClosesPicker()
    {
        var normal = new TaskDto { Id = Guid.NewGuid(), Note = "Einmalig" };
        var template = new TaskDto { Id = Guid.NewGuid(), Note = "Regelmäßig" };
        _viewModel.NormalTasks.Returns([normal]);
        _viewModel.Templates.Returns([template]);
        var cut = Render<FocusTimerPage>();
        cut.Find(PickerTriggerSelector).Click();

        cut.FindAll(".timer-task-option")[1].Click();

        _viewModel.Received(1).SelectTaskAsync(normal.Id);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".timer-task-picker-popup")));
    }

    [Fact]
    public void ClickPickerOption_Template_SelectsThatTask()
    {
        var normal = new TaskDto { Id = Guid.NewGuid(), Note = "Einmalig" };
        var template = new TaskDto { Id = Guid.NewGuid(), Note = "Regelmäßig" };
        _viewModel.NormalTasks.Returns([normal]);
        _viewModel.Templates.Returns([template]);
        var cut = Render<FocusTimerPage>();
        cut.Find(PickerTriggerSelector).Click();

        cut.FindAll(".timer-task-option")[2].Click();

        _viewModel.Received(1).SelectTaskAsync(template.Id);
    }

    [Fact]
    public void ClickPickerOption_NoTask_SelectsNothing()
    {
        var cut = Render<FocusTimerPage>();
        cut.Find(PickerTriggerSelector).Click();

        cut.Find(".timer-task-option-no-task").Click();

        _viewModel.Received(1).SelectTaskAsync(null);
    }

    [Fact]
    public void OpenPicker_TasksOfBothKinds_ShowsGroupsWithHeadings()
    {
        var normal = new TaskDto { Id = Guid.NewGuid(), Note = "Einmalig" };
        var template = new TaskDto { Id = Guid.NewGuid(), Note = null };
        _viewModel.NormalTasks.Returns([normal]);
        _viewModel.Templates.Returns([template]);

        var cut = Render<FocusTimerPage>();
        cut.Find(PickerTriggerSelector).Click();

        var headings = cut.FindAll(".timer-task-group-heading");
        Assert.Equal(2, headings.Count);
        Assert.Equal(Text("OneTimeTasks"), headings[0].TextContent);
        Assert.Equal(Text("Templates"), headings[1].TextContent);
        Assert.Empty(cut.FindAll(".timer-task-no-results"));
    }

    [Fact]
    public void OpenPicker_OnlyNormalTasks_ShowsOnlyThatGroup()
    {
        var normal = new TaskDto { Id = Guid.NewGuid(), Note = "Einmalig" };
        _viewModel.NormalTasks.Returns([normal]);

        var cut = Render<FocusTimerPage>();
        cut.Find(PickerTriggerSelector).Click();

        var headings = cut.FindAll(".timer-task-group-heading");
        var heading = Assert.Single(headings);
        Assert.Equal(Text("OneTimeTasks"), heading.TextContent);
    }

    [Fact]
    public void OpenPicker_NoTasksForFilters_ShowsNoResultsHint()
    {
        var cut = Render<FocusTimerPage>();

        cut.Find(PickerTriggerSelector).Click();

        var hint = cut.Find(".timer-task-no-results");
        Assert.Equal(Text("NoTasksForFilters"), hint.TextContent);
    }

    [Fact]
    public void OpenPicker_SelectedTask_IsMarkedAsSelected()
    {
        var normal = new TaskDto { Id = Guid.NewGuid(), Note = "Einmalig" };
        _viewModel.NormalTasks.Returns([normal]);
        _viewModel.SelectedTaskId.Returns(normal.Id);

        var cut = Render<FocusTimerPage>();
        cut.Find(PickerTriggerSelector).Click();

        var options = cut.FindAll(".timer-task-option");
        Assert.False(options[0].ClassList.Contains("selected"));
        Assert.True(options[1].ClassList.Contains("selected"));
    }

    [Fact]
    public void OpenPicker_NoTaskSelected_MarksNoTaskOptionAsSelected()
    {
        var cut = Render<FocusTimerPage>();

        cut.Find(PickerTriggerSelector).Click();

        var noTask = cut.Find(".timer-task-option-no-task");
        Assert.True(noTask.ClassList.Contains("selected"));
    }

    [Fact]
    public void ClickPickerBackdrop_OpenPicker_ClosesPicker()
    {
        var cut = Render<FocusTimerPage>();
        cut.Find(PickerTriggerSelector).Click();

        cut.Find(".timer-task-picker-backdrop").Click();

        Assert.Empty(cut.FindAll(".timer-task-picker-popup"));
    }

    [Fact]
    public void BackPress_PickerClosed_IsNotHandled()
    {
        var handlers = CaptureBackPressHandlers();
        Render<FocusTimerPage>();
        var handler = Assert.Single(handlers);

        var handled = handler();

        Assert.False(handled);
    }

    [Fact]
    public void BackPress_PickerOpen_ClosesPickerAndIsHandled()
    {
        var handlers = CaptureBackPressHandlers();
        var cut = Render<FocusTimerPage>();
        var handler = Assert.Single(handlers);
        cut.Find(PickerTriggerSelector).Click();

        var handled = handler();

        Assert.True(handled);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".timer-task-picker-popup")));
    }

    [Fact]
    public async Task Dispose_RenderedPage_ReleasesBackPressRegistration()
    {
        var registration = Substitute.For<IDisposable>();
        var backPress = Services.GetRequiredService<IUiBackPressService>();
        backPress.RegisterHandler(Arg.Any<Func<bool>>()).Returns(registration);
        Render<FocusTimerPage>();

        await DisposeComponentsAsync();

        registration.Received(1).Dispose();
    }

    [Fact]
    public void Render_Page_SetsThePageTitle()
    {
        var head = Render<HeadOutlet>();

        Render<FocusTimerPage>();

        head.WaitForAssertion(() => Assert.Contains(Text("FocusTimer"), head.Markup));
    }

    [Fact]
    public void OpenPicker_TasksWithoutNote_ShowEmptyOptionTexts()
    {
        var normal = new TaskDto { Id = Guid.NewGuid(), Note = null };
        var template = new TaskDto { Id = Guid.NewGuid(), Note = null };
        _viewModel.NormalTasks.Returns([normal]);
        _viewModel.Templates.Returns([template]);
        var cut = Render<FocusTimerPage>();

        cut.Find(PickerTriggerSelector).Click();

        var options = cut.FindAll(".timer-task-option");
        Assert.Equal(string.Empty, options[1].TextContent.Trim());
        Assert.Equal(string.Empty, options[2].TextContent.Trim());
    }

    [Fact]
    public void OpenPicker_TemplateSelected_IsMarkedAsSelected()
    {
        var template = new TaskDto { Id = Guid.NewGuid(), Note = "Regelmäßig" };
        _viewModel.Templates.Returns([template]);
        _viewModel.SelectedTaskId.Returns(template.Id);
        var cut = Render<FocusTimerPage>();

        cut.Find(PickerTriggerSelector).Click();

        var options = cut.FindAll(".timer-task-option");
        Assert.True(options[1].ClassList.Contains("selected"));
        Assert.False(options[0].ClassList.Contains("selected"));
    }

    [Fact]
    public void OpenPicker_OnlyTemplates_ShowsOnlyThatGroup()
    {
        var template = new TaskDto { Id = Guid.NewGuid(), Note = "Regelmäßig" };
        _viewModel.Templates.Returns([template]);
        var cut = Render<FocusTimerPage>();

        cut.Find(PickerTriggerSelector).Click();

        var heading = Assert.Single(cut.FindAll(".timer-task-group-heading"));
        Assert.Equal(Text("Templates"), heading.TextContent);
    }

    private List<Func<bool>> CaptureBackPressHandlers()
    {
        var captured = new List<Func<bool>>();
        var backPress = Services.GetRequiredService<IUiBackPressService>();
        backPress.RegisterHandler(Arg.Do<Func<bool>>(captured.Add));
        return captured;
    }

    private static PointerEventArgs RingPointer(double angleDegrees)
    {
        const double centre = 130;
        const double radius = 105;
        var radians = angleDegrees * Math.PI / 180;
        return new PointerEventArgs
        {
            OffsetX = centre + (radius * Math.Sin(radians)),
            OffsetY = centre - (radius * Math.Cos(radians))
        };
    }
}
