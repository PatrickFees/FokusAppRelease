using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Services;

public sealed class MauiSystemThemeProvider : ISystemThemeProvider
{
    private Application? _application;

    public event EventHandler? SystemThemeChanged;

    public bool IsSystemDark => (_application ?? Application.Current)?.RequestedTheme == AppTheme.Dark;

    // Application.Current is not yet set while the DI container builds the App, so the app attaches itself later.
    public void Attach(Application application)
    {
        if (_application is not null)
        {
            return;
        }

        _application = application;
        application.RequestedThemeChanged += (_, _) => SystemThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}
