namespace FokusApp.Core.Services.Interfaces;

public interface ICalendarAutoSyncRunner
{
    Task RunIfDueAsync();
}
