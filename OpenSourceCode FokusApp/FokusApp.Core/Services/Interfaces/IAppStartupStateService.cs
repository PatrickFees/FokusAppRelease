namespace FokusApp.Core.Services.Interfaces;

public enum AppStartupStatus
{
    Loading,
    Ready,
    Failed
}

public interface IAppStartupStateService
{
    event EventHandler? StateChanged;

    AppStartupStatus Status { get; }

    Exception? Failure { get; }

    Task WaitForReadyAsync(CancellationToken cancellationToken = default);

    void MarkReady();

    void MarkFailed(Exception exception);
}
