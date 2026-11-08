namespace FokusApp.Core.Services.Interfaces;

public interface IChangelogSettingsService
{
    string? LastSeenChangelogVersion { get; }

    Task SetLastSeenChangelogVersionAsync(string version);
}
