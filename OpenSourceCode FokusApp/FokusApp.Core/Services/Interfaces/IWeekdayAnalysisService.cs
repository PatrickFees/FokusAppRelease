using FokusApp.Core.Models;

namespace FokusApp.Core.Services.Interfaces;

public interface IWeekdayAnalysisService
{

    Task<WeekdayAnalysis> AnalyzeAsync(DateTime referenceDay);
}
