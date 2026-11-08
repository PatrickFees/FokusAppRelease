using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Core.Enums;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;

namespace FokusApp.Components.Settings;
public sealed class SettingsViewModel : ObservableObject, ISettingsViewModel
{
    private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;

    private readonly IRingtoneCatalogService _ringtoneCatalogService;
    private readonly IRewardingSettingsService _rewardingSettingsService;
    private readonly IDefaultScopeSettingsService _defaultScopeSettingsService;
    private readonly ITimerAlertModeSettingsService _timerAlertModeSettingsService;
    private readonly ITimerAlertEffectService _timerAlertEffectService;
    private readonly IRingtoneSettingsService _ringtoneSettingsService;
    private readonly IAppStartupStateService _appStartupStateService;
    private readonly ITimerAndAudioService _timerAndAudioService;
    private readonly IDataImportExportService _dataImportExportService;
    private readonly ICalendarExportService _calendarExportService;
    private readonly ICalendarImportPlatformAdapter _calendarImportPlatformAdapter;
    private readonly ICalendarAutoSyncSettingsService _calendarAutoSyncSettingsService;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly IUiTextProvider _textProvider;
    private readonly List<RingtoneOption> _ringtoneOptions = [];

    private RingtoneOption? _selectedRingtone;

    public IReadOnlyList<RingtoneOption> AvailableRingtones => _ringtoneOptions;

    public string? SelectedRingtonePath => _selectedRingtone?.FilePath;

    public SettingsViewModel(
        IRingtoneCatalogService ringtoneCatalogService,
        IRewardingSettingsService rewardingSettingsService,
        IDefaultScopeSettingsService defaultScopeSettingsService,
        ITimerAlertModeSettingsService timerAlertModeSettingsService,
        ITimerAlertEffectService timerAlertEffectService,
        IRingtoneSettingsService ringtoneSettingsService,
        IAppStartupStateService appStartupStateService,
        ITimerAndAudioService timerAndAudioService,
        IDataImportExportService dataImportExportService,
        ICalendarExportService calendarExportService,
        ICalendarImportPlatformAdapter calendarImportPlatformAdapter,
        ICalendarAutoSyncSettingsService calendarAutoSyncSettingsService,
        IAppErrorCaptureService appErrorCaptureService,
        IUiTextProvider textProvider)
    {
        _ringtoneCatalogService = ringtoneCatalogService;
        _rewardingSettingsService = rewardingSettingsService;
        _defaultScopeSettingsService = defaultScopeSettingsService;
        _timerAlertModeSettingsService = timerAlertModeSettingsService;
        _timerAlertEffectService = timerAlertEffectService;
        _ringtoneSettingsService = ringtoneSettingsService;
        _appStartupStateService = appStartupStateService;
        _timerAndAudioService = timerAndAudioService;
        _dataImportExportService = dataImportExportService;
        _calendarExportService = calendarExportService;
        _calendarImportPlatformAdapter = calendarImportPlatformAdapter;
        _calendarAutoSyncSettingsService = calendarAutoSyncSettingsService;
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

    public bool MicroFeedbackEnabled => _rewardingSettingsService.Rewarding.MicroFeedbackEnabled;

    public bool ReviewsEnabled => _rewardingSettingsService.Rewarding.ReviewsEnabled;

    public bool CompanionEnabled => _rewardingSettingsService.Rewarding.CompanionEnabled;

    public BalanceGoal BalanceGoal => _rewardingSettingsService.Rewarding.BalanceGoal;

    public int CustomForYou => _rewardingSettingsService.Rewarding.CustomForYou;

    public int CustomToDo => _rewardingSettingsService.Rewarding.CustomToDo;

    public DefaultScope DefaultScope => _defaultScopeSettingsService.DefaultScope;

    public async Task SetDefaultScopeAsync(DefaultScope scope)
    {
        try
        {
            await _defaultScopeSettingsService.SetDefaultScopeAsync(scope);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(SettingsViewModel)}.{nameof(SetDefaultScopeAsync)}");
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    public bool TimerAlertSoundEnabled => _timerAlertModeSettingsService.TimerAlertMode.HasFlag(TimerAlertMode.Sound);

    public bool TimerAlertVibrationEnabled => _timerAlertModeSettingsService.TimerAlertMode.HasFlag(TimerAlertMode.Vibration);

    public bool TimerAlertAdaptToRingerModeEnabled => _timerAlertModeSettingsService.TimerAlertMode.HasFlag(TimerAlertMode.AdaptToRingerMode);

    public bool IsTimerAlertVibrationAvailable => _timerAlertEffectService.SupportsNativeEffects;

    public bool IsTimerAlertAdaptToRingerModeAvailable => _timerAlertEffectService.SupportsNativeEffects;

    public Task SetTimerAlertSoundEnabledAsync(bool enabled) => SetTimerAlertFlagAsync(TimerAlertMode.Sound, enabled);

    public Task SetTimerAlertVibrationEnabledAsync(bool enabled) => SetTimerAlertFlagAsync(TimerAlertMode.Vibration, enabled);

    public Task SetTimerAlertAdaptToRingerModeEnabledAsync(bool enabled) => SetTimerAlertFlagAsync(TimerAlertMode.AdaptToRingerMode, enabled);

    private async Task SetTimerAlertFlagAsync(TimerAlertMode flag, bool enabled)
    {
        try
        {
            var current = _timerAlertModeSettingsService.TimerAlertMode;
            var updated = enabled ? current | flag : current & ~flag;
            await _timerAlertModeSettingsService.SetTimerAlertModeAsync(updated);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(SettingsViewModel)}.{nameof(SetTimerAlertFlagAsync)}");
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    public Task SetMicroFeedbackEnabledAsync(bool enabled) =>
        UpdateRewardingAsync(rewarding => rewarding with { MicroFeedbackEnabled = enabled });

    public Task SetReviewsEnabledAsync(bool enabled) =>
        UpdateRewardingAsync(rewarding => rewarding with { ReviewsEnabled = enabled });

    public Task SetCompanionEnabledAsync(bool enabled) =>
        UpdateRewardingAsync(rewarding => rewarding with { CompanionEnabled = enabled });

    public Task SetBalanceGoalAsync(BalanceGoal goal) =>
        UpdateRewardingAsync(rewarding => rewarding with { BalanceGoal = goal });

    public Task SetCustomBalanceAsync(int forYou, int toDo) =>
        UpdateRewardingAsync(rewarding => rewarding with
        {
            BalanceGoal = BalanceGoal.Custom,
            CustomForYou = AppRewardingSettings.ClampCustomPart(forYou),
            CustomToDo = AppRewardingSettings.ClampCustomPart(toDo)
        });

    private async Task UpdateRewardingAsync(Func<AppRewardingSettings, AppRewardingSettings> change)
    {
        try
        {
            await _rewardingSettingsService.SetRewardingAsync(change(_rewardingSettingsService.Rewarding));
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(SettingsViewModel)}.{nameof(UpdateRewardingAsync)}");
        }
        finally
        {
            NotifyStateChanged();
        }
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
        await _ringtoneSettingsService.SetSelectedRingtoneAsync(matchedOption.Id);
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

    public async Task ExportCalendarAsync()
    {
        await ExecuteDataTransferAsync(() => _calendarExportService.ExportAsync()).ConfigureAwait(false);
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

    public bool IsCalendarImportAvailable => _calendarImportPlatformAdapter.IsAvailable;

    public bool IsCalendarImportOpen { get; private set; }

    public void OpenCalendarImport()
    {
        IsCalendarImportOpen = true;
        NotifyStateChanged();
    }

    public void CloseCalendarImport()
    {
        IsCalendarImportOpen = false;
        NotifyStateChanged();
    }

    public bool IsCalendarAutoSyncAvailable => _calendarImportPlatformAdapter.IsAvailable;

    public bool CalendarAutoSyncEnabled => _calendarAutoSyncSettingsService.Enabled;

    public DateTime? CalendarAutoSyncLastRunAt => _calendarAutoSyncSettingsService.LastRunAt;

    public bool IsCalendarAutoSyncPickerOpen { get; private set; }

    public async Task SetCalendarAutoSyncEnabledAsync(bool enabled)
    {
        try
        {
            await _calendarAutoSyncSettingsService.SetEnabledAsync(enabled);
            if (enabled)
            {
                var selectedCalendarIds = await _calendarAutoSyncSettingsService.GetSelectedCalendarIdsAsync();
                if (selectedCalendarIds.Count == 0)
                {
                    IsCalendarAutoSyncPickerOpen = true;
                }
            }
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService
                .CaptureUiErrorAsync(ex, $"{nameof(SettingsViewModel)}.{nameof(SetCalendarAutoSyncEnabledAsync)}");
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    public void OpenCalendarAutoSyncPicker()
    {
        IsCalendarAutoSyncPickerOpen = true;
        NotifyStateChanged();
    }

    public void CloseCalendarAutoSyncPicker()
    {
        IsCalendarAutoSyncPickerOpen = false;
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
            var selected = await _ringtoneSettingsService.GetSelectedRingtoneAsync();
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
            await _ringtoneSettingsService.SetSelectedRingtoneAsync(firstOption.Id).ConfigureAwait(false);
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
        OnPropertyChanged(nameof(IsCalendarImportAvailable));
        OnPropertyChanged(nameof(IsCalendarImportOpen));
        OnPropertyChanged(nameof(IsCalendarAutoSyncAvailable));
        OnPropertyChanged(nameof(CalendarAutoSyncEnabled));
        OnPropertyChanged(nameof(CalendarAutoSyncLastRunAt));
        OnPropertyChanged(nameof(IsCalendarAutoSyncPickerOpen));
        OnPropertyChanged(nameof(MicroFeedbackEnabled));
        OnPropertyChanged(nameof(ReviewsEnabled));
        OnPropertyChanged(nameof(CompanionEnabled));
        OnPropertyChanged(nameof(BalanceGoal));
        OnPropertyChanged(nameof(CustomForYou));
        OnPropertyChanged(nameof(CustomToDo));
        OnPropertyChanged(nameof(DefaultScope));
        OnPropertyChanged(nameof(TimerAlertSoundEnabled));
        OnPropertyChanged(nameof(TimerAlertVibrationEnabled));
        OnPropertyChanged(nameof(TimerAlertAdaptToRingerModeEnabled));
        OnPropertyChanged(nameof(IsTimerAlertVibrationAvailable));
        OnPropertyChanged(nameof(IsTimerAlertAdaptToRingerModeAvailable));
    }
}

public sealed record RingtoneOption(Guid Id, string FilePath);
