using System.ComponentModel;
using FokusApp.Components.Calender;
using FokusApp.Core.Models;

namespace FokusApp.Components.PopUps.Calendar;

public interface ICalendarPopupViewModel : INotifyPropertyChanged
{
    DateTime SelectedDate { get; }
    DateTime VisibleMonth { get; }
    IReadOnlyList<CalenderWeekModel> Weeks { get; }
    IReadOnlyList<TaskDto> ForYouTasks { get; }
    IReadOnlyList<TaskDto> ToDoTasks { get; }

    void Open(DateTime initialDate);
    void GoToPreviousMonth();
    void GoToNextMonth();

    void GoToMonth(int year, int month);
    void SelectDate(DateTime date);
    void HandleHorizontalSwipe(double swipeDelta);
}
