using FokusApp.Core.DataBase.ErrorLogging.Entities;

namespace FokusApp.Core.DataBase.Repositories.Interfaces;

public interface IAppErrorLogRepository
{
    Task AddAsync(AppErrorLogEntity errorLog);
    Task<List<AppErrorLogEntity>> GetLatestAsync();
}
