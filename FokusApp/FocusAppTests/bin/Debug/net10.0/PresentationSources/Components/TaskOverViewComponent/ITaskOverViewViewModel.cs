using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Components.SharedUI.FilterComponent;
using FokusApp.Core.Models;
using System.ComponentModel;

namespace FokusApp.Components.TaskOverViewComponent;

public interface ITaskOverViewViewModel : INotifyPropertyChanged
{
    IReadOnlyList<TaskDto> ActiveTasks { get; }
    IReadOnlyList<TaskDto> RegularTasks { get; }
    IReadOnlyList<TaskDto> CompletedTasks { get; }
    FilterModel Filter { get; }
    bool IsFilterPanelOpen { get; }
    TaskEditPopUpInputModel? TaskEditorInput { get; }
    bool IsAddNoteModalOpen { get; }

    Task InitializeAsync();
    void ToggleFilterPanel();
    void CloseFilterPanel();
    void ApplyOverviewFilters(FilterModel payload);
    Task HandleTaskEditPopupCloseAsync(TaskEditPopUpClosePayload payload);
    void OpenAddNoteModal();
    void StartEditTask(Guid taskId);
    Task DeleteTaskAsync(Guid taskId);
    bool IsTaskActive(Guid? taskId);
}
