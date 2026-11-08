using FokusApp.Core.Models;

namespace FokusApp.Core.Interfaces;

public interface ICalendarImportPlatformAdapter
{

    bool IsAvailable { get; }

    Task<bool> HasPermissionAsync();

    Task<bool> HasPermissionWithoutPromptAsync();

    Task<IReadOnlyList<DeviceCalendarInfo>> GetCalendarsAsync();

    Task<IReadOnlyList<DeviceCalendarEvent>> GetRepeatingEventsAsync(IReadOnlyList<string> calendarIds);

    Task<IReadOnlyList<DeviceCalendarEvent>> GetSingleOccurrencesAsync(
        IReadOnlyList<string> calendarIds, DateTime from, DateTime to);
}
