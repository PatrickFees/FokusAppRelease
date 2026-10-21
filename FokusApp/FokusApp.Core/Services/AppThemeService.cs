using FokusApp.Core.Enums;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class AppThemeService : IAppThemeService, IDisposable
{
    private readonly IUserSettingsService _userSettingsService;
    private readonly ISystemThemeProvider _systemThemeProvider;

    public AppThemeService(IUserSettingsService userSettingsService, ISystemThemeProvider systemThemeProvider)
    {
        _userSettingsService = userSettingsService;
        _systemThemeProvider = systemThemeProvider;
        _systemThemeProvider.SystemThemeChanged += HandleSystemThemeChanged;
    }

    public event EventHandler? ThemeChanged;

    public AppColorTheme SelectedTheme { get; private set; } = AppColorTheme.System;

    public AppColorTheme EffectiveTheme => SelectedTheme == AppColorTheme.System
        ? (_systemThemeProvider.IsSystemDark ? AppColorTheme.Dark : AppColorTheme.White)
        : SelectedTheme;

    public Task InitializeAsync()
    {
        SelectedTheme = _userSettingsService.CurrentTheme;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task ReloadAsync() => InitializeAsync();

    public async Task SetThemeAsync(AppColorTheme theme)
    {
        await _userSettingsService.SetThemeAsync(theme).ConfigureAwait(false);

        var persistedTheme = _userSettingsService.CurrentTheme;
        if (persistedTheme == SelectedTheme)
        {
            return;
        }

        SelectedTheme = persistedTheme;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _systemThemeProvider.SystemThemeChanged -= HandleSystemThemeChanged;
    }

    private void HandleSystemThemeChanged(object? sender, EventArgs args)
    {
        if (SelectedTheme == AppColorTheme.System)
        {
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
