using FokusApp.Core.Models;
using FokusApp.Core.Services;

namespace FocusAppTests.Services;

public class TaskReminderPlannerTests
{
    private static readonly DateTime Now = new(2026, 3, 10, 9, 0, 0);

    [Fact]
    public void Plan_TaskWithTimeAndReminder_ReturnsReminderBeforeTheTime()
    {
        var task = CreateTask(Now.Date, new TimeSpan(14, 30, 0), 30);

        var reminders = TaskReminderPlanner.Plan([task], Now);

        var reminder = Assert.Single(reminders);
        Assert.Equal(task.Id, reminder.TaskId);
        Assert.Equal(new DateTime(2026, 3, 10, 14, 0, 0), reminder.TriggerAt);
        Assert.Equal(new DateTime(2026, 3, 10, 14, 30, 0), reminder.DueAt);
        Assert.Equal(30, reminder.MinutesBefore);
    }

    [Fact]
    public void Plan_ReminderAtTheTime_TriggersAtTheTime()
    {
        var task = CreateTask(Now.Date, new TimeSpan(18, 0, 0), 0);

        var reminders = TaskReminderPlanner.Plan([task], Now);

        var reminder = Assert.Single(reminders);
        Assert.Equal(new DateTime(2026, 3, 10, 18, 0, 0), reminder.TriggerAt);
    }

    [Fact]
    public void Plan_ReminderAlreadyPast_IsSkipped()
    {
        var task = CreateTask(Now.Date, new TimeSpan(9, 15, 0), 30);

        var reminders = TaskReminderPlanner.Plan([task], Now);

        Assert.Empty(reminders);
    }

    [Fact]
    public void Plan_TaskWithoutReminder_IsSkipped()
    {
        var task = CreateTask(Now.Date, new TimeSpan(14, 30, 0), null);

        var reminders = TaskReminderPlanner.Plan([task], Now);

        Assert.Empty(reminders);
    }

    [Fact]
    public void Plan_TaskWithoutTime_IsSkipped()
    {
        var task = CreateTask(Now.Date, null, 10);

        var reminders = TaskReminderPlanner.Plan([task], Now);

        Assert.Empty(reminders);
    }

    [Fact]
    public void Plan_DoneTask_IsSkipped()
    {
        var task = CreateTask(Now.Date, new TimeSpan(14, 30, 0), 10);
        task.IsDone = true;

        var reminders = TaskReminderPlanner.Plan([task], Now);

        Assert.Empty(reminders);
    }

    [Fact]
    public void Plan_Template_IsSkipped()
    {
        var task = CreateTask(null, new TimeSpan(14, 30, 0), 10);
        task.IsTemplate = true;

        var reminders = TaskReminderPlanner.Plan([task], Now);

        Assert.Empty(reminders);
    }

    [Fact]
    public void Plan_ReminderBeyondPlanningDays_IsSkipped()
    {
        var task = CreateTask(Now.Date.AddDays(TaskReminderPlanner.PlanningDays + 1), new TimeSpan(12, 0, 0), 0);

        var reminders = TaskReminderPlanner.Plan([task], Now);

        Assert.Empty(reminders);
    }

    [Fact]
    public void Plan_SeveralTasks_AreSortedByTriggerTime()
    {
        var late = CreateTask(Now.Date, new TimeSpan(20, 0, 0), 0);
        var early = CreateTask(Now.Date, new TimeSpan(11, 0, 0), 0);
        var tomorrow = CreateTask(Now.Date.AddDays(1), new TimeSpan(8, 0, 0), 0);

        var reminders = TaskReminderPlanner.Plan([late, tomorrow, early], Now);

        var taskIds = reminders.Select(reminder => reminder.TaskId).ToList();
        Assert.Equal([early.Id!.Value, late.Id!.Value, tomorrow.Id!.Value], taskIds);
    }

    [Fact]
    public void Plan_MoreThanTheMaximum_KeepsTheSoonest()
    {
        var tasks = new List<TaskDto>();
        for (var minute = 1; minute <= TaskReminderPlanner.MaximumReminders + 5; minute++)
        {
            var time = TimeSpan.FromMinutes(9 * 60 + minute);
            tasks.Add(CreateTask(Now.Date, time, 0));
        }

        var reminders = TaskReminderPlanner.Plan(tasks, Now);

        Assert.Equal(TaskReminderPlanner.MaximumReminders, reminders.Count);
        Assert.Equal(new DateTime(2026, 3, 10, 9, 1, 0), reminders[0].TriggerAt);
    }

    private static TaskDto CreateTask(DateTime? date, TimeSpan? time, int? reminderMinutes)
    {
        return new TaskDto
        {
            Id = Guid.NewGuid(),
            Note = "Aufgabe",
            TaskDate = date,
            TaskTime = time,
            ReminderMinutesBefore = reminderMinutes
        };
    }
}
