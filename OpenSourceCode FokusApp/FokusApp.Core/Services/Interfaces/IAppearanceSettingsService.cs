using FokusApp.Core.Enums;

namespace FokusApp.Core.Services.Interfaces;

public interface IAppearanceSettingsService
{
    AppLanguage CurrentLanguage { get; }
    AppColorTheme CurrentTheme { get; }
    AppColorMode CurrentColorMode { get; }

    Task SetLanguageAsync(AppLanguage language);
    Task SetThemeAsync(AppColorTheme theme);
    Task SetColorModeAsync(AppColorMode mode);
}
