using FokusApp.Core.Interfaces;

namespace FokusApp.Services;

public sealed class MauiAppEnvironmentInfo : IAppEnvironmentInfo
{
    public string? Platform => DeviceInfo.Current.Platform.ToString();

    public string? OsVersion => DeviceInfo.Current.VersionString;

    public string? AppVersion => $"{AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";
}
