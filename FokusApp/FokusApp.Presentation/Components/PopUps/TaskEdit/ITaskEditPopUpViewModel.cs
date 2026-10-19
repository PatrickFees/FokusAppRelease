using FokusApp.Components.SharedUI.TaskDayButton;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using System.ComponentModel;

namespace FokusApp.Components.PopUps.TaskEdit;

public interface ITaskEditPopUpViewModel : INotifyPropertyChanged
{
    int TimerStepMinutes { get; }
    bool ShowCalendarPopup { get; }
    bool IsUpdateMode { get; }
    bool IsRegularTaskLocked { get; }
    TaskDto Model { get; }
    bool HasSelectedTaskPositivity { get; }
    string FocusTimerDisplay { get; }
    string BreakTimerDisplay { get; }
    DateTime CalendarDate { get; }
    bool IsTaskDateAllowed { get; }
    bool HasRegularTemplateOptions { get; }
    IReadOnlyList<TaskEditRegularTemplateOptionModel> RegularTemplateOptions { get; }
    int SelectedPriorityIndex { get; }
    int MaximumPriorityIndex { get; }
    bool CanSave { get; }
    TaskDayButtonModel TaskDayButtonModel { get; }

    void Open(TaskEditPopUpInputModel? inputModel);
    void SetPriorityIndex(int index);
    void SelectTaskPositivity(bool isPositive);
    void SetDate(DateTime? selectedDate);
    void SetRegularity(bool isRegular);
    int GetTimerMinutes(TimerType type);
    void AdjustTimer(TimerType type, int deltaMinutes);
    void SetTimerMinutes(TimerType type, int minutes);
    TaskEditPopUpClosePayload CreateClosePayload(
        TaskEditPopUpCloseAction action,
        Guid? regularTemplateTaskId);
    void RequestOpeningCalendarPopUp();
    void CloseCalendarPopup();
    void Close();
}
