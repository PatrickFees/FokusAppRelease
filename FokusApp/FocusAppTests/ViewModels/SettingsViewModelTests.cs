using FokusApp.Components.Settings;
using FokusApp.Core.DataBase.Entities;
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
    private readonly IUserSettingsService _userSettings = Substitute.For<IUserSettingsService>();
    private readonly IAppStartupStateService _startupState = Substitute.For<IAppStartupStateService>();
    private readonly ITimerAndAudioService _audioService = Substitute.For<ITimerAndAudioService>();
    private readonly IDataImportExportService _importExport = Substitute.For<IDataImportExportService>();
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly IUiTextProvider _textProvider = TestTexts.Create();
    private readonly SettingsViewModel _viewModel;

    public SettingsViewModelTests()
    {
        _ringtoneCatalog.DefaultRingtoneFilePath.Returns(DefaultRingtonePath);
        SetRingtones();
        _viewModel = new SettingsViewModel(
            _ringtoneCatalog,
            _userSettings,
            _startupState,
            _audioService,
            _importExport,
            _errorCapture,
            _textProvider);
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
        _userSettings.GetSelectedRingtoneAsync().Returns(second);

        await _viewModel.InitializeAsync();

        Assert.Equal("Raw/B.mp3", _viewModel.SelectedRingtonePath);
        await _userSettings.DidNotReceive().SetSelectedRingtoneAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task InitializeAsync_NoSavedRingtone_SelectsAndStoresFirstOption()
    {
        var first = CreateRingtone("Raw/A.mp3");
        var second = CreateRingtone("Raw/B.mp3");
        SetRingtones(second, first);

        await _viewModel.InitializeAsync();

        Assert.Equal("Raw/A.mp3", _viewModel.SelectedRingtonePath);
        await _userSettings.Received(1).SetSelectedRingtoneAsync(first.Id);
    }

    [Fact]
    public async Task InitializeAsync_SavedRingtoneNotInCatalog_FallsBackToFirstOption()
    {
        var first = CreateRingtone("Raw/A.mp3");
        var removed = CreateRingtone("Raw/Weg.mp3");
        SetRingtones(first);
        _userSettings.GetSelectedRingtoneAsync().Returns(removed);

        await _viewModel.InitializeAsync();

        Assert.Equal("Raw/A.mp3", _viewModel.SelectedRingtonePath);
        await _userSettings.Received(1).SetSelectedRingtoneAsync(first.Id);
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
        _userSettings.GetSelectedRingtoneAsync().ThrowsAsync(failure);

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
        _userSettings.ClearReceivedCalls();

        await _viewModel.SelectRingtoneAsync("raw/b.MP3");

        Assert.Equal("Raw/B.mp3", _viewModel.SelectedRingtonePath);
        await _userSettings.Received(1).SetSelectedRingtoneAsync(second.Id);
    }

    [Fact]
    public async Task SelectRingtoneAsync_SameRingtoneAgain_StoresOnlyOnce()
    {
        var first = CreateRingtone("Raw/A.mp3");
        var second = CreateRingtone("Raw/B.mp3");
        SetRingtones(first, second);
        await _viewModel.InitializeAsync();
        _userSettings.ClearReceivedCalls();

        await _viewModel.SelectRingtoneAsync("Raw/B.mp3");
        await _viewModel.SelectRingtoneAsync("Raw/B.mp3");

        await _userSettings.Received(1).SetSelectedRingtoneAsync(second.Id);
    }

    [Fact]
    public async Task SelectRingtoneAsync_UnknownPath_KeepsCurrentSelection()
    {
        var first = CreateRingtone("Raw/A.mp3");
        SetRingtones(first);
        await _viewModel.InitializeAsync();
        _userSettings.ClearReceivedCalls();

        await _viewModel.SelectRingtoneAsync("Raw/Unbekannt.mp3");

        Assert.Equal("Raw/A.mp3", _viewModel.SelectedRingtonePath);
        await _userSettings.DidNotReceive().SetSelectedRingtoneAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task SelectRingtoneAsync_Blank_ClearsSelectionWithoutStoring()
    {
        var first = CreateRingtone("Raw/A.mp3");
        SetRingtones(first);
        await _viewModel.InitializeAsync();
        _userSettings.ClearReceivedCalls();

        await _viewModel.SelectRingtoneAsync(null);

        Assert.Null(_viewModel.SelectedRingtonePath);
        await _userSettings.DidNotReceive().SetSelectedRingtoneAsync(Arg.Any<Guid>());
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
        _userSettings.GetSelectedRingtoneAsync().Returns(importedRingtone);
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
        _userSettings.GetSelectedRingtoneAsync().Returns(startRingtone);
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
