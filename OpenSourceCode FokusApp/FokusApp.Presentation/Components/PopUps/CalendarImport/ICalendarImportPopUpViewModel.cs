using System.ComponentModel;

namespace FokusApp.Components.PopUps.CalendarImport;

public interface ICalendarImportPopUpViewModel : INotifyPropertyChanged
{
    bool IsLoading { get; }
    IReadOnlyList<CalendarOptionModel> Calendars { get; }
    DateTime From { get; }
    DateTime To { get; }

    bool CanChangeScope { get; }

    bool IsWorkTask { get; }
    string? ResultMessage { get; }
    bool? ResultIsSuccess { get; }
    bool CanImport { get; }

    Task OpenAsync();

    void ToggleCalendar(string calendarId);
    void SetFrom(DateTime date);
    void SetTo(DateTime date);
    void SetIsWorkTask(bool isWorkTask);
    Task ImportAsync();
}

public sealed record CalendarOptionModel(string Id, string DisplayName, string AccountName, bool IsSelected, bool IsLocalDefault = false);
