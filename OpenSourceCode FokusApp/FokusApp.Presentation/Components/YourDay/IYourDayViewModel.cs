using System.ComponentModel;
using FokusApp.Components.Calender;
using FokusApp.Components.PopUps.TaskEdit;
using FokusApp.Components.PopUps.TaskSeriesChoice;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;

namespace FokusApp.Components.YourDay;

public interface IYourDayViewModel : INotifyPropertyChanged
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

    bool IsSeriesChoiceOpen { get; }
    TaskSeriesAction PendingSeriesAction { get; }
    bool CanStartSelectedTask { get; }
    bool CanCompleteSelectedTask { get; }
    bool CanDeleteSelectedTask { get; }
    bool IsSelectedActionTaskActive { get; }
    int PastTaskCount { get; }
    Guid? RecentlyCompletedTaskId { get; }

    Task InitializeAsync();
    void GoToPreviousMonth();
    void GoToNextMonth();
    void GoToToday();
    void OpenPastTasks();
    void ToggleCalendarCollapsed();
    void MoveSelectedDay(bool forwards);
    void SelectDay(DateTime date);
    void OpenTaskEditorForNewTask(DateTime date);
    void OpenTaskEditorForNewTask(bool isPositive);
    Task HandleTaskEditPopupCloseAsync(TaskEditPopUpClosePayload payload);
    void OpenTaskActionPopup(Guid taskId);
    void CloseTaskActionPopup();
    void StartEditSelectedTask();
    Task ChooseSeriesScopeAsync(TaskSeriesScope scope);
    void CancelSeriesChoice();
    Task MarkSelectedTaskAsDoneAsync();
    Task DeleteSelectedTaskAsync();
    Task<bool> ExecutePrimarySelectedTaskActionAsync();
}
