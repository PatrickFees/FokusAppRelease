using FokusApp.Core.Enums;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class DefaultScopeSettingsService : IDefaultScopeSettingsService
{
    public event EventHandler? DefaultScopeChanged;

    private readonly ISettingStore _settingStore;

    public DefaultScopeSettingsService(ISettingStore settingStore)
    {
        _settingStore = settingStore;
        _settingStore.SettingsLoaded += OnSettingsLoaded;
    }

    public DefaultScope DefaultScope => _settingStore.Read(setting => setting is null
        ? DefaultScope.Both
        : NormalizeDefaultScope(setting.DefaultScope));

    public async Task SetDefaultScopeAsync(DefaultScope scope)
    {
        if (!Enum.IsDefined(scope))
        {
            return;
        }

        var changed = await _settingStore.UpdateAsync(current =>
        {
            if (current.DefaultScope == (int)scope)
            {
                return null;
            }

            var updated = SettingSnapshot.Copy(current);
            updated.DefaultScope = (int)scope;
            return updated;
        }).ConfigureAwait(false);

        if (changed)
        {
            DefaultScopeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnSettingsLoaded(object? sender, EventArgs e) => DefaultScopeChanged?.Invoke(this, EventArgs.Empty);

    private static DefaultScope NormalizeDefaultScope(int scope) =>
        Enum.IsDefined((DefaultScope)scope) ? (DefaultScope)scope : DefaultScope.Both;
}
