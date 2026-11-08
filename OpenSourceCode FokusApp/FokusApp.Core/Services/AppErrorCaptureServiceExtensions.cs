using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public static class AppErrorCaptureServiceExtensions
{

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
