using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Enums;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Services.Interfaces;
using System.Text;

namespace FokusApp.Core.Services;

public sealed class CalendarExportService : ICalendarExportService
{
    private const string ExportFileName = "FokusAppKalender.ics";
    private const string CalendarMimeType = "text/calendar";

    private readonly ITaskRepository _taskRepository;
    private readonly IDataTransferFileService _fileService;
    private readonly IDataTransferTextProvider _textProvider;
    private readonly TimeProvider _timeProvider;

    public CalendarExportService(
        ITaskRepository taskRepository,
        IDataTransferFileService fileService,
        IDataTransferTextProvider textProvider,
        TimeProvider timeProvider)
    {
        _taskRepository = taskRepository;
        _fileService = fileService;
        _textProvider = textProvider;
        _timeProvider = timeProvider;
    }

    public async Task<DataTransferResult> ExportAsync()
    {
        var tasks = await _taskRepository.GetAllAsync().ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow();

        var exportableTasks = tasks.Where(task => task.TaskDate is not null && task.SeriesId is null);

        var ics = BuildIcs(exportableTasks, now);
        var shareTitle = _textProvider.GetText("CalendarExportShareTitle");
        await _fileService.ShareExportFileAsync(ExportFileName, shareTitle, ics, CalendarMimeType).ConfigureAwait(false);

        return DataTransferResult.Success(_textProvider.GetText("CalendarExportCreated", ExportFileName));
    }

    private static string BuildIcs(IEnumerable<TaskEntity> tasks, DateTimeOffset now)
    {
        var builder = new StringBuilder();
        AppendFoldedLine(builder, "BEGIN:VCALENDAR");
        AppendFoldedLine(builder, "VERSION:2.0");
        AppendFoldedLine(builder, "PRODID:-//FokusApp//ICS Export//DE");
        AppendFoldedLine(builder, "CALSCALE:GREGORIAN");

        foreach (var task in tasks)
        {
            AppendVEvent(builder, task, now);
        }

        AppendFoldedLine(builder, "END:VCALENDAR");
        return builder.ToString();
    }

    private static void AppendVEvent(StringBuilder builder, TaskEntity task, DateTimeOffset now)
    {
        var taskDate = task.TaskDate!.Value;

        AppendFoldedLine(builder, "BEGIN:VEVENT");
        AppendFoldedLine(builder, $"UID:{task.Id}@fokusapp");
        AppendFoldedLine(builder, $"DTSTAMP:{IcsTextFormatter.FormatUtcNow(now)}");
        AppendFoldedLine(builder, $"SUMMARY:{IcsTextFormatter.EscapeText(task.Note ?? string.Empty)}");

        if (task.TaskTime is { } taskTime)
        {
            AppendFoldedLine(builder, $"DTSTART:{IcsTextFormatter.FormatDateTimeLocal(taskDate, taskTime)}");
            AppendFoldedLine(builder, $"DTEND:{IcsTextFormatter.FormatDateTimeLocal(taskDate, taskTime + TimeSpan.FromMinutes(task.TimerMinutes))}");
        }
        else
        {
            AppendFoldedLine(builder, $"DTSTART;VALUE=DATE:{IcsTextFormatter.FormatDateOnly(taskDate)}");
            AppendFoldedLine(builder, $"DTEND;VALUE=DATE:{IcsTextFormatter.FormatDateOnly(taskDate.AddDays(1))}");
        }

        if (task.RepeatUnit != TaskRepeatUnit.None)
        {
            var rrule = TaskRepeatRuleBuilder.TryBuild(
                task.RepeatUnit, task.RepeatInterval, task.RepeatWeekdays,
                task.RepeatWeekdayOrdinal, task.RepeatMonth, task.RepeatWeekStart, task.RepeatEndDate);
            if (rrule is not null)
            {
                AppendFoldedLine(builder, $"RRULE:{rrule}");
            }
        }

        if (task.TaskTime is not null && task.ReminderMinutesBefore is { } reminderMinutesBefore)
        {
            AppendFoldedLine(builder, "BEGIN:VALARM");
            AppendFoldedLine(builder, $"TRIGGER:-PT{reminderMinutesBefore}M");
            AppendFoldedLine(builder, "ACTION:DISPLAY");
            AppendFoldedLine(builder, $"DESCRIPTION:{IcsTextFormatter.EscapeText(task.Note ?? string.Empty)}");
            AppendFoldedLine(builder, "END:VALARM");
        }

        AppendFoldedLine(builder, "END:VEVENT");
    }

    private static void AppendFoldedLine(StringBuilder builder, string contentLine)
    {
        builder.Append(IcsTextFormatter.FoldLine(contentLine));
        builder.Append("\r\n");
    }
}
