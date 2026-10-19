using FokusApp.Components.SharedUI.FilterComponent;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using System.ComponentModel;

namespace FokusApp.Components.FocusTimerComponent;

public interface IFocusTimerViewModel : INotifyPropertyChanged
{
    bool IsRunning { get; }
    bool IsTimerActive { get; }
    bool IsBreakMode { get; }
    bool IsNotificationPermissionMissing { get; }
    string SelectedTaskNote { get; }
    TaskPriority SelectedTaskPriority { get; }
    bool CanResetTimer { get; }
    bool HasAvailableTasks { get; }
    FilterModel TaskFilter { get; }
    IReadOnlyList<TaskDto> NormalTasks { get; }
    IReadOnlyList<TaskDto> RegularTasks { get; }
    TaskDto SelectedTask { get; }
    double CountdownProgress { get; }
    double SelectedMinuteRatio { get; }
    int InputMinutes { get; }
    Guid? SelectedTaskId { get; }
    bool CanToggleTimer { get; }
    string FormattedTime { get; }

    Task InitializeAsync();
    Task SwitchTimerToStartOrStop();
    Task ResetTimer();
    void SetMinutes(int minutes);
    void PrepareTaskPicker();
    Task ApplyFilterChangesAsync(FilterModel payload);
    Task SelectTaskAsync(Guid? taskId);
}
