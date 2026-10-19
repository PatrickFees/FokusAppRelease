using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FokusApp.Core.DataBase.Repositories.Implementations;

public sealed class DataImportRepository : IDataImportRepository
{
    private readonly IDbContextFactory<FokusAppDbContext> _contextFactory;

    public DataImportRepository(IDbContextFactory<FokusAppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task ReplaceAllAsync(
        IReadOnlyCollection<RingtonesEntity> ringtones,
        IReadOnlyCollection<SettingEntity> settings,
        IReadOnlyCollection<TaskEntity> tasks,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ringtones);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(tasks);

        await using var context = await _contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = await context.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await context.Tasks.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await context.Settings.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await context.Ringtones.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

            await context.Ringtones.AddRangeAsync(ringtones, cancellationToken).ConfigureAwait(false);
            await context.Settings.AddRangeAsync(settings, cancellationToken).ConfigureAwait(false);
            await context.Tasks.AddRangeAsync(tasks, cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }
}
