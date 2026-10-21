namespace FokusApp.Core.Services.Interfaces;

public interface ISystemThemeProvider
{
    event EventHandler? SystemThemeChanged;

    bool IsSystemDark { get; }
}
