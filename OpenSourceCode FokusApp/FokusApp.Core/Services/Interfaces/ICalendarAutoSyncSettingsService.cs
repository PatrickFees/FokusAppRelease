namespace FokusApp.Core.Services.Interfaces;

public interface ICalendarAutoSyncSettingsService
{
    bool Enabled { get; }

    DateTime? LastRunAt { get; }

    Task<IReadOnlyList<string>> GetSelectedCalendarIdsAsync();

    Task SetEnabledAsync(bool enabled);

    Task SetSelectedCalendarIdsAsync(IReadOnlyList<string> calendarIds);

    Task RecordRunAsync(DateTime runAt);
}
