using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FokusApp.Core.DataBase.Repositories.Implementations;

public sealed class CalendarAutoSyncCalendarRepository : ICalendarAutoSyncCalendarRepository
{
    private readonly IDbContextFactory<FokusAppDbContext> _contextFactory;

    public CalendarAutoSyncCalendarRepository(IDbContextFactory<FokusAppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<CalendarAutoSyncCalendarEntity>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.CalendarAutoSyncCalendars
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task ReplaceAllAsync(IReadOnlyList<string> calendarIds)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await context.CalendarAutoSyncCalendars.ExecuteDeleteAsync().ConfigureAwait(false);

        if (calendarIds.Count == 0)
        {
            return;
        }

        await context.CalendarAutoSyncCalendars
            .AddRangeAsync(calendarIds.Select(calendarId => new CalendarAutoSyncCalendarEntity
            {
                Id = Guid.NewGuid(),
                CalendarId = calendarId
            }))
            .ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}
