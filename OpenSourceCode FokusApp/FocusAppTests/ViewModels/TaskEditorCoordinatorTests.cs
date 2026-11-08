using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Core.Enums;
using FokusApp.Core.Extensions;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FocusAppTests.ViewModels;

public class TaskEditorCoordinatorTests
{
    private readonly IScopeService _scopeService = Substitute.For<IScopeService>();
    private readonly ITaskCacheService _taskCache = Substitute.For<ITaskCacheService>();
    private readonly IUiTextProvider _textProvider = TestTexts.Create();
    private readonly ITaskPriorityFormatter _priorityFormatter = Substitute.For<ITaskPriorityFormatter>();
    private readonly ITaskSeriesService _taskSeriesService = Substitute.For<ITaskSeriesService>();
    private readonly TaskEditorCoordinator _coordinator;

    public TaskEditorCoordinatorTests()
    {
        _scopeService.IsWorkScope.Returns(true);
        _priorityFormatter.Format(Arg.Any<TaskPriority>()).Returns(call => call.Arg<TaskPriority>().ToString());
        _coordinator = new TaskEditorCoordinator(_scopeService, _taskCache, _textProvider, _priorityFormatter, _taskSeriesService);
    }

    [Fact]
    public void Constructor_NoOpenEditor_HasNoInput()
    {
        Assert.False(_coordinator.IsOpen);
        Assert.Null(_coordinator.Input);
    }

    [Fact]
    public void Open_NewTask_StoresInputWithDayWithoutTimeAndPositivity()
    {
        var dayWithTime = new DateTime(2026, 3, 10, 14, 30, 0);

        _coordinator.Open(null, dayWithTime, [], true);

        var input = _coordinator.Input;
        Assert.True(_coordinator.IsOpen);
        Assert.NotNull(input);
        Assert.Null(input.Task);
        Assert.Equal(TestTasks.Day, input.ContextualTaskDate);
        Assert.True(input.InitialIsPositive);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Open_LockTaskDate_IsPassedToInput(bool lockTaskDate)
    {
        _coordinator.LockTaskDate = lockTaskDate;

        _coordinator.Open(null, TestTasks.Day, []);

        Assert.Equal(lockTaskDate, _coordinator.Input!.IsTaskDateLocked);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Open_Scope_IsTakenFromScopeService(bool isWorkScope)
    {
        _scopeService.IsWorkScope.Returns(isWorkScope);

        _coordinator.Open(null, TestTasks.Day, []);

        var input = _coordinator.Input;
        Assert.NotNull(input);
        Assert.Equal(isWorkScope, input.IsWorkScope);
    }

    [Fact]
    public void Open_TemplatesInSource_OffersOnlyOpenTemplatesWithText()
    {
        var template = TestTasks.Create("Sport", isTemplate: true, priority: TaskPriority.Hoch);
        var doneTemplate = TestTasks.Create("Alt", isTemplate: true, isDone: true);
        var normalTask = TestTasks.Create("Normal");
        IReadOnlyList<TaskDto> source = [normalTask, template, doneTemplate];

        _coordinator.Open(null, TestTasks.Day, source);

        var input = _coordinator.Input;
        Assert.NotNull(input);
        var option = Assert.Single(input.TemplateOptions);
        Assert.Equal(template.Id, option.TaskId);
        Assert.Equal("TemplateOption:Sport|Hoch", option.Text);
    }

    [Fact]
    public void Open_ForYouAndToDoTemplates_MarkTheirKindInTheOptions()
    {
        var forYou = TestTasks.Create("Spazieren", isTemplate: true);
        forYou.IsPositive = true;
        var toDo = TestTasks.Create("Steuer", isTemplate: true);
        toDo.IsPositive = false;
        IReadOnlyList<TaskDto> source = [forYou, toDo];

        _coordinator.Open(null, TestTasks.Day, source);

        var options = _coordinator.Input!.TemplateOptions;
        Assert.True(options.Single(option => option.TaskId == forYou.Id).IsPositive);
        Assert.False(options.Single(option => option.TaskId == toDo.Id).IsPositive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Open_TemplateWithoutNote_UsesUntitledText(string? note)
    {
        var template = TestTasks.Create(note!, isTemplate: true);
        IReadOnlyList<TaskDto> source = [template];

        _coordinator.Open(null, TestTasks.Day, source);

        var input = _coordinator.Input;
        Assert.NotNull(input);
        var option = Assert.Single(input.TemplateOptions);
        Assert.Equal("TemplateOption:Untitled|Keine", option.Text);
    }

    [Fact]
    public void Open_TemplateNoteWithWhitespace_TrimsNote()
    {
        var template = TestTasks.Create("  Sport  ", isTemplate: true);
        IReadOnlyList<TaskDto> source = [template];

        _coordinator.Open(null, TestTasks.Day, source);

        var input = _coordinator.Input;
        Assert.NotNull(input);
        var option = Assert.Single(input.TemplateOptions);
        Assert.Equal("TemplateOption:Sport|Keine", option.Text);
    }

    [Fact]
    public void Close_OpenEditor_ClearsInput()
    {
        _coordinator.Open(null, TestTasks.Day, []);

        _coordinator.Close();

        Assert.False(_coordinator.IsOpen);
        Assert.Null(_coordinator.Input);
    }

    [Fact]
    public void TryOpenTemplateCopy_ValidTemplate_OpensOneTimeCopyForDay()
    {
        var template = TestTasks.Create("Sport", isTemplate: true, priority: TaskPriority.Hoch, timerMinutes: 30);
        IReadOnlyList<TaskDto> source = [template];
        var targetDay = new DateTime(2026, 3, 12, 9, 0, 0);

        var opened = _coordinator.TryOpenTemplateCopy(template.Id, targetDay, source);

        var input = _coordinator.Input;
        Assert.True(opened);
        Assert.NotNull(input);
        var copy = input.Task;
        Assert.NotNull(copy);
        Assert.Null(copy.Id);
        Assert.Equal("Sport", copy.Note);
        Assert.Equal(TaskPriority.Hoch, copy.Priority);
        Assert.Equal(30, copy.TimerMinutes);
        Assert.False(copy.IsTemplate);
        Assert.False(copy.IsDone);
        Assert.Equal(targetDay.Date, copy.TaskDate);
        Assert.Equal(default, copy.CreationDate);
    }

    [Fact]
    public void TryOpenTemplateCopy_ValidTemplate_KeepsTemplateUnchanged()
    {
        var template = TestTasks.Create("Sport", isTemplate: true);
        IReadOnlyList<TaskDto> source = [template];

        _coordinator.TryOpenTemplateCopy(template.Id, TestTasks.Day, source);

        Assert.True(template.IsTemplate);
        Assert.NotNull(template.Id);
        Assert.Null(template.TaskDate);
    }

    [Fact]
    public void TryOpenTemplateCopy_NoTemplateId_ReturnsFalseAndStaysClosed()
    {
        var template = TestTasks.Create(isTemplate: true);
        IReadOnlyList<TaskDto> source = [template];

        var opened = _coordinator.TryOpenTemplateCopy(null, TestTasks.Day, source);

        Assert.False(opened);
        Assert.False(_coordinator.IsOpen);
    }

    [Fact]
    public void TryOpenTemplateCopy_UnknownId_ReturnsFalse()
    {
        var template = TestTasks.Create(isTemplate: true);
        IReadOnlyList<TaskDto> source = [template];
        var unknownId = Guid.NewGuid();

        var opened = _coordinator.TryOpenTemplateCopy(unknownId, TestTasks.Day, source);

        Assert.False(opened);
        Assert.False(_coordinator.IsOpen);
    }

    [Fact]
    public void TryOpenTemplateCopy_DoneTemplate_ReturnsFalse()
    {
        var doneTemplate = TestTasks.Create(isTemplate: true, isDone: true);
        IReadOnlyList<TaskDto> source = [doneTemplate];

        var opened = _coordinator.TryOpenTemplateCopy(doneTemplate.Id, TestTasks.Day, source);

        Assert.False(opened);
    }

    [Fact]
    public void TryOpenTemplateCopy_NormalTask_ReturnsFalse()
    {
        var normalTask = TestTasks.Create();
        IReadOnlyList<TaskDto> source = [normalTask];

        var opened = _coordinator.TryOpenTemplateCopy(normalTask.Id, TestTasks.Day, source);

        Assert.False(opened);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task SaveAsync_EmptyNote_ReturnsFalseAndKeepsEditorOpen(string? note)
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var task = TestTasks.Create(note!);
        var payload = CreatePayload(task, isUpdateMode: false, hasChanges: true);

        var saved = await _coordinator.SaveAsync(payload);

        Assert.False(saved);
        Assert.True(_coordinator.IsOpen);
        await _taskCache.DidNotReceive().AddTaskAsync(Arg.Any<TaskDto>());
        await _taskCache.DidNotReceive().UpdateTaskAsync(Arg.Any<TaskDto>());
    }

    [Fact]
    public async Task SaveAsync_NewTask_AddsTaskAndClosesEditor()
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var task = TestTasks.Create("Neu");
        var payload = CreatePayload(task, isUpdateMode: false, hasChanges: true);

        var saved = await _coordinator.SaveAsync(payload);

        Assert.True(saved);
        Assert.False(_coordinator.IsOpen);
        await _taskCache.Received(1).AddTaskAsync(task);
        await _taskCache.DidNotReceive().UpdateTaskAsync(Arg.Any<TaskDto>());
    }

    [Fact]
    public async Task SaveAsync_NewTemplateWithTodayCopyEnabled_AddsTemplateAndTodayTask()
    {
        var clock = TestTasks.CreateClock();
        var coordinator = new TaskEditorCoordinator(_scopeService, _taskCache, _textProvider, _priorityFormatter, _taskSeriesService, clock)
        {
            CreateTodayTaskForNewTemplate = true
        };
        coordinator.Open(null, TestTasks.Day, []);
        var template = TestTasks.Create("Sport", isTemplate: true);
        var payload = CreatePayload(template, isUpdateMode: false, hasChanges: true);

        var saved = await coordinator.SaveAsync(payload);

        Assert.True(saved);
        await _taskCache.Received(1).AddTaskAsync(template);
        await _taskCache.Received(1).AddTaskAsync(Arg.Is<TaskDto>(task =>
            !ReferenceEquals(task, template) &&
            task.Note == "Sport" &&
            !task.IsTemplate &&
            !task.IsDone &&
            task.Id == null &&
            task.TaskDate == TestTasks.Today));
    }

    [Fact]
    public async Task SaveAsync_NewTemplateWithTodayCopyDisabled_AddsOnlyTemplate()
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var template = TestTasks.Create("Sport", isTemplate: true);
        var payload = CreatePayload(template, isUpdateMode: false, hasChanges: true);

        await _coordinator.SaveAsync(payload);

        await _taskCache.Received(1).AddTaskAsync(Arg.Any<TaskDto>());
        await _taskCache.Received(1).AddTaskAsync(template);
    }

    [Fact]
    public async Task SaveAsync_NewNormalTaskWithTodayCopyEnabled_AddsOnlyThatTask()
    {
        _coordinator.CreateTodayTaskForNewTemplate = true;
        _coordinator.Open(null, TestTasks.Day, []);
        var task = TestTasks.Create("Einmalig");
        var payload = CreatePayload(task, isUpdateMode: false, hasChanges: true);

        await _coordinator.SaveAsync(payload);

        await _taskCache.Received(1).AddTaskAsync(Arg.Any<TaskDto>());
        await _taskCache.Received(1).AddTaskAsync(task);
    }

    [Fact]
    public async Task SaveAsync_UpdateTemplateWithTodayCopyEnabled_DoesNotAddTodayTask()
    {
        _coordinator.CreateTodayTaskForNewTemplate = true;
        _coordinator.Open(null, TestTasks.Day, []);
        var template = TestTasks.Create("Sport", isTemplate: true);
        var payload = CreatePayload(template, isUpdateMode: true, hasChanges: true);

        await _coordinator.SaveAsync(payload);

        await _taskCache.DidNotReceive().AddTaskAsync(Arg.Any<TaskDto>());
        await _taskCache.Received(1).UpdateTaskAsync(template);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveAsync_UpdateTaskMarkedAsTemplate_KeepsTaskAndAddsTemplate(bool isDone)
    {
        var completedAt = TestTasks.Today.AddHours(8);
        var existing = TestTasks.Create("Sport", isDone: isDone);
        existing.CompletedAt = isDone ? completedAt : null;
        _taskCache.FindTaskById(existing.Id!.Value).Returns(existing);
        _coordinator.Open(existing, TestTasks.Day, []);
        var edited = CreateTemplateEdit(existing);
        var payload = CreatePayload(edited, isUpdateMode: true, hasChanges: true);

        var saved = await _coordinator.SaveAsync(payload);

        Assert.True(saved);
        Assert.False(_coordinator.IsOpen);
        await _taskCache.Received(1).UpdateTaskAsync(Arg.Is<TaskDto>(task =>
            task.Id == existing.Id &&
            !task.IsTemplate &&
            task.IsDone == isDone &&
            task.TaskDate == TestTasks.Day));
        await _taskCache.Received(1).AddTaskAsync(Arg.Is<TaskDto>(task =>
            task.Id == null &&
            task.Note == "Sport" &&
            task.IsTemplate &&
            !task.IsDone &&
            task.CompletedAt == null &&
            task.TaskDate == null));
    }

    [Fact]
    public async Task SaveAsync_UpdateRepeatingTaskMarkedAsTemplate_KeepsRepetitionOnTask()
    {
        var existing = TestTasks.Create("Sport");
        existing.RepeatUnit = TaskRepeatUnit.Day;
        existing.RepeatInterval = 2;
        _taskCache.FindTaskById(existing.Id!.Value).Returns(existing);
        _coordinator.Open(existing, TestTasks.Day, []);
        var edited = CreateTemplateEdit(existing);
        var payload = CreatePayload(edited, isUpdateMode: true, hasChanges: true);

        await _coordinator.SaveAsync(payload);

        await _taskCache.Received(1).UpdateTaskAsync(Arg.Is<TaskDto>(task =>
            task.RepeatUnit == TaskRepeatUnit.Day &&
            task.RepeatInterval == 2));
        await _taskCache.Received(1).AddTaskAsync(Arg.Is<TaskDto>(task =>
            task.RepeatUnit == TaskRepeatUnit.None));
    }

    [Fact]
    public async Task SaveAsync_UpdateExistingTemplate_UpdatesOnlyTheTemplate()
    {
        var existing = TestTasks.Create("Sport", isTemplate: true);
        _taskCache.FindTaskById(existing.Id!.Value).Returns(existing);
        _coordinator.Open(existing, TestTasks.Day, []);
        var edited = TaskExtension.Clone(existing);
        var payload = CreatePayload(edited, isUpdateMode: true, hasChanges: true);

        await _coordinator.SaveAsync(payload);

        await _taskCache.Received(1).UpdateTaskAsync(edited);
        await _taskCache.DidNotReceive().AddTaskAsync(Arg.Any<TaskDto>());
    }

    [Fact]
    public async Task SaveAsync_UpdateWithChanges_UpdatesTaskAndClosesEditor()
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var task = TestTasks.Create("Geändert");
        var payload = CreatePayload(task, isUpdateMode: true, hasChanges: true);

        var saved = await _coordinator.SaveAsync(payload);

        Assert.True(saved);
        Assert.False(_coordinator.IsOpen);
        await _taskCache.Received(1).UpdateTaskAsync(task);
        await _taskCache.DidNotReceive().AddTaskAsync(Arg.Any<TaskDto>());
    }

    [Fact]
    public async Task SaveAsync_UpdateWithoutChanges_DoesNotWriteButClosesEditor()
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var task = TestTasks.Create("Unverändert");
        var payload = CreatePayload(task, isUpdateMode: true, hasChanges: false);

        var saved = await _coordinator.SaveAsync(payload);

        Assert.True(saved);
        Assert.False(_coordinator.IsOpen);
        await _taskCache.DidNotReceive().UpdateTaskAsync(Arg.Any<TaskDto>());
        await _taskCache.DidNotReceive().AddTaskAsync(Arg.Any<TaskDto>());
    }

    [Fact]
    public async Task SaveAsync_NewTask_TellsTheSeriesServiceWithoutPreviousTask()
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var task = TestTasks.Create("Neu");
        var payload = CreatePayload(task, isUpdateMode: false, hasChanges: true);

        await _coordinator.SaveAsync(payload);

        await _taskSeriesService.Received(1).HandleSavedTaskAsync(task, null);
    }

    [Fact]
    public async Task SaveAsync_UpdateWithChanges_TellsTheSeriesServiceWithThePreviousTask()
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var previous = TestTasks.Create("Vorher");
        var task = TestTasks.Create("Nachher");
        task.Id = previous.Id;
        _taskCache.FindTaskById(previous.Id!.Value).Returns(previous);
        var payload = CreatePayload(task, isUpdateMode: true, hasChanges: true);

        await _coordinator.SaveAsync(payload);

        await _taskSeriesService.Received(1).HandleSavedTaskAsync(task, previous);
    }

    [Fact]
    public async Task SaveAsync_EditorOpenedForThisTaskOnly_TellsTheSeriesServiceThatScope()
    {
        var previous = TestTasks.Create("Vorher");
        var task = TestTasks.Create("Nachher");
        task.Id = previous.Id;
        _taskCache.FindTaskById(previous.Id!.Value).Returns(previous);
        _coordinator.Open(previous, TestTasks.Day, [], seriesScope: TaskSeriesScope.ThisTaskOnly);
        var payload = CreatePayload(task, isUpdateMode: true, hasChanges: true);

        await _coordinator.SaveAsync(payload);

        await _taskSeriesService.Received(1).HandleSavedTaskAsync(task, previous, TaskSeriesScope.ThisTaskOnly);
    }

    [Fact]
    public async Task SaveAsync_EditorReopenedAfterThisTaskOnly_GoesBackToWholeSeries()
    {
        var previous = TestTasks.Create("Vorher");
        var task = TestTasks.Create("Nachher");
        task.Id = previous.Id;
        _taskCache.FindTaskById(previous.Id!.Value).Returns(previous);
        _coordinator.Open(previous, TestTasks.Day, [], seriesScope: TaskSeriesScope.ThisTaskOnly);
        _coordinator.Close();
        _coordinator.Open(previous, TestTasks.Day, []);
        var payload = CreatePayload(task, isUpdateMode: true, hasChanges: true);

        await _coordinator.SaveAsync(payload);

        await _taskSeriesService.Received(1).HandleSavedTaskAsync(task, previous, TaskSeriesScope.WholeSeries);
    }

    [Fact]
    public async Task SaveAsync_UpdateWithoutChanges_DoesNotTellTheSeriesService()
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var task = TestTasks.Create("Unverändert");
        var payload = CreatePayload(task, isUpdateMode: true, hasChanges: false);

        await _coordinator.SaveAsync(payload);

        await _taskSeriesService.DidNotReceive().HandleSavedTaskAsync(Arg.Any<TaskDto>(), Arg.Any<TaskDto?>());
    }

    [Fact]
    public async Task SaveAsync_EmptyNote_DoesNotTellTheSeriesService()
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var task = TestTasks.Create(string.Empty);
        var payload = CreatePayload(task, isUpdateMode: false, hasChanges: true);

        await _coordinator.SaveAsync(payload);

        await _taskSeriesService.DidNotReceive().HandleSavedTaskAsync(Arg.Any<TaskDto>(), Arg.Any<TaskDto?>());
    }

    [Fact]
    public async Task HandleCloseAsync_EditorNotOpen_IgnoresPayload()
    {
        var task = TestTasks.Create("Neu");
        var payload = CreateActionPayload(TaskEditPopUpCloseAction.Save, task);

        var result = await _coordinator.HandleCloseAsync(payload, TestTasks.Day, []);

        Assert.Equal(TaskEditorCloseOutcome.Ignored, result.Outcome);
        await _taskCache.DidNotReceive().AddTaskAsync(Arg.Any<TaskDto>());
    }

    [Fact]
    public async Task HandleCloseAsync_Cancel_ClosesEditorWithoutSaving()
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var task = TestTasks.Create("Neu");
        var payload = CreateActionPayload(TaskEditPopUpCloseAction.Cancel, task);

        var result = await _coordinator.HandleCloseAsync(payload, TestTasks.Day, []);

        Assert.Equal(TaskEditorCloseOutcome.Closed, result.Outcome);
        Assert.False(_coordinator.IsOpen);
        await _taskCache.DidNotReceive().AddTaskAsync(Arg.Any<TaskDto>());
    }

    [Fact]
    public async Task HandleCloseAsync_SaveNewTask_AddsTaskAndReportsClosed()
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var task = TestTasks.Create("Neu");
        var payload = CreateActionPayload(TaskEditPopUpCloseAction.Save, task);

        var result = await _coordinator.HandleCloseAsync(payload, TestTasks.Day, []);

        Assert.Equal(TaskEditorCloseOutcome.Closed, result.Outcome);
        Assert.Null(result.Error);
        Assert.False(_coordinator.IsOpen);
        await _taskCache.Received(1).AddTaskAsync(task);
    }

    [Fact]
    public async Task HandleCloseAsync_SaveWithoutNote_ReportsNotSavedAndKeepsEditorOpen()
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var task = TestTasks.Create("  ");
        var payload = CreateActionPayload(TaskEditPopUpCloseAction.Save, task);

        var result = await _coordinator.HandleCloseAsync(payload, TestTasks.Day, []);

        Assert.Equal(TaskEditorCloseOutcome.NotSaved, result.Outcome);
        Assert.True(_coordinator.IsOpen);
    }

    [Fact]
    public async Task HandleCloseAsync_SaveFails_ReportsErrorAndKeepsEditorOpen()
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var task = TestTasks.Create("Neu");
        var payload = CreateActionPayload(TaskEditPopUpCloseAction.Save, task);
        var failure = new InvalidOperationException("Fehler");
        _taskCache.AddTaskAsync(Arg.Any<TaskDto>()).ThrowsAsync(failure);

        var result = await _coordinator.HandleCloseAsync(payload, TestTasks.Day, []);

        Assert.Equal(TaskEditorCloseOutcome.SaveFailed, result.Outcome);
        Assert.Same(failure, result.Error);
        Assert.True(_coordinator.IsOpen);
    }

    [Fact]
    public async Task HandleCloseAsync_CopyTemplate_OpensCopyForGivenDay()
    {
        var template = TestTasks.Create("Vorlage", isTemplate: true);
        IReadOnlyList<TaskDto> source = [template];
        _coordinator.Open(null, TestTasks.Day, source);
        var payload = CreateActionPayload(TaskEditPopUpCloseAction.CopyTemplate, template, template.Id);

        var result = await _coordinator.HandleCloseAsync(payload, TestTasks.Day, source);

        Assert.Equal(TaskEditorCloseOutcome.TemplateCopyOpened, result.Outcome);
        var input = _coordinator.Input;
        Assert.NotNull(input);
        Assert.NotNull(input.Task);
        Assert.Null(input.Task.Id);
        Assert.Equal(TestTasks.Day, input.Task.TaskDate);
    }

    [Fact]
    public async Task HandleCloseAsync_CopyUnknownTemplate_IsIgnoredAndKeepsEditor()
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var inputBefore = _coordinator.Input;
        var unknownId = Guid.NewGuid();
        var task = TestTasks.Create();
        var payload = CreateActionPayload(TaskEditPopUpCloseAction.CopyTemplate, task, unknownId);

        var result = await _coordinator.HandleCloseAsync(payload, TestTasks.Day, []);

        Assert.Equal(TaskEditorCloseOutcome.Ignored, result.Outcome);
        Assert.Same(inputBefore, _coordinator.Input);
    }

    [Fact]
    public async Task HandleCloseAsync_EditTemplate_RequestsEditAndLeavesEditorToViewModel()
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var inputBefore = _coordinator.Input;
        var template = TestTasks.Create("Vorlage", isTemplate: true);
        var payload = CreateActionPayload(TaskEditPopUpCloseAction.EditTemplate, template, template.Id);

        var result = await _coordinator.HandleCloseAsync(payload, TestTasks.Day, []);

        Assert.Equal(TaskEditorCloseOutcome.TemplateEditRequested, result.Outcome);
        Assert.Same(inputBefore, _coordinator.Input);
    }

    [Fact]
    public async Task HandleCloseAsync_EditTemplateWithoutId_IsIgnored()
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var task = TestTasks.Create();
        var payload = CreateActionPayload(TaskEditPopUpCloseAction.EditTemplate, task);

        var result = await _coordinator.HandleCloseAsync(payload, TestTasks.Day, []);

        Assert.Equal(TaskEditorCloseOutcome.Ignored, result.Outcome);
    }

    [Fact]
    public async Task HandleCloseAsync_UnknownAction_IsIgnoredAndKeepsEditor()
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var inputBefore = _coordinator.Input;
        var task = TestTasks.Create();
        var unknownAction = (TaskEditPopUpCloseAction)99;
        var payload = CreateActionPayload(unknownAction, task);

        var result = await _coordinator.HandleCloseAsync(payload, TestTasks.Day, []);

        Assert.Equal(TaskEditorCloseOutcome.Ignored, result.Outcome);
        Assert.Same(inputBefore, _coordinator.Input);
    }

    [Fact]
    public async Task HandleCloseAsync_CopyDoneTemplate_IsIgnored()
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var finished = TestTasks.Create("Erledigt", isTemplate: true, isDone: true);
        var payload = CreateActionPayload(TaskEditPopUpCloseAction.CopyTemplate, finished, finished.Id);

        var result = await _coordinator.HandleCloseAsync(payload, TestTasks.Day, [finished]);

        Assert.Equal(TaskEditorCloseOutcome.Ignored, result.Outcome);
    }

    private static TaskDto CreateTemplateEdit(TaskDto existing)
    {
        var edited = TaskExtension.Clone(existing);
        edited.IsTemplate = true;
        edited.TaskDate = null;
        edited.RepeatUnit = TaskRepeatUnit.None;
        edited.RepeatInterval = 1;
        return edited;
    }

    private static TaskEditPopUpClosePayload CreatePayload(TaskDto task, bool isUpdateMode, bool hasChanges)
    {
        return new TaskEditPopUpClosePayload
        {
            Action = TaskEditPopUpCloseAction.Save,
            Task = task,
            IsUpdateMode = isUpdateMode,
            HasChanges = hasChanges
        };
    }

    private static TaskEditPopUpClosePayload CreateActionPayload(
        TaskEditPopUpCloseAction action,
        TaskDto task,
        Guid? templateTaskId = null)
    {
        return new TaskEditPopUpClosePayload
        {
            Action = action,
            Task = task,
            TemplateTaskId = templateTaskId
        };
    }
}
