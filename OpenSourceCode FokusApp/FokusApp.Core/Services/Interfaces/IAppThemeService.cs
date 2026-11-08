using FokusApp.Core.Enums;

namespace FokusApp.Core.Services.Interfaces;

public interface IAppThemeService
{
    event EventHandler? ThemeChanged;

    AppColorTheme SelectedTheme { get; }

    AppColorMode SelectedMode { get; }

    Task InitializeAsync();

    Task ReloadAsync();

    Task SetThemeAsync(AppColorTheme theme);

    Task SetColorModeAsync(AppColorMode mode);
}
