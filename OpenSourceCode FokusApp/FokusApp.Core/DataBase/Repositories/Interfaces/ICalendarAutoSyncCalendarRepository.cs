using FokusApp.Core.DataBase.Entities;

namespace FokusApp.Core.DataBase.Repositories.Interfaces;

public interface ICalendarAutoSyncCalendarRepository
{

    Task<List<CalendarAutoSyncCalendarEntity>> GetAllAsync();

    Task ReplaceAllAsync(IReadOnlyList<string> calendarIds);
}
