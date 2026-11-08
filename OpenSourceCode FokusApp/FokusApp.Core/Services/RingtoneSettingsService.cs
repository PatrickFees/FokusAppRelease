using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class RingtoneSettingsService : IRingtoneSettingsService
{
    private readonly ISettingStore _settingStore;
    private readonly IRingtoneCatalogService _ringtoneCatalogService;

    public RingtoneSettingsService(ISettingStore settingStore, IRingtoneCatalogService ringtoneCatalogService)
    {
        _settingStore = settingStore;
        _ringtoneCatalogService = ringtoneCatalogService;
    }

    public Task<RingtonesEntity?> GetSelectedRingtoneAsync()
    {
        return Task.FromResult(_settingStore.Read(setting =>
        {
            var ringtone = setting?.Ringtone ?? (setting is null ? null : _ringtoneCatalogService.FindById(setting.RingtoneId));
            return ringtone is null ? null : SettingSnapshot.CopyRingtone(ringtone);
        }));
    }

    public async Task SetSelectedRingtoneAsync(Guid ringtoneId)
    {
        var ringtone = _ringtoneCatalogService.FindById(ringtoneId);
        if (ringtone is null)
        {
            return;
        }

        await _settingStore.UpdateAsync(current =>
        {
            var updated = SettingSnapshot.Copy(current);
            updated.RingtoneId = ringtone.Id;
            updated.Ringtone = ringtone;
            return updated;
        }).ConfigureAwait(false);
    }
}
