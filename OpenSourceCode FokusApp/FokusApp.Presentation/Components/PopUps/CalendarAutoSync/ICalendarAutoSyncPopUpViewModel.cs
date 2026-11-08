using System.ComponentModel;
using FokusApp.Components.PopUps.CalendarImport;

namespace FokusApp.Components.PopUps.CalendarAutoSync;

public interface ICalendarAutoSyncPopUpViewModel : INotifyPropertyChanged
{
    bool IsLoading { get; }
    IReadOnlyList<CalendarOptionModel> Calendars { get; }
    bool CanSave { get; }
    string? ResultMessage { get; }
    bool? ResultIsSuccess { get; }

    Task OpenAsync();

    void ToggleCalendar(string calendarId);
    Task SaveAsync();
}
