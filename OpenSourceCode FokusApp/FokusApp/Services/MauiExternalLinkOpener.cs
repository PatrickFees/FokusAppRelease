using FokusApp.Core.Interfaces;

namespace FokusApp.Services;

public sealed class MauiExternalLinkOpener : IExternalLinkOpener
{
    public Task OpenAsync(string url) => Launcher.Default.OpenAsync(new Uri(url));
}
