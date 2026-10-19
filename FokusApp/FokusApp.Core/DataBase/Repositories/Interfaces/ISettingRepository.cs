using FokusApp.Core.DataBase.Entities;

namespace FokusApp.Core.DataBase.Repositories.Interfaces;

public interface ISettingRepository
{
    Task<List<SettingEntity>> GetAllAsync();
    Task AddAsync(SettingEntity setting);
    Task AddRangeAsync(IEnumerable<SettingEntity> settings);
    Task UpdateAsync(SettingEntity setting);
    Task<bool> AnySettingExistsAsync();
    Task DeleteAllAsync();
}
