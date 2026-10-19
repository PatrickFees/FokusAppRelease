namespace FokusApp.Core.Services;

public sealed class AppErrorCaptureOptions
{
    public string Source { get; init; } = "App";
    public bool IsFatal { get; init; }
    public bool IsUiFailure { get; init; }
    public string? Route { get; init; }
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
