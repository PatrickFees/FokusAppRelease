namespace FokusApp.Core.Interfaces;

public interface IAppEnvironmentInfo
{
    string? Platform { get; }

    string? OsVersion { get; }

    string? AppVersion { get; }
}
