using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using Microsoft.Extensions.Time.Testing;

namespace FocusAppTests.ViewModels;

public static class TestTasks
{
    public static readonly DateTime Day = new(2026, 3, 10);

    public static readonly DateTime Today = new(2026, 3, 15);

    public static FakeTimeProvider CreateClock()
    {
        var now = new DateTimeOffset(Today.AddHours(9).AddMinutes(30), TimeSpan.Zero);
        return new FakeTimeProvider(now);
    }

    public static TaskDto Create(
        string note = "Aufgabe",
        bool isTemplate = false,
        bool isDone = false,
        bool isPositive = false,
        DateTime? taskDate = null,
        TaskPriority priority = TaskPriority.Keine,
        int timerMinutes = 25,
        int breakMinutes = 7,
        bool isWork = true)
    {
        var date = taskDate;
        if (date is null && !isTemplate)
        {
            date = Day;
        }

        return new TaskDto
        {
            Id = Guid.NewGuid(),
            Note = note,
            IsTemplate = isTemplate,
            IsDone = isDone,
            IsPositive = isPositive,
            IsWorkTask = isWork,
            TaskDate = date,
            Priority = priority,
            TimerMinutes = timerMinutes,
            BreakMinutes = breakMinutes,
            CreationDate = Day.AddHours(8)
        };
    }
}
