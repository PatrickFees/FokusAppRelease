using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Core.Services;

namespace FokusApp.Core.Extensions;

public static class TaskExtension
{
    public static TaskDto CreateDefaultTask(DateTime taskDate, bool isWorkScope = true)
    {
        return new TaskDto
        {
            Note = string.Empty,
            Priority = TaskPriority.Keine,
            TimerMinutes = 0,
            BreakMinutes = 5,
            TaskDate = taskDate.Date,
            IsTemplate = false,
            IsWorkTask = isWorkScope
        };
    }

    public static TaskDto CreateDefaultActiveTask(bool isWorkScope, DateTime today)
    {
        return new TaskDto
        {
            Id = null,
            Note = string.Empty,
            IsWorkTask = isWorkScope,
            TaskDate = today.Date
        };
    }

    public static bool IsPastOpenTask(TaskDto task, DateTime today)
    {
        ArgumentNullException.ThrowIfNull(task);

        return !task.IsDone && !task.IsTemplate && (task.TaskDate ?? task.CreationDate).Date < today.Date;
    }

    public static DateTime GetNextWeekendDay(DateTime today)
    {
        var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;
        return today.Date.AddDays(daysUntilSaturday == 0 ? 7 : daysUntilSaturday);
    }

    public static DateTime ResolvePastTaskTargetDate(DateTime today, PastTaskTarget target) => target switch
    {
        PastTaskTarget.Today => today,
        PastTaskTarget.Tomorrow => today.AddDays(1),
        PastTaskTarget.Weekend => GetNextWeekendDay(today),
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
    };

    public static DateTime? GetEffectiveCompletedDate(TaskDto task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (!task.IsDone)
        {
            return null;
        }

        return (task.CompletedAt ?? task.TaskDate ?? task.CreationDate).Date;
    }

    public static TaskDto Clone(TaskDto task)
    {
        ArgumentNullException.ThrowIfNull(task);

        var clone = new TaskDto { Id = task.Id };
        CopyFields(task, clone);
        clone.TaskDate = task.TaskDate?.Date;
        return clone;
    }

    public static TaskDto NormalizeTaskForPersistence(TaskDto task)
    {
        var normalizedTask = Clone(task);
        if (!normalizedTask.IsDone)
        {
            normalizedTask.CompletedAt = null;
        }

        normalizedTask.TaskDate = normalizedTask.IsTemplate
            ? null
            : normalizedTask.TaskDate?.Date;
        NormalizeSchedule(normalizedTask);
        return normalizedTask;
    }

    public static bool IsSeriesStart(TaskDto task)
    {
        ArgumentNullException.ThrowIfNull(task);

        return task.RepeatUnit != TaskRepeatUnit.None &&
            task.SeriesId is null &&
            !task.IsTemplate &&
            task.TaskDate.HasValue;
    }

    public static Guid? GetSeriesId(TaskDto task)
    {
        ArgumentNullException.ThrowIfNull(task);

        return task.SeriesId ?? (IsSeriesStart(task) ? task.Id : null);
    }

    public static TaskDto CreateSeriesTask(TaskDto seriesStart, DateTime day, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(seriesStart);

        var seriesTask = Clone(seriesStart);
        seriesTask.Id = Guid.NewGuid();
        seriesTask.IsDone = false;
        seriesTask.CompletedAt = null;
        seriesTask.CreationDate = now;
        seriesTask.TaskDate = day.Date;
        seriesTask.SeriesId = seriesStart.Id;
        seriesTask.RepeatGeneratedUntil = null;
        return seriesTask;
    }

    private static void NormalizeSchedule(TaskDto task)
    {
        task.TaskTime = NormalizeTaskTime(task.TaskTime);
        task.ReminderMinutesBefore = task.TaskTime.HasValue &&
            task.ReminderMinutesBefore is >= 0 and <= AppConstants.MaximumReminderMinutes
            ? task.ReminderMinutesBefore
            : null;

        if (task.IsTemplate)
        {
            task.SeriesId = null;
        }

        if (task.IsTemplate || task.RepeatUnit == TaskRepeatUnit.None || !Enum.IsDefined(task.RepeatUnit))
        {
            task.RepeatUnit = TaskRepeatUnit.None;
            task.RepeatInterval = 1;
            task.RepeatWeekdays = TaskWeekdays.None;
            task.RepeatGeneratedUntil = null;
            task.RepeatWeekdayOrdinal = null;
            task.RepeatMonth = null;
            task.RepeatWeekStart = null;
            task.RepeatEndDate = null;
            return;
        }

        task.RepeatInterval = TaskRepeatCalculator.NormalizeInterval(task.RepeatInterval);
        task.RepeatGeneratedUntil = task.SeriesId is null ? task.RepeatGeneratedUntil?.Date : null;
        task.RepeatEndDate = NormalizeRepeatEndDate(task.RepeatEndDate, task.TaskDate);

        switch (task.RepeatUnit)
        {
            case TaskRepeatUnit.Week:
                task.RepeatWeekdays = NormalizeWeekdays(task.RepeatWeekdays, task.TaskDate);
                task.RepeatWeekdayOrdinal = null;
                task.RepeatMonth = null;
                task.RepeatWeekStart = NormalizeWeekStart(task.RepeatWeekStart);
                break;
            case TaskRepeatUnit.MonthWeekday:
                task.RepeatWeekdays = NormalizeSingleWeekday(task.RepeatWeekdays, task.TaskDate);
                task.RepeatWeekdayOrdinal = NormalizeWeekdayOrdinal(task.RepeatWeekdayOrdinal);
                task.RepeatMonth = null;
                task.RepeatWeekStart = null;
                break;
            case TaskRepeatUnit.YearWeekday:
                task.RepeatWeekdays = NormalizeSingleWeekday(task.RepeatWeekdays, task.TaskDate);
                task.RepeatWeekdayOrdinal = NormalizeWeekdayOrdinal(task.RepeatWeekdayOrdinal);
                task.RepeatMonth = task.RepeatMonth is >= 1 and <= 12
                    ? task.RepeatMonth
                    : (task.TaskDate ?? DateTime.MinValue).Month;
                task.RepeatWeekStart = null;
                break;
            default:
                task.RepeatWeekdays = TaskWeekdays.None;
                task.RepeatWeekdayOrdinal = null;
                task.RepeatMonth = null;
                task.RepeatWeekStart = null;
                break;
        }
    }

    private static TimeSpan? NormalizeTaskTime(TimeSpan? time)
    {
        if (time is not { } value || value < TimeSpan.Zero || value >= TimeSpan.FromDays(1))
        {
            return null;
        }

        return TimeSpan.FromMinutes(Math.Floor(value.TotalMinutes));
    }

    private static TaskWeekdays NormalizeWeekdays(TaskWeekdays weekdays, DateTime? taskDate)
    {
        var selected = weekdays & TaskWeekdays.All;
        return selected != TaskWeekdays.None
            ? selected
            : TaskRepeatCalculator.ToWeekdays((taskDate ?? DateTime.MinValue).DayOfWeek);
    }

    private static TaskWeekdays NormalizeSingleWeekday(TaskWeekdays weekdays, DateTime? taskDate) =>
        TaskRepeatCalculator.ToSingleWeekday(weekdays)
            ?? TaskRepeatCalculator.ToWeekdays((taskDate ?? DateTime.MinValue).DayOfWeek);

    private static TaskWeekdays? NormalizeWeekStart(TaskWeekdays? weekStart)
    {
        if (weekStart is not { } value)
        {
            return null;
        }

        return TaskRepeatCalculator.ToSingleWeekday(value);
    }

    private static int NormalizeWeekdayOrdinal(int? ordinal) =>
        ordinal.HasValue && TaskRepeatCalculator.IsValidWeekdayOrdinal(ordinal.Value) ? ordinal.Value : 1;

    private static DateTime? NormalizeRepeatEndDate(DateTime? endDate, DateTime? taskDate) =>
        endDate?.Date is { } end && taskDate?.Date is { } start && end >= start ? end : null;

    public static TaskDto MapToDto(TaskEntity entity)
    {
        var dto = new TaskDto { Id = entity.Id };
        CopyFields(entity, dto);
        return dto;
    }

    public static TaskEntity MapToEntity(TaskDto source)
    {
        var dto = NormalizeTaskForPersistence(source);

        var entity = new TaskEntity { Id = RequireTaskId(dto) };
        CopyFields(dto, entity);
        return entity;
    }

    public static TaskCategory GetCategory(TaskDto task)
    {
        if (task.IsDone)
        {
            return TaskCategory.Done;
        }

        return task.IsTemplate ? TaskCategory.Template : TaskCategory.Normal;
    }

    public static void UpdateFrom(this TaskEntity target, TaskEntity source)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        CopyFields(source, target);
    }

    private static void CopyFields(ITaskFields source, ITaskFields target)
    {
        target.IsDone = source.IsDone;
        target.Note = source.Note;
        target.IsTemplate = source.IsTemplate;
        target.IsWorkTask = source.IsWorkTask;
        target.IsPositive = source.IsPositive;
        target.CreationDate = source.CreationDate;
        target.TaskDate = source.TaskDate;
        target.CompletedAt = source.CompletedAt;
        target.Priority = source.Priority;
        target.BreakMinutes = source.BreakMinutes;
        target.TimerMinutes = source.TimerMinutes;
        target.TaskTime = source.TaskTime;
        target.ReminderMinutesBefore = source.ReminderMinutesBefore;
        target.RepeatUnit = source.RepeatUnit;
        target.RepeatInterval = source.RepeatInterval;
        target.RepeatWeekdays = source.RepeatWeekdays;
        target.SeriesId = source.SeriesId;
        target.RepeatGeneratedUntil = source.RepeatGeneratedUntil;
        target.RepeatWeekdayOrdinal = source.RepeatWeekdayOrdinal;
        target.RepeatMonth = source.RepeatMonth;
        target.RepeatWeekStart = source.RepeatWeekStart;
        target.RepeatEndDate = source.RepeatEndDate;
        target.SourceCalendarEventId = source.SourceCalendarEventId;
    }

    private static Guid RequireTaskId(TaskDto task)
    {
        return NormalizeId(task.Id)
            ?? throw new InvalidOperationException("Die Aufgabe besitzt keine persistierbare ID.");
    }

    private static Guid? NormalizeId(Guid? id) => id is null || id == Guid.Empty ? null : id;
}
