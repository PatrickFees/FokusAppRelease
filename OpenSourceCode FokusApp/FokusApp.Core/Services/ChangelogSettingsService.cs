using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class ChangelogSettingsService : IChangelogSettingsService
{
    private readonly ISettingStore _settingStore;

    public ChangelogSettingsService(ISettingStore settingStore)
    {
        _settingStore = settingStore;
    }

    public string? LastSeenChangelogVersion => _settingStore.Read(setting => setting?.LastSeenChangelogVersion);

    public async Task SetLastSeenChangelogVersionAsync(string version)
    {
        await _settingStore.UpdateAsync(current =>
        {
            if (current.LastSeenChangelogVersion == version)
            {
                return null;
            }

            var updated = SettingSnapshot.Copy(current);
            updated.LastSeenChangelogVersion = version;
            return updated;
        }).ConfigureAwait(false);
    }
}
