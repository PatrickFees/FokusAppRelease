using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FokusApp.Core.DataBase.Repositories.Implementations;

public sealed class SettingRepository : ISettingRepository
{
    private readonly IDbContextFactory<FokusAppDbContext> _contextFactory;

    public SettingRepository(IDbContextFactory<FokusAppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<bool> AnySettingExistsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.Settings.AsNoTracking().AnyAsync().ConfigureAwait(false);
    }

    public async Task<List<SettingEntity>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.Settings
            .AsNoTracking()
            .Include(setting => setting.Ringtone)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task AddAsync(SettingEntity setting)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await context.Settings.AddAsync(CreatePersistenceCopy(setting)).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task AddRangeAsync(IEnumerable<SettingEntity> settings)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await context.Settings
            .AddRangeAsync(settings.Select(CreatePersistenceCopy))
            .ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(SettingEntity setting)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var existingSetting = await context.Settings
            .FirstOrDefaultAsync(item => item.Id == setting.Id)
            .ConfigureAwait(false);

        if (existingSetting is null)
        {
            throw new InvalidOperationException($"Setting with id '{setting.Id}' does not exist.");
        }

        existingSetting.Theme = setting.Theme;
        existingSetting.Language = setting.Language;
        existingSetting.RingtoneId = setting.RingtoneId;
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await context.Settings.ExecuteDeleteAsync().ConfigureAwait(false);
    }

    private static SettingEntity CreatePersistenceCopy(SettingEntity setting) => new()
    {
        Id = setting.Id,
        Theme = setting.Theme,
        Language = setting.Language,
        RingtoneId = setting.RingtoneId,
        Ringtone = null!
    };
}
