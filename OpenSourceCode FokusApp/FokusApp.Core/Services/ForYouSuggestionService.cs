using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class ForYouSuggestionService : IForYouSuggestionService
{
    private readonly IForYouSuggestionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ForYouSuggestionService(IForYouSuggestionRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public event EventHandler? SuggestionsChanged;

    public async Task<IReadOnlyList<ForYouSuggestionDto>> GetStoredSuggestionsAsync()
    {
        var stored = await _repository.GetAllAsync().ConfigureAwait(false);
        return stored.Select(ForYouSuggestionDto.FromEntity).ToList();
    }

    public async Task<bool> AddAsync(string text)
    {
        var cleaned = Clean(text);
        if (cleaned.Length == 0)
        {
            return false;
        }

        var existing = await _repository.GetAllAsync().ConfigureAwait(false);
        if (ContainsText(existing, cleaned, ignoredId: null))
        {
            return false;
        }

        await _repository.AddAsync(new ForYouSuggestionEntity
        {
            Id = Guid.NewGuid(),
            Text = cleaned,
            CreatedAt = _timeProvider.GetLocalNow().DateTime
        }).ConfigureAwait(false);

        SuggestionsChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public async Task<bool> UpdateAsync(Guid id, string text)
    {
        var cleaned = Clean(text);
        if (cleaned.Length == 0)
        {
            return false;
        }

        var existing = await _repository.GetAllAsync().ConfigureAwait(false);
        if (existing.All(item => item.Id != id) || ContainsText(existing, cleaned, ignoredId: id))
        {
            return false;
        }

        await _repository.UpdateAsync(new ForYouSuggestionEntity { Id = id, Text = cleaned }).ConfigureAwait(false);

        SuggestionsChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id).ConfigureAwait(false);
        SuggestionsChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<bool> UpdateBuiltInAsync(string poolKey, string text)
    {
        var cleaned = Clean(text);
        if (cleaned.Length == 0 || !IsPoolKey(poolKey))
        {
            return false;
        }

        var existing = await _repository.GetAllAsync().ConfigureAwait(false);
        var row = existing.FirstOrDefault(item => item.PoolKey == poolKey);
        if (ContainsText(existing, cleaned, ignoredId: row?.Id))
        {
            return false;
        }

        if (row is null)
        {
            await _repository.AddAsync(new ForYouSuggestionEntity
            {
                Id = Guid.NewGuid(),
                Text = cleaned,
                CreatedAt = _timeProvider.GetLocalNow().DateTime,
                PoolKey = poolKey
            }).ConfigureAwait(false);
        }
        else
        {
            await _repository.UpdateAsync(new ForYouSuggestionEntity { Id = row.Id, Text = cleaned }).ConfigureAwait(false);
        }

        SuggestionsChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public async Task DeleteBuiltInAsync(string poolKey)
    {
        if (!IsPoolKey(poolKey))
        {
            return;
        }

        var existing = await _repository.GetAllAsync().ConfigureAwait(false);
        var row = existing.FirstOrDefault(item => item.PoolKey == poolKey);
        if (row is null)
        {
            await _repository.AddAsync(new ForYouSuggestionEntity
            {
                Id = Guid.NewGuid(),
                CreatedAt = _timeProvider.GetLocalNow().DateTime,
                PoolKey = poolKey,
                IsDeleted = true
            }).ConfigureAwait(false);
        }
        else
        {
            await _repository.UpdateAsync(new ForYouSuggestionEntity { Id = row.Id, Text = row.Text, IsDeleted = true }).ConfigureAwait(false);
        }

        SuggestionsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static bool IsPoolKey(string poolKey) =>
        ForYouSuggestionPool.Entries.Any(entry => entry.TextKey == poolKey);

    private static string Clean(string? text)
    {
        var trimmed = (text ?? string.Empty).Trim();
        return trimmed.Length <= ForYouSuggestionEntity.MaximumTextLength
            ? trimmed
            : trimmed[..ForYouSuggestionEntity.MaximumTextLength].TrimEnd();
    }

    private static bool ContainsText(IEnumerable<ForYouSuggestionEntity> suggestions, string text, Guid? ignoredId) =>
        suggestions.Any(item => item.Id != ignoredId &&
                                !item.IsDeleted &&
                                string.Equals(item.Text, text, StringComparison.CurrentCultureIgnoreCase));
}
