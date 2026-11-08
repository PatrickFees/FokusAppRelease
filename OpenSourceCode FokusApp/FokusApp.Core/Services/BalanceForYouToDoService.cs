using FokusApp.Core.DataBase.Repositories.Interfaces;
using FokusApp.Core.Extensions;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class BalanceForYouToDoService : IBalanceForYouToDoService
{
    private const int DaysInWeek = 7;

    private readonly ITaskRepository _taskRepository;
    private readonly IRewardingSettingsService _rewardingSettingsService;
    private readonly TimeProvider _timeProvider;

    public BalanceForYouToDoService(
        ITaskRepository taskRepository,
        IRewardingSettingsService rewardingSettingsService,
        TimeProvider timeProvider)
    {
        _taskRepository = taskRepository;
        _rewardingSettingsService = rewardingSettingsService;
        _timeProvider = timeProvider;
    }

    public async Task<DayBalance> GetDayAsync(DateTime day)
    {
        var tasks = await LoadConcreteTasksAsync().ConfigureAwait(false);
        return BuildDay(day.Date, tasks);
    }

    public Task<DayBalance> GetTodayAsync() => GetDayAsync(GetToday());

    public async Task<DayTaskCount> GetTodayTaskCountAsync(bool isWorkScope)
    {
        var tasks = await LoadConcreteTasksAsync().ConfigureAwait(false);
        var today = GetToday();

        var counted = tasks
            .Where(task => task.IsWorkTask == isWorkScope)
            .Where(task => TaskExtension.GetEffectiveCompletedDate(task) == today ||
                           (!task.IsTemplate && task.TaskDate?.Date == today))
            .ToList();

        var forYou = counted.Count(task => task.IsPositive);
        var doneForYou = counted.Count(task => task.IsDone && task.IsPositive);
        var doneToDo = counted.Count(task => task.IsDone && !task.IsPositive);
        return new DayTaskCount(today, forYou, counted.Count - forYou, doneForYou, doneToDo);
    }

    public async Task<WeekBalance> GetWeekAsync(DateTime dayInWeek)
    {
        var tasks = await LoadConcreteTasksAsync().ConfigureAwait(false);
        var weekStart = BalanceForYouToDoRules.GetWeekStart(dayInWeek);

        var days = Enumerable.Range(0, DaysInWeek)
            .Select(offset => BuildDay(weekStart.AddDays(offset), tasks))
            .ToList();

        return new WeekBalance(
            weekStart,
            days,
            days.Count(day => day.IsBalanceDay),
            days.Count(day => day.MeetsGoal),
            days.Sum(day => day.DoneForYou),
            days.Sum(day => day.DoneToDo),
            days.Sum(day => day.FocusMinutes),
            FindBestDay(days));
    }

    public Task<WeekBalance> GetCurrentWeekAsync() => GetWeekAsync(GetToday());

    public async Task<IReadOnlyList<WeekdayBalance>> GetWeekdayPlannedVersusDoneAsync(DateTime firstDay, DateTime lastDay)
    {
        var tasks = await LoadConcreteTasksAsync().ConfigureAwait(false);
        var start = BalanceForYouToDoRules.GetWeekStart(firstDay);
        var end = BalanceForYouToDoRules.GetWeekStart(lastDay).AddDays(DaysInWeek - 1);

        var plannedForYou = new int[DaysInWeek];
        var plannedToDo = new int[DaysInWeek];
        var doneForYou = new int[DaysInWeek];
        var doneToDo = new int[DaysInWeek];

        for (var day = start; day <= end; day = day.AddDays(1))
        {
            var balance = BuildDay(day, tasks);
            var index = ((int)day.DayOfWeek + 6) % DaysInWeek;
            plannedForYou[index] += balance.PlannedForYou;
            plannedToDo[index] += balance.PlannedToDo;
            doneForYou[index] += balance.DoneForYou;
            doneToDo[index] += balance.DoneToDo;
        }

        return Enumerable.Range(0, DaysInWeek)
            .Select(index => new WeekdayBalance(
                (DayOfWeek)(((int)DayOfWeek.Monday + index) % DaysInWeek),
                plannedForYou[index],
                plannedToDo[index],
                doneForYou[index],
                doneToDo[index]))
            .ToList();
    }

    private DateTime GetToday() => _timeProvider.GetLocalNow().Date;

    private async Task<List<TaskDto>> LoadConcreteTasksAsync()
    {
        var entities = await _taskRepository.GetAllAsync().ConfigureAwait(false);
        return entities
            .Select(TaskExtension.MapToDto)
            .Where(task => !task.IsTemplate || task.IsDone)
            .ToList();
    }

    private DayBalance BuildDay(DateTime day, List<TaskDto> tasks)
    {
        var doneToday = tasks
            .Where(task => TaskExtension.GetEffectiveCompletedDate(task) == day)
            .ToList();
        var plannedToday = tasks
            .Where(task => !task.IsTemplate && task.TaskDate?.Date == day)
            .ToList();

        var doneForYou = doneToday.Count(task => task.IsPositive);
        var doneToDo = doneToday.Count - doneForYou;
        var rewarding = _rewardingSettingsService.Rewarding;

        return new DayBalance(
            day,
            doneForYou,
            doneToDo,
            plannedToday.Count(task => task.IsPositive),
            plannedToday.Count(task => !task.IsPositive),
            doneToday.Sum(task => Math.Max(0, task.TimerMinutes)),
            BalanceForYouToDoRules.IsBalanceDay(doneForYou, doneToDo),
            BalanceForYouToDoRules.MeetsGoal(
                doneForYou,
                doneToDo,
                rewarding.BalanceGoal,
                rewarding.CustomForYou,
                rewarding.CustomToDo));
    }

    private static DateTime? FindBestDay(List<DayBalance> days)
    {
        return days
            .Where(day => day.DoneTotal > 0)
            .OrderByDescending(day => day.IsBalanceDay)
            .ThenByDescending(day => day.DoneTotal)
            .ThenBy(day => day.Date)
            .Select(day => (DateTime?)day.Date)
            .FirstOrDefault();
    }
}
