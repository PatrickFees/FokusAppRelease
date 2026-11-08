using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FokusApp.Core.DataBase.Repositories.Implementations;

public sealed class ForYouSuggestionRepository : IForYouSuggestionRepository
{
    private readonly IDbContextFactory<FokusAppDbContext> _contextFactory;

    public ForYouSuggestionRepository(IDbContextFactory<FokusAppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<ForYouSuggestionEntity>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var suggestions = await context.ForYouSuggestions
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);

        return suggestions
            .OrderBy(suggestion => suggestion.CreatedAt)
            .ThenBy(suggestion => suggestion.Text, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task AddAsync(ForYouSuggestionEntity suggestion)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await context.ForYouSuggestions.AddAsync(suggestion).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(ForYouSuggestionEntity suggestion)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var existing = await context.ForYouSuggestions
            .FirstOrDefaultAsync(item => item.Id == suggestion.Id)
            .ConfigureAwait(false);

        if (existing is null)
        {
            return;
        }

        existing.Text = suggestion.Text;
        existing.IsDeleted = suggestion.IsDeleted;
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await context.ForYouSuggestions
            .Where(item => item.Id == id)
            .ExecuteDeleteAsync()
            .ConfigureAwait(false);
    }
}
