using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Core.Models;
using FokusApp.Core.Enums;

namespace FokusApp.Components.SharedUI.FilterComponent; 
public sealed partial class FilterModel : ObservableObject
{
    private IReadOnlyCollection<TaskPriority> _selectedPriorities = [];
    public IReadOnlyCollection<TaskPriority> SelectedPriorities 
    {
        get => _selectedPriorities;
        set
        {
            var normalizedPriorities = NormalizeAndOrderPriorities(value);

            SetProperty(ref _selectedPriorities, normalizedPriorities);
        }
    }

    [ObservableProperty]
    private DateTime? selectedDate;
    [ObservableProperty]
    private string searchText = string.Empty;
    [ObservableProperty]
    private bool isSortDescending = true;
  

    public FilterModel Snapshot()
    {
        return new FilterModel
        {
            SearchText = SearchText,
            SelectedPriorities = SelectedPriorities,
            IsSortDescending = IsSortDescending,
            SelectedDate = SelectedDate
        };
    }

    public void Apply(FilterModel? source)
    {
        var model = source ?? new FilterModel();

        SearchText = model.SearchText;
        SelectedPriorities = model.SelectedPriorities;
        IsSortDescending = model.IsSortDescending;
        SelectedDate = model.SelectedDate;
    }

    public bool HasSameFilterState(FilterModel other)
    {
        if (other is null)
        {
            return false;
        }

        if (!string.Equals(SearchText, other.SearchText, StringComparison.Ordinal)
            || IsSortDescending != other.IsSortDescending
            || SelectedDate?.Date != other.SelectedDate?.Date)
        {
            return false;
        }

        return SelectedPriorities.OrderBy(priority => priority)
            .SequenceEqual(other.SelectedPriorities.OrderBy(priority => priority));
    }

    public TaskFilterOptions ToTaskFilterOptions(
        bool keepRegularTemplatesVisibleWhenDayFiltered = false)
    {
        return new TaskFilterOptions
        {
            SearchText = SearchText,
            SelectedPriorities = SelectedPriorities,
            IsSortDescending = IsSortDescending,
            SelectedDate = SelectedDate,
            KeepRegularTemplatesVisibleWhenDayFiltered = keepRegularTemplatesVisibleWhenDayFiltered
        };
    }

    private static TaskPriority NormalizePriority(TaskPriority priority)
    {
        return Enum.IsDefined(typeof(TaskPriority), priority)
            ? priority
            : TaskPriority.Keine;
    }

    private static IReadOnlyList<TaskPriority> NormalizeAndOrderPriorities(
        IReadOnlyCollection<TaskPriority>? priorities)
    {
        return (priorities ?? [])
            .Select(NormalizePriority)
            .Distinct()
            .OrderBy( priority => priority)
            .ToList();
    }
}
