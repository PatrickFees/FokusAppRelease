using FokusApp.Core.Enums;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class ScopeService : IScopeService
{
    public event EventHandler? ScopeChanged;

    private readonly Lock _gate = new();
    private readonly IDefaultScopeSettingsService _defaultScopeSettingsService;
    private bool _isWorkScope = true;

    public ScopeService(IDefaultScopeSettingsService defaultScopeSettingsService)
    {
        _defaultScopeSettingsService = defaultScopeSettingsService;

        ApplyDefaultScope();
        _defaultScopeSettingsService.DefaultScopeChanged += OnDefaultScopeChanged;
    }

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

    public bool CanSwitch => CurrentDefaultScope == DefaultScope.Both;

    private DefaultScope CurrentDefaultScope => _defaultScopeSettingsService.DefaultScope;

    public void ToggleScope()
    {
        if (!CanSwitch)
        {
            return;
        }

        lock (_gate)
        {
            _isWorkScope = !_isWorkScope;
        }

        ScopeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnDefaultScopeChanged(object? sender, EventArgs e)
    {
        if (ApplyDefaultScope())
        {
            ScopeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool ApplyDefaultScope()
    {
        lock (_gate)
        {
            var target = CurrentDefaultScope switch
            {
                DefaultScope.Work => true,
                DefaultScope.Private => false,
                _ => _isWorkScope
            };

            if (target == _isWorkScope)
            {
                return false;
            }

            _isWorkScope = target;
            return true;
        }
    }
}
