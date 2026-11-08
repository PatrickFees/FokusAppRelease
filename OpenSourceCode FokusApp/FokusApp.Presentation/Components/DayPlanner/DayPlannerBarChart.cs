namespace FokusApp.Components.DayPlanner;

public static class DayPlannerBarChart
{

    public static (int Existing, int PlannedStart, int Planned) GetLayers(int total, int done, int baseline)
    {
        var existing = Math.Min(baseline, total);
        var plannedStart = Math.Max(existing, done);
        return (existing, plannedStart, total - plannedStart);
    }

    public static double BarPercent(int count, int plannedForYou, int plannedToDo, int requiredForYou, int allowedToDo)
    {
        var highest = Math.Max(1, new[] { plannedForYou, plannedToDo, requiredForYou, allowedToDo }.Max());
        return count * 100.0 / highest;
    }
}
