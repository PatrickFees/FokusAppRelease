namespace FokusApp.Core.Services.Interfaces;

public interface ITaskReminderService
{

    Task StartAsync();

    Task RefreshAsync();
}
