using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using Microsoft.Extensions.Time.Testing;

namespace FocusAppTests.ViewModels;

public static class TestTasks
{
    public static readonly DateTime Day = new(2026, 3, 10);

    /// <summary>Fester "heute"-Tag der Tests, bewusst verschieden von <see cref="Day"/>.</summary>
    public static readonly DateTime Today = new(2026, 3, 15);

    public static FakeTimeProvider CreateClock()
    {
        var now = new DateTimeOffset(Today.AddHours(9).AddMinutes(30), TimeSpan.Zero);
        return new FakeTimeProvider(now);
    }

    public static TaskDto Create(
        string note = "Aufgabe",
        bool isRegular = false,
        bool isDone = false,
        bool isPositive = false,
        DateTime? taskDate = null,
        TaskPriority priority = TaskPriority.Keine,
        int timerMinutes = 25,
        int breakMinutes = 7,
        bool isWork = true)
    {
        var date = taskDate;
        if (date is null && !isRegular)
        {
            date = Day;
        }

        return new TaskDto
        {
            Id = Guid.NewGuid(),
            Note = note,
            IsRegularTask = isRegular,
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
