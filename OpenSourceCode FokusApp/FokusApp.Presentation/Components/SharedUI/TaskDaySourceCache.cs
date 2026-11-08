using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Components.SharedUI;

public sealed class TaskDaySourceCache
{
    private readonly ITaskCacheService _taskCacheService;
    private readonly List<TaskDto> _normalTasks = [];
    private readonly List<TaskDto> _templates = [];
    private readonly List<TaskDto> _doneTasks = [];

    public TaskDaySourceCache(ITaskCacheService taskCacheService)
    {
        _taskCacheService = taskCacheService;
    }

    public IReadOnlyList<TaskDto> NormalTasks => _normalTasks;
    public IReadOnlyList<TaskDto> Templates => _templates;
    public IReadOnlyList<TaskDto> DoneTasks => _doneTasks;

    public void RefreshNormal(DateTime? selectedDay) =>
        ReplaceTasks(_normalTasks, _taskCacheService.GetNormalTasks(selectedDay));

    public void RefreshTemplate(DateTime? selectedDay) =>
        ReplaceTasks(_templates, _taskCacheService.GetTemplates(selectedDay));

    public void RefreshDone(DateTime? selectedDay) =>
        ReplaceTasks(_doneTasks, _taskCacheService.GetDoneTasks(selectedDay));

    public TaskDto? FindById(Guid taskId)
    {
        return _normalTasks.FirstOrDefault(task => task.Id == taskId)
            ?? _templates.FirstOrDefault(task => task.Id == taskId)
            ?? _doneTasks.FirstOrDefault(task => task.Id == taskId);
    }

    public TaskDto? FindVisibleById(Guid taskId)
    {
        return _normalTasks.FirstOrDefault(task => task.Id == taskId)
            ?? _templates.FirstOrDefault(task =>
                !TaskFilterService.IsOpenTemplate(task) && task.Id == taskId)
            ?? _doneTasks.FirstOrDefault(task => task.Id == taskId);
    }

    private static void ReplaceTasks(List<TaskDto> target, IReadOnlyList<TaskDto> source)
    {
        target.Clear();
        target.AddRange(source);
    }
}
