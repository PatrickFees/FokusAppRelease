namespace FokusApp.Core.Models;

public sealed record DayBalance(
    DateTime Date,
    int DoneForYou,
    int DoneToDo,
    int PlannedForYou,
    int PlannedToDo,
    int FocusMinutes,
    bool IsBalanceDay,
    bool MeetsGoal)
{
    public int DoneTotal => DoneForYou + DoneToDo;
}
