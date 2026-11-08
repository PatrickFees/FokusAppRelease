using FokusApp.Core.Models;

namespace FokusApp.Core.Services;

public static class TaskReminderPlanner
{
    public const int PlanningDays = 14;
    public const int MaximumReminders = 60;

    public static IReadOnlyList<TaskReminder> Plan(IEnumerable<TaskDto> tasks, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        var latest = now.AddDays(PlanningDays);

        return tasks
            .Where(task =>
                task is { Id: not null, IsDone: false, IsTemplate: false } &&
                task.TaskDate.HasValue &&
                task.TaskTime.HasValue &&
                task.ReminderMinutesBefore.HasValue)
            .Select(task =>
            {
                var dueAt = task.TaskDate!.Value.Date + task.TaskTime!.Value;
                var minutesBefore = task.ReminderMinutesBefore!.Value;
                return new TaskReminder(
                    task.Id!.Value,
                    task.Note?.Trim() ?? string.Empty,
                    dueAt.AddMinutes(-minutesBefore),
                    dueAt,
                    minutesBefore);
            })
            .Where(reminder => reminder.TriggerAt > now && reminder.TriggerAt <= latest)
            .OrderBy(reminder => reminder.TriggerAt)
            .Take(MaximumReminders)
            .ToList();
    }
}
