namespace FokusApp.Core.Services.Interfaces;

public interface ICompanionMoodService
{

    Task<CompanionMood> GetRecentMoodAsync();
}
