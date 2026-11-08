using FokusApp.Core.Extensions;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FocusAppTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using static FocusAppTests.TestSupport.WaitForAsyncHelper;

namespace FocusAppTests.Services;

public class TaskReminderServiceTests
{
    private static readonly DateTime Today = new(2026, 3, 10);
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryTaskRepository _repository = new();
    private readonly ITaskCacheService _taskCache = Substitute.For<ITaskCacheService>();
    private readonly IDayChangeService _dayChange = Substitute.For<IDayChangeService>();
    private readonly ITaskReminderPlatformAdapter _platform = Substitute.For<ITaskReminderPlatformAdapter>();
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly TaskReminderService _service;
    private readonly List<IReadOnlyList<TaskReminder>> _syncedPlans = [];

    public TaskReminderServiceTests()
    {
        _platform.SyncAsync(Arg.Do<IReadOnlyList<TaskReminder>>(plan => _syncedPlans.Add(plan)))
            .Returns(Task.CompletedTask);
        _service = new TaskReminderService(
            _repository,
            _taskCache,
            _dayChange,
            _platform,
            _errorCapture,
            _timeProvider);
    }

    [Fact]
    public async Task RefreshAsync_TaskWithReminder_SyncsItToThePlatform()
    {
        var task = CreateTask(new TimeSpan(14, 0, 0), 15, isWork: true);
        _repository.Seed(TaskExtension.MapToEntity(task));

        await _service.RefreshAsync();

        var plan = Assert.Single(_syncedPlans);
        var reminder = Assert.Single(plan);
        Assert.Equal(task.Id, reminder.TaskId);
        Assert.Equal(new DateTime(2026, 3, 10, 13, 45, 0), reminder.TriggerAt);
    }

    [Fact]
    public async Task RefreshAsync_TasksOfBothScopes_SyncsBoth()
    {
        var workTask = CreateTask(new TimeSpan(14, 0, 0), 0, isWork: true);
        var privateTask = CreateTask(new TimeSpan(16, 0, 0), 0, isWork: false);
        _repository.Seed(TaskExtension.MapToEntity(workTask), TaskExtension.MapToEntity(privateTask));

        await _service.RefreshAsync();

        var plan = Assert.Single(_syncedPlans);
        Assert.Equal(2, plan.Count);
    }

    [Fact]
    public async Task RefreshAsync_NoReminders_SyncsAnEmptyPlanSoOldOnesAreCancelled()
    {
        await _service.RefreshAsync();

        var plan = Assert.Single(_syncedPlans);
        Assert.Empty(plan);
    }

    [Fact]
    public async Task StartAsync_Always_PlansOnceImmediately()
    {
        await _service.StartAsync();

        Assert.Single(_syncedPlans);
    }

    [Fact]
    public async Task StartAsync_TasksChangedLater_PlansAgain()
    {
        await _service.StartAsync();

        _taskCache.NormalTasksChanged += Raise.Event();
        await WaitForAsync(() => _syncedPlans.Count >= 2, delayMs: 20);

        Assert.Equal(2, _syncedPlans.Count);
    }

    [Fact]
    public async Task StartAsync_DoneTasksChangedLater_PlansAgain()
    {
        await _service.StartAsync();

        _taskCache.DoneTasksChanged += Raise.Event();
        await WaitForAsync(() => _syncedPlans.Count >= 2, delayMs: 20);

        Assert.Equal(2, _syncedPlans.Count);
    }

    [Fact]
    public async Task StartAsync_DayChangedLater_PlansAgain()
    {
        await _service.StartAsync();

        _dayChange.DayChanged += Raise.Event<EventHandler<DayChangedEventArgs>>(
            this,
            new DayChangedEventArgs(Today, Today.AddDays(1)));
        await WaitForAsync(() => _syncedPlans.Count >= 2, delayMs: 20);

        Assert.Equal(2, _syncedPlans.Count);
    }

    [Fact]
    public async Task StartAsync_CalledTwice_FollowsChangesOnlyOnce()
    {
        await _service.StartAsync();
        await _service.StartAsync();
        _syncedPlans.Clear();

        _taskCache.NormalTasksChanged += Raise.Event();
        await WaitForAsync(() => _syncedPlans.Count >= 1, delayMs: 20);
        await Task.Delay(50);

        Assert.Single(_syncedPlans);
    }

    [Fact]
    public async Task StartAsync_PlatformFailsOnChange_CapturesTheErrorAndKeepsRunning()
    {
        await _service.StartAsync();
        var failure = new InvalidOperationException("Alarm nicht erlaubt");
        _platform.SyncAsync(Arg.Any<IReadOnlyList<TaskReminder>>()).Returns(Task.FromException(failure));

        _taskCache.NormalTasksChanged += Raise.Event();
        await WaitForAsync(() => _errorCapture.ReceivedCalls().Any(), delayMs: 20);

        await _errorCapture.Received(1).CaptureAsync(failure, Arg.Any<AppErrorCaptureOptions>());
    }

    [Fact]
    public async Task StartAsync_PlatformFailsOnInitialSync_CapturesTheErrorAndDoesNotThrow()
    {
        var failure = new NullReferenceException("The current Activity cannot be detected.");
        _platform.SyncAsync(Arg.Any<IReadOnlyList<TaskReminder>>()).Returns(Task.FromException(failure));

        await _service.StartAsync();

        await _errorCapture.Received(1).CaptureAsync(failure, Arg.Any<AppErrorCaptureOptions>());
    }

    private static TaskDto CreateTask(TimeSpan time, int reminderMinutes, bool isWork)
    {
        return new TaskDto
        {
            Id = Guid.NewGuid(),
            Note = "Aufgabe",
            IsWorkTask = isWork,
            TaskDate = Today,
            TaskTime = time,
            ReminderMinutesBefore = reminderMinutes,
            CreationDate = Today
        };
    }

}
