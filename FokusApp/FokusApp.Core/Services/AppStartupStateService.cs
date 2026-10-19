using FokusApp.Core.Services.Interfaces;
using System.Runtime.ExceptionServices;

namespace FokusApp.Core.Services;

public sealed class AppStartupStateService : IAppStartupStateService
{
    private readonly object _stateLock = new();
    private readonly TaskCompletionSource<AppStartupStatus> _completionSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private AppStartupStatus _status = AppStartupStatus.Loading;
    private Exception? _failure;

    public event EventHandler? StateChanged;

    public AppStartupStatus Status
    {
        get
        {
            lock (_stateLock)
            {
                return _status;
            }
        }
    }

    public Exception? Failure
    {
        get
        {
            lock (_stateLock)
            {
                return _failure;
            }
        }
    }

    public async Task WaitForReadyAsync(CancellationToken cancellationToken = default)
    {
        var status = await _completionSource.Task
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        if (status == AppStartupStatus.Ready)
        {
            return;
        }

        var failure = Failure ?? new InvalidOperationException("App startup failed without an exception.");
        ExceptionDispatchInfo.Capture(failure).Throw();
    }

    public void MarkReady()
    {
        if (!TrySetTerminalState(AppStartupStatus.Ready, null))
        {
            return;
        }

        _completionSource.TrySetResult(AppStartupStatus.Ready);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void MarkFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (!TrySetTerminalState(AppStartupStatus.Failed, exception))
        {
            return;
        }

        _completionSource.TrySetResult(AppStartupStatus.Failed);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool TrySetTerminalState(AppStartupStatus status, Exception? failure)
    {
        lock (_stateLock)
        {
            if (_status != AppStartupStatus.Loading)
            {
                return false;
            }

            _status = status;
            _failure = failure;
            return true;
        }
    }
}
