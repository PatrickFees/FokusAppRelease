namespace FokusApp.Core.Services.Interfaces;

public interface IUpdateSettingsService
{
    string? DismissedUpdateVersion { get; }

    Task SetDismissedUpdateVersionAsync(string version);
}
