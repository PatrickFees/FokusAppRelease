using FokusApp.Core;
using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.Enums;
using FokusApp.Core.Extensions;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FocusAppTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using static FocusAppTests.TestSupport.WaitForAsyncHelper;

namespace FocusAppTests.Services;

public class TaskSeriesServiceTests
{

    private static readonly DateTime Today = new(2026, 3, 10);
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 14, 30, 0, TimeSpan.Zero);

    private readonly InMemoryTaskRepository _repository = new();
    private readonly ITaskCacheService _taskCache = Substitute.For<ITaskCacheService>();
    private readonly IDayChangeService _dayChange = Substitute.For<IDayChangeService>();
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly TaskSeriesService _service;

    public TaskSeriesServiceTests()
    {
        _service = new TaskSeriesService(_repository, _taskCache, _dayChange, _errorCapture, _timeProvider);
    }

    [Fact]
    public async Task EnsureTasksAsync_WeeklySeriesStart_CreatesFollowingTasksUpToTheHorizon()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week);
        _repository.Seed(TaskExtension.MapToEntity(start));

        await _service.EnsureTasksAsync();

        var seriesTasks = await GetSeriesTasksAsync(start);
        var expectedDays = Enumerable.Range(1, 12).Select(week => Today.AddDays(week * 7)).ToList();
        var actualDays = seriesTasks.Select(task => task.TaskDate!.Value).OrderBy(day => day).ToList();
        Assert.Equal(expectedDays, actualDays);
        Assert.All(seriesTasks, task => Assert.False(task.IsDone));
        Assert.All(seriesTasks, task => Assert.Equal(start.Note, task.Note));
        Assert.All(seriesTasks, task => Assert.Equal(start.TaskTime, task.TaskTime));
        Assert.All(seriesTasks, task => Assert.Equal(start.ReminderMinutesBefore, task.ReminderMinutesBefore));
        await _taskCache.Received(1).ReloadAsync();
    }

    [Fact]
    public async Task EnsureTasksAsync_Always_MarksTheHorizonOnTheSeriesStart()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week);
        _repository.Seed(TaskExtension.MapToEntity(start));

        await _service.EnsureTasksAsync();

        var storedStart = await GetTaskAsync(start.Id!.Value);
        var horizon = Today.AddDays(AppConstants.RepeatHorizonDays);
        Assert.Equal(horizon, storedStart.RepeatGeneratedUntil);
    }

    [Fact]
    public async Task EnsureTasksAsync_CalledTwice_DoesNotCreateDuplicates()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Day);
        _repository.Seed(TaskExtension.MapToEntity(start));

        await _service.EnsureTasksAsync();
        var countAfterFirstCall = _repository.AddedTasks.Count;
        await _service.EnsureTasksAsync();

        Assert.Equal(countAfterFirstCall, _repository.AddedTasks.Count);
        await _taskCache.Received(1).ReloadAsync();
    }

    [Fact]
    public async Task EnsureTasksAsync_NextDay_AddsOnlyTheNewlyReachedDay()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Day);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();
        var countAfterFirstCall = _repository.AddedTasks.Count;
        _timeProvider.Advance(TimeSpan.FromDays(1));

        await _service.EnsureTasksAsync();

        Assert.Equal(countAfterFirstCall + 1, _repository.AddedTasks.Count);
        var newestTask = _repository.AddedTasks[^1];
        var newHorizon = Today.AddDays(1 + AppConstants.RepeatHorizonDays);
        Assert.Equal(newHorizon, newestTask.TaskDate);
    }

    [Fact]
    public async Task EnsureTasksAsync_DeletedFutureTask_IsNotCreatedAgain()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Day);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();
        var seriesTasks = await GetSeriesTasksAsync(start);
        var deletedTask = seriesTasks.First(task => task.TaskDate == Today.AddDays(3));
        await _repository.DeleteAsync(deletedTask.Id);

        await _service.EnsureTasksAsync();

        var tasksAfter = await GetSeriesTasksAsync(start);
        Assert.DoesNotContain(tasksAfter, task => task.TaskDate == Today.AddDays(3));
    }

    [Fact]
    public async Task EnsureTasksAsync_SeriesStartInThePast_CreatesNothingBeforeToday()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Day, date: Today.AddDays(-5));
        _repository.Seed(TaskExtension.MapToEntity(start));

        await _service.EnsureTasksAsync();

        var seriesTasks = await GetSeriesTasksAsync(start);
        Assert.Equal(Today, seriesTasks.Min(task => task.TaskDate));
    }

    [Fact]
    public async Task EnsureTasksAsync_SeriesStartInTheFuture_StartsTheDayAfterTheStart()
    {
        var futureStart = Today.AddDays(10);
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Day, date: futureStart);
        _repository.Seed(TaskExtension.MapToEntity(start));

        await _service.EnsureTasksAsync();

        var seriesTasks = await GetSeriesTasksAsync(start);
        Assert.Equal(futureStart.AddDays(1), seriesTasks.Min(task => task.TaskDate));
    }

    [Fact]
    public async Task EnsureTasksAsync_DoneSeriesStart_StillContinuesTheSeries()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week);
        start.IsDone = true;
        start.CompletedAt = Now.DateTime;
        _repository.Seed(TaskExtension.MapToEntity(start));

        await _service.EnsureTasksAsync();

        var seriesTasks = await GetSeriesTasksAsync(start);
        Assert.NotEmpty(seriesTasks);
    }

    [Fact]
    public async Task EnsureTasksAsync_TasksThatDoNotStartASeries_CreatesNothing()
    {
        var plainTask = TestTasksFactory.Create(Today);
        var template = TestTasksFactory.Create(null, isTemplate: true);
        var seriesTask = CreateSeriesStart(unit: TaskRepeatUnit.Day);
        seriesTask.SeriesId = Guid.NewGuid();
        _repository.Seed(
            TaskExtension.MapToEntity(plainTask),
            TaskExtension.MapToEntity(template),
            TaskExtension.MapToEntity(seriesTask));

        await _service.EnsureTasksAsync();

        Assert.Empty(_repository.AddedTasks);
        await _taskCache.DidNotReceive().ReloadAsync();
    }

    [Fact]
    public async Task HandleSavedTaskAsync_NewSeriesStart_CreatesFollowingTasks()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week);
        _repository.Seed(TaskExtension.MapToEntity(start));

        await _service.HandleSavedTaskAsync(start, null);

        var seriesTasks = await GetSeriesTasksAsync(start);
        Assert.Equal(12, seriesTasks.Count);
    }

    [Fact]
    public async Task HandleSavedTaskAsync_PlainTask_DoesNothing()
    {
        var task = TestTasksFactory.Create(Today);
        _repository.Seed(TaskExtension.MapToEntity(task));

        await _service.HandleSavedTaskAsync(task, null);

        Assert.Empty(_repository.AddedTasks);
        Assert.Empty(_repository.UpdatedTasks);
        await _taskCache.DidNotReceive().ReloadAsync();
    }

    [Fact]
    public async Task HandleSavedTaskAsync_SeriesStartChanged_ReplacesFutureOpenTasks()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();
        var previous = TaskExtension.Clone(start);
        var edited = TaskExtension.Clone(start);
        edited.Note = "Neuer Text";
        edited.TaskTime = new TimeSpan(7, 45, 0);
        await _repository.UpdateAsync(TaskExtension.MapToEntity(edited));

        await _service.HandleSavedTaskAsync(edited, previous);

        var seriesTasks = await GetSeriesTasksAsync(start);
        Assert.Equal(12, seriesTasks.Count);
        Assert.All(seriesTasks, task => Assert.Equal("Neuer Text", task.Note));
        Assert.All(seriesTasks, task => Assert.Equal(new TimeSpan(7, 45, 0), task.TaskTime));
    }

    [Fact]
    public async Task HandleSavedTaskAsync_SeriesStartChanged_KeepsDoneAndPastTasks()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Day, date: Today.AddDays(-3));
        _repository.Seed(TaskExtension.MapToEntity(start));
        var doneTask = TaskExtension.CreateSeriesTask(start, Today.AddDays(2), Now.DateTime);
        doneTask.IsDone = true;
        doneTask.CompletedAt = Now.DateTime;
        var pastTask = TaskExtension.CreateSeriesTask(start, Today.AddDays(-1), Now.DateTime);
        _repository.Seed(TaskExtension.MapToEntity(doneTask), TaskExtension.MapToEntity(pastTask));
        var previous = TaskExtension.Clone(start);
        var edited = TaskExtension.Clone(start);
        edited.Note = "Neuer Text";

        await _service.HandleSavedTaskAsync(edited, previous);

        var stored = await _repository.GetAllAsync();
        Assert.Contains(stored, task => task.Id == doneTask.Id);
        Assert.Contains(stored, task => task.Id == pastTask.Id);
    }

    [Fact]
    public async Task HandleSavedTaskAsync_RepetitionSwitchedOff_RemovesFutureOpenTasks()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();
        var previous = TaskExtension.Clone(start);
        var edited = TaskExtension.Clone(start);
        edited.RepeatUnit = TaskRepeatUnit.None;
        await _repository.UpdateAsync(TaskExtension.MapToEntity(edited));

        await _service.HandleSavedTaskAsync(edited, previous);

        var seriesTasks = await GetSeriesTasksAsync(start);
        Assert.Empty(seriesTasks);
        await _taskCache.Received().ReloadAsync();
    }

    [Fact]
    public async Task HandleSavedTaskAsync_RuleChangedOnSeriesTask_AppliesRuleToTheSeriesStart()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();
        var seriesTasks = await GetSeriesTasksAsync(start);
        var editedEntity = seriesTasks.First(task => task.TaskDate == Today.AddDays(7));
        var previous = TaskExtension.MapToDto(editedEntity);
        var edited = TaskExtension.Clone(previous);
        edited.RepeatUnit = TaskRepeatUnit.Day;
        await _repository.UpdateAsync(TaskExtension.MapToEntity(edited));

        await _service.HandleSavedTaskAsync(edited, previous);

        var storedStart = await GetTaskAsync(start.Id!.Value);
        Assert.Equal(TaskRepeatUnit.Day, storedStart.RepeatUnit);
        var tasksAfter = await GetSeriesTasksAsync(start);
        var days = tasksAfter.Select(task => task.TaskDate!.Value).ToList();
        Assert.Equal(days.Count, days.Distinct().Count());
        Assert.Equal(AppConstants.RepeatHorizonDays, days.Count);
        Assert.Contains(tasksAfter, task => task.Id == edited.Id);
    }

    [Fact]
    public async Task HandleSavedTaskAsync_RepeatWeekStartChangedOnSeriesTask_AppliesRuleToTheSeriesStart()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week, interval: 2);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();
        var seriesTasks = await GetSeriesTasksAsync(start);
        var editedEntity = seriesTasks.First(task => task.TaskDate == Today.AddDays(14));
        var previous = TaskExtension.MapToDto(editedEntity);
        var edited = TaskExtension.Clone(previous);
        edited.RepeatWeekStart = TaskWeekdays.Sunday;
        await _repository.UpdateAsync(TaskExtension.MapToEntity(edited));

        await _service.HandleSavedTaskAsync(edited, previous);

        var storedStart = await GetTaskAsync(start.Id!.Value);
        Assert.Equal(TaskWeekdays.Sunday, storedStart.RepeatWeekStart);
    }

    [Fact]
    public async Task EnsureTasksAsync_RepeatEndDateBeforeHorizon_StopsGeneratingAtTheEndDate()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Day);
        start.RepeatEndDate = Today.AddDays(5);
        _repository.Seed(TaskExtension.MapToEntity(start));

        await _service.EnsureTasksAsync();

        var seriesTasks = await GetSeriesTasksAsync(start);
        Assert.Equal(5, seriesTasks.Count);
        Assert.All(seriesTasks, task => Assert.True(task.TaskDate <= Today.AddDays(5)));
    }

    [Fact]
    public async Task HandleSavedTaskAsync_RepeatEndDateChangedOnSeriesTask_AppliesRuleToTheSeriesStart()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Day);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();
        var seriesTasks = await GetSeriesTasksAsync(start);
        var editedEntity = seriesTasks.First(task => task.TaskDate == Today.AddDays(1));
        var previous = TaskExtension.MapToDto(editedEntity);
        var edited = TaskExtension.Clone(previous);
        edited.RepeatEndDate = Today.AddDays(5);
        await _repository.UpdateAsync(TaskExtension.MapToEntity(edited));

        await _service.HandleSavedTaskAsync(edited, previous);

        var storedStart = await GetTaskAsync(start.Id!.Value);
        Assert.Equal(Today.AddDays(5), storedStart.RepeatEndDate);
        var tasksAfter = await GetSeriesTasksAsync(start);
        Assert.All(tasksAfter, task => Assert.True(task.TaskDate <= Today.AddDays(5)));
    }

    [Fact]
    public async Task HandleSavedTaskAsync_RuleOfSeriesTaskUnchanged_DoesNotTouchTheSeries()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();
        var seriesTasks = await GetSeriesTasksAsync(start);
        var previous = TaskExtension.MapToDto(seriesTasks[0]);
        var edited = TaskExtension.Clone(previous);
        edited.Note = "Nur diese Aufgabe";
        var deletedBefore = _repository.DeletedIds.Count;

        await _service.HandleSavedTaskAsync(edited, previous, TaskSeriesScope.ThisTaskOnly);

        Assert.Equal(deletedBefore, _repository.DeletedIds.Count);
    }

    [Fact]
    public async Task HandleSavedTaskAsync_SeriesTaskEditedForWholeSeries_AppliesTheChangeToTheStartAndTheFollowingTasks()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();
        var seriesTasks = await GetSeriesTasksAsync(start);
        var editedEntity = seriesTasks.First(task => task.TaskDate == Today.AddDays(14));
        var previous = TaskExtension.MapToDto(editedEntity);
        var edited = TaskExtension.Clone(previous);
        edited.Note = "Ganze Serie";
        edited.TaskTime = new TimeSpan(6, 15, 0);
        await _repository.UpdateAsync(TaskExtension.MapToEntity(edited));

        await _service.HandleSavedTaskAsync(edited, previous, TaskSeriesScope.WholeSeries);

        var storedStart = await GetTaskAsync(start.Id!.Value);
        Assert.Equal("Ganze Serie", storedStart.Note);
        Assert.Equal(new TimeSpan(6, 15, 0), storedStart.TaskTime);
        var tasksAfter = await GetSeriesTasksAsync(start);
        Assert.Equal(12, tasksAfter.Count);
        Assert.All(tasksAfter, task => Assert.Equal("Ganze Serie", task.Note));
        Assert.Contains(tasksAfter, task => task.Id == edited.Id && task.TaskDate == Today.AddDays(14));
    }

    [Fact]
    public async Task HandleSavedTaskAsync_SeriesTaskEditedForWholeSeries_KeepsDoneAndPastTasks()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Day, date: Today.AddDays(-3));
        _repository.Seed(TaskExtension.MapToEntity(start));
        var doneTask = TaskExtension.CreateSeriesTask(start, Today.AddDays(2), Now.DateTime);
        doneTask.IsDone = true;
        doneTask.CompletedAt = Now.DateTime;
        var pastTask = TaskExtension.CreateSeriesTask(start, Today.AddDays(-1), Now.DateTime);
        var editedTask = TaskExtension.CreateSeriesTask(start, Today.AddDays(1), Now.DateTime);
        _repository.Seed(
            TaskExtension.MapToEntity(doneTask),
            TaskExtension.MapToEntity(pastTask),
            TaskExtension.MapToEntity(editedTask));
        var previous = TaskExtension.Clone(editedTask);
        var edited = TaskExtension.Clone(editedTask);
        edited.Note = "Neuer Text";

        await _service.HandleSavedTaskAsync(edited, previous, TaskSeriesScope.WholeSeries);

        var storedDone = await GetTaskAsync(doneTask.Id!.Value);
        var storedPast = await GetTaskAsync(pastTask.Id!.Value);
        Assert.Equal("Sport", storedDone.Note);
        Assert.Equal("Sport", storedPast.Note);
    }

    [Fact]
    public async Task HandleSavedTaskAsync_SeriesTaskEditedForThisTaskOnly_DoesNotTouchTheSeries()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();
        var seriesTasks = await GetSeriesTasksAsync(start);
        var previous = TaskExtension.MapToDto(seriesTasks[0]);
        var edited = TaskExtension.Clone(previous);
        edited.Note = "Nur diese";
        edited.TaskTime = new TimeSpan(6, 15, 0);
        var deletedBefore = _repository.DeletedIds.Count;

        await _service.HandleSavedTaskAsync(edited, previous, TaskSeriesScope.ThisTaskOnly);

        var storedStart = await GetTaskAsync(start.Id!.Value);
        Assert.Equal("Sport", storedStart.Note);
        Assert.Equal(deletedBefore, _repository.DeletedIds.Count);
    }

    [Fact]
    public async Task HandleSavedTaskAsync_SeriesStartEditedForThisTaskOnly_KeepsTheFollowingTasks()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();
        var previous = TaskExtension.Clone(start);
        var edited = TaskExtension.Clone(start);
        edited.Note = "Nur der erste";
        await _repository.UpdateAsync(TaskExtension.MapToEntity(edited));
        var deletedBefore = _repository.DeletedIds.Count;

        await _service.HandleSavedTaskAsync(edited, previous, TaskSeriesScope.ThisTaskOnly);

        var seriesTasks = await GetSeriesTasksAsync(start);
        Assert.Equal(12, seriesTasks.Count);
        Assert.All(seriesTasks, task => Assert.Equal("Sport", task.Note));
        Assert.Equal(deletedBefore, _repository.DeletedIds.Count);
    }

    [Fact]
    public async Task HandleSavedTaskAsync_SeriesStartRuleChangedForThisTaskOnly_StillRenewsTheFollowingTasks()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();
        var previous = TaskExtension.Clone(start);
        var edited = TaskExtension.Clone(start);
        edited.RepeatUnit = TaskRepeatUnit.Day;
        await _repository.UpdateAsync(TaskExtension.MapToEntity(edited));

        await _service.HandleSavedTaskAsync(edited, previous, TaskSeriesScope.ThisTaskOnly);

        var seriesTasks = await GetSeriesTasksAsync(start);
        Assert.Equal(AppConstants.RepeatHorizonDays, seriesTasks.Count);
    }

    [Fact]
    public async Task DeleteTaskAsync_FollowingTaskForThisTaskOnly_RemovesOnlyThatTaskForGood()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();
        var seriesTasks = await GetSeriesTasksAsync(start);
        var deleted = seriesTasks.First(task => task.TaskDate == Today.AddDays(14));

        var newStartId = await _service.DeleteTaskAsync(deleted.Id, TaskSeriesScope.ThisTaskOnly);
        await _service.EnsureTasksAsync();

        Assert.Null(newStartId);
        var tasksAfter = await GetSeriesTasksAsync(start);
        Assert.Equal(11, tasksAfter.Count);
        Assert.DoesNotContain(tasksAfter, task => task.Id == deleted.Id);
        await _taskCache.Received().ReloadAsync();
    }

    [Fact]
    public async Task DeleteTaskAsync_SeriesStartForThisTaskOnly_NextOpenTaskTakesOverTheRule()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();
        var seriesTasks = await GetSeriesTasksAsync(start);
        var successor = seriesTasks.OrderBy(task => task.TaskDate).First();

        var newStartId = await _service.DeleteTaskAsync(start.Id!.Value, TaskSeriesScope.ThisTaskOnly);

        Assert.Equal(successor.Id, newStartId);
        var all = await _repository.GetAllAsync();
        Assert.DoesNotContain(all, task => task.Id == start.Id);
        var newStart = all.Single(task => task.Id == successor.Id);
        Assert.Null(newStart.SeriesId);
        Assert.Equal(TaskRepeatUnit.Week, newStart.RepeatUnit);
        Assert.Equal(Today.AddDays(7), newStart.TaskDate);
        var followers = all.Where(task => task.SeriesId == newStartId).ToList();
        Assert.Equal(11, followers.Count);
    }

    [Fact]
    public async Task DeleteTaskAsync_SeriesStartForThisTaskOnly_SeriesGoesOnWithoutDuplicates()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Day);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();
        await _service.DeleteTaskAsync(start.Id!.Value, TaskSeriesScope.ThisTaskOnly);
        _timeProvider.Advance(TimeSpan.FromDays(1));

        await _service.EnsureTasksAsync();

        var all = await _repository.GetAllAsync();
        var days = all.Select(task => task.TaskDate!.Value).ToList();
        Assert.Equal(days.Count, days.Distinct().Count());
        Assert.Equal(Today.AddDays(1 + AppConstants.RepeatHorizonDays), days.Max());
    }

    [Fact]
    public async Task DeleteTaskAsync_SeriesStartWithoutFollowingTasks_CreatesTheNextFirstTask()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Year);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();

        var newStartId = await _service.DeleteTaskAsync(start.Id!.Value, TaskSeriesScope.ThisTaskOnly);

        Assert.NotNull(newStartId);
        var all = await _repository.GetAllAsync();
        var newStart = Assert.Single(all);
        Assert.Equal(newStartId, newStart.Id);
        Assert.Equal(Today.AddYears(1), newStart.TaskDate);
        Assert.Null(newStart.SeriesId);
        Assert.Equal(TaskRepeatUnit.Year, newStart.RepeatUnit);
        Assert.False(newStart.IsDone);
    }

    [Fact]
    public async Task DeleteTaskAsync_WholeSeriesFromAFollowingTask_RemovesAllOpenTasksAndEndsTheRepetition()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();
        var seriesTasks = await GetSeriesTasksAsync(start);

        var newStartId = await _service.DeleteTaskAsync(seriesTasks[3].Id, TaskSeriesScope.WholeSeries);
        await _service.EnsureTasksAsync();

        Assert.Null(newStartId);
        var all = await _repository.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task DeleteTaskAsync_WholeSeriesFromTheStart_RemovesAllOpenTasks()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();

        await _service.DeleteTaskAsync(start.Id!.Value, TaskSeriesScope.WholeSeries);

        var all = await _repository.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task DeleteTaskAsync_WholeSeriesWithDoneTask_KeepsItAsOneTimeTask()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();
        var seriesTasks = await GetSeriesTasksAsync(start);
        var doneId = seriesTasks[0].Id;
        await MarkDoneAsync(doneId);

        await _service.DeleteTaskAsync(start.Id!.Value, TaskSeriesScope.WholeSeries);

        var all = await _repository.GetAllAsync();
        var remaining = Assert.Single(all);
        Assert.Equal(doneId, remaining.Id);
        Assert.True(remaining.IsDone);
        Assert.Null(remaining.SeriesId);
    }

    [Fact]
    public async Task DeleteTaskAsync_WholeSeriesWithDoneStart_KeepsTheStartWithoutRule()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week);
        start.IsDone = true;
        start.CompletedAt = Now.DateTime;
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();

        await _service.DeleteTaskAsync(start.Id!.Value, TaskSeriesScope.WholeSeries);
        await _service.EnsureTasksAsync();

        var all = await _repository.GetAllAsync();
        var remaining = Assert.Single(all);
        Assert.Equal(start.Id, remaining.Id);
        Assert.Equal(TaskRepeatUnit.None, remaining.RepeatUnit);
        Assert.Null(remaining.RepeatGeneratedUntil);
    }

    [Fact]
    public async Task DeleteTaskAsync_WholeSeriesWithActiveTask_KeepsTheActiveTask()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Week);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();
        var seriesTasks = await GetSeriesTasksAsync(start);
        var activeId = seriesTasks[2].Id;

        await _service.DeleteTaskAsync(start.Id!.Value, TaskSeriesScope.WholeSeries, activeId);
        await _service.EnsureTasksAsync();

        var all = await _repository.GetAllAsync();
        var remaining = Assert.Single(all);
        Assert.Equal(activeId, remaining.Id);
        Assert.Null(remaining.SeriesId);
    }

    [Fact]
    public async Task DeleteTaskAsync_PlainTask_DeletesIt()
    {
        var task = TestTasksFactory.Create(Today);
        _repository.Seed(TaskExtension.MapToEntity(task));

        await _service.DeleteTaskAsync(task.Id!.Value, TaskSeriesScope.WholeSeries);

        var all = await _repository.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task DeleteTaskAsync_UnknownTask_DoesNothing()
    {
        var task = TestTasksFactory.Create(Today);
        _repository.Seed(TaskExtension.MapToEntity(task));

        await _service.DeleteTaskAsync(Guid.NewGuid(), TaskSeriesScope.WholeSeries);

        var all = await _repository.GetAllAsync();
        Assert.Single(all);
        await _taskCache.DidNotReceive().ReloadAsync();
    }

    [Fact]
    public async Task DayChanged_Always_CreatesTheTasksOfTheNewDay()
    {
        var start = CreateSeriesStart(unit: TaskRepeatUnit.Day);
        _repository.Seed(TaskExtension.MapToEntity(start));
        await _service.EnsureTasksAsync();
        var countBefore = _repository.AddedTasks.Count;
        _timeProvider.Advance(TimeSpan.FromDays(1));

        _dayChange.DayChanged += Raise.Event<EventHandler<DayChangedEventArgs>>(
            this,
            new DayChangedEventArgs(Today, Today.AddDays(1)));
        await WaitForAsync(() => _repository.AddedTasks.Count > countBefore, delayMs: 20);

        Assert.Equal(countBefore + 1, _repository.AddedTasks.Count);
    }

    private static TaskDto CreateSeriesStart(
        TaskRepeatUnit unit,
        DateTime? date = null,
        int interval = 1)
    {
        var task = TestTasksFactory.Create(date ?? Today);
        task.Note = "Sport";
        task.TaskTime = new TimeSpan(18, 30, 0);
        task.ReminderMinutesBefore = 15;
        task.RepeatUnit = unit;
        task.RepeatInterval = interval;
        return TaskExtension.NormalizeTaskForPersistence(task);
    }

    private async Task<List<TaskEntity>> GetSeriesTasksAsync(TaskDto start)
    {
        var all = await _repository.GetAllAsync();
        return all.Where(task => task.SeriesId == start.Id).ToList();
    }

    private async Task<TaskEntity> GetTaskAsync(Guid id)
    {
        var all = await _repository.GetAllAsync();
        return all.Single(task => task.Id == id);
    }

    private async Task MarkDoneAsync(Guid id)
    {
        var entity = await GetTaskAsync(id);
        var task = TaskExtension.MapToDto(entity);
        task.IsDone = true;
        task.CompletedAt = Now.DateTime;
        await _repository.UpdateAsync(TaskExtension.MapToEntity(task));
    }

    private static class TestTasksFactory
    {
        public static TaskDto Create(DateTime? date, bool isTemplate = false)
        {
            return new TaskDto
            {
                Id = Guid.NewGuid(),
                Note = "Aufgabe",
                IsTemplate = isTemplate,
                TaskDate = date,
                CreationDate = Today.AddDays(-1)
            };
        }
    }
}
