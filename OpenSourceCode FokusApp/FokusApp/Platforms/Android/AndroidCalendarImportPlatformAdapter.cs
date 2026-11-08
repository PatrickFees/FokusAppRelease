using Android.Provider;
using FokusApp.Core;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Models;

namespace FokusApp.Platforms.Android;

public sealed class AndroidCalendarImportPlatformAdapter : ICalendarImportPlatformAdapter
{
    private static readonly string[] CalendarColumns = ["_id", "calendar_displayName", "account_name", "account_type"];

    private const string LocalAccountType = "LOCAL";
    private static readonly string[] EventColumns = ["_id", "title", "rrule", "dtstart", "allDay", "duration", "calendar_id", "dtend"];
    private static readonly string[] InstanceColumns = ["event_id", "begin", "calendar_id"];
    private static readonly string[] ReminderColumns = ["minutes"];

    public bool IsAvailable => true;

    public Task<bool> HasPermissionAsync() => CalendarPermissionHelper.EnsureReadCalendarPermissionAsync();

    public Task<bool> HasPermissionWithoutPromptAsync() => CalendarPermissionHelper.CheckReadCalendarPermissionAsync();

    public Task<IReadOnlyList<DeviceCalendarInfo>> GetCalendarsAsync()
    {
        var result = new List<DeviceCalendarInfo>();
        var resolver = global::Android.App.Application.Context!.ContentResolver!;

        using var cursor = resolver.Query(CalendarContract.Calendars.ContentUri!, CalendarColumns, null, null, null);
        while (cursor is not null && cursor.MoveToNext())
        {
            result.Add(new DeviceCalendarInfo(
                cursor.GetLong(0).ToString(),
                cursor.GetString(1) ?? string.Empty,
                cursor.GetString(2) ?? string.Empty,
                string.Equals(cursor.GetString(3), LocalAccountType, StringComparison.OrdinalIgnoreCase)));
        }

        return Task.FromResult<IReadOnlyList<DeviceCalendarInfo>>(result);
    }

    public Task<IReadOnlyList<DeviceCalendarEvent>> GetRepeatingEventsAsync(IReadOnlyList<string> calendarIds)
    {
        if (calendarIds.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<DeviceCalendarEvent>>([]);
        }

        var placeholders = string.Join(",", calendarIds.Select(_ => "?"));
        var selection = $"calendar_id IN ({placeholders}) AND rrule IS NOT NULL";
        var events = QueryEvents(selection, calendarIds.ToArray());
        return Task.FromResult<IReadOnlyList<DeviceCalendarEvent>>(events);
    }

    public Task<IReadOnlyList<DeviceCalendarEvent>> GetSingleOccurrencesAsync(
        IReadOnlyList<string> calendarIds, DateTime from, DateTime to)
    {
        if (calendarIds.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<DeviceCalendarEvent>>([]);
        }

        var resolver = global::Android.App.Application.Context!.ContentResolver!;
        var fromMillis = ToUtcMillis(from.Date);
        var toMillis = ToUtcMillis(to.Date.AddDays(1));
        var calendarIdSet = calendarIds.ToHashSet();

        var occurrencesByEventId = new Dictionary<string, List<DateTime>>();
        using (var cursor = CalendarContract.Instances.Query(resolver, InstanceColumns, fromMillis, toMillis))
        {
            while (cursor is not null && cursor.MoveToNext())
            {
                var calendarId = cursor.GetLong(2).ToString();
                if (!calendarIdSet.Contains(calendarId))
                {
                    continue;
                }

                var eventId = cursor.GetLong(0).ToString();
                var occurrenceLocal = FromUtcMillis(cursor.GetLong(1));
                if (!occurrencesByEventId.TryGetValue(eventId, out var occurrences))
                {
                    occurrences = [];
                    occurrencesByEventId[eventId] = occurrences;
                }

                occurrences.Add(occurrenceLocal);
            }
        }

        if (occurrencesByEventId.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<DeviceCalendarEvent>>([]);
        }

        var eventIds = occurrencesByEventId.Keys.ToArray();
        var placeholders = string.Join(",", eventIds.Select(_ => "?"));
        var baseEvents = QueryEvents($"_id IN ({placeholders})", eventIds)
            .ToDictionary(deviceEvent => deviceEvent.EventId);

        var result = new List<DeviceCalendarEvent>();
        foreach (var (eventId, occurrences) in occurrencesByEventId)
        {
            if (!baseEvents.TryGetValue(eventId, out var baseEvent))
            {
                continue;
            }

            foreach (var occurrence in occurrences)
            {
                result.Add(baseEvent with
                {
                    OccurrenceDate = occurrence,
                    TimeOfDay = baseEvent.IsAllDay ? null : occurrence.TimeOfDay
                });
            }
        }

        return Task.FromResult<IReadOnlyList<DeviceCalendarEvent>>(result);
    }

    private static List<DeviceCalendarEvent> QueryEvents(string selection, string[] selectionArgs)
    {
        var result = new List<DeviceCalendarEvent>();
        var resolver = global::Android.App.Application.Context!.ContentResolver!;

        using var cursor = resolver.Query(CalendarContract.Events.ContentUri!, EventColumns, selection, selectionArgs, null);
        while (cursor is not null && cursor.MoveToNext())
        {
            var eventId = cursor.GetLong(0).ToString();
            var title = cursor.GetString(1) ?? string.Empty;
            var rrule = cursor.IsNull(2) ? null : cursor.GetString(2);
            var startLocal = FromUtcMillis(cursor.GetLong(3));
            var isAllDay = cursor.GetInt(4) != 0;
            var calendarId = cursor.GetLong(6).ToString();
            var durationMinutes = cursor.IsNull(7)
                ? ParseDurationMinutes(cursor.IsNull(5) ? null : cursor.GetString(5))
                : (int?)Math.Max(0, (int)Math.Round((FromUtcMillis(cursor.GetLong(7)) - startLocal).TotalMinutes));
            var reminderMinutes = GetEarliestReminderMinutes(resolver, eventId);

            result.Add(new DeviceCalendarEvent(
                eventId,
                calendarId,
                title,
                startLocal,
                isAllDay ? null : startLocal.TimeOfDay,
                durationMinutes is { } minutes ? TimeSpan.FromMinutes(minutes) : null,
                isAllDay,
                rrule,
                startLocal,
                reminderMinutes));
        }

        return result;
    }

    private static int? GetEarliestReminderMinutes(global::Android.Content.ContentResolver resolver, string eventId)
    {
        using var cursor = resolver.Query(
            CalendarContract.Reminders.ContentUri!, ReminderColumns, "event_id = ?", [eventId], null);

        int? earliest = null;
        while (cursor is not null && cursor.MoveToNext())
        {
            var minutes = cursor.GetInt(0);
            if (earliest is null || minutes > earliest)
            {
                earliest = minutes;
            }
        }

        return earliest is { } value ? AppConstants.ReminderMinuteOptions
            .OrderBy(option => Math.Abs(option - value))
            .First() : null;
    }

    private static int? ParseDurationMinutes(string? duration)
    {
        if (string.IsNullOrWhiteSpace(duration) || !duration.StartsWith('P'))
        {
            return null;
        }

        var timePart = duration.Contains('T') ? duration[(duration.IndexOf('T') + 1)..] : null;
        var datePart = duration[1..(duration.Contains('T') ? duration.IndexOf('T') : duration.Length)];

        var totalMinutes = 0;
        if (datePart.EndsWith('D') && int.TryParse(datePart[..^1], out var days))
        {
            totalMinutes += days * 24 * 60;
        }

        if (timePart is not null)
        {
            if (timePart.Contains('H') && int.TryParse(timePart[..timePart.IndexOf('H')], out var hours))
            {
                totalMinutes += hours * 60;
            }

            var minuteIndex = timePart.IndexOf('M');
            if (minuteIndex > 0)
            {
                var minuteStart = timePart.Contains('H') ? timePart.IndexOf('H') + 1 : 0;
                if (int.TryParse(timePart[minuteStart..minuteIndex], out var minutes))
                {
                    totalMinutes += minutes;
                }
            }
        }

        return totalMinutes > 0 ? totalMinutes : null;
    }

    private static long ToUtcMillis(DateTime localDate) =>
        new DateTimeOffset(DateTime.SpecifyKind(localDate, DateTimeKind.Local)).ToUnixTimeMilliseconds();

    private static DateTime FromUtcMillis(long millis) =>
        DateTimeOffset.FromUnixTimeMilliseconds(millis).ToLocalTime().DateTime;
}
