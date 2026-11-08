using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Enums;
using FokusApp.Core.Extensions;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class CalendarImportService : ICalendarImportService
{

    private const int AllDayTimerMinutes = 30;

    private readonly ICalendarImportPlatformAdapter _platformAdapter;
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskSeriesService _taskSeriesService;
    private readonly ITaskCacheService _taskCacheService;
    private readonly IDataTransferTextProvider _textProvider;
    private readonly TimeProvider _timeProvider;

    public CalendarImportService(
        ICalendarImportPlatformAdapter platformAdapter,
        ITaskRepository taskRepository,
        ITaskSeriesService taskSeriesService,
        ITaskCacheService taskCacheService,
        IDataTransferTextProvider textProvider,
        TimeProvider timeProvider)
    {
        _platformAdapter = platformAdapter;
        _taskRepository = taskRepository;
        _taskSeriesService = taskSeriesService;
        _taskCacheService = taskCacheService;
        _textProvider = textProvider;
        _timeProvider = timeProvider;
    }

    public bool IsAvailable => _platformAdapter.IsAvailable;

    public async Task<IReadOnlyList<DeviceCalendarInfo>> GetAvailableCalendarsAsync()
    {
        if (!IsAvailable || !await _platformAdapter.HasPermissionAsync().ConfigureAwait(false))
        {
            return [];
        }

        return await _platformAdapter.GetCalendarsAsync().ConfigureAwait(false);
    }

    public async Task<CalendarImportResult> ImportAsync(
        IReadOnlyList<string> calendarIds, DateTime from, DateTime to, bool isWorkTask)
    {
        if (!IsAvailable || !await _platformAdapter.HasPermissionAsync().ConfigureAwait(false))
        {
            return CalendarImportResult.Failure(_textProvider.GetText("CalendarImportPermissionMissing"));
        }

        if (calendarIds.Count == 0)
        {
            return CalendarImportResult.Failure(_textProvider.GetText("CalendarImportNoCalendars"));
        }

        var now = _timeProvider.GetLocalNow();
        var today = now.Date;

        var repeatingEvents = await _platformAdapter.GetRepeatingEventsAsync(calendarIds).ConfigureAwait(false);
        var singleOccurrences = await _platformAdapter
            .GetSingleOccurrencesAsync(calendarIds, from.Date, to.Date)
            .ConfigureAwait(false);

        var existingTasks = await _taskRepository.GetAllAsync().ConfigureAwait(false);
        var existingSourceIds = existingTasks
            .Where(task => task.SourceCalendarEventId is not null)
            .Select(task => task.SourceCalendarEventId!)
            .ToHashSet();

        var newTasks = new List<TaskEntity>();
        var importedCount = 0;
        var duplicateCount = 0;
        var skippedCount = 0;

        var handledAsSeriesEventIds = new HashSet<string>();

        foreach (var deviceEvent in repeatingEvents)
        {
            if (!CalendarRepeatRuleParser.TryParse(
                    deviceEvent.RawRepeatRule,
                    deviceEvent.SeriesAnchorDate,
                    out var unit,
                    out var interval,
                    out var weekdays,
                    out var weekdayOrdinal,
                    out var month,
                    out var weekStart,
                    out var repeatUntil))
            {

                continue;
            }

            handledAsSeriesEventIds.Add(deviceEvent.EventId);

            var nextOccurrence = TaskRepeatCalculator
                .GetOccurrences(
                    deviceEvent.SeriesAnchorDate,
                    unit,
                    interval,
                    weekdays,
                    today,
                    today.AddYears(AppConstants.RepeatSearchYears),
                    weekdayOrdinal,
                    month,
                    weekStart,
                    repeatUntil)
                .FirstOrDefault();

            if (nextOccurrence == default)
            {
                skippedCount++;
                continue;
            }

            if (!existingSourceIds.Add(deviceEvent.EventId))
            {
                duplicateCount++;
                continue;
            }

            var seriesStart = CreateTaskDto(deviceEvent, nextOccurrence, deviceEvent.EventId, isWorkTask, now);
            seriesStart.RepeatUnit = unit;
            seriesStart.RepeatInterval = interval;
            seriesStart.RepeatWeekdays = weekdays;
            seriesStart.RepeatWeekdayOrdinal = weekdayOrdinal;
            seriesStart.RepeatMonth = month;
            seriesStart.RepeatWeekStart = weekStart;
            seriesStart.RepeatEndDate = repeatUntil;

            newTasks.Add(TaskExtension.MapToEntity(seriesStart));
            importedCount++;
        }

        foreach (var deviceEvent in singleOccurrences)
        {
            if (deviceEvent.RawRepeatRule is not null && handledAsSeriesEventIds.Contains(deviceEvent.EventId))
            {
                continue;
            }

            var sourceId = deviceEvent.RawRepeatRule is null
                ? deviceEvent.EventId
                : $"{deviceEvent.EventId}_{deviceEvent.OccurrenceDate:yyyyMMddHHmm}";

            if (!existingSourceIds.Add(sourceId))
            {
                duplicateCount++;
                continue;
            }

            var task = CreateTaskDto(deviceEvent, deviceEvent.OccurrenceDate, sourceId, isWorkTask, now);
            newTasks.Add(TaskExtension.MapToEntity(task));
            importedCount++;
        }

        if (newTasks.Count > 0)
        {
            await _taskRepository.AddRangeAsync(newTasks).ConfigureAwait(false);
            await _taskSeriesService.EnsureTasksAsync().ConfigureAwait(false);
            await _taskCacheService.ReloadAsync().ConfigureAwait(false);
        }

        var message = _textProvider.GetText("CalendarImportCompleted", importedCount, duplicateCount, skippedCount);
        return CalendarImportResult.Success(importedCount, duplicateCount, skippedCount, message);
    }

    public async Task<CalendarSyncResult> SyncAsync(IReadOnlyList<string> calendarIds, bool isWorkTask)
    {
        if (!IsAvailable || calendarIds.Count == 0 ||
            !await _platformAdapter.HasPermissionWithoutPromptAsync().ConfigureAwait(false))
        {
            return CalendarSyncResult.Failure();
        }

        var now = _timeProvider.GetLocalNow();
        var today = now.Date;
        var windowEnd = today.AddDays(AppConstants.CalendarAutoSyncWindowDays);

        var repeatingEvents = await _platformAdapter.GetRepeatingEventsAsync(calendarIds).ConfigureAwait(false);
        var singleOccurrences = await _platformAdapter
            .GetSingleOccurrencesAsync(calendarIds, today, windowEnd)
            .ConfigureAwait(false);
        var existingTasks = await _taskRepository.GetAllAsync().ConfigureAwait(false);

        var handledAsSeriesEventIds = repeatingEvents
            .Where(deviceEvent => CalendarRepeatRuleParser.TryParse(
                deviceEvent.RawRepeatRule, deviceEvent.SeriesAnchorDate,
                out _, out _, out _, out _, out _, out _, out _))
            .Select(deviceEvent => deviceEvent.EventId)
            .ToHashSet();

        var seriesChanges = await SyncRepeatingEventsAsync(repeatingEvents, existingTasks, isWorkTask, now).ConfigureAwait(false);
        var singleChanges = await SyncSingleOccurrencesAsync(
            singleOccurrences, existingTasks, handledAsSeriesEventIds, today, windowEnd, isWorkTask, now).ConfigureAwait(false);

        var changedCount = seriesChanges + singleChanges;
        if (changedCount > 0)
        {
            await _taskSeriesService.EnsureTasksAsync().ConfigureAwait(false);
            await _taskCacheService.ReloadAsync().ConfigureAwait(false);
        }

        return CalendarSyncResult.Success(changedCount);
    }

    private static bool IsUntouched(TaskEntity task) =>
        !task.IsDone && task.CompletedAt is null &&
        task.Note == task.LastSyncedNote &&
        task.TaskTime == task.LastSyncedTaskTime &&
        task.TimerMinutes == task.LastSyncedTimerMinutes &&
        task.ReminderMinutesBefore == task.LastSyncedReminderMinutesBefore;

    private static void StampSyncSnapshot(TaskEntity entity)
    {
        entity.LastSyncedNote = entity.Note;
        entity.LastSyncedTaskTime = entity.TaskTime;
        entity.LastSyncedTimerMinutes = entity.TimerMinutes;
        entity.LastSyncedReminderMinutesBefore = entity.ReminderMinutesBefore;
    }

    private async Task<int> SyncRepeatingEventsAsync(
        IReadOnlyList<DeviceCalendarEvent> repeatingEvents,
        IReadOnlyList<TaskEntity> existingTasks,
        bool isWorkTask,
        DateTimeOffset now)
    {
        var today = now.Date;
        var changedCount = 0;
        var deviceEventIds = repeatingEvents.Select(deviceEvent => deviceEvent.EventId).ToHashSet();
        var existingSeriesStarts = existingTasks
            .Where(task => task.SourceCalendarEventId is not null && TaskExtension.IsSeriesStart(TaskExtension.MapToDto(task)))
            .ToDictionary(task => task.SourceCalendarEventId!, task => task, StringComparer.Ordinal);

        var newSeriesStarts = new List<TaskEntity>();

        foreach (var deviceEvent in repeatingEvents)
        {
            if (!CalendarRepeatRuleParser.TryParse(
                    deviceEvent.RawRepeatRule,
                    deviceEvent.SeriesAnchorDate,
                    out var unit,
                    out var interval,
                    out var weekdays,
                    out var weekdayOrdinal,
                    out var month,
                    out var weekStart,
                    out var repeatUntil))
            {

                continue;
            }

            if (!existingSeriesStarts.TryGetValue(deviceEvent.EventId, out var existingStart))
            {
                var nextOccurrence = TaskRepeatCalculator
                    .GetOccurrences(
                        deviceEvent.SeriesAnchorDate,
                        unit,
                        interval,
                        weekdays,
                        today,
                        today.AddYears(AppConstants.RepeatSearchYears),
                        weekdayOrdinal,
                        month,
                        weekStart,
                        repeatUntil)
                    .FirstOrDefault();
                if (nextOccurrence == default)
                {
                    continue;
                }

                var seriesStart = CreateTaskDto(deviceEvent, nextOccurrence, deviceEvent.EventId, isWorkTask, now);
                seriesStart.RepeatUnit = unit;
                seriesStart.RepeatInterval = interval;
                seriesStart.RepeatWeekdays = weekdays;
                seriesStart.RepeatWeekdayOrdinal = weekdayOrdinal;
                seriesStart.RepeatMonth = month;
                seriesStart.RepeatWeekStart = weekStart;
                seriesStart.RepeatEndDate = repeatUntil;

                var entity = TaskExtension.MapToEntity(seriesStart);
                StampSyncSnapshot(entity);
                newSeriesStarts.Add(entity);
                changedCount++;
                continue;
            }

            if (!IsUntouched(existingStart))
            {
                continue;
            }

            var newNote = deviceEvent.Title;
            var newTaskTime = deviceEvent.IsAllDay ? null : deviceEvent.TimeOfDay;
            var newTimerMinutes = deviceEvent.IsAllDay
                ? AllDayTimerMinutes
                : ComputeTimerMinutes(deviceEvent.Duration) ?? existingStart.TimerMinutes;
            var newReminder = ResolveReminderMinutesBefore(deviceEvent);

            var ruleUnchanged = existingStart.RepeatUnit == unit && existingStart.RepeatInterval == interval &&
                existingStart.RepeatWeekdays == weekdays && existingStart.RepeatWeekdayOrdinal == weekdayOrdinal &&
                existingStart.RepeatMonth == month && existingStart.RepeatWeekStart == weekStart &&
                existingStart.RepeatEndDate == repeatUntil;
            var fieldsUnchanged = existingStart.Note == newNote && existingStart.TaskTime == newTaskTime &&
                existingStart.TimerMinutes == newTimerMinutes && existingStart.ReminderMinutesBefore == newReminder;
            if (ruleUnchanged && fieldsUnchanged)
            {
                continue;
            }

            var previousDto = TaskExtension.MapToDto(existingStart);
            var savedDto = TaskExtension.Clone(previousDto);
            savedDto.Note = newNote;
            savedDto.TaskTime = newTaskTime;
            savedDto.TimerMinutes = newTimerMinutes;
            savedDto.ReminderMinutesBefore = newReminder;
            savedDto.RepeatUnit = unit;
            savedDto.RepeatInterval = interval;
            savedDto.RepeatWeekdays = weekdays;
            savedDto.RepeatWeekdayOrdinal = weekdayOrdinal;
            savedDto.RepeatMonth = month;
            savedDto.RepeatWeekStart = weekStart;
            savedDto.RepeatEndDate = repeatUntil;

            await _taskSeriesService.HandleSavedTaskAsync(savedDto, previousDto, TaskSeriesScope.WholeSeries).ConfigureAwait(false);
            await _taskRepository
                .ApplyCalendarSyncUpdateAsync(existingStart.Id, newNote, newTaskTime, newTimerMinutes, newReminder)
                .ConfigureAwait(false);
            changedCount++;
        }

        foreach (var (eventId, seriesStart) in existingSeriesStarts)
        {
            if (deviceEventIds.Contains(eventId))
            {
                continue;
            }

            if (IsUntouched(seriesStart))
            {
                await _taskSeriesService.DeleteTaskAsync(seriesStart.Id, TaskSeriesScope.WholeSeries).ConfigureAwait(false);
            }
            else
            {
                await _taskRepository.DetachFromCalendarSyncAsync(seriesStart.Id).ConfigureAwait(false);
            }

            changedCount++;
        }

        if (newSeriesStarts.Count > 0)
        {
            await _taskRepository.AddRangeAsync(newSeriesStarts).ConfigureAwait(false);
        }

        return changedCount;
    }

    private async Task<int> SyncSingleOccurrencesAsync(
        IReadOnlyList<DeviceCalendarEvent> singleOccurrences,
        IReadOnlyList<TaskEntity> existingTasks,
        HashSet<string> handledAsSeriesEventIds,
        DateTime today,
        DateTime windowEnd,
        bool isWorkTask,
        DateTimeOffset now)
    {
        var changedCount = 0;
        var deviceEventsBySourceId = new Dictionary<string, DeviceCalendarEvent>(StringComparer.Ordinal);
        foreach (var deviceEvent in singleOccurrences)
        {
            if (deviceEvent.RawRepeatRule is not null && handledAsSeriesEventIds.Contains(deviceEvent.EventId))
            {

                continue;
            }

            var sourceId = deviceEvent.RawRepeatRule is null
                ? deviceEvent.EventId
                : $"{deviceEvent.EventId}_{deviceEvent.OccurrenceDate:yyyyMMddHHmm}";
            deviceEventsBySourceId[sourceId] = deviceEvent;
        }

        var existingBySourceId = existingTasks
            .Where(task => task.SourceCalendarEventId is not null && !TaskExtension.IsSeriesStart(TaskExtension.MapToDto(task)))
            .GroupBy(task => task.SourceCalendarEventId!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var newTasks = new List<TaskEntity>();

        foreach (var (sourceId, deviceEvent) in deviceEventsBySourceId)
        {
            if (!existingBySourceId.TryGetValue(sourceId, out var existingTask))
            {
                var task = CreateTaskDto(deviceEvent, deviceEvent.OccurrenceDate, sourceId, isWorkTask, now);
                var entity = TaskExtension.MapToEntity(task);
                StampSyncSnapshot(entity);
                newTasks.Add(entity);
                changedCount++;
                continue;
            }

            if (!IsUntouched(existingTask))
            {
                continue;
            }

            var newNote = deviceEvent.Title;
            var newTaskTime = deviceEvent.IsAllDay ? null : deviceEvent.TimeOfDay;
            var newTimerMinutes = deviceEvent.IsAllDay
                ? AllDayTimerMinutes
                : ComputeTimerMinutes(deviceEvent.Duration) ?? existingTask.TimerMinutes;
            var newReminder = ResolveReminderMinutesBefore(deviceEvent);

            if (existingTask.Note == newNote && existingTask.TaskTime == newTaskTime &&
                existingTask.TimerMinutes == newTimerMinutes && existingTask.ReminderMinutesBefore == newReminder)
            {
                continue;
            }

            await _taskRepository
                .ApplyCalendarSyncUpdateAsync(existingTask.Id, newNote, newTaskTime, newTimerMinutes, newReminder)
                .ConfigureAwait(false);
            changedCount++;
        }

        foreach (var (sourceId, task) in existingBySourceId)
        {
            if (deviceEventsBySourceId.ContainsKey(sourceId))
            {
                continue;
            }

            if (task.TaskDate is not { } taskDate || taskDate < today || taskDate > windowEnd)
            {

                continue;
            }

            if (IsUntouched(task))
            {
                await _taskRepository.DeleteAsync(task.Id).ConfigureAwait(false);
            }
            else
            {
                await _taskRepository.DetachFromCalendarSyncAsync(task.Id).ConfigureAwait(false);
            }

            changedCount++;
        }

        if (newTasks.Count > 0)
        {
            await _taskRepository.AddRangeAsync(newTasks).ConfigureAwait(false);
        }

        return changedCount;
    }

    private static TaskDto CreateTaskDto(
        DeviceCalendarEvent deviceEvent, DateTime taskDate, string sourceCalendarEventId, bool isWorkTask, DateTimeOffset now)
    {
        var task = TaskExtension.CreateDefaultTask(taskDate, isWorkTask);
        task.Id = Guid.NewGuid();
        task.CreationDate = now.DateTime;
        task.Note = deviceEvent.Title;
        task.SourceCalendarEventId = sourceCalendarEventId;

        if (deviceEvent.IsAllDay)
        {
            task.TaskTime = null;
            task.TimerMinutes = AllDayTimerMinutes;
        }
        else
        {
            task.TaskTime = deviceEvent.TimeOfDay;
            task.TimerMinutes = ComputeTimerMinutes(deviceEvent.Duration) ?? task.TimerMinutes;
            task.ReminderMinutesBefore = ResolveReminderMinutesBefore(deviceEvent);
        }

        return task;
    }

    private static int? ResolveReminderMinutesBefore(DeviceCalendarEvent deviceEvent) =>
        deviceEvent.IsAllDay ? null : deviceEvent.ReminderMinutesBeforeFromDevice ?? 0;

    private static int? ComputeTimerMinutes(TimeSpan? duration)
    {
        if (duration is not { } value)
        {
            return null;
        }

        var minutes = (int)Math.Round(value.TotalMinutes);
        return minutes is >= AppConstants.MinimumMinutes and <= AppConstants.MaximumMinutes ? minutes : null;
    }
}
