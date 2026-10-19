using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FocusAppTests.ViewModels;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FocusAppTests.Services;

public class ActiveTaskServiceTests
{
    private readonly ITaskCacheService _taskCache = Substitute.For<ITaskCacheService>();
    private readonly IScopeService _scopeService = Substitute.For<IScopeService>();
    private readonly FakeTimeProvider _clock = TestTasks.CreateClock();
    private readonly ActiveTaskService _service;
    private int _changedCount;

    public ActiveTaskServiceTests()
    {
        _scopeService.IsWorkScope.Returns(true);
        _service = new ActiveTaskService(_taskCache, _scopeService, _clock);
        _service.ActiveTaskChanged += (_, _) => _changedCount++;
    }

    private TaskDto AddTaskToCache(bool isWorkTask = true, bool isDone = false)
    {
        var task = new TaskDto
        {
            Id = Guid.NewGuid(),
            Note = "Aufgabe",
            IsWorkTask = isWorkTask,
            IsDone = isDone
        };
        _taskCache.FindTaskById(task.Id.Value).Returns(task);
        return task;
    }

    [Fact]
    public void Constructor_Clock_CreatesDefaultActiveTaskForTodayOfClock()
    {
        Assert.Equal(TestTasks.Today, _service.ActiveTask.TaskDate);
    }

    [Fact]
    public void SetActiveTask_ResetAfterMidnight_CreatesDefaultActiveTaskForNewToday()
    {
        var task = AddTaskToCache();
        _service.SetActiveTask(task.Id);
        _clock.Advance(TimeSpan.FromDays(1));

        _service.SetActiveTask(null);

        Assert.Null(_service.ActiveTaskId);
        Assert.Equal(new DateTime(2026, 3, 16), _service.ActiveTask.TaskDate);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_Scope_CreatesDefaultActiveTaskForScope(bool isWorkScope)
    {
        _scopeService.IsWorkScope.Returns(isWorkScope);

        var service = new ActiveTaskService(_taskCache, _scopeService, _clock);

        Assert.Null(service.ActiveTaskId);
        Assert.Equal(isWorkScope, service.ActiveTask.IsWorkTask);
    }

    [Fact]
    public void SetActiveTask_ExistingOpenTaskInScope_BecomesActiveAndRaisesEvent()
    {
        var task = AddTaskToCache();

        _service.SetActiveTask(task.Id);

        Assert.Equal(task.Id, _service.ActiveTaskId);
        Assert.Equal(1, _changedCount);
    }

    [Fact]
    public void SetActiveTask_SameTaskTwice_RaisesEventOnlyOnce()
    {
        var task = AddTaskToCache();

        _service.SetActiveTask(task.Id);
        _service.SetActiveTask(task.Id);

        Assert.Equal(1, _changedCount);
    }

    [Fact]
    public void SetActiveTask_UnknownId_KeepsDefaultTaskWithoutEvent()
    {
        var unknownId = Guid.NewGuid();
        _service.SetActiveTask(unknownId);

        Assert.Null(_service.ActiveTaskId);
        Assert.Equal(0, _changedCount);
    }

    [Fact]
    public void SetActiveTask_DoneTask_IsNotActivated()
    {
        var task = AddTaskToCache(isDone: true);

        _service.SetActiveTask(task.Id);

        Assert.Null(_service.ActiveTaskId);
        Assert.Equal(0, _changedCount);
    }

    [Fact]
    public void SetActiveTask_TaskOfOtherScope_IsNotActivated()
    {
        var task = AddTaskToCache(isWorkTask: false);

        _service.SetActiveTask(task.Id);

        Assert.Null(_service.ActiveTaskId);
    }

    [Theory]
    [MemberData(nameof(ResetIds))]
    public void SetActiveTask_NullOrEmptyId_ResetsActiveTaskAndRaisesEvent(Guid? resetId)
    {
        var task = AddTaskToCache();
        _service.SetActiveTask(task.Id);

        _service.SetActiveTask(resetId);

        Assert.Null(_service.ActiveTaskId);
        Assert.Equal(2, _changedCount);
    }

    [Fact]
    public void IsTaskActive_ActiveTaskId_ReturnsTrue()
    {
        var task = AddTaskToCache();
        var taskId = task.Id!.Value;
        _service.SetActiveTask(task.Id);

        var result = _service.IsTaskActive(taskId);

        Assert.True(result);
    }

    [Fact]
    public void IsTaskActive_OtherOrEmptyId_ReturnsFalse()
    {
        var task = AddTaskToCache();
        var otherId = Guid.NewGuid();
        _service.SetActiveTask(task.Id);

        var isOtherIdActive = _service.IsTaskActive(otherId);
        var isEmptyIdActive = _service.IsTaskActive(Guid.Empty);

        Assert.False(isOtherIdActive);
        Assert.False(isEmptyIdActive);
    }

    [Fact]
    public void ActiveTask_ModifyingReturnedTask_DoesNotChangeServiceState()
    {
        var task = AddTaskToCache();
        _service.SetActiveTask(task.Id);

        var copy = _service.ActiveTask;
        copy.Note = "Geändert";

        Assert.Equal("Aufgabe", _service.ActiveTask.Note);
    }

    [Fact]
    public void TasksLoaded_WithActiveTask_ResetsActiveTaskAndRaisesEvent()
    {
        var task = AddTaskToCache();
        _service.SetActiveTask(task.Id);

        _taskCache.TasksLoaded += Raise.Event();

        Assert.Null(_service.ActiveTaskId);
        Assert.Equal(2, _changedCount);
    }

    [Fact]
    public void ScopeChanged_WithActiveTask_ResetsActiveTaskToNewScope()
    {
        var task = AddTaskToCache();
        _service.SetActiveTask(task.Id);
        _scopeService.IsWorkScope.Returns(false);

        _scopeService.ScopeChanged += Raise.Event();

        Assert.Null(_service.ActiveTaskId);
        Assert.False(_service.ActiveTask.IsWorkTask);
        Assert.Equal(2, _changedCount);
    }

    [Fact]
    public void TaskCacheChanged_NoActiveTask_DoesNotRaiseEvent()
    {
        _taskCache.NormalTasksChanged += Raise.Event();

        Assert.Equal(0, _changedCount);
    }

    [Theory]
    [InlineData(nameof(ITaskCacheService.NormalTasksChanged))]
    [InlineData(nameof(ITaskCacheService.RegularTasksChanged))]
    [InlineData(nameof(ITaskCacheService.DoneTasksChanged))]
    public void TaskCacheChanged_ActiveTaskStillValid_KeepsActiveTaskAndRaisesEvent(string changedEvent)
    {
        var task = AddTaskToCache();
        _service.SetActiveTask(task.Id);

        RaiseCacheEvent(changedEvent);

        Assert.Equal(task.Id, _service.ActiveTaskId);
        Assert.Equal(2, _changedCount);
    }

    [Fact]
    public void TaskCacheChanged_ActiveTaskWasDeleted_ResetsActiveTask()
    {
        var task = AddTaskToCache();
        _service.SetActiveTask(task.Id);
        _taskCache.FindTaskById(task.Id!.Value).Returns((TaskDto?)null);

        _taskCache.NormalTasksChanged += Raise.Event();

        Assert.Null(_service.ActiveTaskId);
        Assert.Equal(2, _changedCount);
    }

    [Fact]
    public void TaskCacheChanged_ActiveTaskWasFinished_ResetsActiveTask()
    {
        var task = AddTaskToCache();
        _service.SetActiveTask(task.Id);
        var finished = new TaskDto { Id = task.Id, IsWorkTask = true, IsDone = true };
        _taskCache.FindTaskById(task.Id!.Value).Returns(finished);

        _taskCache.DoneTasksChanged += Raise.Event();

        Assert.Null(_service.ActiveTaskId);
    }

    [Fact]
    public void TaskCacheChanged_ActiveTaskWasUpdated_ProvidesUpdatedTask()
    {
        var task = AddTaskToCache();
        _service.SetActiveTask(task.Id);
        var updated = new TaskDto { Id = task.Id, IsWorkTask = true, Note = "Neu" };
        _taskCache.FindTaskById(task.Id!.Value).Returns(updated);

        _taskCache.NormalTasksChanged += Raise.Event();

        Assert.Equal("Neu", _service.ActiveTask.Note);
    }

    public static TheoryData<Guid?> ResetIds => new() { null, Guid.Empty };

    private void RaiseCacheEvent(string eventName)
    {
        switch (eventName)
        {
            case nameof(ITaskCacheService.NormalTasksChanged):
                _taskCache.NormalTasksChanged += Raise.Event();
                break;
            case nameof(ITaskCacheService.RegularTasksChanged):
                _taskCache.RegularTasksChanged += Raise.Event();
                break;
            default:
                _taskCache.DoneTasksChanged += Raise.Event();
                break;
        }
    }
}
