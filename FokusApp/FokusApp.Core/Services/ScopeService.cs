using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class ScopeService : IScopeService
{
    public event EventHandler? ScopeChanged;

    private readonly Lock _gate = new();
    private bool _isWorkScope = true;

    public bool IsWorkScope
    {
        get
        {
            lock (_gate)
            {
                return _isWorkScope;
            }
        }
    }

    public void ToggleScope()
    {
        lock (_gate)
        {
            _isWorkScope = !_isWorkScope;
        }

        ScopeChanged?.Invoke(this, EventArgs.Empty);
    }
}
