using FokusApp.Core.DataBase.Entities;

namespace FokusApp.Core.DataBase.Repositories.Interfaces;

public interface IForYouSuggestionRepository
{

    Task<List<ForYouSuggestionEntity>> GetAllAsync();

    Task AddAsync(ForYouSuggestionEntity suggestion);

    Task UpdateAsync(ForYouSuggestionEntity suggestion);

    Task DeleteAsync(Guid id);
}
