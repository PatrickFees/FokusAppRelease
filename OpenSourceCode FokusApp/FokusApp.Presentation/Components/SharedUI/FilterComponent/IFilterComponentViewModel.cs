using CommunityToolkit.Mvvm.Input;
using FokusApp.Core.Enums;
using System.ComponentModel;

namespace FokusApp.Components.SharedUI.FilterComponent;

public interface IFilterComponentViewModel : INotifyPropertyChanged
{
    IRelayCommand<TaskPriority> TogglePriorityFilterCommand { get; }
    IRelayCommand ToggleSortDirectionCommand { get; }
    IRelayCommand ResetFiltersCommand { get; }
    IRelayCommand CloseCommand { get; }
    FilterModel Model { get; }
    bool IsOpen { get; }
    bool IsDayCalendarOpen { get; }
    DateTime DisplayDate { get; }

    void Open(
        bool resetFilters = true,
        string? initialSearchText = null,
        IReadOnlyCollection<TaskPriority>? initialSelectedPriorities = null,
        bool initialIsSortDescending = true,
        DateTime? initialSelectedDate = null);
    FilterModel CreateFilterPayload();
    bool HasFilterChanged();
    void MarkFilterChangesNotified();
    void ResetLastNotifiedFilterPayload();
    void OpenDayCalendar();
    void CloseDayCalendar();
    void SelectDate(DateTime date);
}
