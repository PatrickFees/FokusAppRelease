using System.ComponentModel;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Core.Models;

namespace FokusApp.Components.Calender;

public interface ICalenderViewModel : INotifyPropertyChanged
{
    DateTime VisibleMonth { get; }
    DateTime SelectedDay { get; }
    IReadOnlyList<CalenderWeekModel> Weeks { get; }
    bool IsCalendarCollapsed { get; }
    IReadOnlyList<TaskDto> ForYouTasks { get; }
    IReadOnlyList<TaskDto> ToDoTasks { get; }
    TaskEditPopUpInputModel? TaskEditorInput { get; }
    bool IsTaskEditorOpen { get; }
    bool IsTaskActionPopupOpen { get; }
    bool CanStartSelectedTask { get; }
    bool CanCompleteSelectedTask { get; }
    bool CanDeleteSelectedTask { get; }
    bool IsSelectedActionTaskActive { get; }

    Task InitializeAsync();
    void GoToPreviousMonth();
    void GoToNextMonth();
    void GoToToday();
    void ToggleCalendarCollapsed();
    void MoveSelectedDay(bool forwards);
    void SelectDay(DateTime date);
    void OpenTaskEditorForNewTask(DateTime date);
    void OpenTaskEditorForNewTask(bool isPositive);
    Task HandleTaskEditPopupCloseAsync(TaskEditPopUpClosePayload payload);
    void OpenTaskActionPopup(Guid taskId);
    void CloseTaskActionPopup();
    void StartEditSelectedTask();
    Task MarkSelectedTaskAsDoneAsync();
    Task DeleteSelectedTaskAsync();
    Task<bool> ExecutePrimarySelectedTaskActionAsync();
}
