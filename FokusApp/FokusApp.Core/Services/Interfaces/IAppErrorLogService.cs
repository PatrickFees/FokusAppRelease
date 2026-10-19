using FokusApp.Core.DataBase.ErrorLogging.Entities;

namespace FokusApp.Core.Services.Interfaces;

public interface IAppErrorLogService
{
    Task LogExceptionAsync(Exception exception, AppErrorCaptureContext context);
    Task<List<AppErrorLogEntity>> GetLatestAsync();
}
