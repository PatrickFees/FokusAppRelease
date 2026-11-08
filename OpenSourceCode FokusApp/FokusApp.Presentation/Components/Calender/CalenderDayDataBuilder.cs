using System.Globalization;
using FokusApp.Components.PopUps.Calendar;
using FokusApp.Components.SharedUI;
using FokusApp.Core.Models;
using FokusApp.Core.Services;

namespace FokusApp.Components.Calender;

public static class CalenderDayDataBuilder
{
    private const int MaxDotsPerDay = 3;

    public static IReadOnlyList<CalenderWeekModel> BuildWeeks(
        DateTime visibleMonth,
        DateTime selectedDay,
        DateTime today,
        TaskDaySourceCache taskSource)
    {
        var firstVisibleDay = CalendarGridHelper.GetFirstVisibleCalendarDay(visibleMonth);
        var tasksByDay = taskSource.NormalTasks
            .Concat(taskSource.Templates.Where(task => !TaskFilterService.IsOpenTemplate(task)))
            .Concat(taskSource.DoneTasks)
            .GroupBy(task => (task.TaskDate ?? task.CreationDate).Date)
            .ToDictionary(group => group.Key, group => group.ToList());

        var weeks = new List<CalenderWeekModel>(CalendarGridHelper.WeeksInGrid);
        for (var weekIndex = 0; weekIndex < CalendarGridHelper.WeeksInGrid; weekIndex++)
        {
            var weekStart = firstVisibleDay.AddDays(weekIndex * CalendarGridHelper.DaysInWeek);
            var days = new List<CalenderDayCellModel>(CalendarGridHelper.DaysInWeek);
            for (var dayIndex = 0; dayIndex < CalendarGridHelper.DaysInWeek; dayIndex++)
            {
                var date = weekStart.AddDays(dayIndex);
                tasksByDay.TryGetValue(date, out var dayTasks);
                days.Add(BuildDayCell(date, visibleMonth, selectedDay, today, dayTasks ?? []));
            }

            weeks.Add(new CalenderWeekModel
            {
                WeekNumber = ISOWeek.GetWeekOfYear(weekStart),
                Days = days
            });
        }

        return weeks;
    }

    public static IReadOnlyList<TaskDto> BuildDayTasks(
        bool isPositive,
        DateTime selectedDay,
        TaskDaySourceCache taskSource)
    {
        return TaskFilterService.Apply(
            new TaskFilterOptions
            {
                SelectedDate = selectedDay,
                IsPositive = isPositive,
                IsOpenTemplate = false,
                IsSortDescending = true,
                SortDoneTasksLast = true
            },
            taskSource.NormalTasks,
            taskSource.Templates,
            taskSource.DoneTasks);
    }

    private static CalenderDayCellModel BuildDayCell(
        DateTime date,
        DateTime visibleMonth,
        DateTime selectedDay,
        DateTime today,
        List<TaskDto> dayTasks)
    {
        var dots = dayTasks
            .OrderBy(task => task.IsDone)
            .ThenByDescending(task => task.Priority)
            .Take(MaxDotsPerDay)
            .Select(task => new CalenderDotModel { Priority = task.Priority, IsDone = task.IsDone })
            .ToList();

        return new CalenderDayCellModel
        {
            Date = date,
            IsInCurrentMonth = date.Month == visibleMonth.Month && date.Year == visibleMonth.Year,
            IsToday = date == today,
            IsSelected = date == selectedDay,
            Dots = dots,
            HasMoreTasks = dayTasks.Count > MaxDotsPerDay
        };
    }
}
