namespace FokusApp.Core.Services.Interfaces;

public interface IAppErrorCaptureService
{
    Task CaptureAsync(Exception exception, AppErrorCaptureOptions? options = null);
}
