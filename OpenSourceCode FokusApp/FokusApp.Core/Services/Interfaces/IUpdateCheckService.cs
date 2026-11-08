using FokusApp.Core.Models;

namespace FokusApp.Core.Services.Interfaces;

public interface IUpdateCheckService
{
    Task<AppUpdateInfo?> GetAvailableUpdateAsync(CancellationToken cancellationToken = default);
}
