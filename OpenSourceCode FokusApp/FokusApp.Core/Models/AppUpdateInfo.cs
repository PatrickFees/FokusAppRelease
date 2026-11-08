namespace FokusApp.Core.Models;

public sealed class AppUpdateInfo
{
    public required string Version { get; init; }

    public required string DownloadUrl { get; init; }
}
