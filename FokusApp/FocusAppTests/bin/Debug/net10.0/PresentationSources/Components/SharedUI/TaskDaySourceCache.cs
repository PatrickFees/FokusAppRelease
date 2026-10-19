using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Components.SharedUI;

/// <summary>
/// Holds a ViewModel-owned snapshot of normal/regular/done tasks as returned by
/// ITaskCacheService (already scope- and day-filtered there). Deliberately does not
/// delegate lookups to ITaskCacheService.FindTaskById, which searches the entire
/// cache across all scopes and dates - callers here only ever act on tasks that are
/// part of the currently loaded, scope-filtered snapshot.
/// </summary>
public sealed class TaskDaySourceCache
{
    private readonly ITaskCacheService _taskCacheService;
    private readonly List<TaskDto> _normalTasks = [];
    private readonly List<TaskDto> _regularTasks = [];
    private readonly List<TaskDto> _doneTasks = [];

    public TaskDaySourceCache(ITaskCacheService taskCacheService)
    {
        _taskCacheService = taskCacheService;
    }

    public IReadOnlyList<TaskDto> NormalTasks => _normalTasks;
    public IReadOnlyList<TaskDto> RegularTasks => _regularTasks;
    public IReadOnlyList<TaskDto> DoneTasks => _doneTasks;

    public void RefreshNormal(DateTime? selectedDay) =>
        ReplaceTasks(_normalTasks, _taskCacheService.GetNormalTasks(selectedDay));

    public void RefreshRegular(DateTime? selectedDay) =>
        ReplaceTasks(_regularTasks, _taskCacheService.GetRegularTasks(selectedDay));

    public void RefreshDone(DateTime? selectedDay) =>
        ReplaceTasks(_doneTasks, _taskCacheService.GetDoneTasks(selectedDay));

    public TaskDto? FindById(Guid taskId)
    {
        return _normalTasks.FirstOrDefault(task => task.Id == taskId)
            ?? _regularTasks.FirstOrDefault(task => task.Id == taskId)
            ?? _doneTasks.FirstOrDefault(task => task.Id == taskId);
    }

    public TaskDto? FindVisibleById(Guid taskId)
    {
        return _normalTasks.FirstOrDefault(task => task.Id == taskId)
            ?? _regularTasks.FirstOrDefault(task =>
                !TaskFilterService.IsRegularTemplate(task) && task.Id == taskId)
            ?? _doneTasks.FirstOrDefault(task => task.Id == taskId);
    }

    private static void ReplaceTasks(List<TaskDto> target, IReadOnlyList<TaskDto> source)
    {
        target.Clear();
        target.AddRange(source);
    }
}
