using System.ComponentModel;

namespace FokusApp.Components.PopUps.Calendar;

public interface ICalendarPopupViewModel : INotifyPropertyChanged
{
    DateTime SelectedDate { get; }
    CalendarMonthModel CurrentMonth { get; }

    void Open(DateTime initialDate);
    void GoToPreviousMonth();
    void GoToNextMonth();
    void SelectDate(DateTime date);
    void HandleHorizontalSwipe(double swipeDelta);
}
