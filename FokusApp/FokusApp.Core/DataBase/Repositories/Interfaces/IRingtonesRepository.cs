using FokusApp.Core.DataBase.Entities;

namespace FokusApp.Core.DataBase.Repositories.Interfaces;

public interface IRingtonesRepository
{
    Task<List<RingtonesEntity>> GetAllAsync();
    Task AddAsync(RingtonesEntity ringtone);
    Task AddRangeAsync(IEnumerable<RingtonesEntity> ringtones);
    Task DeleteAllAsync();
}
