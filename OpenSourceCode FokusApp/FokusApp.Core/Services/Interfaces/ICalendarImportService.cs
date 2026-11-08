using FokusApp.Core.Models;

namespace FokusApp.Core.Services.Interfaces;

public interface ICalendarImportService
{

    bool IsAvailable { get; }

    Task<IReadOnlyList<DeviceCalendarInfo>> GetAvailableCalendarsAsync();

    Task<CalendarImportResult> ImportAsync(
        IReadOnlyList<string> calendarIds, DateTime from, DateTime to, bool isWorkTask);

    Task<CalendarSyncResult> SyncAsync(IReadOnlyList<string> calendarIds, bool isWorkTask);
}

public sealed record CalendarSyncResult(bool IsSuccess, int ChangedCount)
{
    public static CalendarSyncResult Success(int changedCount) => new(true, changedCount);
    public static CalendarSyncResult Failure() => new(false, 0);
}

public sealed record CalendarImportResult(
    bool IsSuccess,
    int ImportedCount,
    int DuplicateCount,
    int SkippedCount,
    string Message)
{
    public static CalendarImportResult Success(int importedCount, int duplicateCount, int skippedCount, string message)
        => new(true, importedCount, duplicateCount, skippedCount, message);

    public static CalendarImportResult Failure(string message) => new(false, 0, 0, 0, message);
}
