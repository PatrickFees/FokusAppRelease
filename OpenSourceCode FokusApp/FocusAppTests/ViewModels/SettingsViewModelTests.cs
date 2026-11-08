using FokusApp.Components.Settings;
using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.Enums;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FocusAppTests.ViewModels;

public class SettingsViewModelTests
{
    private const string DefaultRingtonePath = "Raw/Standard.mp3";

    private readonly IRingtoneCatalogService _ringtoneCatalog = Substitute.For<IRingtoneCatalogService>();
    private readonly IRewardingSettingsService _rewardingSettings = Substitute.For<IRewardingSettingsService>();
    private readonly IDefaultScopeSettingsService _defaultScopeSettings = Substitute.For<IDefaultScopeSettingsService>();
    private readonly ITimerAlertModeSettingsService _timerAlertModeSettings = Substitute.For<ITimerAlertModeSettingsService>();
    private readonly ITimerAlertEffectService _timerAlertEffectService = Substitute.For<ITimerAlertEffectService>();
    private readonly IRingtoneSettingsService _ringtoneSettings = Substitute.For<IRingtoneSettingsService>();
    private readonly IAppStartupStateService _startupState = Substitute.For<IAppStartupStateService>();
    private readonly ITimerAndAudioService _audioService = Substitute.For<ITimerAndAudioService>();
    private readonly IDataImportExportService _importExport = Substitute.For<IDataImportExportService>();
    private readonly ICalendarExportService _calendarExport = Substitute.For<ICalendarExportService>();
    private readonly ICalendarImportPlatformAdapter _calendarImportPlatformAdapter = Substitute.For<ICalendarImportPlatformAdapter>();
    private readonly ICalendarAutoSyncSettingsService _calendarAutoSyncSettings = Substitute.For<ICalendarAutoSyncSettingsService>();
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly IUiTextProvider _textProvider = TestTexts.Create();
    private readonly SettingsViewModel _viewModel;

    public SettingsViewModelTests()
    {
        _ringtoneCatalog.DefaultRingtoneFilePath.Returns(DefaultRingtonePath);
        SetRingtones();
        _viewModel = new SettingsViewModel(
            _ringtoneCatalog,
            _rewardingSettings,
            _defaultScopeSettings,
            _timerAlertModeSettings,
            _timerAlertEffectService,
            _ringtoneSettings,
            _startupState,
            _audioService,
            _importExport,
            _calendarExport,
            _calendarImportPlatformAdapter,
            _calendarAutoSyncSettings,
            _errorCapture,
            _textProvider);
    }

    [Fact]
    public void Rewarding_StoredValues_AreExposedByViewModel()
    {
        _rewardingSettings.Rewarding.Returns(new AppRewardingSettings(false, true, false, BalanceGoal.TwoToThree));

        Assert.False(_viewModel.MicroFeedbackEnabled);
        Assert.True(_viewModel.ReviewsEnabled);
        Assert.False(_viewModel.CompanionEnabled);
        Assert.Equal(BalanceGoal.TwoToThree, _viewModel.BalanceGoal);
    }

    [Fact]
    public async Task SetMicroFeedbackEnabledAsync_Off_ChangesOnlyThatSwitch()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default);

        await _viewModel.SetMicroFeedbackEnabledAsync(false);

        await _rewardingSettings.Received(1).SetRewardingAsync(AppRewardingSettings.Default with { MicroFeedbackEnabled = false });
    }

    [Fact]
    public async Task SetReviewsEnabledAsync_Off_ChangesOnlyThatSwitch()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default);

        await _viewModel.SetReviewsEnabledAsync(false);

        await _rewardingSettings.Received(1).SetRewardingAsync(AppRewardingSettings.Default with { ReviewsEnabled = false });
    }

    [Fact]
    public async Task SetCompanionEnabledAsync_Off_ChangesOnlyThatSwitch()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default);

        await _viewModel.SetCompanionEnabledAsync(false);

        await _rewardingSettings.Received(1).SetRewardingAsync(AppRewardingSettings.Default with { CompanionEnabled = false });
    }

    [Fact]
    public async Task SetDefaultScopeAsync_Scope_IsPassedOn()
    {
        await _viewModel.SetDefaultScopeAsync(DefaultScope.Work);

        await _defaultScopeSettings.Received(1).SetDefaultScopeAsync(DefaultScope.Work);
    }

    [Fact]
    public void DefaultScope_Always_ComesFromTheSettings()
    {
        _defaultScopeSettings.DefaultScope.Returns(DefaultScope.Private);

        Assert.Equal(DefaultScope.Private, _viewModel.DefaultScope);
    }

    [Theory]
    [InlineData(TimerAlertMode.Sound, true, false, false)]
    [InlineData(TimerAlertMode.Vibration, false, true, false)]
    [InlineData(TimerAlertMode.Sound | TimerAlertMode.AdaptToRingerMode, true, false, true)]
    public void TimerAlert_StoredMode_IsExposedPerFlag(TimerAlertMode stored, bool sound, bool vibration, bool adaptToRingerMode)
    {
        _timerAlertModeSettings.TimerAlertMode.Returns(stored);

        Assert.Equal(sound, _viewModel.TimerAlertSoundEnabled);
        Assert.Equal(vibration, _viewModel.TimerAlertVibrationEnabled);
        Assert.Equal(adaptToRingerMode, _viewModel.TimerAlertAdaptToRingerModeEnabled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TimerAlert_Availability_ComesFromTheEffectService(bool supported)
    {
        _timerAlertEffectService.SupportsNativeEffects.Returns(supported);

        Assert.Equal(supported, _viewModel.IsTimerAlertVibrationAvailable);
        Assert.Equal(supported, _viewModel.IsTimerAlertAdaptToRingerModeAvailable);
    }

    [Fact]
    public async Task SetTimerAlertVibrationEnabledAsync_On_AddsFlagWithoutRemovingOthers()
    {
        _timerAlertModeSettings.TimerAlertMode.Returns(TimerAlertMode.Sound);

        await _viewModel.SetTimerAlertVibrationEnabledAsync(true);

        await _timerAlertModeSettings.Received(1)
            .SetTimerAlertModeAsync(TimerAlertMode.Sound | TimerAlertMode.Vibration);
    }

    [Fact]
    public async Task SetTimerAlertSoundEnabledAsync_Off_RemovesOnlyThatFlag()
    {
        _timerAlertModeSettings.TimerAlertMode.Returns(TimerAlertMode.Sound | TimerAlertMode.Vibration);

        await _viewModel.SetTimerAlertSoundEnabledAsync(false);

        await _timerAlertModeSettings.Received(1).SetTimerAlertModeAsync(TimerAlertMode.Vibration);
    }

    [Fact]
    public async Task SetTimerAlertAdaptToRingerModeEnabledAsync_On_AddsFlagWithoutRemovingOthers()
    {
        _timerAlertModeSettings.TimerAlertMode.Returns(TimerAlertMode.Sound);

        await _viewModel.SetTimerAlertAdaptToRingerModeEnabledAsync(true);

        await _timerAlertModeSettings.Received(1)
            .SetTimerAlertModeAsync(TimerAlertMode.Sound | TimerAlertMode.AdaptToRingerMode);
    }

[Fact]
    public async Task SetTimerAlertVibrationEnabledAsync_SavingFails_CapturesUiErrorInsteadOfThrowing()
    {
        var failure = new InvalidOperationException("kaputt");
        _timerAlertModeSettings.SetTimerAlertModeAsync(Arg.Any<TimerAlertMode>()).ThrowsAsync(failure);

        await _viewModel.SetTimerAlertVibrationEnabledAsync(true);

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "SettingsViewModel.SetTimerAlertFlagAsync"));
    }

    [Fact]
    public async Task SetBalanceGoalAsync_Goal_KeepsTheSwitches()
    {
        var current = AppRewardingSettings.Default with { CompanionEnabled = false };
        _rewardingSettings.Rewarding.Returns(current);

        await _viewModel.SetBalanceGoalAsync(BalanceGoal.ThreeToTwo);

        await _rewardingSettings.Received(1).SetRewardingAsync(current with { BalanceGoal = BalanceGoal.ThreeToTwo });
    }

    [Fact]
    public async Task SetCustomBalanceAsync_Ratio_SelectsCustomGoalAndStoresTheValues()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default);

        await _viewModel.SetCustomBalanceAsync(2, 7);

        await _rewardingSettings.Received(1).SetRewardingAsync(
            AppRewardingSettings.Default with { BalanceGoal = BalanceGoal.Custom, CustomForYou = 2, CustomToDo = 7 });
    }

    [Fact]
    public async Task SetCustomBalanceAsync_OutOfRange_IsClamped()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default);

        await _viewModel.SetCustomBalanceAsync(0, 99);

        await _rewardingSettings.Received(1).SetRewardingAsync(
            AppRewardingSettings.Default with { BalanceGoal = BalanceGoal.Custom, CustomForYou = 1, CustomToDo = 20 });
    }

    [Fact]
    public async Task SetMicroFeedbackEnabledAsync_Change_NotifiesPropertyChanged()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default);
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        await _viewModel.SetMicroFeedbackEnabledAsync(false);

        Assert.Contains(nameof(ISettingsViewModel.MicroFeedbackEnabled), changed);
    }

    [Fact]
    public async Task SetMicroFeedbackEnabledAsync_SavingFails_CapturesUiErrorInsteadOfThrowing()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default);
        var failure = new InvalidOperationException("kaputt");
        _rewardingSettings.SetRewardingAsync(Arg.Any<AppRewardingSettings>()).ThrowsAsync(failure);

        await _viewModel.SetMicroFeedbackEnabledAsync(false);

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "SettingsViewModel.UpdateRewardingAsync"));
    }

    [Fact]
    public async Task InitializeAsync_Ringtones_AreSortedByPathIgnoringCase()
    {
        var zebra = CreateRingtone("Raw/zebra.mp3");
        var alpha = CreateRingtone("Raw/Alpha.mp3");
        var beta = CreateRingtone("raw/beta.mp3");
        SetRingtones(zebra, alpha, beta);

        await _viewModel.InitializeAsync();

        var paths = _viewModel.AvailableRingtones.Select(option => option.FilePath).ToList();
        Assert.Equal(["Raw/Alpha.mp3", "raw/beta.mp3", "Raw/zebra.mp3"], paths);
        await _startupState.Received(1).WaitForReadyAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InitializeAsync_RingtoneWithoutPath_IsSkipped(string? path)
    {
        var valid = CreateRingtone("Raw/Gueltig.mp3");
        var invalid = CreateRingtone(path);
        SetRingtones(valid, invalid);

        await _viewModel.InitializeAsync();

        var option = Assert.Single(_viewModel.AvailableRingtones);
        Assert.Equal(valid.Id, option.Id);
    }

    [Fact]
    public async Task InitializeAsync_SavedRingtoneExists_IsSelectedWithoutSavingAgain()
    {
        var first = CreateRingtone("Raw/A.mp3");
        var second = CreateRingtone("Raw/B.mp3");
        SetRingtones(first, second);
        _ringtoneSettings.GetSelectedRingtoneAsync().Returns(second);

        await _viewModel.InitializeAsync();

        Assert.Equal("Raw/B.mp3", _viewModel.SelectedRingtonePath);
        await _ringtoneSettings.DidNotReceive().SetSelectedRingtoneAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task InitializeAsync_NoSavedRingtone_SelectsAndStoresFirstOption()
    {
        var first = CreateRingtone("Raw/A.mp3");
        var second = CreateRingtone("Raw/B.mp3");
        SetRingtones(second, first);

        await _viewModel.InitializeAsync();

        Assert.Equal("Raw/A.mp3", _viewModel.SelectedRingtonePath);
        await _ringtoneSettings.Received(1).SetSelectedRingtoneAsync(first.Id);
    }

    [Fact]
    public async Task InitializeAsync_SavedRingtoneNotInCatalog_FallsBackToFirstOption()
    {
        var first = CreateRingtone("Raw/A.mp3");
        var removed = CreateRingtone("Raw/Weg.mp3");
        SetRingtones(first);
        _ringtoneSettings.GetSelectedRingtoneAsync().Returns(removed);

        await _viewModel.InitializeAsync();

        Assert.Equal("Raw/A.mp3", _viewModel.SelectedRingtonePath);
        await _ringtoneSettings.Received(1).SetSelectedRingtoneAsync(first.Id);
    }

    [Fact]
    public async Task InitializeAsync_EmptyCatalog_HasNoSelection()
    {
        await _viewModel.InitializeAsync();

        Assert.Empty(_viewModel.AvailableRingtones);
        Assert.Null(_viewModel.SelectedRingtonePath);
    }

    [Fact]
    public async Task InitializeAsync_SettingsFail_CapturesErrorAndStaysUsable()
    {
        var first = CreateRingtone("Raw/A.mp3");
        SetRingtones(first);
        var failure = new InvalidOperationException("Fehler");
        _ringtoneSettings.GetSelectedRingtoneAsync().ThrowsAsync(failure);

        await _viewModel.InitializeAsync();

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "SettingsViewModel.EnsureSelectionAsync" &&
                options.IsUiFailure));
        Assert.Single(_viewModel.AvailableRingtones);
    }

    [Fact]
    public async Task SelectRingtoneAsync_KnownPath_SelectsAndStoresRingtoneIgnoringCase()
    {
        var first = CreateRingtone("Raw/A.mp3");
        var second = CreateRingtone("Raw/B.mp3");
        SetRingtones(first, second);
        await _viewModel.InitializeAsync();
        _ringtoneSettings.ClearReceivedCalls();

        await _viewModel.SelectRingtoneAsync("raw/b.MP3");

        Assert.Equal("Raw/B.mp3", _viewModel.SelectedRingtonePath);
        await _ringtoneSettings.Received(1).SetSelectedRingtoneAsync(second.Id);
    }

    [Fact]
    public async Task SelectRingtoneAsync_SameRingtoneAgain_StoresOnlyOnce()
    {
        var first = CreateRingtone("Raw/A.mp3");
        var second = CreateRingtone("Raw/B.mp3");
        SetRingtones(first, second);
        await _viewModel.InitializeAsync();
        _ringtoneSettings.ClearReceivedCalls();

        await _viewModel.SelectRingtoneAsync("Raw/B.mp3");
        await _viewModel.SelectRingtoneAsync("Raw/B.mp3");

        await _ringtoneSettings.Received(1).SetSelectedRingtoneAsync(second.Id);
    }

    [Fact]
    public async Task SelectRingtoneAsync_UnknownPath_KeepsCurrentSelection()
    {
        var first = CreateRingtone("Raw/A.mp3");
        SetRingtones(first);
        await _viewModel.InitializeAsync();
        _ringtoneSettings.ClearReceivedCalls();

        await _viewModel.SelectRingtoneAsync("Raw/Unbekannt.mp3");

        Assert.Equal("Raw/A.mp3", _viewModel.SelectedRingtonePath);
        await _ringtoneSettings.DidNotReceive().SetSelectedRingtoneAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task SelectRingtoneAsync_Blank_ClearsSelectionWithoutStoring()
    {
        var first = CreateRingtone("Raw/A.mp3");
        SetRingtones(first);
        await _viewModel.InitializeAsync();
        _ringtoneSettings.ClearReceivedCalls();

        await _viewModel.SelectRingtoneAsync(null);

        Assert.Null(_viewModel.SelectedRingtonePath);
        await _ringtoneSettings.DidNotReceive().SetSelectedRingtoneAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task PlaySelectedRingtoneAsync_RingtoneSelected_PlaysSelectedFile()
    {
        var first = CreateRingtone("Raw/A.mp3");
        SetRingtones(first);
        await _viewModel.InitializeAsync();

        await _viewModel.PlaySelectedRingtoneAsync();

        await _audioService.Received(1).PlaySoundAsync("Raw/A.mp3");
    }

    [Fact]
    public async Task PlaySelectedRingtoneAsync_NothingSelected_PlaysDefaultRingtone()
    {
        await _viewModel.PlaySelectedRingtoneAsync();

        await _audioService.Received(1).PlaySoundAsync(DefaultRingtonePath);
    }

    [Fact]
    public async Task ExportDataAsync_Success_ShowsServiceMessageAsSuccess()
    {
        var result = DataTransferResult.Success("Export fertig");
        _importExport.ExportAsync().Returns(result);

        await _viewModel.ExportDataAsync();

        Assert.Equal("Export fertig", _viewModel.DataTransferMessage);
        Assert.True(_viewModel.IsDataTransferMessageSuccess);
    }

    [Fact]
    public async Task ExportDataAsync_Failure_ShowsServiceMessageAsError()
    {
        var result = DataTransferResult.Failure("Export fehlgeschlagen");
        _importExport.ExportAsync().Returns(result);

        await _viewModel.ExportDataAsync();

        Assert.Equal("Export fehlgeschlagen", _viewModel.DataTransferMessage);
        Assert.False(_viewModel.IsDataTransferMessageSuccess);
    }

    [Fact]
    public async Task ExportDataAsync_ServiceThrows_ShowsGenericMessageAndCapturesError()
    {
        var failure = new InvalidOperationException("Fehler");
        _importExport.ExportAsync().ThrowsAsync(failure);

        await _viewModel.ExportDataAsync();

        Assert.Equal("DataActionFailed", _viewModel.DataTransferMessage);
        Assert.False(_viewModel.IsDataTransferMessageSuccess);
        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "SettingsViewModel.ExecuteDataTransferAsync" &&
                options.IsUiFailure));
    }

    [Fact]
    public async Task ImportDataAsync_Success_ShowsMessageAndReloadsRingtones()
    {
        var oldRingtone = CreateRingtone("Raw/Alt.mp3");
        SetRingtones(oldRingtone);
        await _viewModel.InitializeAsync();
        var importedRingtone = CreateRingtone("Raw/Importiert.mp3");
        SetRingtones(importedRingtone);
        _ringtoneSettings.GetSelectedRingtoneAsync().Returns(importedRingtone);
        var result = DataTransferResult.Success("Import fertig");
        _importExport.ImportAsync().Returns(result);

        await _viewModel.ImportDataAsync();

        Assert.Equal("Import fertig", _viewModel.DataTransferMessage);
        Assert.True(_viewModel.IsDataTransferMessageSuccess);
        var option = Assert.Single(_viewModel.AvailableRingtones);
        Assert.Equal(importedRingtone.Id, option.Id);
        Assert.Equal("Raw/Importiert.mp3", _viewModel.SelectedRingtonePath);
    }

    [Fact]
    public async Task ImportDataAsync_Failure_DoesNotReloadRingtones()
    {
        var oldRingtone = CreateRingtone("Raw/Alt.mp3");
        SetRingtones(oldRingtone);
        await _viewModel.InitializeAsync();
        _ringtoneCatalog.ClearReceivedCalls();
        var result = DataTransferResult.Failure("Import fehlgeschlagen");
        _importExport.ImportAsync().Returns(result);

        await _viewModel.ImportDataAsync();

        Assert.Equal("Import fehlgeschlagen", _viewModel.DataTransferMessage);
        Assert.False(_viewModel.IsDataTransferMessageSuccess);
        _ringtoneCatalog.DidNotReceive().GetAllRingtones();
    }

    [Fact]
    public async Task ImportDataAsync_Canceled_ShowsPopupWithoutMessageAndDoesNotReloadRingtones()
    {
        _ringtoneCatalog.ClearReceivedCalls();
        var result = DataTransferResult.Canceled("Abgebrochen");
        _importExport.ImportAsync().Returns(result);

        await _viewModel.ImportDataAsync();

        Assert.True(_viewModel.IsImportCanceledPopupOpen);
        Assert.Equal(string.Empty, _viewModel.DataTransferMessage);
        _ringtoneCatalog.DidNotReceive().GetAllRingtones();
    }

    [Fact]
    public async Task CloseImportCanceledPopup_OpenPopup_ClosesIt()
    {
        var result = DataTransferResult.Canceled("Abgebrochen");
        _importExport.ImportAsync().Returns(result);
        await _viewModel.ImportDataAsync();

        _viewModel.CloseImportCanceledPopup();

        Assert.False(_viewModel.IsImportCanceledPopupOpen);
    }

    [Fact]
    public async Task ImportDataAsync_Success_DoesNotShowCanceledPopup()
    {
        var result = DataTransferResult.Success("Import fertig");
        _importExport.ImportAsync().Returns(result);

        await _viewModel.ImportDataAsync();

        Assert.False(_viewModel.IsImportCanceledPopupOpen);
    }

    [Fact]
    public async Task RequestResetData_Initially_OpensConfirmationWithoutResetting()
    {
        _viewModel.RequestResetData();

        Assert.True(_viewModel.IsResetConfirmationOpen);
        await _importExport.DidNotReceive().ResetToStartDataAsync();
    }

    [Fact]
    public async Task CancelResetData_OpenConfirmation_ClosesItWithoutResetting()
    {
        _viewModel.RequestResetData();

        _viewModel.CancelResetData();

        Assert.False(_viewModel.IsResetConfirmationOpen);
        await _importExport.DidNotReceive().ResetToStartDataAsync();
    }

    [Fact]
    public async Task ConfirmResetDataAsync_Success_ResetsShowsMessageAndReloadsRingtones()
    {
        var oldRingtone = CreateRingtone("Raw/Alt.mp3");
        SetRingtones(oldRingtone);
        await _viewModel.InitializeAsync();
        var startRingtone = CreateRingtone("Raw/Start.mp3");
        SetRingtones(startRingtone);
        _ringtoneSettings.GetSelectedRingtoneAsync().Returns(startRingtone);
        var result = DataTransferResult.Success("Zurückgesetzt");
        _importExport.ResetToStartDataAsync().Returns(result);
        _viewModel.RequestResetData();

        await _viewModel.ConfirmResetDataAsync();

        Assert.False(_viewModel.IsResetConfirmationOpen);
        Assert.Equal("Zurückgesetzt", _viewModel.DataTransferMessage);
        Assert.True(_viewModel.IsDataTransferMessageSuccess);
        Assert.Equal("Raw/Start.mp3", _viewModel.SelectedRingtonePath);
        await _importExport.Received(1).ResetToStartDataAsync();
    }

    [Fact]
    public async Task ConfirmResetDataAsync_Failure_ShowsErrorAndDoesNotReloadRingtones()
    {
        _ringtoneCatalog.ClearReceivedCalls();
        var result = DataTransferResult.Failure("Zurücksetzen fehlgeschlagen");
        _importExport.ResetToStartDataAsync().Returns(result);
        _viewModel.RequestResetData();

        await _viewModel.ConfirmResetDataAsync();

        Assert.False(_viewModel.IsDataTransferMessageSuccess);
        Assert.Equal("Zurücksetzen fehlgeschlagen", _viewModel.DataTransferMessage);
        _ringtoneCatalog.DidNotReceive().GetAllRingtones();
    }

    [Fact]
    public async Task ConfirmResetDataAsync_ServiceThrows_ShowsGenericMessageAndCapturesError()
    {
        var failure = new InvalidOperationException("Fehler");
        _importExport.ResetToStartDataAsync().ThrowsAsync(failure);
        _viewModel.RequestResetData();

        await _viewModel.ConfirmResetDataAsync();

        Assert.False(_viewModel.IsResetConfirmationOpen);
        Assert.Equal("DataActionFailed", _viewModel.DataTransferMessage);
        await _errorCapture.Received(1).CaptureAsync(failure, Arg.Any<AppErrorCaptureOptions?>());
    }

    [Fact]
    public async Task ImportDataAsync_ServiceThrows_ShowsGenericMessageAndCapturesError()
    {
        var failure = new InvalidOperationException("Fehler");
        _importExport.ImportAsync().ThrowsAsync(failure);

        await _viewModel.ImportDataAsync();

        Assert.Equal("DataActionFailed", _viewModel.DataTransferMessage);
        Assert.False(_viewModel.IsDataTransferMessageSuccess);
        await _errorCapture.Received(1).CaptureAsync(failure, Arg.Any<AppErrorCaptureOptions?>());
    }

    [Fact]
    public async Task ExportDataAsync_Finished_RaisesMessageChanges()
    {
        var result = DataTransferResult.Success("Export fertig");
        _importExport.ExportAsync().Returns(result);
        var changed = TrackChanges();

        await _viewModel.ExportDataAsync();

        Assert.Contains(nameof(SettingsViewModel.DataTransferMessage), changed);
        Assert.Contains(nameof(SettingsViewModel.IsDataTransferMessageSuccess), changed);
    }

    [Fact]
    public async Task ClearDataTransferMessage_ExistingMessage_EmptiesMessage()
    {
        var result = DataTransferResult.Success("Export fertig");
        _importExport.ExportAsync().Returns(result);
        await _viewModel.ExportDataAsync();

        _viewModel.ClearDataTransferMessage();

        Assert.Equal(string.Empty, _viewModel.DataTransferMessage);
    }

    [Fact]
    public void CustomRatio_StoredSettings_AreReadFromTheSettingsService()
    {
        var rewarding = AppRewardingSettings.Default with { BalanceGoal = BalanceGoal.Custom, CustomForYou = 3, CustomToDo = 8 };
        _rewardingSettings.Rewarding.Returns(rewarding);

        Assert.Equal(3, _viewModel.CustomForYou);
        Assert.Equal(8, _viewModel.CustomToDo);
    }

    [Fact]
    public async Task SelectRingtoneAsync_BlankWithoutSelection_DoesNothing()
    {
        var ringtone = CreateRingtone("Raw/A.mp3");
        SetRingtones(ringtone);
        await _viewModel.InitializeAsync();
        await _viewModel.SelectRingtoneAsync(null);
        _ringtoneSettings.ClearReceivedCalls();

        await _viewModel.SelectRingtoneAsync("  ");

        await _ringtoneSettings.DidNotReceive().SetSelectedRingtoneAsync(Arg.Any<Guid>());
        Assert.Null(_viewModel.SelectedRingtonePath);
    }

    private void SetRingtones(params RingtonesEntity[] ringtones)
    {
        IReadOnlyList<RingtonesEntity> list = ringtones;
        _ringtoneCatalog.GetAllRingtones().Returns(list);
    }

    private List<string?> TrackChanges()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        return changed;
    }

    private static RingtonesEntity CreateRingtone(string? path)
    {
        return new RingtonesEntity
        {
            Id = Guid.NewGuid(),
            RingtoneFilePath = path
        };
    }
}
