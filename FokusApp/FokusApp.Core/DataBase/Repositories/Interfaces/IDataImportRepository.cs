using FokusApp.Core.DataBase.Entities;

namespace FokusApp.Core.DataBase.Repositories.Interfaces;

public interface IDataImportRepository
{
    Task ReplaceAllAsync(
        IReadOnlyCollection<RingtonesEntity> ringtones,
        IReadOnlyCollection<SettingEntity> settings,
        IReadOnlyCollection<TaskEntity> tasks,
        CancellationToken cancellationToken = default);
}
