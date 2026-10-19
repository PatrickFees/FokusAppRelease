using System.ComponentModel;

namespace FokusApp.Components.Settings;

public interface ISettingsViewModel : INotifyPropertyChanged
{
    IReadOnlyList<RingtoneOption> AvailableRingtones { get; }
    string? SelectedRingtonePath { get; }
    string DataTransferMessage { get; }
    bool IsDataTransferMessageSuccess { get; }
    bool IsImportCanceledPopupOpen { get; }
    bool IsResetConfirmationOpen { get; }

    Task InitializeAsync();
    Task PlaySelectedRingtoneAsync();
    Task SelectRingtoneAsync(string? value);
    void ClearDataTransferMessage();
    Task ExportDataAsync();
    Task ImportDataAsync();
    void CloseImportCanceledPopup();
    void RequestResetData();
    void CancelResetData();
    Task ConfirmResetDataAsync();
}
