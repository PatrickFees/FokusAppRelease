namespace FokusApp.Core.Models;

public sealed record DeviceCalendarEvent(
    string EventId,
    string CalendarId,
    string Title,
    DateTime OccurrenceDate,
    TimeSpan? TimeOfDay,
    TimeSpan? Duration,
    bool IsAllDay,
    string? RawRepeatRule,
    DateTime SeriesAnchorDate,
    int? ReminderMinutesBeforeFromDevice);
