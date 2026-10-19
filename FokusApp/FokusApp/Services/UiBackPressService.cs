namespace FokusApp.Services;

public sealed class UiBackPressService : IUiBackPressService
{
    private readonly List<Func<bool>> _handlers = [];
    private readonly object _sync = new();

    public IDisposable RegisterHandler(Func<bool> _handler)
    {
        if (_handler is null)
        {
            throw new ArgumentNullException(nameof(_handler));
        }

        lock (_sync)
        {
            _handlers.Add(_handler);
        }

        return new HandlerRegistration(this, _handler);
    }

    public bool TryHandleBackPress()
    {
        Func<bool>[] snapshot;

        lock (_sync)
        {
            snapshot = _handlers.ToArray();
        }

        for (var i = snapshot.Length - 1; i >= 0; i--)
        {
            if (snapshot[i].Invoke())
            {
                return true;
            }
        }

        return false;
    }

    private void Unregister(Func<bool> _handler)
    {
        lock (_sync)
        {
            _handlers.Remove(_handler);
        }
    }

    private sealed class HandlerRegistration : IDisposable
    {
        private readonly UiBackPressService _owner;
        private readonly Func<bool> _handler;
        private bool _disposed;

        public HandlerRegistration(UiBackPressService _owner, Func<bool> _handler)
        {
            this._owner = _owner;
            this._handler = _handler;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _owner.Unregister(_handler);
        }
    }
}
