using FokusApp.Core.Interfaces;

namespace FokusApp.Services;

public sealed class MauiClipboardService : IClipboardService
{
    public Task SetTextAsync(string text) => Clipboard.Default.SetTextAsync(text);
}
