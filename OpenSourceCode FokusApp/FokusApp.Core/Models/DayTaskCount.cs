namespace FokusApp.Core.Models;

public sealed record DayTaskCount(DateTime Date, int ForYou, int ToDo, int DoneForYou = 0, int DoneToDo = 0)
{
    public int Total => ForYou + ToDo;
}
