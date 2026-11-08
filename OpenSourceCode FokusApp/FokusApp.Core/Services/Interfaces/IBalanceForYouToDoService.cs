using FokusApp.Core.Models;

namespace FokusApp.Core.Services.Interfaces;

public interface IBalanceForYouToDoService
{
    Task<DayBalance> GetDayAsync(DateTime day);

    Task<DayBalance> GetTodayAsync();

    Task<DayTaskCount> GetTodayTaskCountAsync(bool isWorkScope);

    Task<WeekBalance> GetWeekAsync(DateTime dayInWeek);

    Task<WeekBalance> GetCurrentWeekAsync();

    Task<IReadOnlyList<WeekdayBalance>> GetWeekdayPlannedVersusDoneAsync(DateTime firstDay, DateTime lastDay);
}
