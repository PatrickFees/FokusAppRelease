namespace FokusApp.Core.Services.Interfaces;

public interface IScopeService
{
    event EventHandler? ScopeChanged;

    bool IsWorkScope { get; }

    void ToggleScope();
}
