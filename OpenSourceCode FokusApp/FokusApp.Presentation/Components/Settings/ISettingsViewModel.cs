using System.ComponentModel;
using FokusApp.Core.Enums;

namespace FokusApp.Components.Settings;

public interface ISettingsViewModel : INotifyPropertyChanged
{
    IReadOnlyList<RingtoneOption> AvailableRingtones { get; }
    string? SelectedRingtonePath { get; }
    string DataTransferMessage { get; }
    bool IsDataTransferMessageSuccess { get; }
    bool IsImportCanceledPopupOpen { get; }
    bool IsResetConfirmationOpen { get; }
    bool IsCalendarImportAvailable { get; }
    bool IsCalendarImportOpen { get; }
    bool IsCalendarAutoSyncAvailable { get; }
    bool CalendarAutoSyncEnabled { get; }
    DateTime? CalendarAutoSyncLastRunAt { get; }
    bool IsCalendarAutoSyncPickerOpen { get; }
    bool MicroFeedbackEnabled { get; }
    bool ReviewsEnabled { get; }
    bool CompanionEnabled { get; }
    BalanceGoal BalanceGoal { get; }
    int CustomForYou { get; }
    int CustomToDo { get; }
    DefaultScope DefaultScope { get; }
    bool TimerAlertSoundEnabled { get; }
    bool TimerAlertVibrationEnabled { get; }
    bool TimerAlertAdaptToRingerModeEnabled { get; }
    bool IsTimerAlertVibrationAvailable { get; }
    bool IsTimerAlertAdaptToRingerModeAvailable { get; }

    Task InitializeAsync();
    Task SetMicroFeedbackEnabledAsync(bool enabled);
    Task SetReviewsEnabledAsync(bool enabled);
    Task SetCompanionEnabledAsync(bool enabled);
    Task SetBalanceGoalAsync(BalanceGoal goal);
    Task SetCustomBalanceAsync(int forYou, int toDo);
    Task SetDefaultScopeAsync(DefaultScope scope);
    Task SetTimerAlertSoundEnabledAsync(bool enabled);
    Task SetTimerAlertVibrationEnabledAsync(bool enabled);
    Task SetTimerAlertAdaptToRingerModeEnabledAsync(bool enabled);
    Task PlaySelectedRingtoneAsync();
    Task SelectRingtoneAsync(string? value);
    void ClearDataTransferMessage();
    Task ExportDataAsync();
    Task ImportDataAsync();
    Task ExportCalendarAsync();
    void CloseImportCanceledPopup();
    void RequestResetData();
    void CancelResetData();
    Task ConfirmResetDataAsync();
    void OpenCalendarImport();
    void CloseCalendarImport();
    Task SetCalendarAutoSyncEnabledAsync(bool enabled);
    void OpenCalendarAutoSyncPicker();
    void CloseCalendarAutoSyncPicker();
}
