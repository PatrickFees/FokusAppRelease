namespace FokusApp.Core.Models;

public sealed record WeekdayBalance(
    DayOfWeek Day,
    int PlannedForYou,
    int PlannedToDo,
    int DoneForYou,
    int DoneToDo);
