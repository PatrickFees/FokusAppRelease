namespace FokusApp.Core.Models;

public sealed record WeekBalance(
    DateTime WeekStart,
    IReadOnlyList<DayBalance> Days,
    int BalanceDays,
    int GoalDays,
    int DoneForYou,
    int DoneToDo,
    int FocusMinutes,
    DateTime? BestDay)
{
    public int DoneTotal => DoneForYou + DoneToDo;

    public double? ForYouShare => DoneTotal == 0 ? null : (double)DoneForYou / DoneTotal;
}
