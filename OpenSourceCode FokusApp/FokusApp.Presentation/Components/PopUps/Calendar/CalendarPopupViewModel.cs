using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Components.Calender;
using FokusApp.Components.SharedUI;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Components.PopUps.Calendar;

public partial class CalendarPopupViewModel : ObservableObject, ICalendarPopupViewModel
{
    private const double SwipeThreshold = 45;

    private readonly TimeProvider _timeProvider;
    private readonly TaskDaySourceCache _taskSource;

    [ObservableProperty]
    private DateTime selectedDate;
    [ObservableProperty]
    private DateTime visibleMonth;
    [ObservableProperty]
    private IReadOnlyList<CalenderWeekModel> weeks = [];
    [ObservableProperty]
    private IReadOnlyList<TaskDto> forYouTasks = [];
    [ObservableProperty]
    private IReadOnlyList<TaskDto> toDoTasks = [];

    public CalendarPopupViewModel(TimeProvider timeProvider, ITaskCacheService taskCacheService)
    {
        _timeProvider = timeProvider;
        _taskSource = new TaskDaySourceCache(taskCacheService);
        var today = _timeProvider.GetLocalNow().Date;
        selectedDate = today;
        visibleMonth = CalendarGridHelper.GetFirstDayOfMonth(today);
        Rebuild();
    }

    public void Open(DateTime initialDate)
    {
        SelectedDate = initialDate.Date;
        VisibleMonth = CalendarGridHelper.GetFirstDayOfMonth(SelectedDate);
        Rebuild();
    }

    public void GoToPreviousMonth()
    {
        VisibleMonth = VisibleMonth.AddMonths(-1);
        Rebuild();
    }

    public void GoToNextMonth()
    {
        VisibleMonth = VisibleMonth.AddMonths(1);
        Rebuild();
    }

    public void GoToMonth(int year, int month)
    {
        if (month is < 1 or > 12)
        {
            return;
        }

        var target = new DateTime(year, month, 1);
        if (target == VisibleMonth)
        {
            return;
        }

        VisibleMonth = target;
        var day = Math.Min(SelectedDate.Day, DateTime.DaysInMonth(target.Year, target.Month));
        SelectedDate = new DateTime(target.Year, target.Month, day);
        Rebuild();
    }

    public void SelectDate(DateTime date)
    {
        var normalizedDate = date.Date;
        var normalizedVisibleMonth = CalendarGridHelper.GetFirstDayOfMonth(normalizedDate);
        var hasChanged = normalizedDate != SelectedDate || normalizedVisibleMonth != VisibleMonth;

        SelectedDate = normalizedDate;
        VisibleMonth = normalizedVisibleMonth;

        if (hasChanged)
        {
            Rebuild();
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

    private void Rebuild()
    {
        _taskSource.RefreshNormal(null);
        _taskSource.RefreshTemplate(null);
        _taskSource.RefreshDone(null);

        var today = _timeProvider.GetLocalNow().Date;
        Weeks = CalenderDayDataBuilder.BuildWeeks(VisibleMonth, SelectedDate, today, _taskSource);
        ForYouTasks = CalenderDayDataBuilder.BuildDayTasks(isPositive: true, SelectedDate, _taskSource);
        ToDoTasks = CalenderDayDataBuilder.BuildDayTasks(isPositive: false, SelectedDate, _taskSource);
    }
}
