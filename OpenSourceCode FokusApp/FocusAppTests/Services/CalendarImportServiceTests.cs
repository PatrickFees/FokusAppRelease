using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.Enums;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FocusAppTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FocusAppTests.Services;

public class CalendarImportServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTime Today = Now.Date;

    private readonly InMemoryTaskRepository _taskRepository = new();
    private readonly ICalendarImportPlatformAdapter _platformAdapter = Substitute.For<ICalendarImportPlatformAdapter>();
    private readonly ITaskSeriesService _taskSeriesService = Substitute.For<ITaskSeriesService>();
    private readonly ITaskCacheService _taskCacheService = Substitute.For<ITaskCacheService>();
    private readonly IDataTransferTextProvider _textProvider = Substitute.For<IDataTransferTextProvider>();
    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly CalendarImportService _service;

    public CalendarImportServiceTests()
    {
        _platformAdapter.IsAvailable.Returns(true);
        _platformAdapter.HasPermissionAsync().Returns(true);
        _platformAdapter.GetRepeatingEventsAsync(Arg.Any<IReadOnlyList<string>>()).Returns([]);
        _platformAdapter
            .GetSingleOccurrencesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([]);
        _textProvider.GetText(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgAt<string>(0));

        _service = new CalendarImportService(
            _platformAdapter, _taskRepository, _taskSeriesService, _taskCacheService, _textProvider, _timeProvider);
    }

    private static DeviceCalendarEvent CreateEvent(
        string eventId,
        string title,
        DateTime occurrenceDate,
        TimeSpan? timeOfDay = null,
        TimeSpan? duration = null,
        bool isAllDay = false,
        string? repeatRule = null,
        DateTime? seriesAnchorDate = null,
        int? reminderMinutesBefore = null) =>
        new(
            eventId,
            "calendar-1",
            title,
            occurrenceDate,
            timeOfDay,
            duration,
            isAllDay,
            repeatRule,
            seriesAnchorDate ?? occurrenceDate,
            reminderMinutesBefore);

    [Fact]
    public async Task ImportAsync_PlatformNotAvailable_ReturnsFailureWithoutQueryingEvents()
    {
        _platformAdapter.IsAvailable.Returns(false);

        var result = await _service.ImportAsync(["calendar-1"], Today, Today.AddDays(30), isWorkTask: true);

        Assert.False(result.IsSuccess);
        Assert.Equal("CalendarImportPermissionMissing", result.Message);
        await _platformAdapter.DidNotReceive().GetRepeatingEventsAsync(Arg.Any<IReadOnlyList<string>>());
    }

    [Fact]
    public async Task ImportAsync_PermissionMissing_ReturnsFailure()
    {
        _platformAdapter.HasPermissionAsync().Returns(false);

        var result = await _service.ImportAsync(["calendar-1"], Today, Today.AddDays(30), isWorkTask: true);

        Assert.False(result.IsSuccess);
        Assert.Equal("CalendarImportPermissionMissing", result.Message);
    }

    [Fact]
    public async Task ImportAsync_NoCalendarsSelected_ReturnsFailure()
    {
        var result = await _service.ImportAsync([], Today, Today.AddDays(30), isWorkTask: true);

        Assert.False(result.IsSuccess);
        Assert.Equal("CalendarImportNoCalendars", result.Message);
    }

    [Fact]
    public async Task ImportAsync_SingleNonRepeatingEvent_AddsToDoTaskWithSourceId()
    {
        var deviceEvent = CreateEvent(
            "event-1", "Zahnarzt", Today.AddDays(2), timeOfDay: new TimeSpan(14, 30, 0), duration: TimeSpan.FromMinutes(45));
        _platformAdapter
            .GetSingleOccurrencesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([deviceEvent]);

        var result = await _service.ImportAsync(["calendar-1"], Today, Today.AddDays(30), isWorkTask: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ImportedCount);
        var added = Assert.Single(_taskRepository.AddedTasks);
        Assert.Equal("Zahnarzt", added.Note);
        Assert.Equal(Today.AddDays(2), added.TaskDate);
        Assert.Equal(new TimeSpan(14, 30, 0), added.TaskTime);
        Assert.Equal(45, added.TimerMinutes);
        Assert.False(added.IsPositive);
        Assert.False(added.IsTemplate);
        Assert.True(added.IsWorkTask);
        Assert.Equal("event-1", added.SourceCalendarEventId);
        Assert.Equal(TaskRepeatUnit.None, added.RepeatUnit);
        await _taskSeriesService.Received(1).EnsureTasksAsync();
        await _taskCacheService.Received(1).ReloadAsync();
    }

    [Fact]
    public async Task ImportAsync_AllDayEvent_HasNoTimeAndThirtyMinutesFocus()
    {
        var deviceEvent = CreateEvent("event-allday", "Urlaub", Today.AddDays(5), isAllDay: true);
        _platformAdapter
            .GetSingleOccurrencesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([deviceEvent]);

        var result = await _service.ImportAsync(["calendar-1"], Today, Today.AddDays(30), isWorkTask: false);

        Assert.True(result.IsSuccess);
        var added = Assert.Single(_taskRepository.AddedTasks);
        Assert.Null(added.TaskTime);
        Assert.Equal(30, added.TimerMinutes);
        Assert.False(added.IsWorkTask);
        Assert.Null(added.ReminderMinutesBefore);
    }

    [Fact]
    public async Task ImportAsync_TimedEventWithDeviceReminder_CarriesReminderOver()
    {
        var deviceEvent = CreateEvent(
            "event-reminder", "Meeting", Today.AddDays(1), timeOfDay: new TimeSpan(10, 0, 0), reminderMinutesBefore: 15);
        _platformAdapter
            .GetSingleOccurrencesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([deviceEvent]);

        await _service.ImportAsync(["calendar-1"], Today, Today.AddDays(30), isWorkTask: true);

        var added = Assert.Single(_taskRepository.AddedTasks);
        Assert.Equal(15, added.ReminderMinutesBefore);
    }

    [Fact]
    public async Task ImportAsync_TimedEventWithoutDeviceReminder_DefaultsToAtTheTime()
    {
        var deviceEvent = CreateEvent(
            "event-no-reminder", "Meeting", Today.AddDays(1), timeOfDay: new TimeSpan(10, 0, 0));
        _platformAdapter
            .GetSingleOccurrencesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([deviceEvent]);

        await _service.ImportAsync(["calendar-1"], Today, Today.AddDays(30), isWorkTask: true);

        var added = Assert.Single(_taskRepository.AddedTasks);
        Assert.Equal(0, added.ReminderMinutesBefore);
    }

    [Fact]
    public async Task ImportAsync_EventAlreadyImported_IsSkippedAsDuplicate()
    {
        var existing = new TaskEntity { Id = Guid.NewGuid(), SourceCalendarEventId = "event-1", CreationDate = Now.DateTime };
        _taskRepository.Seed(existing);

        var deviceEvent = CreateEvent("event-1", "Zahnarzt", Today.AddDays(2));
        _platformAdapter
            .GetSingleOccurrencesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([deviceEvent]);

        var result = await _service.ImportAsync(["calendar-1"], Today, Today.AddDays(30), isWorkTask: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.DuplicateCount);
        Assert.Empty(_taskRepository.AddedTasks);
    }

    [Fact]
    public async Task ImportAsync_WeeklyRepeatingEvent_CreatesSeriesStartAndIgnoresMatchingInstance()
    {
        var anchor = Today.AddDays(-7);
        var seriesEvent = CreateEvent(
            "series-1", "Yoga", anchor, timeOfDay: new TimeSpan(8, 0, 0),
            repeatRule: "FREQ=WEEKLY", seriesAnchorDate: anchor);
        _platformAdapter.GetRepeatingEventsAsync(Arg.Any<IReadOnlyList<string>>()).Returns([seriesEvent]);

        var overlappingInstance = seriesEvent with { OccurrenceDate = Today.AddDays(1) };
        _platformAdapter
            .GetSingleOccurrencesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([overlappingInstance]);

        var result = await _service.ImportAsync(["calendar-1"], Today, Today.AddDays(30), isWorkTask: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ImportedCount);
        var added = Assert.Single(_taskRepository.AddedTasks);
        Assert.Equal(TaskRepeatUnit.Week, added.RepeatUnit);
        Assert.Equal("series-1", added.SourceCalendarEventId);
        Assert.True(added.TaskDate >= Today);
    }

    [Fact]
    public async Task ImportAsync_WeeklyRepeatingEventWithWkst_CarriesWeekStartOver()
    {
        var anchor = Today.AddDays(-7);
        var seriesEvent = CreateEvent(
            "series-wkst", "Yoga", anchor, timeOfDay: new TimeSpan(8, 0, 0),
            repeatRule: "FREQ=WEEKLY;INTERVAL=2;WKST=SU", seriesAnchorDate: anchor);
        _platformAdapter.GetRepeatingEventsAsync(Arg.Any<IReadOnlyList<string>>()).Returns([seriesEvent]);

        var result = await _service.ImportAsync(["calendar-1"], Today, Today.AddDays(30), isWorkTask: true);

        Assert.True(result.IsSuccess);
        var added = Assert.Single(_taskRepository.AddedTasks);
        Assert.Equal(TaskWeekdays.Sunday, added.RepeatWeekStart);
    }

    [Fact]
    public async Task ImportAsync_WeeklyRepeatingEventWithoutWkst_LeavesWeekStartNull()
    {
        var anchor = Today.AddDays(-7);
        var seriesEvent = CreateEvent(
            "series-no-wkst", "Yoga", anchor, timeOfDay: new TimeSpan(8, 0, 0),
            repeatRule: "FREQ=WEEKLY", seriesAnchorDate: anchor);
        _platformAdapter.GetRepeatingEventsAsync(Arg.Any<IReadOnlyList<string>>()).Returns([seriesEvent]);

        var result = await _service.ImportAsync(["calendar-1"], Today, Today.AddDays(30), isWorkTask: true);

        Assert.True(result.IsSuccess);
        var added = Assert.Single(_taskRepository.AddedTasks);
        Assert.Null(added.RepeatWeekStart);
    }

    [Fact]
    public async Task ImportAsync_WeeklyRepeatingEventWithUntil_CarriesRepeatEndDateOver()
    {
        var anchor = Today.AddDays(-7);
        var seriesEvent = CreateEvent(
            "series-until", "Yoga", anchor, timeOfDay: new TimeSpan(8, 0, 0),
            repeatRule: "FREQ=WEEKLY;UNTIL=20260601T000000Z", seriesAnchorDate: anchor);
        _platformAdapter.GetRepeatingEventsAsync(Arg.Any<IReadOnlyList<string>>()).Returns([seriesEvent]);

        var result = await _service.ImportAsync(["calendar-1"], Today, Today.AddDays(30), isWorkTask: true);

        Assert.True(result.IsSuccess);
        var added = Assert.Single(_taskRepository.AddedTasks);
        Assert.Equal(new DateTime(2026, 6, 1), added.RepeatEndDate);
    }

    [Fact]
    public async Task ImportAsync_WeeklyRepeatingEventWithoutUntilOrCount_LeavesRepeatEndDateNull()
    {
        var anchor = Today.AddDays(-7);
        var seriesEvent = CreateEvent(
            "series-no-until", "Yoga", anchor, timeOfDay: new TimeSpan(8, 0, 0),
            repeatRule: "FREQ=WEEKLY", seriesAnchorDate: anchor);
        _platformAdapter.GetRepeatingEventsAsync(Arg.Any<IReadOnlyList<string>>()).Returns([seriesEvent]);

        var result = await _service.ImportAsync(["calendar-1"], Today, Today.AddDays(30), isWorkTask: true);

        Assert.True(result.IsSuccess);
        var added = Assert.Single(_taskRepository.AddedTasks);
        Assert.Null(added.RepeatEndDate);
    }

    [Fact]
    public async Task ImportAsync_UnmappableRepeatingEvent_ImportsEachInstanceIndividually()
    {
        const string rule = "FREQ=MONTHLY;BYDAY=1MO,3FR";
        var baseEvent = CreateEvent("series-2", "Lesekreis", Today, repeatRule: rule);
        _platformAdapter.GetRepeatingEventsAsync(Arg.Any<IReadOnlyList<string>>()).Returns([baseEvent]);

        var firstInstance = baseEvent with { OccurrenceDate = Today.AddDays(3) };
        var secondInstance = baseEvent with { OccurrenceDate = Today.AddDays(17) };
        _platformAdapter
            .GetSingleOccurrencesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([firstInstance, secondInstance]);

        var result = await _service.ImportAsync(["calendar-1"], Today, Today.AddDays(30), isWorkTask: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.ImportedCount);
        Assert.All(_taskRepository.AddedTasks, task => Assert.Equal(TaskRepeatUnit.None, task.RepeatUnit));
        Assert.All(_taskRepository.AddedTasks, task => Assert.StartsWith("series-2_", task.SourceCalendarEventId));
    }

    [Fact]
    public async Task SyncAsync_PlatformNotAvailable_ReturnsFailureWithoutQueryingEvents()
    {
        _platformAdapter.IsAvailable.Returns(false);

        var result = await _service.SyncAsync(["calendar-1"], isWorkTask: true);

        Assert.False(result.IsSuccess);
        await _platformAdapter.DidNotReceive().GetRepeatingEventsAsync(Arg.Any<IReadOnlyList<string>>());
    }

    [Fact]
    public async Task SyncAsync_NoCalendarsSelected_ReturnsFailureWithoutQueryingEvents()
    {
        var result = await _service.SyncAsync([], isWorkTask: true);

        Assert.False(result.IsSuccess);
        await _platformAdapter.DidNotReceive().GetRepeatingEventsAsync(Arg.Any<IReadOnlyList<string>>());
    }

    [Fact]
    public async Task SyncAsync_PermissionMissingWithoutPrompt_ReturnsFailureAndNeverPrompts()
    {
        _platformAdapter.HasPermissionWithoutPromptAsync().Returns(false);

        var result = await _service.SyncAsync(["calendar-1"], isWorkTask: true);

        Assert.False(result.IsSuccess);
        await _platformAdapter.DidNotReceive().HasPermissionAsync();
    }

    [Fact]
    public async Task SyncAsync_NewSingleEvent_AddsTaskAndStampsSyncSnapshot()
    {
        _platformAdapter.HasPermissionWithoutPromptAsync().Returns(true);
        var deviceEvent = CreateEvent(
            "event-1", "Zahnarzt", Today.AddDays(2), timeOfDay: new TimeSpan(14, 30, 0), duration: TimeSpan.FromMinutes(45));
        _platformAdapter
            .GetSingleOccurrencesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([deviceEvent]);

        var result = await _service.SyncAsync(["calendar-1"], isWorkTask: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ChangedCount);
        var added = Assert.Single(_taskRepository.AddedTasks);
        Assert.Equal("Zahnarzt", added.Note);
        Assert.Equal("event-1", added.SourceCalendarEventId);
        Assert.Equal("Zahnarzt", added.LastSyncedNote);
        Assert.Equal(new TimeSpan(14, 30, 0), added.LastSyncedTaskTime);
        Assert.Equal(45, added.LastSyncedTimerMinutes);
        await _taskSeriesService.Received(1).EnsureTasksAsync();
        await _taskCacheService.Received(1).ReloadAsync();
    }

    [Fact]
    public async Task SyncAsync_UntouchedSingleEvent_ChangedOnDevice_UpdatesTaskAndSnapshot()
    {
        _platformAdapter.HasPermissionWithoutPromptAsync().Returns(true);
        var existing = CreateSyncedSingleTask(
            "event-1", "Zahnarzt", Today.AddDays(2), new TimeSpan(14, 0, 0), 30, reminderMinutesBefore: null);
        _taskRepository.Seed(existing);

        var deviceEvent = CreateEvent(
            "event-1", "Zahnarzt (verschoben)", Today.AddDays(2), timeOfDay: new TimeSpan(15, 0, 0), duration: TimeSpan.FromMinutes(45));
        _platformAdapter
            .GetSingleOccurrencesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([deviceEvent]);

        var result = await _service.SyncAsync(["calendar-1"], isWorkTask: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ChangedCount);
        var updated = Assert.Single(await _taskRepository.GetAllAsync());
        Assert.Equal("Zahnarzt (verschoben)", updated.Note);
        Assert.Equal(new TimeSpan(15, 0, 0), updated.TaskTime);
        Assert.Equal(45, updated.TimerMinutes);
        Assert.Equal(updated.Note, updated.LastSyncedNote);
    }

    [Fact]
    public async Task SyncAsync_TouchedSingleEvent_ChangedOnDevice_LeavesTaskUnchanged()
    {
        _platformAdapter.HasPermissionWithoutPromptAsync().Returns(true);
        var existing = CreateSyncedSingleTask(
            "event-1", "Zahnarzt", Today.AddDays(2), new TimeSpan(14, 0, 0), 30, reminderMinutesBefore: null);
        existing.Note = "Zahnarzt (von mir angepasst)";
        _taskRepository.Seed(existing);

        var deviceEvent = CreateEvent(
            "event-1", "Zahnarzt (verschoben)", Today.AddDays(2), timeOfDay: new TimeSpan(15, 0, 0), duration: TimeSpan.FromMinutes(45));
        _platformAdapter
            .GetSingleOccurrencesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([deviceEvent]);

        var result = await _service.SyncAsync(["calendar-1"], isWorkTask: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ChangedCount);
        var unchanged = Assert.Single(await _taskRepository.GetAllAsync());
        Assert.Equal("Zahnarzt (von mir angepasst)", unchanged.Note);
        Assert.Equal(new TimeSpan(14, 0, 0), unchanged.TaskTime);
    }

    [Fact]
    public async Task SyncAsync_UntouchedSingleEvent_DeletedOnDevice_DeletesTask()
    {
        _platformAdapter.HasPermissionWithoutPromptAsync().Returns(true);
        var existing = CreateSyncedSingleTask(
            "event-1", "Zahnarzt", Today.AddDays(2), new TimeSpan(14, 0, 0), 30, reminderMinutesBefore: null);
        _taskRepository.Seed(existing);

        var result = await _service.SyncAsync(["calendar-1"], isWorkTask: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ChangedCount);
        Assert.Contains(existing.Id, _taskRepository.DeletedIds);
    }

    [Fact]
    public async Task SyncAsync_TouchedSingleEvent_DeletedOnDevice_DetachesInsteadOfDeleting()
    {
        _platformAdapter.HasPermissionWithoutPromptAsync().Returns(true);
        var existing = CreateSyncedSingleTask(
            "event-1", "Zahnarzt", Today.AddDays(2), new TimeSpan(14, 0, 0), 30, reminderMinutesBefore: null);
        existing.IsDone = true;
        existing.CompletedAt = Now.DateTime;
        _taskRepository.Seed(existing);

        var result = await _service.SyncAsync(["calendar-1"], isWorkTask: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ChangedCount);
        Assert.DoesNotContain(existing.Id, _taskRepository.DeletedIds);
        var remaining = Assert.Single(await _taskRepository.GetAllAsync());
        Assert.Null(remaining.SourceCalendarEventId);
    }

    [Fact]
    public async Task SyncAsync_NewRepeatingEvent_CreatesSeriesStartAndStampsSyncSnapshot()
    {
        _platformAdapter.HasPermissionWithoutPromptAsync().Returns(true);
        var anchor = Today.AddDays(-7);
        var seriesEvent = CreateEvent(
            "series-1", "Yoga", anchor, timeOfDay: new TimeSpan(8, 0, 0), repeatRule: "FREQ=WEEKLY", seriesAnchorDate: anchor);
        _platformAdapter.GetRepeatingEventsAsync(Arg.Any<IReadOnlyList<string>>()).Returns([seriesEvent]);

        var result = await _service.SyncAsync(["calendar-1"], isWorkTask: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ChangedCount);
        var added = Assert.Single(_taskRepository.AddedTasks);
        Assert.Equal(TaskRepeatUnit.Week, added.RepeatUnit);
        Assert.Equal("series-1", added.SourceCalendarEventId);
        Assert.Equal("Yoga", added.LastSyncedNote);
    }

    [Fact]
    public async Task SyncAsync_UntouchedSeriesStart_RuleChangedOnDevice_UpdatesWholeSeriesViaTaskSeriesService()
    {
        _platformAdapter.HasPermissionWithoutPromptAsync().Returns(true);
        var anchor = Today.AddDays(-7);
        var seriesStart = CreateSyncedSeriesStart("series-1", "Yoga", anchor, new TimeSpan(8, 0, 0), TaskRepeatUnit.Week);
        _taskRepository.Seed(seriesStart);

        var seriesEvent = CreateEvent(
            "series-1", "Yoga", anchor, timeOfDay: new TimeSpan(9, 0, 0), repeatRule: "FREQ=WEEKLY;INTERVAL=2", seriesAnchorDate: anchor);
        _platformAdapter.GetRepeatingEventsAsync(Arg.Any<IReadOnlyList<string>>()).Returns([seriesEvent]);

        var result = await _service.SyncAsync(["calendar-1"], isWorkTask: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ChangedCount);
        await _taskSeriesService.Received(1).HandleSavedTaskAsync(
            Arg.Is<TaskDto>(dto => dto.TaskTime == new TimeSpan(9, 0, 0) && dto.RepeatInterval == 2),
            Arg.Any<TaskDto>(),
            TaskSeriesScope.WholeSeries);
    }

    [Fact]
    public async Task SyncAsync_TouchedSeriesStart_RuleChangedOnDevice_LeavesSeriesUnchanged()
    {
        _platformAdapter.HasPermissionWithoutPromptAsync().Returns(true);
        var anchor = Today.AddDays(-7);
        var seriesStart = CreateSyncedSeriesStart("series-1", "Yoga", anchor, new TimeSpan(8, 0, 0), TaskRepeatUnit.Week);
        seriesStart.Note = "Yoga (von mir angepasst)";
        _taskRepository.Seed(seriesStart);

        var seriesEvent = CreateEvent(
            "series-1", "Yoga", anchor, timeOfDay: new TimeSpan(9, 0, 0), repeatRule: "FREQ=WEEKLY;INTERVAL=2", seriesAnchorDate: anchor);
        _platformAdapter.GetRepeatingEventsAsync(Arg.Any<IReadOnlyList<string>>()).Returns([seriesEvent]);

        var result = await _service.SyncAsync(["calendar-1"], isWorkTask: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ChangedCount);
        await _taskSeriesService.DidNotReceive().HandleSavedTaskAsync(
            Arg.Any<TaskDto>(), Arg.Any<TaskDto>(), Arg.Any<TaskSeriesScope>());
    }

    [Fact]
    public async Task SyncAsync_UntouchedSeriesStart_DeletedOnDevice_DeletesWholeSeriesViaTaskSeriesService()
    {
        _platformAdapter.HasPermissionWithoutPromptAsync().Returns(true);
        var anchor = Today.AddDays(-7);
        var seriesStart = CreateSyncedSeriesStart("series-1", "Yoga", anchor, new TimeSpan(8, 0, 0), TaskRepeatUnit.Week);
        _taskRepository.Seed(seriesStart);

        var result = await _service.SyncAsync(["calendar-1"], isWorkTask: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ChangedCount);
        await _taskSeriesService.Received(1).DeleteTaskAsync(seriesStart.Id, TaskSeriesScope.WholeSeries);
    }

    [Fact]
    public async Task SyncAsync_TouchedSeriesStart_DeletedOnDevice_DetachesInsteadOfDeletingTheSeries()
    {
        _platformAdapter.HasPermissionWithoutPromptAsync().Returns(true);
        var anchor = Today.AddDays(-7);
        var seriesStart = CreateSyncedSeriesStart("series-1", "Yoga", anchor, new TimeSpan(8, 0, 0), TaskRepeatUnit.Week);
        seriesStart.Note = "Yoga (von mir angepasst)";
        _taskRepository.Seed(seriesStart);

        var result = await _service.SyncAsync(["calendar-1"], isWorkTask: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ChangedCount);
        await _taskSeriesService.DidNotReceive().DeleteTaskAsync(Arg.Any<Guid>(), Arg.Any<TaskSeriesScope>());
        var remaining = Assert.Single(await _taskRepository.GetAllAsync());
        Assert.Null(remaining.SourceCalendarEventId);
    }

    [Fact]
    public async Task SyncAsync_NothingChanged_DoesNotRefreshSeriesOrCache()
    {
        _platformAdapter.HasPermissionWithoutPromptAsync().Returns(true);
        var existing = CreateSyncedSingleTask(
            "event-1", "Zahnarzt", Today.AddDays(2), new TimeSpan(14, 0, 0), 45, reminderMinutesBefore: 0);
        _taskRepository.Seed(existing);

        var deviceEvent = CreateEvent(
            "event-1", "Zahnarzt", Today.AddDays(2), timeOfDay: new TimeSpan(14, 0, 0), duration: TimeSpan.FromMinutes(45));
        _platformAdapter
            .GetSingleOccurrencesAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns([deviceEvent]);

        var result = await _service.SyncAsync(["calendar-1"], isWorkTask: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ChangedCount);
        await _taskSeriesService.DidNotReceive().EnsureTasksAsync();
        await _taskCacheService.DidNotReceive().ReloadAsync();
    }

    private TaskEntity CreateSyncedSingleTask(
        string sourceCalendarEventId, string note, DateTime taskDate, TimeSpan? taskTime, int timerMinutes, int? reminderMinutesBefore) =>
        new()
        {
            Id = Guid.NewGuid(),
            CreationDate = Now.DateTime,
            SourceCalendarEventId = sourceCalendarEventId,
            Note = note,
            TaskDate = taskDate,
            TaskTime = taskTime,
            TimerMinutes = timerMinutes,
            ReminderMinutesBefore = reminderMinutesBefore,
            LastSyncedNote = note,
            LastSyncedTaskTime = taskTime,
            LastSyncedTimerMinutes = timerMinutes,
            LastSyncedReminderMinutesBefore = reminderMinutesBefore
        };

    private TaskEntity CreateSyncedSeriesStart(
        string sourceCalendarEventId, string note, DateTime taskDate, TimeSpan? taskTime, TaskRepeatUnit repeatUnit) =>
        new()
        {
            Id = Guid.NewGuid(),
            CreationDate = Now.DateTime,
            SourceCalendarEventId = sourceCalendarEventId,
            Note = note,
            TaskDate = taskDate,
            TaskTime = taskTime,
            TimerMinutes = 30,
            RepeatUnit = repeatUnit,
            RepeatInterval = 1,
            LastSyncedNote = note,
            LastSyncedTaskTime = taskTime,
            LastSyncedTimerMinutes = 30,
            LastSyncedReminderMinutesBefore = null
        };
}
