using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;

namespace FokusApp.Components.Settings; 
public sealed class SettingsViewModel : ObservableObject, ISettingsViewModel
{
    private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;

    private readonly IRingtoneCatalogService _ringtoneCatalogService;
    private readonly IUserSettingsService _userSettingsService;
    private readonly IAppStartupStateService _appStartupStateService;
    private readonly ITimerAndAudioService _timerAndAudioService;
    private readonly IDataImportExportService _dataImportExportService;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly IUiTextProvider _textProvider;
    private readonly List<RingtoneOption> _ringtoneOptions = [];

    private RingtoneOption? _selectedRingtone;

    public IReadOnlyList<RingtoneOption> AvailableRingtones => _ringtoneOptions;

    public string? SelectedRingtonePath => _selectedRingtone?.FilePath;

    public SettingsViewModel(
        IRingtoneCatalogService ringtoneCatalogService,
        IUserSettingsService userSettingsService,
        IAppStartupStateService appStartupStateService,
        ITimerAndAudioService timerAndAudioService,
        IDataImportExportService dataImportExportService,
        IAppErrorCaptureService appErrorCaptureService,
        IUiTextProvider textProvider)
    {
        _ringtoneCatalogService = ringtoneCatalogService;
        _userSettingsService = userSettingsService;
        _appStartupStateService = appStartupStateService;
        _timerAndAudioService = timerAndAudioService;
        _dataImportExportService = dataImportExportService;
        _appErrorCaptureService = appErrorCaptureService;
        _textProvider = textProvider;
    }

    public async Task InitializeAsync()
    {
        await _appStartupStateService.WaitForReadyAsync();
        LoadRingtones();
        await EnsureSelectionAsync();
        NotifyStateChanged();
    }

    public async Task PlaySelectedRingtoneAsync()
    {
        var ringtonePath = SelectedRingtonePath;

        if (string.IsNullOrWhiteSpace(ringtonePath))
        {
            ringtonePath = _ringtoneCatalogService.DefaultRingtoneFilePath;
        }

        await _timerAndAudioService.PlaySoundAsync(ringtonePath).ConfigureAwait(false);
    }

    public async Task SelectRingtoneAsync(string? value)
    {
        var matchedOption = string.IsNullOrWhiteSpace(value)
            ? null
            : _ringtoneOptions.FirstOrDefault(option => PathComparer.Equals(option.FilePath, value));

        if (!string.IsNullOrWhiteSpace(value) && matchedOption is null)
        {
            return;
        }

        if (matchedOption is null)
        {
            if (_selectedRingtone is null)
            {
                return;
            }

            _selectedRingtone = null;
            NotifyStateChanged();
            return;
        }

        if (_selectedRingtone is not null && _selectedRingtone.Id == matchedOption.Id)
        {
            return;
        }

        _selectedRingtone = matchedOption;
        await _userSettingsService.SetSelectedRingtoneAsync(matchedOption.Id);
        NotifyStateChanged();
    }


    public string DataTransferMessage { get; private set; } = string.Empty;

    public bool IsDataTransferMessageSuccess { get; private set; }

    public bool IsImportCanceledPopupOpen { get; private set; }

    public bool IsResetConfirmationOpen { get; private set; }

    public void ClearDataTransferMessage()
    {
        DataTransferMessage = string.Empty;
        NotifyStateChanged();
    }

    public async Task ExportDataAsync()
    {
        await ExecuteDataTransferAsync(() => _dataImportExportService.ExportAsync()).ConfigureAwait(false);
    }

    public async Task ImportDataAsync()
    {
        var result = await ExecuteDataTransferAsync(() => _dataImportExportService.ImportAsync());

        if (result?.IsCanceled == true)
        {
            DataTransferMessage = string.Empty;
            IsImportCanceledPopupOpen = true;
            NotifyStateChanged();
            return;
        }

        if (result?.IsSuccess == true)
        {
            await ReloadRingtonesAsync();
        }
    }

    public void CloseImportCanceledPopup()
    {
        IsImportCanceledPopupOpen = false;
        NotifyStateChanged();
    }

    public void RequestResetData()
    {
        IsResetConfirmationOpen = true;
        NotifyStateChanged();
    }

    public void CancelResetData()
    {
        IsResetConfirmationOpen = false;
        NotifyStateChanged();
    }

    public async Task ConfirmResetDataAsync()
    {
        IsResetConfirmationOpen = false;
        var result = await ExecuteDataTransferAsync(() => _dataImportExportService.ResetToStartDataAsync());

        if (result?.IsSuccess == true)
        {
            await ReloadRingtonesAsync();
        }
    }

    private async Task ReloadRingtonesAsync()
    {
        LoadRingtones();
        await EnsureSelectionAsync();
        NotifyStateChanged();
    }

    private async Task<DataTransferResult?> ExecuteDataTransferAsync(Func<Task<DataTransferResult>> action)
    {
        DataTransferResult? result = null;
        try
        {
            result = await action();
            DataTransferMessage = result.Message;
            IsDataTransferMessageSuccess = result.IsSuccess;
        }
        catch (Exception ex)
        {
            DataTransferMessage = _textProvider.GetText("DataActionFailed");
            IsDataTransferMessageSuccess = false;
            await _appErrorCaptureService
                .CaptureUiErrorAsync(ex, $"{nameof(SettingsViewModel)}.{nameof(ExecuteDataTransferAsync)}");
        }

        NotifyStateChanged();
        return result;
    }

    private void LoadRingtones()
    {
        _ringtoneOptions.Clear();

        var ringtones = _ringtoneCatalogService.GetAllRingtones();

        foreach (var ringtone in ringtones
            .Where(ringtone => !string.IsNullOrWhiteSpace(ringtone.RingtoneFilePath))
            .OrderBy(ringtone => ringtone.RingtoneFilePath, PathComparer))
        {
            var filePath = ringtone.RingtoneFilePath!;
            _ringtoneOptions.Add(new RingtoneOption(ringtone.Id, filePath));
        }

        OnPropertyChanged(nameof(AvailableRingtones));
    }

    private async Task EnsureSelectionAsync()
    {
        try
        {
            var selected = await _userSettingsService.GetSelectedRingtoneAsync();
            if (selected is not null)
            {
                var matchedOption = _ringtoneOptions.FirstOrDefault(option => option.Id == selected.Id);
                if (matchedOption is not null)
                {
                    _selectedRingtone = matchedOption;
                    return;
                }
            }

            var firstOption = _ringtoneOptions.FirstOrDefault();
            if (firstOption is null)
            {
                _selectedRingtone = null;
                return;
            }

            _selectedRingtone = firstOption;
            await _userSettingsService.SetSelectedRingtoneAsync(firstOption.Id).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService
                .CaptureUiErrorAsync(ex, $"{nameof(SettingsViewModel)}.{nameof(EnsureSelectionAsync)}")
                .ConfigureAwait(false);
        }
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(AvailableRingtones));
        OnPropertyChanged(nameof(SelectedRingtonePath));
        OnPropertyChanged(nameof(DataTransferMessage));
        OnPropertyChanged(nameof(IsDataTransferMessageSuccess));
        OnPropertyChanged(nameof(IsImportCanceledPopupOpen));
        OnPropertyChanged(nameof(IsResetConfirmationOpen));
    }
}

public sealed record RingtoneOption(Guid Id, string FilePath);
