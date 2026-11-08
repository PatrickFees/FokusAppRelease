namespace FokusApp.Core.Services.Interfaces;

public interface IAppDataCoordinator
{
    Task InitializeAsync();
    Task ReloadAsync();
}
