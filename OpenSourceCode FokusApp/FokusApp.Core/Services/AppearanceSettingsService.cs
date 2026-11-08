using FokusApp.Core.Enums;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class AppearanceSettingsService : IAppearanceSettingsService
{
    private readonly ISettingStore _settingStore;

    public AppearanceSettingsService(ISettingStore settingStore)
    {
        _settingStore = settingStore;
    }

    public AppLanguage CurrentLanguage => _settingStore.Read(setting => NormalizeLanguage(setting?.Language));

    public AppColorTheme CurrentTheme => _settingStore.Read(setting => NormalizeTheme(setting?.Theme));

    public AppColorMode CurrentColorMode => _settingStore.Read(setting => NormalizeColorMode(setting?.ColorMode));

    public async Task SetLanguageAsync(AppLanguage language)
    {
        var normalizedLanguage = NormalizeLanguage((int)language);
        await _settingStore.UpdateAsync(current =>
        {
            if (current.Language == (int)normalizedLanguage)
            {
                return null;
            }

            var updated = SettingSnapshot.Copy(current);
            updated.Language = (int)normalizedLanguage;
            return updated;
        }).ConfigureAwait(false);
    }

    public async Task SetThemeAsync(AppColorTheme theme)
    {
        var normalizedTheme = NormalizeTheme((int)theme);
        await _settingStore.UpdateAsync(current =>
        {
            if (current.Theme == (int)normalizedTheme)
            {
                return null;
            }

            var updated = SettingSnapshot.Copy(current);
            updated.Theme = (int)normalizedTheme;
            return updated;
        }).ConfigureAwait(false);
    }

    public async Task SetColorModeAsync(AppColorMode mode)
    {
        var normalizedMode = NormalizeColorMode((int)mode);
        await _settingStore.UpdateAsync(current =>
        {
            if (current.ColorMode == (int)normalizedMode)
            {
                return null;
            }

            var updated = SettingSnapshot.Copy(current);
            updated.ColorMode = (int)normalizedMode;
            return updated;
        }).ConfigureAwait(false);
    }

    private static AppColorTheme NormalizeTheme(int? theme) => theme is { } value && Enum.IsDefined((AppColorTheme)value)
        ? (AppColorTheme)value
        : AppColorTheme.Blue;

    private static AppColorMode NormalizeColorMode(int? mode) => mode is { } value && Enum.IsDefined((AppColorMode)value)
        ? (AppColorMode)value
        : AppColorMode.Light;

    private static AppLanguage NormalizeLanguage(int? language) => language switch
    {
        (int)AppLanguage.German => AppLanguage.German,
        _ => AppLanguage.English
    };
}
