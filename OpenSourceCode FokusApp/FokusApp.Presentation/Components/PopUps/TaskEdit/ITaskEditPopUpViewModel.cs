using FokusApp.Components.PopUps.TaskSchedule;
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
    bool IsTemplateLocked { get; }
    TaskDto Model { get; }
    bool HasSelectedTaskPositivity { get; }
    string FocusTimerDisplay { get; }
    string BreakTimerDisplay { get; }
    DateTime CalendarDate { get; }
    bool IsTaskDateAllowed { get; }
    bool HasTemplateOptions { get; }
    IReadOnlyList<TaskEditTemplateOptionModel> TemplateOptions { get; }
    int SelectedPriorityIndex { get; }
    int MaximumPriorityIndex { get; }
    bool CanSave { get; }
    TaskDayButtonModel TaskDayButtonModel { get; }

    bool HasSchedule { get; }

    string? ScheduleTimeDisplay { get; }
    bool IsRepeating { get; }

    bool CanRepeat { get; }

    void Open(TaskEditPopUpInputModel? inputModel);
    TaskScheduleModel CreateScheduleModel();
    void ApplySchedule(TaskScheduleModel schedule);
    void SetPriorityIndex(int index);
    void SelectTaskPositivity(bool isPositive);
    void SetDate(DateTime? selectedDate);
    void SetTemplateFlag(bool isTemplate);
    int GetTimerMinutes(TimerType type);
    void AdjustTimer(TimerType type, int deltaMinutes);
    void SetTimerMinutes(TimerType type, int minutes);
    TaskEditPopUpClosePayload CreateClosePayload(
        TaskEditPopUpCloseAction action,
        Guid? templateTaskId);
    void RequestOpeningCalendarPopUp();
    void CloseCalendarPopup();
    void Close();
}
