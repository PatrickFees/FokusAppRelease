using FokusApp.Core.Enums;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class AppThemeService : IAppThemeService
{
    private readonly IAppearanceSettingsService _appearanceSettingsService;

    public AppThemeService(IAppearanceSettingsService appearanceSettingsService)
    {
        _appearanceSettingsService = appearanceSettingsService;
    }

    public event EventHandler? ThemeChanged;

    public AppColorTheme SelectedTheme { get; private set; } = AppColorTheme.Blue;

    public AppColorMode SelectedMode { get; private set; } = AppColorMode.Light;

    public Task InitializeAsync()
    {
        SelectedTheme = _appearanceSettingsService.CurrentTheme;
        SelectedMode = _appearanceSettingsService.CurrentColorMode;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task ReloadAsync() => InitializeAsync();

    public async Task SetThemeAsync(AppColorTheme theme)
    {
        await _appearanceSettingsService.SetThemeAsync(theme).ConfigureAwait(false);

        var persistedTheme = _appearanceSettingsService.CurrentTheme;
        if (persistedTheme == SelectedTheme)
        {
            return;
        }

        SelectedTheme = persistedTheme;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SetColorModeAsync(AppColorMode mode)
    {
        await _appearanceSettingsService.SetColorModeAsync(mode).ConfigureAwait(false);

        var persistedMode = _appearanceSettingsService.CurrentColorMode;
        if (persistedMode == SelectedMode)
        {
            return;
        }

        SelectedMode = persistedMode;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}
