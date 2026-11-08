using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.Enums;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Services;
using FocusAppTests.Fakes;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FocusAppTests.Services;

public class CalendarExportServiceTests
{
    private const string ExportFileName = "FokusAppKalender.ics";

    private static readonly DateTimeOffset Now = new(2026, 3, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryTaskRepository _taskRepository = new();
    private readonly IDataTransferFileService _fileService = Substitute.For<IDataTransferFileService>();
    private readonly IDataTransferTextProvider _textProvider = Substitute.For<IDataTransferTextProvider>();
    private readonly FakeTimeProvider _timeProvider = new(Now);
    private readonly CalendarExportService _service;

    public CalendarExportServiceTests()
    {
        _textProvider.GetText(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgAt<string>(0));
        _service = new CalendarExportService(_taskRepository, _fileService, _textProvider, _timeProvider);
    }

    private static TaskEntity CreateTask(
        DateTime? taskDate = null,
        TimeSpan? taskTime = null,
        string note = "Aufgabe",
        bool isDone = false,
        int timerMinutes = 25,
        int? reminderMinutesBefore = null,
        TaskRepeatUnit repeatUnit = TaskRepeatUnit.None,
        Guid? seriesId = null) => new()
    {
        Id = Guid.NewGuid(),
        Note = note,
        TaskDate = taskDate,
        TaskTime = taskTime,
        TimerMinutes = timerMinutes,
        IsDone = isDone,
        ReminderMinutesBefore = reminderMinutesBefore,
        RepeatUnit = repeatUnit,
        RepeatInterval = 1,
        SeriesId = seriesId
    };

    private async Task<string> ExportAndCaptureIcsAsync()
    {
        string? capturedContent = null;
        await _fileService.ShareExportFileAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Do<string>(content => capturedContent = content), Arg.Any<string>());

        await _service.ExportAsync();

        Assert.NotNull(capturedContent);
        return capturedContent!;
    }

    [Fact]
    public async Task ExportAsync_TaskWithoutDate_IsExcluded()
    {
        _taskRepository.Seed(CreateTask(taskDate: null));

        var ics = await ExportAndCaptureIcsAsync();

        Assert.DoesNotContain("BEGIN:VEVENT", ics);
    }

    [Fact]
    public async Task ExportAsync_DoneTaskWithDate_IsStillIncluded()
    {
        _taskRepository.Seed(CreateTask(taskDate: new DateTime(2026, 3, 15), isDone: true));

        var ics = await ExportAndCaptureIcsAsync();

        Assert.Contains("BEGIN:VEVENT", ics);
    }

    [Fact]
    public async Task ExportAsync_SeriesStart_IncludesRrule()
    {
        _taskRepository.Seed(CreateTask(
            taskDate: new DateTime(2026, 3, 15),
            repeatUnit: TaskRepeatUnit.Day,
            seriesId: null));

        var ics = await ExportAndCaptureIcsAsync();

        Assert.Contains("RRULE:FREQ=DAILY", ics);
    }

    [Fact]
    public async Task ExportAsync_MaterializedSeriesFollowUp_IsExcluded()
    {
        _taskRepository.Seed(CreateTask(
            taskDate: new DateTime(2026, 3, 16),
            repeatUnit: TaskRepeatUnit.Day,
            seriesId: Guid.NewGuid()));

        var ics = await ExportAndCaptureIcsAsync();

        Assert.DoesNotContain("BEGIN:VEVENT", ics);
    }

    [Fact]
    public async Task ExportAsync_AllDayTask_UsesValueDateStartAndEnd()
    {
        _taskRepository.Seed(CreateTask(taskDate: new DateTime(2026, 3, 15), taskTime: null));

        var ics = await ExportAndCaptureIcsAsync();

        Assert.Contains("DTSTART;VALUE=DATE:20260315", ics);
        Assert.Contains("DTEND;VALUE=DATE:20260316", ics);
    }

    [Fact]
    public async Task ExportAsync_TimedTask_UsesDateTimeStartAndEnd()
    {
        _taskRepository.Seed(CreateTask(
            taskDate: new DateTime(2026, 3, 15), taskTime: new TimeSpan(9, 0, 0), timerMinutes: 30));

        var ics = await ExportAndCaptureIcsAsync();

        Assert.Contains("DTSTART:20260315T090000", ics);
        Assert.Contains("DTEND:20260315T093000", ics);
    }

    [Fact]
    public async Task ExportAsync_TaskWithReminder_IncludesValarmTrigger()
    {
        _taskRepository.Seed(CreateTask(
            taskDate: new DateTime(2026, 3, 15), taskTime: new TimeSpan(9, 0, 0), reminderMinutesBefore: 10));

        var ics = await ExportAndCaptureIcsAsync();

        Assert.Contains("BEGIN:VALARM", ics);
        Assert.Contains("TRIGGER:-PT10M", ics);
    }

    [Fact]
    public async Task ExportAsync_SharesFileWithCalendarMimeTypeAndFileName()
    {
        _taskRepository.Seed(CreateTask(taskDate: new DateTime(2026, 3, 15)));

        await _service.ExportAsync();

        await _fileService.Received(1).ShareExportFileAsync(
            ExportFileName, Arg.Any<string>(), Arg.Any<string>(), "text/calendar");
    }

    [Fact]
    public async Task ExportAsync_ReturnsSuccessWithCreatedMessage()
    {
        _taskRepository.Seed(CreateTask(taskDate: new DateTime(2026, 3, 15)));

        var result = await _service.ExportAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("CalendarExportCreated", result.Message);
    }
}
