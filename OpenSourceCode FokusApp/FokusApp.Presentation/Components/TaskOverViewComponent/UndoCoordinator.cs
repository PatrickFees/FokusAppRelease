namespace FokusApp.Components.TaskOverViewComponent;

public sealed class UndoCoordinator : IDisposable
{
    private const int DefaultVisibleSeconds = 5;

    private readonly TimeProvider _timeProvider;
    private readonly Action _onChanged;
    private readonly int _visibleSeconds;

    private ITimer? _timer;
    private Func<Task>? _undoAction;

    public UndoCoordinator(TimeProvider timeProvider, Action onChanged, int visibleSeconds = DefaultVisibleSeconds)
    {
        _timeProvider = timeProvider;
        _onChanged = onChanged;
        _visibleSeconds = visibleSeconds;
    }

    public UndoNotice? Current { get; private set; }

    public void Show(UndoNotice notice, Func<Task> undoAction)
    {
        _timer?.Dispose();
        _undoAction = undoAction;
        Current = notice;
        _onChanged();
        _timer = _timeProvider.CreateTimer(
            _ => Dismiss(),
            null,
            TimeSpan.FromSeconds(_visibleSeconds),
            Timeout.InfiniteTimeSpan);
    }

    public void Dismiss()
    {
        _timer?.Dispose();
        _timer = null;
        _undoAction = null;

        if (Current is not null)
        {
            Current = null;
            _onChanged();
        }
    }

    public async Task UndoAsync(Func<Exception, Task> onError)
    {
        var undoAction = _undoAction;
        Dismiss();

        if (undoAction is null)
        {
            return;
        }

        try
        {
            await undoAction();
        }
        catch (Exception ex)
        {
            await onError(ex);
        }
    }

    public void Dispose() => _timer?.Dispose();
}
