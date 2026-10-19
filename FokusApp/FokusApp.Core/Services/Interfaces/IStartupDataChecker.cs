namespace FokusApp.Core.Services.Interfaces;

public interface IStartupDataChecker
{
    Task EnsureFirstStartDataAsync();
}
