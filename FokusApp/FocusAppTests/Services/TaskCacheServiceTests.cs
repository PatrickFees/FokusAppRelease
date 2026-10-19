using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.Enums;
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
        _service.RegularTasksChanged += (_, _) => _raisedEvents.Add("Regular");
        _service.DoneTasksChanged += (_, _) => _raisedEvents.Add("Done");
    }

    [Fact]
    public async Task InitializeAsync_StoredTasks_LoadsAllCategoriesAndRaisesTasksLoadedOnce()
    {
        var normalTask = CreateEntity();
        var regularTask = CreateEntity(isRegular: true);
        var doneTask = CreateEntity(isDone: true);
        _repository.Seed(normalTask, regularTask, doneTask);

        await _service.InitializeAsync();

        var normalTasks = _service.GetNormalTasks();
        var regularTasks = _service.GetRegularTasks();
        var doneTasks = _service.GetDoneTasks();
        Assert.Single(normalTasks);
        Assert.Single(regularTasks);
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
        Assert.Equal(["Loaded", "Normal", "Regular", "Done"], _raisedEvents);
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
    public async Task GetRegularTasks_SelectedDay_AlwaysIncludesUndatedTemplates()
    {
        var template = CreateEntity(isRegular: true);
        _repository.Seed(template);
        await _service.InitializeAsync();

        var result = _service.GetRegularTasks(Day);

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
    public async Task AddTaskAsync_RegularTask_IsStoredWithoutTaskDateAndRaisesOnlyRegularChanged()
    {
        var task = new TaskDto { Note = "Täglich", IsRegularTask = true, TaskDate = Day };

        await _service.AddTaskAsync(task);

        var addedTask = Assert.Single(_repository.AddedTasks);
        var regularTasks = _service.GetRegularTasks();
        Assert.Null(addedTask.TaskDate);
        Assert.Single(regularTasks);
        Assert.Equal(["Regular"], _raisedEvents);
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
    public async Task UpdateTaskAsync_NormalBecomesRegular_MovesCategoryAndRaisesBothEvents()
    {
        var entity = CreateEntity(taskDate: Day);
        _repository.Seed(entity);
        await _service.InitializeAsync();
        _raisedEvents.Clear();
        var updated = _service.FindTaskById(entity.Id)!;
        updated.IsRegularTask = true;

        await _service.UpdateTaskAsync(updated);

        var normalTasks = _service.GetNormalTasks();
        var regularTasks = _service.GetRegularTasks();
        var updatedEntity = Assert.Single(_repository.UpdatedTasks);
        Assert.Empty(normalTasks);
        Assert.Single(regularTasks);
        Assert.Equal(["Normal", "Regular"], _raisedEvents);
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
    public async Task FinishTaskAsync_RegularTask_KeepsTemplateAndCreatesDoneEntryAtCurrentTime()
    {
        var template = CreateEntity(isRegular: true, note: "Täglich", priority: TaskPriority.Hoch);
        var taskToFinish = new TaskDto { Id = template.Id };
        _repository.Seed(template);
        await _service.InitializeAsync();
        _raisedEvents.Clear();

        await _service.FinishTaskAsync(taskToFinish);

        var regularTasks = _service.GetRegularTasks();
        var keptTemplate = Assert.Single(regularTasks);
        var historyEntry = Assert.Single(_repository.AddedTasks);
        Assert.Equal(template.Id, keptTemplate.Id);
        Assert.NotEqual(template.Id, historyEntry.Id);
        Assert.True(historyEntry.IsDone);
        Assert.False(historyEntry.IsRegularTask);
        Assert.Equal("Täglich", historyEntry.Note);
        Assert.Equal(TaskPriority.Hoch, historyEntry.Priority);
        Assert.Equal(Now.DateTime, historyEntry.CreationDate);
        Assert.Equal(Day, historyEntry.TaskDate);
        Assert.Equal(["Done"], _raisedEvents);
    }

    [Fact]
    public async Task FinishTaskAsync_RegularTaskAfterTimeAdvances_UsesNewCurrentDate()
    {
        var template = CreateEntity(isRegular: true);
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

    public static TheoryData<Guid?> MissingIds => new() { null, Guid.Empty };

    private static TaskEntity CreateEntity(
        string note = "Aufgabe",
        bool isDone = false,
        bool isRegular = false,
        bool isWork = true,
        DateTime? taskDate = null,
        TaskPriority priority = TaskPriority.Keine)
    {
        return new TaskEntity
        {
            Id = Guid.NewGuid(),
            Note = note,
            IsDone = isDone,
            IsRegularTask = isRegular,
            IsWorkTask = isWork,
            TaskDate = isRegular ? null : taskDate,
            Priority = priority,
            CreationDate = Day
        };
    }
}
