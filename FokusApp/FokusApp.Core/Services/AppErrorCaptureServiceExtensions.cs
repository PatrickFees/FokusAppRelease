using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public static class AppErrorCaptureServiceExtensions
{
    // Shared shape for non-fatal failures caught in UI-layer catch blocks
    // (ViewModel calls into repositories/services): logged, never fatal.
    public static Task CaptureUiErrorAsync(this IAppErrorCaptureService service, Exception exception, string source)
    {
        return service.CaptureAsync(exception, new AppErrorCaptureOptions
        {
            Source = source,
            IsFatal = false,
            IsUiFailure = true
        });
    }
}
