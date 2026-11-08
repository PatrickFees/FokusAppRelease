using System.Globalization;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Services;

namespace FokusApp.Components.DayPlanner;

public static class DayPlannerInsightFormatter
{
    public static DayPlannerInsight? Build(WeekdayAnalysis analysis, DateTime today, IUiTextProvider textProvider)
    {
        var insight = analysis.Insights.FirstOrDefault(item => item.Day == today.DayOfWeek);
        if (insight is null)
        {
            return null;
        }

        var key = insight.Kind == WeekdayInsightKind.MostlyDuties ? "WeekdayInsightMostlyDuties" : "WeekdayInsightRarelyDone";
        return new DayPlannerInsight(textProvider.GetText(key, CultureInfo.CurrentUICulture.DateTimeFormat.GetDayName(today.DayOfWeek)));
    }
}
