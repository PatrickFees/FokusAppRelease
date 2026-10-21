using FokusApp.Core.Enums;

namespace FokusApp.Core.Services.Interfaces;

public interface IAppThemeService
{
    event EventHandler? ThemeChanged;

    AppColorTheme SelectedTheme { get; }

    // The theme that is actually rendered: "System" is resolved to White or Dark.
    AppColorTheme EffectiveTheme { get; }

    Task InitializeAsync();

    Task ReloadAsync();

    Task SetThemeAsync(AppColorTheme theme);
}
