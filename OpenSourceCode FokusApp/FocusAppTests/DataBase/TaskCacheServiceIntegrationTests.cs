using FokusApp.Core.DataBase.Repositories.Implementations;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FocusAppTests.DataBase;

public class TaskCacheServiceIntegrationTests : IDisposable
{
    private static readonly DateTime Day = new(2026, 3, 10);
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 14, 30, 0, TimeSpan.Zero);

    private readonly SqliteTestDatabase _database = new();
    private readonly TaskRepository _repository;
    private readonly IScopeService _scopeService = Substitute.For<IScopeService>();
    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly TaskCacheService _service;

    public TaskCacheServiceIntegrationTests()
    {
        _database.MigrateApp();
        _repository = new TaskRepository(_database.AppFactory);
        _scopeService.IsWorkScope.Returns(true);
        _service = new TaskCacheService(_repository, _scopeService, _timeProvider);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task InitializeAsync_StoredTasks_LoadsCategoriesFromDatabase()
    {
        var normal = TestEntities.CreateTask();
        var template = TestEntities.CreateTask(isTemplate: true);
        var done = TestEntities.CreateTask(isDone: true);
        await _repository.AddRangeAsync([normal, template, done]);

        await _service.InitializeAsync();

        var normalTasks = _service.GetNormalTasks();
        var templates = _service.GetTemplates();
        var doneTasks = _service.GetDoneTasks();
        var normalTask = Assert.Single(normalTasks);
        var storedTemplate = Assert.Single(templates);
        var doneTask = Assert.Single(doneTasks);
        Assert.Equal(normal.Id, normalTask.Id);
        Assert.Equal(template.Id, template.Id);
        Assert.Equal(done.Id, doneTask.Id);
    }

    [Fact]
    public async Task AddTaskAsync_NormalTask_PersistsAndAppearsInCache()
    {
        await _service.InitializeAsync();
        var task = CreateDto(note: "Neu");

        await _service.AddTaskAsync(task);

        var stored = await _repository.GetNormalTasksAsync();
        var storedTask = Assert.Single(stored);
        var cached = _service.GetNormalTasks();
        var cachedTask = Assert.Single(cached);
        Assert.Equal("Neu", storedTask.Note);
        Assert.Equal(storedTask.Id, cachedTask.Id);
        Assert.Equal(task.Id, cachedTask.Id);
    }

    [Fact]
    public async Task AddTaskAsync_TemplateWithDate_NormalizesDateAndAvoidsConstraintViolation()
    {
        await _service.InitializeAsync();
        var task = CreateDto(isTemplate: true, taskDate: Day);

        await _service.AddTaskAsync(task);

        var stored = await _repository.GetTemplatesAsync();
        var storedTask = Assert.Single(stored);
        Assert.Null(storedTask.TaskDate);
    }

    [Fact]
    public async Task UpdateTaskAsync_ChangeFromNormalToTemplate_MovesTaskBetweenCategoriesInDatabase()
    {
        var entity = TestEntities.CreateTask();
        await _repository.AddAsync(entity);
        await _service.InitializeAsync();
        var updated = CreateDto(id: entity.Id, isTemplate: true, note: "Jetzt regelmäßig");

        await _service.UpdateTaskAsync(updated);

        var normal = await _repository.GetNormalTasksAsync();
        var templates = await _repository.GetTemplatesAsync();
        var storedTemplate = Assert.Single(templates);
        var cachedTemplate = _service.GetTemplates();
        Assert.Empty(normal);
        Assert.Equal("Jetzt regelmäßig", storedTemplate.Note);
        Assert.Null(storedTemplate.TaskDate);
        Assert.Single(cachedTemplate);
        Assert.Empty(_service.GetNormalTasks());
    }

    [Fact]
    public async Task DeleteTaskAsync_ExistingTask_RemovesFromDatabaseAndCache()
    {
        var keep = TestEntities.CreateTask();
        var remove = TestEntities.CreateTask();
        await _repository.AddRangeAsync([keep, remove]);
        await _service.InitializeAsync();

        await _service.DeleteTaskAsync(remove.Id);

        var stored = await _repository.GetAllAsync();
        var cached = _service.GetNormalTasks();
        var storedTask = Assert.Single(stored);
        var cachedTask = Assert.Single(cached);
        Assert.Equal(keep.Id, storedTask.Id);
        Assert.Equal(keep.Id, cachedTask.Id);
    }

    [Fact]
    public async Task FinishTaskAsync_NormalTask_PersistsDoneFlag()
    {
        var entity = TestEntities.CreateTask();
        await _repository.AddAsync(entity);
        await _service.InitializeAsync();
        var taskToFinish = new TaskDto { Id = entity.Id };

        await _service.FinishTaskAsync(taskToFinish);

        var done = await _repository.GetDoneTasksAsync();
        var normal = await _repository.GetNormalTasksAsync();
        var doneTask = Assert.Single(done);
        Assert.Equal(entity.Id, doneTask.Id);
        Assert.Empty(normal);
        Assert.Single(_service.GetDoneTasks());
    }

    [Fact]
    public async Task FinishTaskAsync_Template_KeepsTemplateAndStoresHistoryEntryWithCurrentDate()
    {
        var template = TestEntities.CreateTask(isTemplate: true);
        await _repository.AddAsync(template);
        await _service.InitializeAsync();
        var taskToFinish = new TaskDto { Id = template.Id };

        await _service.FinishTaskAsync(taskToFinish);

        var templates = await _repository.GetTemplatesAsync();
        var done = await _repository.GetDoneTasksAsync();
        var keptTemplate = Assert.Single(templates);
        var historyEntry = Assert.Single(done);
        Assert.Equal(template.Id, keptTemplate.Id);
        Assert.NotEqual(template.Id, historyEntry.Id);
        Assert.False(historyEntry.IsTemplate);
        Assert.Equal(Now.DateTime, historyEntry.CreationDate);
        Assert.Equal(Day, historyEntry.TaskDate);
    }

    [Fact]
    public async Task ReloadAsync_TaskAddedBehindTheCache_IsLoadedFromDatabase()
    {
        await _service.InitializeAsync();
        var external = TestEntities.CreateTask();
        await _repository.AddAsync(external);
        var beforeReload = _service.GetNormalTasks();

        await _service.ReloadAsync();

        var afterReload = _service.GetNormalTasks();
        Assert.Empty(beforeReload);
        Assert.Single(afterReload);
    }

    [Fact]
    public async Task InitializeAsync_NewServiceOnSameDatabase_SeesTasksOfPreviousService()
    {
        await _service.InitializeAsync();
        var task = CreateDto(note: "Bleibt");
        await _service.AddTaskAsync(task);
        var restartedService = new TaskCacheService(_repository, _scopeService, _timeProvider);

        await restartedService.InitializeAsync();

        var restored = restartedService.GetNormalTasks();
        var restoredTask = Assert.Single(restored);
        Assert.Equal("Bleibt", restoredTask.Note);
    }

    [Fact]
    public async Task AddTaskAsync_ManyConcurrentCalls_PersistsAllTasks()
    {
        await _service.InitializeAsync();
        var tasks = Enumerable.Range(0, 20).Select(index => CreateDto(note: $"Aufgabe {index}")).ToList();
        var operations = tasks.Select(task => _service.AddTaskAsync(task)).ToList();

        await Task.WhenAll(operations);

        var stored = await _repository.GetAllAsync();
        var cached = _service.GetNormalTasks();
        Assert.Equal(20, stored.Count);
        Assert.Equal(20, cached.Count);
    }

    private static TaskDto CreateDto(
        Guid? id = null,
        string note = "Aufgabe",
        bool isTemplate = false,
        DateTime? taskDate = null)
    {
        return new TaskDto
        {
            Id = id,
            Note = note,
            IsTemplate = isTemplate,
            IsWorkTask = true,
            CreationDate = Day,
            TaskDate = taskDate ?? Day,
            Priority = TaskPriority.Keine,
            BreakMinutes = 5,
            TimerMinutes = 25
        };
    }
}
