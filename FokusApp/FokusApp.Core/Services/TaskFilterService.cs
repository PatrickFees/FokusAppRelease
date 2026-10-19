using FokusApp.Core.Enums;
using FokusApp.Core.Models;

namespace FokusApp.Core.Services;

public static class TaskFilterService
{
    public static IReadOnlyList<TaskDto> Apply(
        TaskFilterOptions? options,
        params IEnumerable<TaskDto>?[] taskSources)
    {
        var filter = options ?? new TaskFilterOptions();
        var query = taskSources
            .Where(source => source is not null)
            .SelectMany(source => source!);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var normalizedSearch = filter.SearchText.Trim();
            query = query.Where(task =>
                (task.Note ?? string.Empty).Contains(normalizedSearch, StringComparison.CurrentCultureIgnoreCase));
        }

        var selectedPriorities = filter.SelectedPriorities
            .Select(NormalizePriority)
            .ToHashSet();

        if (selectedPriorities.Count > 0)
        {
            query = query.Where(task => selectedPriorities.Contains(NormalizePriority(task.Priority)));
        }

        if (filter.IsPositive.HasValue)
        {
            query = query.Where(task => task.IsPositive == filter.IsPositive.Value);
        }

        if (filter.IsRegularTemplate.HasValue)
        {
            query = query.Where(task => IsRegularTemplate(task) == filter.IsRegularTemplate.Value);
        }

        if (filter.SelectedDate.HasValue)
        {
            var selectedDate = filter.SelectedDate.Value.Date;
            query = query.Where(task =>
                filter.KeepRegularTemplatesVisibleWhenDayFiltered && IsRegularTemplate(task) ||
                ExtractTaskDate(task) == selectedDate);
        }

        if (filter.SortDoneTasksLast)
        {
            return filter.IsSortDescending
                ? query.OrderBy(task => task.IsDone).ThenByDescending(task => task.CreationDate).ToList()
                : query.OrderBy(task => task.IsDone).ThenBy(task => task.CreationDate).ToList();
        }

        return filter.IsSortDescending
            ? query.OrderByDescending(task => task.CreationDate).ToList()
            : query.OrderBy(task => task.CreationDate).ToList();
    }

    public static bool IsRegularTemplate(TaskDto task)
    {
        return task.IsRegularTask && !task.IsDone;
    }

    private static DateTime ExtractTaskDate(TaskDto task)
    {
        return (task.TaskDate ?? task.CreationDate).Date;
    }

    private static TaskPriority NormalizePriority(TaskPriority priority)
    {
        return Enum.IsDefined(typeof(TaskPriority), priority)
            ? priority
            : TaskPriority.Keine;
    }
}
