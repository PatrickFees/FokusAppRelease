using FokusApp.Core.Models;

namespace FokusApp.Core.Interfaces;

public interface ITaskReminderPlatformAdapter
{

    Task SyncAsync(IReadOnlyList<TaskReminder> reminders);
}
