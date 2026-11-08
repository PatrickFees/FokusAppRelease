using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Core.Enums;
using FokusApp.Components.SharedUI.FilterComponent;
using FokusApp.Core.Models;
using System.ComponentModel;

namespace FokusApp.Components.TaskOverViewComponent;

public interface ITaskOverViewViewModel : INotifyPropertyChanged
{
    IReadOnlyList<TaskDto> ActiveTasks { get; }
    IReadOnlyList<TaskDto> OverdueTasks { get; }
    IReadOnlyList<TaskDto> Templates { get; }
    IReadOnlyList<TaskDto> CompletedTasks { get; }
    FilterModel Filter { get; }
    bool IsFilterPanelOpen { get; }
    TaskEditPopUpInputModel? TaskEditorInput { get; }
    bool IsAddNoteModalOpen { get; }

    bool IsReschedulePopupOpen { get; }
    DateTime RescheduleInitialDate { get; }

    UndoNotice? CurrentUndo { get; }

    Task AssignPastTaskAsync(Guid taskId, PastTaskTarget target);
    Task DiscardPastTaskAsync(Guid taskId);
    Task AssignAllPastTasksToTomorrowAsync();
    Task UndoAsync();
    void DismissUndo();
    void OpenReschedulePopup(Guid taskId);
    void CloseReschedulePopup();
    Task ConfirmRescheduleAsync(DateTime newDate);
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
