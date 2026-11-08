using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.Enums;
using FokusApp.Core.Extensions;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FocusAppTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FocusAppTests.Services;

public class TaskCacheServiceTests
{
    private static readonly DateTime Day = new(2026, 3, 10);
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 14, 30, 0, TimeSpan.Zero);

    private readonly InMemoryTaskRepository _repository = new();
    private readonly IScopeService _scopeService = Substitute.For<IScopeService>();
    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly TaskCacheService _service;
    private readonly List<string> _raisedEvents = [];

    public TaskCacheServiceTests()
    {
        _scopeService.IsWorkScope.Returns(true);
        _service = new TaskCacheService(_repository, _scopeService, _timeProvider);
        _service.TasksLoaded += (_, _) => _raisedEvents.Add("Loaded");
        _service.NormalTasksChanged += (_, _) => _raisedEvents.Add("Normal");
        _service.TemplatesChanged += (_, _) => _raisedEvents.Add("Template");
        _service.DoneTasksChanged += (_, _) => _raisedEvents.Add("Done");
    }

    [Fact]
    public async Task InitializeAsync_StoredTasks_LoadsAllCategoriesAndRaisesTasksLoadedOnce()
    {
        var normalTask = CreateEntity();
        var template = CreateEntity(isTemplate: true);
        var doneTask = CreateEntity(isDone: true);
        _repository.Seed(normalTask, template, doneTask);

        await _service.InitializeAsync();

        var normalTasks = _service.GetNormalTasks();
        var templates = _service.GetTemplates();
        var doneTasks = _service.GetDoneTasks();
        Assert.Single(normalTasks);
        Assert.Single(templates);
        Assert.Single(doneTasks);
        Assert.Equal(["Loaded"], _raisedEvents);
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_LoadsOnlyOnce()
    {
        await _service.InitializeAsync();
        await _service.InitializeAsync();

        Assert.Equal(1, _repository.GetNormalCallCount);
        Assert.Equal(["Loaded"], _raisedEvents);
    }

    [Fact]
    public async Task ReloadAsync_ChangedRepository_RefreshesCacheAndRaisesAllEvents()
    {
        await _service.InitializeAsync();
        _raisedEvents.Clear();
        var newTask = CreateEntity();
        _repository.Seed(newTask);

        await _service.ReloadAsync();

        var normalTasks = _service.GetNormalTasks();
        Assert.Single(normalTasks);
        Assert.Equal(["Loaded", "Normal", "Template", "Done"], _raisedEvents);
    }

    [Fact]
    public async Task GetNormalTasks_ScopeIsWork_ReturnsOnlyWorkTasks()
    {
        var workTask = CreateEntity(isWork: true);
        var privateTask = CreateEntity(isWork: false);
        _repository.Seed(workTask, privateTask);
        await _service.InitializeAsync();

        var result = _service.GetNormalTasks();

        var task = Assert.Single(result);
        Assert.Equal(workTask.Id, task.Id);
    }

    [Fact]
    public async Task GetNormalTasks_ScopeChangesToPrivate_ReturnsPrivateTasks()
    {
        var workTask = CreateEntity(isWork: true);
        var privateTask = CreateEntity(isWork: false);
        _repository.Seed(workTask, privateTask);
        await _service.InitializeAsync();
        _scopeService.IsWorkScope.Returns(false);

        var result = _service.GetNormalTasks();

        var task = Assert.Single(result);
        Assert.Equal(privateTask.Id, task.Id);
    }

    [Fact]
    public async Task GetNormalTasks_SelectedDay_ReturnsOnlyTasksOfThatDayIgnoringTime()
    {
        var nextDay = Day.AddDays(1);
        var eveningOfDay = Day.AddHours(18);
        var onDay = CreateEntity(taskDate: Day);
        var otherDay = CreateEntity(taskDate: nextDay);
        _repository.Seed(onDay, otherDay);
        await _service.InitializeAsync();

        var result = _service.GetNormalTasks(eveningOfDay);

        var task = Assert.Single(result);
        Assert.Equal(onDay.Id, task.Id);
    }

    [Fact]
    public async Task GetTemplates_SelectedDay_AlwaysIncludesUndatedTemplates()
    {
        var template = CreateEntity(isTemplate: true);
        _repository.Seed(template);
        await _service.InitializeAsync();

        var result = _service.GetTemplates(Day);

        var task = Assert.Single(result);
        Assert.Equal(template.Id, task.Id);
    }

    [Fact]
    public async Task GetDoneTasks_SelectedDay_DoesNotIncludeTasksOfOtherDays()
    {
        var previousDay = Day.AddDays(-1);
        var onDay = CreateEntity(isDone: true, taskDate: Day);
        var otherDay = CreateEntity(isDone: true, taskDate: previousDay);
        _repository.Seed(onDay, otherDay);
        await _service.InitializeAsync();

        var result = _service.GetDoneTasks(Day);

        var task = Assert.Single(result);
        Assert.Equal(onDay.Id, task.Id);
    }

    [Fact]
    public async Task GetNormalTasks_ModifyingReturnedTask_DoesNotChangeCache()
    {
        var entity = CreateEntity(note: "Original");
        _repository.Seed(entity);
        await _service.InitializeAsync();

        var firstResult = _service.GetNormalTasks();
        firstResult[0].Note = "Geändert";

        var secondResult = _service.GetNormalTasks();
        Assert.Equal("Original", secondResult[0].Note);
    }

    [Fact]
    public async Task FindTaskById_ExistingTask_ReturnsCopy()
    {
        var entity = CreateEntity(note: "Original");
        _repository.Seed(entity);
        await _service.InitializeAsync();

        var firstResult = _service.FindTaskById(entity.Id);
        firstResult!.Note = "Geändert";

        var secondResult = _service.FindTaskById(entity.Id);
        Assert.Equal("Original", secondResult!.Note);
    }

    [Fact]
    public async Task FindTaskById_UnknownId_ReturnsNull()
    {
        var unknownId = Guid.NewGuid();
        await _service.InitializeAsync();

        var result = _service.FindTaskById(unknownId);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddTaskAsync_NullTask_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddTaskAsync(null!));
    }

    [Fact]
    public async Task AddTaskAsync_DoneTask_Throws()
    {
        var task = new TaskDto { IsDone = true };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddTaskAsync(task));

        Assert.Empty(_repository.AddedTasks);
    }

    [Theory]
    [MemberData(nameof(MissingIds))]
    public async Task AddTaskAsync_TaskWithoutId_AssignsNewId(Guid? id)
    {
        var task = new TaskDto { Id = id, Note = "Neu", TaskDate = Day };

        await _service.AddTaskAsync(task);

        var addedTask = Assert.Single(_repository.AddedTasks);
        Assert.NotNull(task.Id);
        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal(task.Id, addedTask.Id);
    }

    [Fact]
    public async Task AddTaskAsync_NormalTask_AppearsInCacheAndRaisesOnlyNormalChanged()
    {
        var task = new TaskDto { Note = "Neu", TaskDate = Day };

        await _service.AddTaskAsync(task);

        var normalTasks = _service.GetNormalTasks();
        var cachedTask = Assert.Single(normalTasks);
        Assert.Equal(task.Id, cachedTask.Id);
        Assert.Equal(["Normal"], _raisedEvents);
    }

    [Fact]
    public async Task AddTaskAsync_Template_IsStoredWithoutTaskDateAndRaisesOnlyTemplateChanged()
    {
        var task = new TaskDto { Note = "Täglich", IsTemplate = true, TaskDate = Day };

        await _service.AddTaskAsync(task);

        var addedTask = Assert.Single(_repository.AddedTasks);
        var templates = _service.GetTemplates();
        Assert.Null(addedTask.TaskDate);
        Assert.Single(templates);
        Assert.Equal(["Template"], _raisedEvents);
    }

    [Fact]
    public async Task UpdateTaskAsync_NullTask_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateTaskAsync(null!));
    }

    [Theory]
    [MemberData(nameof(MissingIds))]
    public async Task UpdateTaskAsync_TaskWithoutId_Throws(Guid? id)
    {
        var task = new TaskDto { Id = id };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateTaskAsync(task));
    }

    [Fact]
    public async Task UpdateTaskAsync_UnknownTask_ThrowsKeyNotFound()
    {
        var unknownTask = new TaskDto { Id = Guid.NewGuid() };
        await _service.InitializeAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateTaskAsync(unknownTask));
    }

    [Fact]
    public async Task UpdateTaskAsync_ChangedNote_UpdatesCacheAndRaisesOnlyNormalChanged()
    {
        var entity = CreateEntity(note: "Alt", taskDate: Day);
        _repository.Seed(entity);
        await _service.InitializeAsync();
        _raisedEvents.Clear();
        var updated = _service.FindTaskById(entity.Id)!;
        updated.Note = "Neu";

        await _service.UpdateTaskAsync(updated);

        var cachedTask = _service.FindTaskById(entity.Id);
        Assert.Equal("Neu", cachedTask!.Note);
        Assert.Equal(["Normal"], _raisedEvents);
    }

    [Fact]
    public async Task UpdateTaskAsync_NormalBecomesTemplate_MovesCategoryAndRaisesBothEvents()
    {
        var entity = CreateEntity(taskDate: Day);
        _repository.Seed(entity);
        await _service.InitializeAsync();
        _raisedEvents.Clear();
        var updated = _service.FindTaskById(entity.Id)!;
        updated.IsTemplate = true;

        await _service.UpdateTaskAsync(updated);

        var normalTasks = _service.GetNormalTasks();
        var templates = _service.GetTemplates();
        var updatedEntity = Assert.Single(_repository.UpdatedTasks);
        Assert.Empty(normalTasks);
        Assert.Single(templates);
        Assert.Equal(["Normal", "Template"], _raisedEvents);
        Assert.Null(updatedEntity.TaskDate);
    }

    [Fact]
    public async Task DeleteTaskAsync_EmptyId_DoesNothing()
    {
        await _service.DeleteTaskAsync(Guid.Empty);

        Assert.Empty(_repository.DeletedIds);
        Assert.Empty(_raisedEvents);
    }

    [Fact]
    public async Task DeleteTaskAsync_UnknownId_DoesNotTouchRepositoryOrRaiseEvents()
    {
        var unknownId = Guid.NewGuid();
        await _service.InitializeAsync();
        _raisedEvents.Clear();

        await _service.DeleteTaskAsync(unknownId);

        Assert.Empty(_repository.DeletedIds);
        Assert.Empty(_raisedEvents);
    }

    [Fact]
    public async Task DeleteTaskAsync_ExistingDoneTask_RemovesItAndRaisesOnlyDoneChanged()
    {
        var entity = CreateEntity(isDone: true);
        _repository.Seed(entity);
        await _service.InitializeAsync();
        _raisedEvents.Clear();

        await _service.DeleteTaskAsync(entity.Id);

        var doneTasks = _service.GetDoneTasks();
        Assert.Empty(doneTasks);
        Assert.Equal([entity.Id], _repository.DeletedIds);
        Assert.Equal(["Done"], _raisedEvents);
    }

    [Fact]
    public async Task FinishTaskAsync_NullTask_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.FinishTaskAsync(null!));
    }

    [Fact]
    public async Task FinishTaskAsync_UnknownTask_ThrowsKeyNotFound()
    {
        var unknownTask = new TaskDto { Id = Guid.NewGuid() };
        await _service.InitializeAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.FinishTaskAsync(unknownTask));
    }

    [Fact]
    public async Task FinishTaskAsync_NormalTask_MovesToDoneAndRaisesNormalAndDoneChanged()
    {
        var entity = CreateEntity(taskDate: Day);
        var taskToFinish = new TaskDto { Id = entity.Id };
        _repository.Seed(entity);
        await _service.InitializeAsync();
        _raisedEvents.Clear();

        await _service.FinishTaskAsync(taskToFinish);

        var normalTasks = _service.GetNormalTasks();
        var doneTasks = _service.GetDoneTasks();
        var doneTask = Assert.Single(doneTasks);
        var updatedEntity = Assert.Single(_repository.UpdatedTasks);
        Assert.Empty(normalTasks);
        Assert.Equal(entity.Id, doneTask.Id);
        Assert.True(updatedEntity.IsDone);
        Assert.Equal(["Normal", "Done"], _raisedEvents);
    }

    [Fact]
    public async Task FinishTaskAsync_NormalTask_RaisesTaskCompletedOnceWithIdAndPositivity()
    {
        var entity = CreateEntity(taskDate: Day);
        entity.IsPositive = true;
        _repository.Seed(entity);
        await _service.InitializeAsync();
        var completed = new List<TaskCompletedEventArgs>();
        _service.TaskCompleted += (_, args) => completed.Add(args);

        await _service.FinishTaskAsync(new TaskDto { Id = entity.Id });

        var args = Assert.Single(completed);
        Assert.Equal(entity.Id, args.TaskId);
        Assert.True(args.IsPositive);
    }

    [Fact]
    public async Task FinishTaskAsync_Template_RaisesTaskCompletedWithIdOfDoneEntry()
    {
        var template = CreateEntity(isTemplate: true);
        _repository.Seed(template);
        await _service.InitializeAsync();
        var completed = new List<TaskCompletedEventArgs>();
        _service.TaskCompleted += (_, args) => completed.Add(args);

        await _service.FinishTaskAsync(new TaskDto { Id = template.Id });

        var args = Assert.Single(completed);
        var historyEntry = Assert.Single(_repository.AddedTasks);
        Assert.Equal(historyEntry.Id, args.TaskId);
        Assert.NotEqual(template.Id, args.TaskId);
    }

    [Fact]
    public async Task TaskCompleted_AfterDoneListsChanged_IsRaisedAfterListChangeEvents()
    {
        var entity = CreateEntity(taskDate: Day);
        _repository.Seed(entity);
        await _service.InitializeAsync();
        _raisedEvents.Clear();
        _service.TaskCompleted += (_, _) => _raisedEvents.Add("Completed");

        await _service.FinishTaskAsync(new TaskDto { Id = entity.Id });

        Assert.Equal(["Normal", "Done", "Completed"], _raisedEvents);
    }

    [Fact]
    public async Task TaskCompleted_AddUpdateDeleteAndReload_AreNotRaised()
    {
        var entity = CreateEntity(taskDate: Day);
        _repository.Seed(entity);
        await _service.InitializeAsync();
        var completedCount = 0;
        _service.TaskCompleted += (_, _) => completedCount++;

        await _service.AddTaskAsync(new TaskDto { Note = "Neu", TaskDate = Day, IsWorkTask = true });
        await _service.UpdateTaskAsync(new TaskDto { Id = entity.Id, Note = "Geändert", TaskDate = Day, IsWorkTask = true });
        await _service.ReloadAsync();
        await _service.DeleteTaskAsync(entity.Id);

        Assert.Equal(0, completedCount);
    }

    [Fact]
    public async Task FinishTaskAsync_NormalTask_SetsCompletedAtToLocalNow()
    {
        var entity = CreateEntity(taskDate: Day);
        _repository.Seed(entity);
        await _service.InitializeAsync();

        await _service.FinishTaskAsync(new TaskDto { Id = entity.Id });

        var doneTask = Assert.Single(_service.GetDoneTasks());
        var stored = Assert.Single(_repository.UpdatedTasks);
        Assert.Equal(Now.DateTime, doneTask.CompletedAt);
        Assert.Equal(Now.DateTime, stored.CompletedAt);
    }

    [Fact]
    public async Task FinishTaskAsync_TimeAdvancesAcrossMidnight_UsesTimeOfFinishing()
    {
        var entity = CreateEntity(taskDate: Day);
        _repository.Seed(entity);
        await _service.InitializeAsync();
        _timeProvider.Advance(TimeSpan.FromHours(10));

        await _service.FinishTaskAsync(new TaskDto { Id = entity.Id });

        var doneTask = Assert.Single(_service.GetDoneTasks());
        Assert.Equal(Now.DateTime.AddHours(10), doneTask.CompletedAt);
        Assert.Equal(Day.AddDays(1), TaskExtension.GetEffectiveCompletedDate(doneTask));
    }

    [Fact]
    public async Task FinishTaskAsync_Template_SetsCompletedAtOnHistoryEntryButNotOnTemplate()
    {
        var template = CreateEntity(isTemplate: true);
        _repository.Seed(template);
        await _service.InitializeAsync();

        await _service.FinishTaskAsync(new TaskDto { Id = template.Id });

        var historyEntry = Assert.Single(_repository.AddedTasks);
        var keptTemplate = Assert.Single(_service.GetTemplates());
        Assert.Equal(Now.DateTime, historyEntry.CompletedAt);
        Assert.Null(keptTemplate.CompletedAt);
    }

    [Fact]
    public async Task UpdateTaskAsync_DoneTaskWithoutCompletedAtInUpdate_KeepsStoredCompletedAt()
    {
        var completedAt = new DateTime(2026, 3, 9, 18, 0, 0);
        var entity = CreateEntity(isDone: true, taskDate: Day);
        entity.CompletedAt = completedAt;
        _repository.Seed(entity);
        await _service.InitializeAsync();
        var editedCopy = new TaskDto { Id = entity.Id, IsDone = true, Note = "Geändert", TaskDate = Day, IsWorkTask = true };

        await _service.UpdateTaskAsync(editedCopy);

        var doneTask = Assert.Single(_service.GetDoneTasks());
        Assert.Equal("Geändert", doneTask.Note);
        Assert.Equal(completedAt, doneTask.CompletedAt);
    }

    [Fact]
    public async Task UpdateTaskAsync_DoneTaskSetBackToOpen_ClearsCompletedAt()
    {
        var entity = CreateEntity(isDone: true, taskDate: Day);
        entity.CompletedAt = new DateTime(2026, 3, 9, 18, 0, 0);
        _repository.Seed(entity);
        await _service.InitializeAsync();
        var reopened = new TaskDto
        {
            Id = entity.Id,
            IsDone = false,
            Note = "Wieder offen",
            TaskDate = Day,
            IsWorkTask = true,
            CompletedAt = entity.CompletedAt
        };

        await _service.UpdateTaskAsync(reopened);

        var openTask = Assert.Single(_service.GetNormalTasks());
        Assert.Null(openTask.CompletedAt);
        Assert.Null(Assert.Single(_repository.UpdatedTasks).CompletedAt);
    }

    [Fact]
    public async Task InitializeAsync_LegacyDoneTaskWithoutCompletedAt_CountsAsDoneOnTaskDate()
    {
        var legacy = CreateEntity(isDone: true, taskDate: Day);
        _repository.Seed(legacy);

        await _service.InitializeAsync();

        var doneTask = Assert.Single(_service.GetDoneTasks());
        Assert.Null(doneTask.CompletedAt);
        Assert.Equal(Day, TaskExtension.GetEffectiveCompletedDate(doneTask));
    }

    [Fact]
    public async Task FinishTaskAsync_Template_KeepsTemplateAndCreatesDoneEntryAtCurrentTime()
    {
        var template = CreateEntity(isTemplate: true, note: "Täglich", priority: TaskPriority.Hoch);
        var taskToFinish = new TaskDto { Id = template.Id };
        _repository.Seed(template);
        await _service.InitializeAsync();
        _raisedEvents.Clear();

        await _service.FinishTaskAsync(taskToFinish);

        var templates = _service.GetTemplates();
        var keptTemplate = Assert.Single(templates);
        var historyEntry = Assert.Single(_repository.AddedTasks);
        Assert.Equal(template.Id, keptTemplate.Id);
        Assert.NotEqual(template.Id, historyEntry.Id);
        Assert.True(historyEntry.IsDone);
        Assert.False(historyEntry.IsTemplate);
        Assert.Equal("Täglich", historyEntry.Note);
        Assert.Equal(TaskPriority.Hoch, historyEntry.Priority);
        Assert.Equal(Now.DateTime, historyEntry.CreationDate);
        Assert.Equal(Day, historyEntry.TaskDate);
        Assert.Equal(["Done"], _raisedEvents);
    }

    [Fact]
    public async Task FinishTaskAsync_TemplateAfterTimeAdvances_UsesNewCurrentDate()
    {
        var template = CreateEntity(isTemplate: true);
        var taskToFinish = new TaskDto { Id = template.Id };
        var twoDays = TimeSpan.FromDays(2);
        _repository.Seed(template);
        await _service.InitializeAsync();
        _timeProvider.Advance(twoDays);

        await _service.FinishTaskAsync(taskToFinish);

        var historyEntry = Assert.Single(_repository.AddedTasks);
        var expectedCreationDate = Now.DateTime.AddDays(2);
        var expectedTaskDate = Day.AddDays(2);
        Assert.Equal(expectedTaskDate, historyEntry.TaskDate);
        Assert.Equal(expectedCreationDate, historyEntry.CreationDate);
    }

    [Fact]
    public async Task InitializeAsync_ConcurrentCalls_LoadsOnlyOnce()
    {
        var repository = Substitute.For<FokusApp.Core.DataBase.Repositories.Interfaces.ITaskRepository>();
        var gate = new TaskCompletionSource<List<TaskEntity>>();
        repository.GetNormalTasksAsync().Returns(gate.Task);
        repository.GetTemplatesAsync().Returns([]);
        repository.GetDoneTasksAsync().Returns([]);
        var service = new TaskCacheService(repository, _scopeService, _timeProvider);

        var first = service.InitializeAsync();
        var second = service.InitializeAsync();
        gate.SetResult([]);
        await Task.WhenAll(first, second);

        await repository.Received(1).GetNormalTasksAsync();
    }

    [Fact]
    public async Task FindTaskById_TemplateAndDoneTask_AreFound()
    {
        var template = CreateEntity(note: "Regelmäßig", isTemplate: true);
        var done = CreateEntity(note: "Fertig", isDone: true);
        _repository.Seed(template, done);
        await _service.InitializeAsync();

        var foundTemplate = _service.FindTaskById(template.Id);
        var foundDone = _service.FindTaskById(done.Id);

        Assert.Equal("Regelmäßig", foundTemplate!.Note);
        Assert.Equal("Fertig", foundDone!.Note);
    }

    [Fact]
    public async Task FindTaskById_EmptyId_ReturnsNull()
    {
        await _service.InitializeAsync();

        var result = _service.FindTaskById(Guid.Empty);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddTaskAsync_TaskWithOwnId_KeepsThatId()
    {
        await _service.InitializeAsync();
        var ownId = Guid.NewGuid();
        var task = new TaskDto { Id = ownId, Note = "Eigene Id", IsWorkTask = true, TaskDate = Day, CreationDate = Day };

        await _service.AddTaskAsync(task);

        var stored = Assert.Single(_repository.AddedTasks);
        Assert.Equal(ownId, stored.Id);
    }

    [Fact]
    public async Task AddTaskAsync_TaskWithoutId_GetsNewId()
    {
        await _service.InitializeAsync();
        var task = new TaskDto { Id = null, Note = "Ohne Id", IsWorkTask = true, TaskDate = Day, CreationDate = Day };

        await _service.AddTaskAsync(task);

        var stored = Assert.Single(_repository.AddedTasks);
        Assert.NotEqual(Guid.Empty, stored.Id);
        Assert.Equal(stored.Id, task.Id);
    }

    [Fact]
    public async Task GetTemplates_DoneTemplateOnOtherDay_IsNotATemplateAndFilteredByDay()
    {
        var doneTemplate = CreateEntity(note: "Erledigt", isDone: true, isTemplate: true);
        doneTemplate.TaskDate = Day.AddDays(3);
        _repository.Seed(doneTemplate);
        await _service.InitializeAsync();

        var tasks = _service.GetTemplates(Day);

        Assert.Empty(tasks);
    }

    public static TheoryData<Guid?> MissingIds => new() { null, Guid.Empty };

    private static TaskEntity CreateEntity(
        string note = "Aufgabe",
        bool isDone = false,
        bool isTemplate = false,
        bool isWork = true,
        DateTime? taskDate = null,
        TaskPriority priority = TaskPriority.Keine)
    {
        return new TaskEntity
        {
            Id = Guid.NewGuid(),
            Note = note,
            IsDone = isDone,
            IsTemplate = isTemplate,
            IsWorkTask = isWork,
            TaskDate = isTemplate ? null : taskDate,
            Priority = priority,
            CreationDate = Day
        };
    }
}
