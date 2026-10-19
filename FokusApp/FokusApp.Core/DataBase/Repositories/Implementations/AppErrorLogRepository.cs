using FokusApp.Core.DataBase.ErrorLogging;
using FokusApp.Core.DataBase.ErrorLogging.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FokusApp.Core.DataBase.Repositories.Implementations;

public class AppErrorLogRepository : IAppErrorLogRepository
{
    private readonly IDbContextFactory<AppErrorDbContext> _contextFactory;

    public AppErrorLogRepository(IDbContextFactory<AppErrorDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task AddAsync(AppErrorLogEntity errorLog)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await context.AppErrorLogs.AddAsync(errorLog).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<List<AppErrorLogEntity>> GetLatestAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.AppErrorLogs
            .AsNoTracking()
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .ThenByDescending(entry => entry.Id)
            .ToListAsync()
            .ConfigureAwait(false);
    }
}
