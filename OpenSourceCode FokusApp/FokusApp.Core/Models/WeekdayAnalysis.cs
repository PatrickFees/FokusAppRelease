namespace FokusApp.Core.Models;

public sealed record WeekdayAnalysis(
    IReadOnlyList<WeekdayBalance> Weekdays,
    int WeeksWithData,
    bool HasEnoughData,
    IReadOnlyList<WeekdayInsight> Insights);
