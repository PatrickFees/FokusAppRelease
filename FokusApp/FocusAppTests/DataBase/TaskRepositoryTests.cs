using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Implementations;
using FokusApp.Core.Enums;
using FocusAppTests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace FocusAppTests.DataBase;

public class TaskRepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();
    private readonly TaskRepository _repository;

    public TaskRepositoryTests()
    {
        _database.MigrateApp();
        _repository = new TaskRepository(_database.AppFactory);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var result = await _repository.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task AddAsync_ValidTask_PersistsAllFields()
    {
        var task = TestEntities.CreateTask();

        await _repository.AddAsync(task);

        var stored = await ReadSingleAsync(task.Id);
        Assert.False(stored.IsDone);
        Assert.Equal("Notiz", stored.Note);
        Assert.False(stored.IsRegularTask);
        Assert.True(stored.IsWorkTask);
        Assert.True(stored.IsPositive);
        Assert.Equal(task.CreationDate, stored.CreationDate);
        Assert.Equal(task.TaskDate, stored.TaskDate);
        Assert.Equal(TaskPriority.Dringend, stored.Priority);
        Assert.Equal(7, stored.BreakMinutes);
        Assert.Equal(25, stored.TimerMinutes);
    }

    [Fact]
    public async Task AddAsync_PrivateTaskWithZeroMinutes_KeepsFalseAndZeroInsteadOfDefaults()
    {
        var task = TestEntities.CreateTask(isWork: false);
        task.IsPositive = false;
        task.BreakMinutes = 0;
        task.TimerMinutes = 0;

        await _repository.AddAsync(task);

        var stored = await ReadSingleAsync(task.Id);
        Assert.False(stored.IsWorkTask);
        Assert.False(stored.IsPositive);
        Assert.Equal(0, stored.BreakMinutes);
        Assert.Equal(0, stored.TimerMinutes);
    }

    [Fact]
    public async Task AddAsync_RegularTaskWithDate_ViolatesCheckConstraint()
    {
        var task = TestEntities.CreateTask(isRegular: true);
        task.TaskDate = TestEntities.BaseDate.Date;

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => _repository.AddAsync(task));

        Assert.Contains("CK_Tasks_RegularTaskHasNoDate", exception.InnerException?.Message);
    }

    [Fact]
    public async Task AddRangeAsync_MultipleTasks_PersistsAll()
    {
        var first = TestEntities.CreateTask();
        var second = TestEntities.CreateTask(isDone: true);
        var tasks = new[] { first, second };

        await _repository.AddRangeAsync(tasks);

        var stored = await _repository.GetAllAsync();
        Assert.Equal(2, stored.Count);
    }

    [Fact]
    public async Task GetNormalTasksAsync_MixedTasks_ReturnsOnlyOpenNonRegularTasks()
    {
        var normal = TestEntities.CreateTask();
        var regular = TestEntities.CreateTask(isRegular: true);
        var done = TestEntities.CreateTask(isDone: true);
        var doneRegular = TestEntities.CreateTask(isDone: true, isRegular: true);
        await _repository.AddRangeAsync([normal, regular, done, doneRegular]);

        var result = await _repository.GetNormalTasksAsync();

        var task = Assert.Single(result);
        Assert.Equal(normal.Id, task.Id);
    }

    [Fact]
    public async Task GetRegularTasksAsync_MixedTasks_ReturnsOnlyOpenRegularTasks()
    {
        var normal = TestEntities.CreateTask();
        var regular = TestEntities.CreateTask(isRegular: true);
        var doneRegular = TestEntities.CreateTask(isDone: true, isRegular: true);
        await _repository.AddRangeAsync([normal, regular, doneRegular]);

        var result = await _repository.GetRegularTasksAsync();

        var task = Assert.Single(result);
        Assert.Equal(regular.Id, task.Id);
    }

    [Fact]
    public async Task GetDoneTasksAsync_MixedTasks_ReturnsDoneNormalAndRegularTasks()
    {
        var normal = TestEntities.CreateTask();
        var done = TestEntities.CreateTask(isDone: true);
        var doneRegular = TestEntities.CreateTask(isDone: true, isRegular: true);
        await _repository.AddRangeAsync([normal, done, doneRegular]);

        var result = await _repository.GetDoneTasksAsync();

        var ids = result.Select(task => task.Id).OrderBy(id => id).ToList();
        var expectedIds = new[] { done.Id, doneRegular.Id }.OrderBy(id => id).ToList();
        Assert.Equal(expectedIds, ids);
    }

    [Fact]
    public async Task GetAllAsync_TasksWithDifferentCreationDates_ReturnsNewestFirst()
    {
        var oldest = TestEntities.CreateTask(creationOffsetMinutes: 0);
        var newest = TestEntities.CreateTask(creationOffsetMinutes: 20);
        var middle = TestEntities.CreateTask(creationOffsetMinutes: 10);
        await _repository.AddRangeAsync([oldest, newest, middle]);

        var result = await _repository.GetAllAsync();

        var ids = result.Select(task => task.Id).ToList();
        Assert.Equal([newest.Id, middle.Id, oldest.Id], ids);
    }

    [Fact]
    public async Task GetTasks_SameDataAsInMemoryFake_ReturnsSameCategoriesInSameOrder()
    {
        var tasks = CreateMixedTasks();
        var fake = new InMemoryTaskRepository();
        fake.Seed(tasks);
        await _repository.AddRangeAsync(tasks);

        var realNormal = await _repository.GetNormalTasksAsync();
        var realRegular = await _repository.GetRegularTasksAsync();
        var realDone = await _repository.GetDoneTasksAsync();
        var fakeNormal = await fake.GetNormalTasksAsync();
        var fakeRegular = await fake.GetRegularTasksAsync();
        var fakeDone = await fake.GetDoneTasksAsync();

        Assert.Equal(Ids(fakeNormal), Ids(realNormal));
        Assert.Equal(Ids(fakeRegular), Ids(realRegular));
        Assert.Equal(Ids(fakeDone), Ids(realDone));
    }

    [Fact]
    public async Task UpdateAsync_ExistingTask_UpdatesAllFields()
    {
        var task = TestEntities.CreateTask();
        await _repository.AddAsync(task);
        var changed = TestEntities.CreateTask(isDone: true, isRegular: true, isWork: false, creationOffsetMinutes: 5);
        changed.Id = task.Id;
        changed.Note = "Geändert";
        changed.Priority = TaskPriority.Wenig;
        changed.BreakMinutes = 3;
        changed.TimerMinutes = 50;

        await _repository.UpdateAsync(changed);

        var stored = await ReadSingleAsync(task.Id);
        Assert.True(stored.IsDone);
        Assert.Equal("Geändert", stored.Note);
        Assert.True(stored.IsRegularTask);
        Assert.False(stored.IsWorkTask);
        Assert.Null(stored.TaskDate);
        Assert.Equal(changed.CreationDate, stored.CreationDate);
        Assert.Equal(TaskPriority.Wenig, stored.Priority);
        Assert.Equal(3, stored.BreakMinutes);
        Assert.Equal(50, stored.TimerMinutes);
    }

    [Fact]
    public async Task UpdateAsync_UnknownId_DoesNothing()
    {
        var existing = TestEntities.CreateTask();
        await _repository.AddAsync(existing);
        var unknown = TestEntities.CreateTask(isDone: true);

        await _repository.UpdateAsync(unknown);

        var stored = await _repository.GetAllAsync();
        var task = Assert.Single(stored);
        Assert.Equal(existing.Id, task.Id);
        Assert.False(task.IsDone);
    }

    [Fact]
    public async Task DeleteAsync_ExistingTask_RemovesOnlyThatTask()
    {
        var keep = TestEntities.CreateTask();
        var remove = TestEntities.CreateTask();
        await _repository.AddRangeAsync([keep, remove]);

        await _repository.DeleteAsync(remove.Id);

        var stored = await _repository.GetAllAsync();
        var task = Assert.Single(stored);
        Assert.Equal(keep.Id, task.Id);
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_DoesNothing()
    {
        var existing = TestEntities.CreateTask();
        await _repository.AddAsync(existing);
        var unknownId = Guid.NewGuid();

        await _repository.DeleteAsync(unknownId);

        var stored = await _repository.GetAllAsync();
        Assert.Single(stored);
    }

    [Fact]
    public async Task DeleteAllAsync_ExistingTasks_RemovesAll()
    {
        await _repository.AddRangeAsync([TestEntities.CreateTask(), TestEntities.CreateTask(isDone: true)]);

        await _repository.DeleteAllAsync();

        var stored = await _repository.GetAllAsync();
        Assert.Empty(stored);
    }

    private async Task<TaskEntity> ReadSingleAsync(Guid id)
    {
        await using var context = await _database.AppFactory.CreateDbContextAsync();
        return await context.Tasks.AsNoTracking().SingleAsync(task => task.Id == id);
    }

    private static TaskEntity[] CreateMixedTasks()
    {
        return
        [
            TestEntities.CreateTask(creationOffsetMinutes: 1),
            TestEntities.CreateTask(creationOffsetMinutes: 9),
            TestEntities.CreateTask(isRegular: true, creationOffsetMinutes: 4),
            TestEntities.CreateTask(isRegular: true, creationOffsetMinutes: 2),
            TestEntities.CreateTask(isDone: true, creationOffsetMinutes: 7),
            TestEntities.CreateTask(isDone: true, isRegular: true, creationOffsetMinutes: 3),
            TestEntities.CreateTask(isDone: true, creationOffsetMinutes: 8)
        ];
    }

    private static List<Guid> Ids(List<TaskEntity> tasks) => tasks.Select(task => task.Id).ToList();
}
