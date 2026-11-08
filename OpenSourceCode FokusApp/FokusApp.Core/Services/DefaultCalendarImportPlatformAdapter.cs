using FokusApp.Core.Interfaces;
using FokusApp.Core.Models;

namespace FokusApp.Core.Services;

public sealed class DefaultCalendarImportPlatformAdapter : ICalendarImportPlatformAdapter
{
    public bool IsAvailable => false;

    public Task<bool> HasPermissionAsync() => Task.FromResult(false);

    public Task<bool> HasPermissionWithoutPromptAsync() => Task.FromResult(false);

    public Task<IReadOnlyList<DeviceCalendarInfo>> GetCalendarsAsync() =>
        throw new NotSupportedException("Calendar import is only available on Android.");

    public Task<IReadOnlyList<DeviceCalendarEvent>> GetRepeatingEventsAsync(IReadOnlyList<string> calendarIds) =>
        throw new NotSupportedException("Calendar import is only available on Android.");

    public Task<IReadOnlyList<DeviceCalendarEvent>> GetSingleOccurrencesAsync(
        IReadOnlyList<string> calendarIds, DateTime from, DateTime to) =>
        throw new NotSupportedException("Calendar import is only available on Android.");
}
