using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Core.Enums;
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
    private readonly TaskEditorCoordinator _coordinator;

    public TaskEditorCoordinatorTests()
    {
        _scopeService.IsWorkScope.Returns(true);
        _priorityFormatter.Format(Arg.Any<TaskPriority>()).Returns(call => call.Arg<TaskPriority>().ToString());
        _coordinator = new TaskEditorCoordinator(_scopeService, _taskCache, _textProvider, _priorityFormatter);
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
    public void Open_RegularTasksInSource_OffersOnlyOpenTemplatesWithText()
    {
        var template = TestTasks.Create("Sport", isRegular: true, priority: TaskPriority.Hoch);
        var doneTemplate = TestTasks.Create("Alt", isRegular: true, isDone: true);
        var normalTask = TestTasks.Create("Normal");
        IReadOnlyList<TaskDto> source = [normalTask, template, doneTemplate];

        _coordinator.Open(null, TestTasks.Day, source);

        var input = _coordinator.Input;
        Assert.NotNull(input);
        var option = Assert.Single(input.RegularTemplateOptions);
        Assert.Equal(template.Id, option.TaskId);
        Assert.Equal("RegularTemplateOption:Sport|Hoch", option.Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Open_TemplateWithoutNote_UsesUntitledText(string? note)
    {
        var template = TestTasks.Create(note!, isRegular: true);
        IReadOnlyList<TaskDto> source = [template];

        _coordinator.Open(null, TestTasks.Day, source);

        var input = _coordinator.Input;
        Assert.NotNull(input);
        var option = Assert.Single(input.RegularTemplateOptions);
        Assert.Equal("RegularTemplateOption:Untitled|Keine", option.Text);
    }

    [Fact]
    public void Open_TemplateNoteWithWhitespace_TrimsNote()
    {
        var template = TestTasks.Create("  Sport  ", isRegular: true);
        IReadOnlyList<TaskDto> source = [template];

        _coordinator.Open(null, TestTasks.Day, source);

        var input = _coordinator.Input;
        Assert.NotNull(input);
        var option = Assert.Single(input.RegularTemplateOptions);
        Assert.Equal("RegularTemplateOption:Sport|Keine", option.Text);
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
    public void TryOpenRegularTemplateCopy_ValidTemplate_OpensOneTimeCopyForDay()
    {
        var template = TestTasks.Create("Sport", isRegular: true, priority: TaskPriority.Hoch, timerMinutes: 30);
        IReadOnlyList<TaskDto> source = [template];
        var targetDay = new DateTime(2026, 3, 12, 9, 0, 0);

        var opened = _coordinator.TryOpenRegularTemplateCopy(template.Id, targetDay, source);

        var input = _coordinator.Input;
        Assert.True(opened);
        Assert.NotNull(input);
        var copy = input.Task;
        Assert.NotNull(copy);
        Assert.Null(copy.Id);
        Assert.Equal("Sport", copy.Note);
        Assert.Equal(TaskPriority.Hoch, copy.Priority);
        Assert.Equal(30, copy.TimerMinutes);
        Assert.False(copy.IsRegularTask);
        Assert.False(copy.IsDone);
        Assert.Equal(targetDay.Date, copy.TaskDate);
        Assert.Equal(default, copy.CreationDate);
    }

    [Fact]
    public void TryOpenRegularTemplateCopy_ValidTemplate_KeepsTemplateUnchanged()
    {
        var template = TestTasks.Create("Sport", isRegular: true);
        IReadOnlyList<TaskDto> source = [template];

        _coordinator.TryOpenRegularTemplateCopy(template.Id, TestTasks.Day, source);

        Assert.True(template.IsRegularTask);
        Assert.NotNull(template.Id);
        Assert.Null(template.TaskDate);
    }

    [Fact]
    public void TryOpenRegularTemplateCopy_NoTemplateId_ReturnsFalseAndStaysClosed()
    {
        var template = TestTasks.Create(isRegular: true);
        IReadOnlyList<TaskDto> source = [template];

        var opened = _coordinator.TryOpenRegularTemplateCopy(null, TestTasks.Day, source);

        Assert.False(opened);
        Assert.False(_coordinator.IsOpen);
    }

    [Fact]
    public void TryOpenRegularTemplateCopy_UnknownId_ReturnsFalse()
    {
        var template = TestTasks.Create(isRegular: true);
        IReadOnlyList<TaskDto> source = [template];
        var unknownId = Guid.NewGuid();

        var opened = _coordinator.TryOpenRegularTemplateCopy(unknownId, TestTasks.Day, source);

        Assert.False(opened);
        Assert.False(_coordinator.IsOpen);
    }

    [Fact]
    public void TryOpenRegularTemplateCopy_DoneTemplate_ReturnsFalse()
    {
        var doneTemplate = TestTasks.Create(isRegular: true, isDone: true);
        IReadOnlyList<TaskDto> source = [doneTemplate];

        var opened = _coordinator.TryOpenRegularTemplateCopy(doneTemplate.Id, TestTasks.Day, source);

        Assert.False(opened);
    }

    [Fact]
    public void TryOpenRegularTemplateCopy_NormalTask_ReturnsFalse()
    {
        var normalTask = TestTasks.Create();
        IReadOnlyList<TaskDto> source = [normalTask];

        var opened = _coordinator.TryOpenRegularTemplateCopy(normalTask.Id, TestTasks.Day, source);

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
    public async Task HandleCloseAsync_CopyRegularTemplate_OpensCopyForGivenDay()
    {
        var template = TestTasks.Create("Vorlage", isRegular: true);
        IReadOnlyList<TaskDto> source = [template];
        _coordinator.Open(null, TestTasks.Day, source);
        var payload = CreateActionPayload(TaskEditPopUpCloseAction.CopyRegularTemplate, template, template.Id);

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
        var payload = CreateActionPayload(TaskEditPopUpCloseAction.CopyRegularTemplate, task, unknownId);

        var result = await _coordinator.HandleCloseAsync(payload, TestTasks.Day, []);

        Assert.Equal(TaskEditorCloseOutcome.Ignored, result.Outcome);
        Assert.Same(inputBefore, _coordinator.Input);
    }

    [Fact]
    public async Task HandleCloseAsync_EditRegularTemplate_RequestsEditAndLeavesEditorToViewModel()
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var inputBefore = _coordinator.Input;
        var template = TestTasks.Create("Vorlage", isRegular: true);
        var payload = CreateActionPayload(TaskEditPopUpCloseAction.EditRegularTemplate, template, template.Id);

        var result = await _coordinator.HandleCloseAsync(payload, TestTasks.Day, []);

        Assert.Equal(TaskEditorCloseOutcome.TemplateEditRequested, result.Outcome);
        Assert.Same(inputBefore, _coordinator.Input);
    }

    [Fact]
    public async Task HandleCloseAsync_EditRegularTemplateWithoutId_IsIgnored()
    {
        _coordinator.Open(null, TestTasks.Day, []);
        var task = TestTasks.Create();
        var payload = CreateActionPayload(TaskEditPopUpCloseAction.EditRegularTemplate, task);

        var result = await _coordinator.HandleCloseAsync(payload, TestTasks.Day, []);

        Assert.Equal(TaskEditorCloseOutcome.Ignored, result.Outcome);
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
        Guid? regularTemplateTaskId = null)
    {
        return new TaskEditPopUpClosePayload
        {
            Action = action,
            Task = task,
            RegularTemplateTaskId = regularTemplateTaskId
        };
    }
}
