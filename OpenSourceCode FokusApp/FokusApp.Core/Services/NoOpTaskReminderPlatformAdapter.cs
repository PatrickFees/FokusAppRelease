using FokusApp.Core.Interfaces;
using FokusApp.Core.Models;

namespace FokusApp.Core.Services;

public sealed class NoOpTaskReminderPlatformAdapter : ITaskReminderPlatformAdapter
{
    public Task SyncAsync(IReadOnlyList<TaskReminder> reminders) => Task.CompletedTask;
}
