using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FokusApp.Core.DataBase.Repositories.Implementations;

public sealed class RingtonesRepository : IRingtonesRepository
{
    private readonly IDbContextFactory<FokusAppDbContext> _contextFactory;

    public RingtonesRepository(IDbContextFactory<FokusAppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<RingtonesEntity>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.Ringtones
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task AddAsync(RingtonesEntity ringtone)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var exists = await context.Ringtones
            .AnyAsync(item => item.RingtoneFilePath == ringtone.RingtoneFilePath)
            .ConfigureAwait(false);

        if (exists)
        {
            return;
        }

        await context.Ringtones.AddAsync(ringtone).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task AddRangeAsync(IEnumerable<RingtonesEntity> ringtones)
    {
        var ringtoneList = ringtones.ToList();
        if (ringtoneList.Count == 0)
        {
            return;
        }

        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);

        var filePaths = ringtoneList
            .Select(ringtone => ringtone.RingtoneFilePath)
            .Where(path => path is not null)
            .ToList();

        var existingPaths = await context.Ringtones
            .Where(ringtone => filePaths.Contains(ringtone.RingtoneFilePath))
            .Select(ringtone => ringtone.RingtoneFilePath)
            .ToListAsync()
            .ConfigureAwait(false);

        var newRingtones = ringtoneList
            .Where(ringtone => !existingPaths.Contains(ringtone.RingtoneFilePath))
            .ToList();

        if (newRingtones.Count == 0)
        {
            return;
        }

        await context.Ringtones.AddRangeAsync(newRingtones).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await context.Ringtones.ExecuteDeleteAsync().ConfigureAwait(false);
    }
}
