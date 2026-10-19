using FokusApp.Core.DataBase.Entities;

namespace FokusApp.Core.Services.Interfaces;

public interface IRingtoneCatalogService
{
    string DefaultRingtoneFilePath { get; }

    Task InitializeAsync();
    Task ReloadAsync();

    IReadOnlyList<RingtonesEntity> GetAllRingtones();
    RingtonesEntity? FindById(Guid id);
}
