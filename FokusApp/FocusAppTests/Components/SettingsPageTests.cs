using System.ComponentModel;
using Bunit;
using FokusApp.Components.Settings;
using FokusApp.Core.Enums;
using FokusApp.Core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FocusAppTests.Components;

public class SettingsPageTests : ComponentTestContext
{
    private readonly ISettingsViewModel _viewModel = Substitute.For<ISettingsViewModel>();
    private readonly IAppLocalizationService _localization = Substitute.For<IAppLocalizationService>();
    private readonly IAppThemeService _themeService = Substitute.For<IAppThemeService>();

    public SettingsPageTests()
    {
        _viewModel.AvailableRingtones.Returns(CreateRingtones());
        _viewModel.SelectedRingtonePath.Returns("Raw/Hello_Sunshine.mp3");
        _localization.CurrentLanguage.Returns(AppLanguage.German);
        _themeService.SelectedTheme.Returns(AppColorTheme.System);

        Services.AddSingleton(_viewModel);
        Services.AddSingleton(_localization);
        Services.AddSingleton(_themeService);
    }

    [Fact]
    public void Render_ShowsOneOptionPerThemeWithSelectedThemeChecked()
    {
        _themeService.SelectedTheme.Returns(AppColorTheme.Dark);

        var cut = Render<SettingsPage>();

        var options = cut.FindAll("button.theme-option");
        var checkedOptions = cut.FindAll("button.theme-option[aria-checked=true]");
        Assert.Equal(Enum.GetValues<AppColorTheme>().Length, options.Count);
        Assert.Single(checkedOptions);
        Assert.Equal("Dark", checkedOptions[0].GetAttribute("data-theme-option"));
    }

    [Fact]
    public void ClickThemeOption_SetsTheme()
    {
        var cut = Render<SettingsPage>();
        var pinkOption = cut.Find("button.theme-option[data-theme-option=Pink]");

        pinkOption.Click();

        _themeService.Received(1).SetThemeAsync(AppColorTheme.Pink);
    }

    [Fact]
    public void Render_InitializesViewModel()
    {
        Render<SettingsPage>();

        _viewModel.Received(1).InitializeAsync();
    }

    [Fact]
    public void Render_NothingOpen_ShowsNoDialog()
    {
        var cut = Render<SettingsPage>();

        var dialogs = cut.FindAll(".confirm-dialog");
        Assert.Empty(dialogs);
    }

    [Fact]
    public void Render_ShowsResetButtonWithDescription()
    {
        var cut = Render<SettingsPage>();

        var resetButton = cut.Find("button.settings-reset-button");
        var description = cut.Find(".settings-reset-description");
        Assert.Equal(Text("ResetData"), resetButton.TextContent.Trim());
        Assert.Equal(Text("ResetDataDescription"), description.TextContent.Trim());
    }

    [Fact]
    public void ClickResetButton_RequestsReset()
    {
        var cut = Render<SettingsPage>();
        var resetButton = cut.Find("button.settings-reset-button");

        resetButton.Click();

        _viewModel.Received(1).RequestResetData();
        _viewModel.DidNotReceive().ConfirmResetDataAsync();
    }

    [Fact]
    public void Render_ResetConfirmationOpen_ShowsDestructiveDialogWithTexts()
    {
        _viewModel.IsResetConfirmationOpen.Returns(true);

        var cut = Render<SettingsPage>();

        var title = cut.Find(".confirm-dialog-title");
        var message = cut.Find(".confirm-dialog-message");
        var confirmButton = cut.Find("button.btn-danger");
        var cancelButton = cut.Find(".confirm-dialog button.btn-secondary");
        Assert.Equal(Text("ResetConfirmTitle"), title.TextContent);
        Assert.Equal(Text("ResetConfirmMessage"), message.TextContent);
        Assert.Equal(Text("ResetConfirmAction"), confirmButton.TextContent.Trim());
        Assert.Equal(Text("Cancel"), cancelButton.TextContent.Trim());
    }

    [Fact]
    public void ResetConfirmation_ClickConfirm_ConfirmsResetOnly()
    {
        _viewModel.IsResetConfirmationOpen.Returns(true);
        var cut = Render<SettingsPage>();
        var confirmButton = cut.Find("button.btn-danger");

        confirmButton.Click();

        _viewModel.Received(1).ConfirmResetDataAsync();
        _viewModel.DidNotReceive().CancelResetData();
    }

    [Fact]
    public void ResetConfirmation_ClickCancel_CancelsWithoutReset()
    {
        _viewModel.IsResetConfirmationOpen.Returns(true);
        var cut = Render<SettingsPage>();
        var cancelButton = cut.Find(".confirm-dialog button.btn-secondary");

        cancelButton.Click();

        _viewModel.Received(1).CancelResetData();
        _viewModel.DidNotReceive().ConfirmResetDataAsync();
    }

    [Fact]
    public void ResetConfirmation_ClickOverlay_CancelsWithoutReset()
    {
        _viewModel.IsResetConfirmationOpen.Returns(true);
        var cut = Render<SettingsPage>();
        var overlay = cut.Find(".confirm-dialog-overlay");

        overlay.Click();

        _viewModel.Received(1).CancelResetData();
        _viewModel.DidNotReceive().ConfirmResetDataAsync();
    }

    [Fact]
    public void Render_ImportCanceledPopupOpen_ShowsNoticeWithOnlyOkButton()
    {
        _viewModel.IsImportCanceledPopupOpen.Returns(true);

        var cut = Render<SettingsPage>();

        var title = cut.Find(".confirm-dialog-title");
        var message = cut.Find(".confirm-dialog-message");
        var buttons = cut.FindAll(".confirm-dialog button");
        var okButton = Assert.Single(buttons);
        Assert.Equal(Text("ImportCanceledTitle"), title.TextContent);
        Assert.Equal(Text("ImportCanceledMessage"), message.TextContent);
        Assert.Equal(Text("Ok"), okButton.TextContent.Trim());
        Assert.Contains("btn-primary", okButton.ClassList);
    }

    [Fact]
    public void ImportCanceledPopup_ClickOk_ClosesPopup()
    {
        _viewModel.IsImportCanceledPopupOpen.Returns(true);
        var cut = Render<SettingsPage>();
        var okButton = cut.Find(".confirm-dialog button");

        okButton.Click();

        _viewModel.Received(1).CloseImportCanceledPopup();
    }

    [Fact]
    public void ImportCanceledPopup_ClickOverlay_ClosesPopup()
    {
        _viewModel.IsImportCanceledPopupOpen.Returns(true);
        var cut = Render<SettingsPage>();
        var overlay = cut.Find(".confirm-dialog-overlay");

        overlay.Click();

        _viewModel.Received(1).CloseImportCanceledPopup();
        _viewModel.DidNotReceive().CancelResetData();
    }

    [Fact]
    public void ViewModelChange_ResetConfirmationOpens_ShowsDialog()
    {
        var cut = Render<SettingsPage>();
        Assert.Empty(cut.FindAll(".confirm-dialog"));
        _viewModel.IsResetConfirmationOpen.Returns(true);

        RaisePropertyChanged(nameof(ISettingsViewModel.IsResetConfirmationOpen));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".confirm-dialog")));
    }

    [Fact]
    public void ClickExport_ExportsData()
    {
        var cut = Render<SettingsPage>();
        var exportButton = cut.Find("button.settings-transfer-button.btn-primary");

        exportButton.Click();

        _viewModel.Received(1).ExportDataAsync();
    }

    [Fact]
    public void ClickImport_ImportsData()
    {
        var cut = Render<SettingsPage>();
        var importButton = cut.FindAll("button.settings-transfer-button")[1];

        importButton.Click();

        _viewModel.Received(1).ImportDataAsync();
        Assert.Equal(Text("Import"), importButton.TextContent.Trim());
    }

    [Fact]
    public void Render_NoTransferMessage_ShowsNoMessage()
    {
        var cut = Render<SettingsPage>();

        var messages = cut.FindAll(".settings-transfer-message");
        Assert.Empty(messages);
    }

    [Theory]
    [InlineData(true, "success")]
    [InlineData(false, "error")]
    public void Render_TransferMessage_ShowsMessageWithStatusClass(bool isSuccess, string expectedClass)
    {
        _viewModel.DataTransferMessage.Returns("Import erfolgreich abgeschlossen.");
        _viewModel.IsDataTransferMessageSuccess.Returns(isSuccess);

        var cut = Render<SettingsPage>();

        var message = cut.Find(".settings-transfer-message");
        Assert.Equal("Import erfolgreich abgeschlossen.", message.TextContent.Trim());
        Assert.Contains(expectedClass, message.ClassList);
    }

    [Fact]
    public void ChangeLanguage_English_SetsLanguageAndClearsTransferMessage()
    {
        var cut = Render<SettingsPage>();
        var languageSelect = cut.Find("#languageSelect");

        languageSelect.Change(nameof(AppLanguage.English));

        _localization.Received(1).SetLanguageAsync(AppLanguage.English);
        _viewModel.Received(1).ClearDataTransferMessage();
    }

    [Fact]
    public void ChangeLanguage_UnknownValue_DoesNothing()
    {
        var cut = Render<SettingsPage>();
        var languageSelect = cut.Find("#languageSelect");

        languageSelect.Change("Klingonisch");

        _localization.DidNotReceive().SetLanguageAsync(Arg.Any<AppLanguage>());
        _viewModel.DidNotReceive().ClearDataTransferMessage();
    }

    [Fact]
    public void Render_Ringtones_ShowsOptionsWithReadableLabels()
    {
        var cut = Render<SettingsPage>();

        var options = cut.FindAll("#ringtoneSelect option");
        var selectedInfo = cut.Find(".settings-selection");
        Assert.Equal(2, options.Count);
        Assert.Equal("Hello Sunshine", options[0].TextContent);
        Assert.Equal("Raw/Hello_Sunshine.mp3", options[0].GetAttribute("value"));
        Assert.Equal("Kurz nervig", options[1].TextContent);
        Assert.Equal(Text("CurrentlySelected", "Hello Sunshine"), selectedInfo.TextContent);
    }

    [Fact]
    public void Render_NoRingtones_ShowsHintWithoutSelect()
    {
        IReadOnlyList<RingtoneOption> noRingtones = [];
        _viewModel.AvailableRingtones.Returns(noRingtones);

        var cut = Render<SettingsPage>();

        var hint = cut.Find(".settings-empty");
        Assert.Equal(Text("NoRingtones"), hint.TextContent);
        Assert.Empty(cut.FindAll("#ringtoneSelect"));
    }

    [Fact]
    public void ChangeRingtone_SelectsRingtone()
    {
        var cut = Render<SettingsPage>();
        var ringtoneSelect = cut.Find("#ringtoneSelect");

        ringtoneSelect.Change("Raw/Kurz_nervig.mp3");

        _viewModel.Received(1).SelectRingtoneAsync("Raw/Kurz_nervig.mp3");
    }

    [Fact]
    public void ClickPlay_PlaysSelectedRingtone()
    {
        var cut = Render<SettingsPage>();
        var playButton = cut.Find(".settings-preview-button");

        playButton.Click();

        _viewModel.Received(1).PlaySelectedRingtoneAsync();
        Assert.Equal(Text("Play"), playButton.TextContent.Trim());
    }

    [Fact]
    public void Render_NoSelectedRingtone_DisablesPlayAndHidesSelection()
    {
        _viewModel.SelectedRingtonePath.Returns((string?)null);

        var cut = Render<SettingsPage>();

        var playButton = cut.Find(".settings-preview-button");
        Assert.True(playButton.HasAttribute("disabled"));
        Assert.Empty(cut.FindAll(".settings-selection"));
    }

    private void RaisePropertyChanged(string propertyName)
    {
        var args = new PropertyChangedEventArgs(propertyName);
        _viewModel.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(_viewModel, args);
    }

    private static List<RingtoneOption> CreateRingtones()
    {
        return
        [
            new RingtoneOption(Guid.NewGuid(), "Raw/Hello_Sunshine.mp3"),
            new RingtoneOption(Guid.NewGuid(), "Raw/Kurz_nervig.mp3")
        ];
    }
}
