using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;

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
            IsRegularTask = false,
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

    public static TaskDto Clone(TaskDto task)
    {
        ArgumentNullException.ThrowIfNull(task);

        return new TaskDto
        {
            Id = task.Id,
            IsDone = task.IsDone,
            Note = task.Note,
            IsRegularTask = task.IsRegularTask,
            IsWorkTask = task.IsWorkTask,
            IsPositive = task.IsPositive,
            CreationDate = task.CreationDate,
            TaskDate = task.TaskDate?.Date,
            Priority = task.Priority,
            BreakMinutes = task.BreakMinutes,
            TimerMinutes = task.TimerMinutes
        };
    }

    public static TaskDto NormalizeTaskForPersistence(TaskDto task)
    {
        var normalizedTask = Clone(task);
        normalizedTask.TaskDate = normalizedTask.IsRegularTask
            ? null
            : normalizedTask.TaskDate?.Date;
        return normalizedTask;
    }

    public static TaskDto MapToDto(TaskEntity entity)
    {
        return new TaskDto
        {
            Id = entity.Id,
            IsDone = entity.IsDone,
            Note = entity.Note,
            IsRegularTask = entity.IsRegularTask,
            IsWorkTask = entity.IsWorkTask,
            IsPositive = entity.IsPositive,
            CreationDate = entity.CreationDate,
            TaskDate = entity.TaskDate,
            Priority = entity.Priority,
            BreakMinutes = entity.BreakMinutes,
            TimerMinutes = entity.TimerMinutes
        };
    }

    public static TaskEntity MapToEntity(TaskDto dto)
    {
        return new TaskEntity
        {
            Id = RequireTaskId(dto),
            IsDone = dto.IsDone,
            Note = dto.Note,
            IsRegularTask = dto.IsRegularTask,
            IsWorkTask = dto.IsWorkTask,
            IsPositive = dto.IsPositive,
            CreationDate = dto.CreationDate,
            TaskDate = dto.IsRegularTask ? null : dto.TaskDate?.Date,
            Priority = dto.Priority,
            BreakMinutes = dto.BreakMinutes,
            TimerMinutes = dto.TimerMinutes
        };
    }

    public static TaskCategory GetCategory(TaskDto task)
    {
        if (task.IsDone)
        {
            return TaskCategory.Done;
        }

        return task.IsRegularTask ? TaskCategory.Regular : TaskCategory.Normal;
    }

    public static void UpdateFrom(this TaskEntity target, TaskEntity source)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        target.IsDone = source.IsDone;
        target.Note = source.Note;
        target.IsRegularTask = source.IsRegularTask;
        target.IsWorkTask = source.IsWorkTask;
        target.IsPositive = source.IsPositive;
        target.CreationDate = source.CreationDate;
        target.TaskDate = source.TaskDate;
        target.Priority = source.Priority;
        target.BreakMinutes = source.BreakMinutes;
        target.TimerMinutes = source.TimerMinutes;
    }

    private static Guid RequireTaskId(TaskDto task)
    {
        return NormalizeId(task.Id)
            ?? throw new InvalidOperationException("Die Aufgabe besitzt keine persistierbare ID.");
    }

    private static Guid? NormalizeId(Guid? id) => id is null || id == Guid.Empty ? null : id;
}
