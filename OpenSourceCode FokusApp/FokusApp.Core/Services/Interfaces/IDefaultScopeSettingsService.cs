using FokusApp.Core.Enums;

namespace FokusApp.Core.Services.Interfaces;

public interface IDefaultScopeSettingsService
{

    event EventHandler? DefaultScopeChanged;

    DefaultScope DefaultScope { get; }

    Task SetDefaultScopeAsync(DefaultScope scope);
}
