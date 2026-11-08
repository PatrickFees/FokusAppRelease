using FokusApp.Core.DataBase.Entities;

namespace FokusApp.Core.Services.Interfaces;

public interface IRingtoneSettingsService
{
    Task<RingtonesEntity?> GetSelectedRingtoneAsync();
    Task SetSelectedRingtoneAsync(Guid ringtoneId);
}
