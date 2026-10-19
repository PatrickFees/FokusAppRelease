using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FokusApp.Core.Enums;
using FokusApp.Core.Services.Interfaces;
using System.ComponentModel;

namespace FokusApp.Components.SharedUI.FilterComponent;

public sealed partial class FilterComponentViewModel : ObservableObject, IFilterComponentViewModel, IDisposable
{
    private readonly TimeProvider _timeProvider;
    private readonly IDayChangeService _dayChangeService;
    private FilterModel _lastNotifiedFilterPayload;

    [ObservableProperty]
    private bool isOpen;

    [ObservableProperty]
    private bool isDayCalendarOpen;

    public FilterComponentViewModel(TimeProvider timeProvider, IDayChangeService dayChangeService)
    {
        _timeProvider = timeProvider;
        _dayChangeService = dayChangeService;
        _dayChangeService.DayChanged += OnDayChanged;
        Model.SelectedDate = GetToday();
        _lastNotifiedFilterPayload = CreateDefaultFilter();
        Model.PropertyChanged += OnModelPropertyChanged;
        TogglePriorityFilterCommand = new RelayCommand<TaskPriority>(TogglePriorityFilter);
        ToggleSortDirectionCommand = new RelayCommand(ToggleSortDirection);
        ResetFiltersCommand = new RelayCommand(ResetFilters);
        CloseCommand = new RelayCommand(Close);
    }

    public IRelayCommand<TaskPriority> TogglePriorityFilterCommand { get; }
    public IRelayCommand ToggleSortDirectionCommand { get; }
    public IRelayCommand ResetFiltersCommand { get; }
    public IRelayCommand CloseCommand { get; }

    public FilterModel Model { get; } = new();

    public IReadOnlyCollection<TaskPriority> SelectedPriorities => Model.SelectedPriorities;

    public DateTime DisplayDate => Model.SelectedDate ?? GetToday();

    public void Open(
        bool resetFilters = true,
        string? initialSearchText = null,
        IReadOnlyCollection<TaskPriority>? initialSelectedPriorities = null,
        bool initialIsSortDescending = true,
        DateTime? initialSelectedDate = null)
    {
        if (resetFilters)
        {
            Model.SearchText = string.Empty;
            Model.SelectedPriorities = [];
            Model.IsSortDescending = true;
            Model.SelectedDate = GetToday();
        }
        else
        {
            Model.SearchText = initialSearchText ?? string.Empty;
            Model.SelectedPriorities = initialSelectedPriorities ?? [];
            Model.IsSortDescending = initialIsSortDescending;
            Model.SelectedDate = initialSelectedDate;
        }

        IsDayCalendarOpen = false;
        IsOpen = true;

        _lastNotifiedFilterPayload = Model.Snapshot();
    }

    public FilterModel CreateFilterPayload()
    {
        return Model.Snapshot();
    }

    public bool HasFilterChanged()
    {
        return !Model.HasSameFilterState(_lastNotifiedFilterPayload);
    }

    public void MarkFilterChangesNotified()
    {
        _lastNotifiedFilterPayload = Model.Snapshot();
    }

    public void ResetLastNotifiedFilterPayload()
    {
        _lastNotifiedFilterPayload = CreateDefaultFilter();
    }

    private DateTime GetToday() => _timeProvider.GetLocalNow().Date;

    private FilterModel CreateDefaultFilter()
    {
        return new FilterModel
        {
            SelectedDate = GetToday()
        };
    }

    private void ResetFilters()
    {
        Model.SearchText = string.Empty;
        Model.SelectedPriorities = [];
        Model.IsSortDescending = true;
        Model.SelectedDate = null;
        IsDayCalendarOpen = false;
    }

    private void TogglePriorityFilter(TaskPriority priority)
    {
        var normalizedPriority = NormalizePriority(priority);
        var selectedPriorities = Model.SelectedPriorities
            .Select(NormalizePriority)
            .ToHashSet();

        if (!selectedPriorities.Add(normalizedPriority))
        {
            selectedPriorities.Remove(normalizedPriority);
        }

        Model.SelectedPriorities = selectedPriorities.ToArray();
    }

    private void ToggleSortDirection()
    {
        Model.IsSortDescending = !Model.IsSortDescending;
    }

    public void OpenDayCalendar()
    {
        IsDayCalendarOpen = true;
    }

    public void CloseDayCalendar()
    {
        IsDayCalendarOpen = false;
    }

    public void SelectDate(DateTime date)
    {
        Model.SelectedDate = date.Date;
        IsDayCalendarOpen = false;
    }

    private void Close()
    {
        IsOpen = false;
    }

    private void OnDayChanged(object? sender, DayChangedEventArgs e)
    {
        if (Model.SelectedDate?.Date == e.PreviousDay)
        {
            Model.SelectedDate = e.CurrentDay;
        }
    }

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (string.Equals(args.PropertyName, nameof(FilterModel.SelectedPriorities), StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(SelectedPriorities));
        }
    }

    public void Dispose()
    {
        _dayChangeService.DayChanged -= OnDayChanged;
        Model.PropertyChanged -= OnModelPropertyChanged;
    }

    private static TaskPriority NormalizePriority(TaskPriority priority)
    {
        return Enum.IsDefined(typeof(TaskPriority), priority)
            ? priority
            : TaskPriority.Keine;
    }
}
