using FokusApp.Core.Enums;

namespace FokusApp.Core.Models;

public sealed class TaskFilterOptions
{
    public string? SearchText { get; init; }
    public IReadOnlyCollection<TaskPriority> SelectedPriorities { get; init; } = [];
    public bool IsSortDescending { get; init; } = true;
    public DateTime? SelectedDate { get; init; }
    public bool KeepRegularTemplatesVisibleWhenDayFiltered { get; init; }
    public bool? IsPositive { get; init; }
    public bool? IsRegularTemplate { get; init; }
    public bool SortDoneTasksLast { get; init; }
}
