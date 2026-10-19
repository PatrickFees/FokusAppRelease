using CommunityToolkit.Mvvm.ComponentModel;

namespace FokusApp.Components.PopUps.Calendar;

public partial class CalendarPopupViewModel : ObservableObject, ICalendarPopupViewModel
{
    private const int DaysInCalendarGrid = 42;
    private const double SwipeThreshold = 45;

    private readonly TimeProvider _timeProvider;
    private DateTime _startOfTheMonth;

    [ObservableProperty]
    private DateTime selectedDate;
    [ObservableProperty]
    private CalendarMonthModel currentMonth;

    public CalendarPopupViewModel(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        var today = _timeProvider.GetLocalNow().Date;
        _startOfTheMonth = GetFirstDayOfMonth(today);
        selectedDate = today;
        currentMonth = BuildMonthModel(today, today, today);
    }

    public void Open(DateTime initialDate)
    {
        SelectedDate = initialDate.Date;
        _startOfTheMonth = GetFirstDayOfMonth(SelectedDate);

        RebuildMonth();

    }

    public void GoToPreviousMonth()
    {
        _startOfTheMonth = _startOfTheMonth.AddMonths(-1);
        RebuildMonth();
    }

    public void GoToNextMonth()
    {
        _startOfTheMonth = _startOfTheMonth.AddMonths(1);
        RebuildMonth();
    }

    public void SelectDate(DateTime date)
    {
        var normalizedDate = date.Date;
        var normalizedVisibleMonthStart = GetFirstDayOfMonth(normalizedDate);
        var shouldRebuild = normalizedDate != SelectedDate || normalizedVisibleMonthStart != _startOfTheMonth;

        SelectedDate = normalizedDate;
        _startOfTheMonth = normalizedVisibleMonthStart;

        if (shouldRebuild)
        {
            RebuildMonth();
        }
    }


    public void HandleHorizontalSwipe(double swipeDelta)
    {
        if (swipeDelta <= -SwipeThreshold)
        {
            GoToNextMonth();
        }


        if (swipeDelta >= SwipeThreshold)
        {
            GoToPreviousMonth();
        }

    }

    private void RebuildMonth()
    {
        var today = _timeProvider.GetLocalNow().Date;
        CurrentMonth = BuildMonthModel(_startOfTheMonth, SelectedDate, today);
    }
    private static CalendarMonthModel BuildMonthModel(DateTime monthStart, DateTime selectedDate, DateTime today)
    {
        var normalizedMonthStart = GetFirstDayOfMonth(monthStart);
        var firstVisibleDate = GetFirstVisibleCalendarDay(normalizedMonthStart);
        var dayCells = new List<CalendarDayCellModel>(DaysInCalendarGrid);
        for (var dayOffset = 0; dayOffset < DaysInCalendarGrid; dayOffset++)
        {
            var currentDate = firstVisibleDate.AddDays(dayOffset);
            dayCells.Add(new CalendarDayCellModel
            {
                Date = currentDate,
                IsInCurrentMonth = currentDate.Month == normalizedMonthStart.Month && currentDate.Year == normalizedMonthStart.Year,
                IsToday = currentDate.Date == today,
                IsSelected = currentDate.Date == selectedDate.Date
            });
        }

        return new CalendarMonthModel
        {
            MonthStart = normalizedMonthStart,
            DayCells = dayCells
        };
    }

    private static DateTime GetFirstDayOfMonth(DateTime date)
    {
        return CalendarGridHelper.GetFirstDayOfMonth(date);
    }

    private static DateTime GetFirstVisibleCalendarDay(DateTime monthStart)
    {
        return CalendarGridHelper.GetFirstVisibleCalendarDay(monthStart);
    }

}
