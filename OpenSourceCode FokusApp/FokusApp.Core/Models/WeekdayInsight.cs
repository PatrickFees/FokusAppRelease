using FokusApp.Core.Enums;

namespace FokusApp.Core.Models;

public sealed record WeekdayInsight(DayOfWeek Day, WeekdayInsightKind Kind);
