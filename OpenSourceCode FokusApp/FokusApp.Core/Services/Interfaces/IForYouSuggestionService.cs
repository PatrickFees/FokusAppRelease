using FokusApp.Core.Models;

namespace FokusApp.Core.Services.Interfaces;

public interface IForYouSuggestionService
{

    event EventHandler? SuggestionsChanged;

    Task<IReadOnlyList<ForYouSuggestionDto>> GetStoredSuggestionsAsync();

    Task<bool> AddAsync(string text);

    Task<bool> UpdateAsync(Guid id, string text);

    Task DeleteAsync(Guid id);

    Task<bool> UpdateBuiltInAsync(string poolKey, string text);

    Task DeleteBuiltInAsync(string poolKey);
}
