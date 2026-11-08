using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class UpdateSettingsService : IUpdateSettingsService
{
    private readonly ISettingStore _settingStore;

    public UpdateSettingsService(ISettingStore settingStore)
    {
        _settingStore = settingStore;
    }

    public string? DismissedUpdateVersion => _settingStore.Read(setting => setting?.DismissedUpdateVersion);

    public async Task SetDismissedUpdateVersionAsync(string version)
    {
        await _settingStore.UpdateAsync(current =>
        {
            if (current.DismissedUpdateVersion == version)
            {
                return null;
            }

            var updated = SettingSnapshot.Copy(current);
            updated.DismissedUpdateVersion = version;
            return updated;
        }).ConfigureAwait(false);
    }
}
