using FokusApp.Components.Onboarding;
using FokusApp.Components.ForYouSuggestions;
using FokusApp.Components.PopUps.Calendar;
using FokusApp.Components.PopUps.CalendarAutoSync;
using FokusApp.Components.PopUps.CalendarImport;
using System.ComponentModel;
using Bunit;
using FokusApp.Components.Settings;
using FokusApp.Core.Enums;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FocusAppTests.Components;

public class SettingsPageTests : ComponentTestContext
{
    private readonly ISettingsViewModel _viewModel = Substitute.For<ISettingsViewModel>();
    private readonly IOnboardingViewModel _onboarding = Substitute.For<IOnboardingViewModel>();
    private readonly IAppLocalizationService _localization = Substitute.For<IAppLocalizationService>();
    private readonly IAppThemeService _themeService = Substitute.For<IAppThemeService>();
    private readonly ICalendarImportPopUpViewModel _calendarImportViewModel = Substitute.For<ICalendarImportPopUpViewModel>();
    private readonly ICalendarAutoSyncPopUpViewModel _calendarAutoSyncViewModel = Substitute.For<ICalendarAutoSyncPopUpViewModel>();

    public SettingsPageTests()
    {
        _viewModel.AvailableRingtones.Returns(CreateRingtones());
        _viewModel.SelectedRingtonePath.Returns("Raw/Hello_Sunshine.mp3");
        _localization.CurrentLanguage.Returns(AppLanguage.German);
        _themeService.SelectedTheme.Returns(AppColorTheme.Blue);
        _calendarImportViewModel.Calendars.Returns(Array.Empty<CalendarOptionModel>());
        _calendarAutoSyncViewModel.Calendars.Returns(Array.Empty<CalendarOptionModel>());

        Services.AddSingleton(_viewModel);
        Services.AddSingleton(_onboarding);
        Services.AddSingleton(Substitute.For<IForYouSuggestionsViewModel>());
        Services.AddSingleton(_localization);
        Services.AddSingleton(_themeService);
        Services.AddSingleton(_calendarImportViewModel);
        Services.AddSingleton(_calendarAutoSyncViewModel);
        Services.AddSingleton(Substitute.For<ICalendarPopupViewModel>());
    }

    [Fact]
    public void Render_ShowsOneOptionPerThemeWithSelectedThemeChecked()
    {
        _themeService.SelectedTheme.Returns(AppColorTheme.Green);

        var cut = Render<SettingsPage>();

        var options = cut.FindAll("button.theme-option[data-theme-option]");
        var checkedOptions = cut.FindAll("button.theme-option[data-theme-option][aria-checked=true]");
        Assert.Equal(Enum.GetValues<AppColorTheme>().Length, options.Count);
        Assert.Single(checkedOptions);
        Assert.Equal("Green", checkedOptions[0].GetAttribute("data-theme-option"));
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
    public void Render_ShowsOneOptionPerColorModeWithSelectedModeChecked()
    {
        _themeService.SelectedMode.Returns(AppColorMode.Dark);

        var cut = Render<SettingsPage>();

        var options = cut.FindAll("button.theme-option[data-color-mode-option]");
        var checkedOptions = cut.FindAll("button.theme-option[data-color-mode-option][aria-checked=true]");
        Assert.Equal(Enum.GetValues<AppColorMode>().Length, options.Count);
        Assert.Single(checkedOptions);
        Assert.Equal("Dark", checkedOptions[0].GetAttribute("data-color-mode-option"));
    }

    [Fact]
    public void ClickColorModeOption_SetsColorMode()
    {
        var cut = Render<SettingsPage>();
        var darkOption = cut.Find("button.theme-option[data-color-mode-option=Dark]");

        darkOption.Click();

        _themeService.Received(1).SetColorModeAsync(AppColorMode.Dark);
    }

    [Fact]
    public void Render_Swatches_ShowTheSelectedModeAndTheSelectedTheme()
    {
        _themeService.SelectedTheme.Returns(AppColorTheme.Pink);
        _themeService.SelectedMode.Returns(AppColorMode.Dark);

        var cut = Render<SettingsPage>();

        var themeSwatch = cut.Find("button[data-theme-option=Red] .theme-swatch");
        var modeSwatch = cut.Find("button[data-color-mode-option=Light] .theme-swatch");
        Assert.Contains("theme-swatch-red", themeSwatch.ClassList);
        Assert.Contains("theme-swatch-dark", themeSwatch.ClassList);
        Assert.Contains("theme-swatch-pink", modeSwatch.ClassList);
        Assert.Contains("theme-swatch-light", modeSwatch.ClassList);
    }

    [Fact]
    public void Render_InitializesViewModel()
    {
        Render<SettingsPage>();

        _viewModel.Received(1).InitializeAsync();
    }

    [Fact]
    public void Render_DefaultScopeSelect_OffersBothWorkAndPrivate()
    {
        var cut = Render<SettingsPage>();

        var options = cut.FindAll("#defaultScopeSelect option");
        Assert.Equal(3, options.Count);
        Assert.Equal(Text("DefaultScopeBoth"), options[0].TextContent);
        Assert.Equal(Text("DefaultScopeWork"), options[1].TextContent);
        Assert.Equal(Text("DefaultScopePrivate"), options[2].TextContent);
    }

    [Fact]
    public void ChangeDefaultScopeSelect_Private_SetsTheDefaultScope()
    {
        var cut = Render<SettingsPage>();

        cut.Find("#defaultScopeSelect").Change(DefaultScope.Private.ToString());

        _viewModel.Received(1).SetDefaultScopeAsync(DefaultScope.Private);
    }

    [Fact]
    public void Render_NothingOpen_ShowsNoDialog()
    {
        var cut = Render<SettingsPage>();

        var dialogs = cut.FindAll(".confirm-dialog");
        Assert.Empty(dialogs);
    }

    [Fact]
    public void Render_ShowsResetButton()
    {
        var cut = Render<SettingsPage>();

        var resetButton = cut.Find("button.settings-reset-button");
        Assert.Equal(Text("ResetData"), resetButton.TextContent.Trim());
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
    public void BackPress_CalendarImportOpen_ClosesPopupOnly()
    {
        _viewModel.IsCalendarImportOpen.Returns(true);
        var handlers = CaptureBackPressHandlers();
        Render<SettingsPage>();

        var handled = InvokeTopmostBackPressHandler(handlers);

        Assert.True(handled);
        _viewModel.Received(1).CloseCalendarImport();
        _viewModel.DidNotReceive().CancelResetData();
        _viewModel.DidNotReceive().CloseImportCanceledPopup();
    }

    [Fact]
    public void BackPress_CalendarImportClosed_IsNotHandledBySettingsPage()
    {
        var handlers = CaptureBackPressHandlers();
        Render<SettingsPage>();

        var handled = InvokeTopmostBackPressHandler(handlers);

        Assert.False(handled);
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
        var exportButton = cut.Find("button.settings-transfer-button.app-button--primary");

        exportButton.Click();

        _viewModel.Received(1).ExportDataAsync();
    }

    [Fact]
    public void ClickExportCalendar_ExportsCalendar()
    {
        var cut = Render<SettingsPage>();
        var exportCalendarButton = cut.FindAll("button.settings-transfer-button")[3];

        exportCalendarButton.Click();

        _viewModel.Received(1).ExportCalendarAsync();
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
        Assert.Equal(2, options.Count);
        Assert.Equal("Hello Sunshine", options[0].TextContent);
        Assert.Equal("Raw/Hello_Sunshine.mp3", options[0].GetAttribute("value"));
        Assert.Equal("Kurz nervig", options[1].TextContent);
        Assert.Empty(cut.FindAll(".settings-selection"));
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

    [Fact]
    public void Render_Rewarding_ShowsThreeSwitchesAndBalanceGoalSelect()
    {
        var cut = Render<SettingsPage>();

        Assert.NotNull(cut.Find("#microFeedbackToggle"));
        Assert.NotNull(cut.Find("#reviewsToggle"));
        Assert.NotNull(cut.Find("#companionToggle"));
        var options = cut.FindAll("#balanceGoalSelect option").Select(option => option.TextContent.Trim()).ToArray();
        Assert.Equal(Enum.GetValues<BalanceGoal>().Length, options.Length);
        Assert.Equal(Text("BalanceGoalOneToOne"), options[0]);
        Assert.Contains(Text("RewardingTitle"), cut.Markup);
    }

    [Fact]
    public void Render_RewardingState_IsReflectedInSwitches()
    {
        _viewModel.MicroFeedbackEnabled.Returns(true);
        _viewModel.ReviewsEnabled.Returns(false);
        _viewModel.CompanionEnabled.Returns(true);

        var cut = Render<SettingsPage>();

        Assert.True(cut.Find("#microFeedbackToggle").HasAttribute("checked"));
        Assert.False(cut.Find("#reviewsToggle").HasAttribute("checked"));
        Assert.True(cut.Find("#companionToggle").HasAttribute("checked"));
    }

    [Fact]
    public void ToggleMicroFeedback_Off_CallsViewModel()
    {
        _viewModel.MicroFeedbackEnabled.Returns(true);
        var cut = Render<SettingsPage>();

        cut.Find("#microFeedbackToggle").Change(false);

        _viewModel.Received(1).SetMicroFeedbackEnabledAsync(false);
    }

    [Fact]
    public void ToggleReviewsAndCompanion_Off_CallViewModel()
    {
        _viewModel.ReviewsEnabled.Returns(true);
        _viewModel.CompanionEnabled.Returns(true);
        var cut = Render<SettingsPage>();

        cut.Find("#reviewsToggle").Change(false);
        cut.Find("#companionToggle").Change(false);

        _viewModel.Received(1).SetReviewsEnabledAsync(false);
        _viewModel.Received(1).SetCompanionEnabledAsync(false);
    }

    [Fact]
    public void ChangeBalanceGoal_ToThreeToTwo_CallsViewModel()
    {
        var cut = Render<SettingsPage>();

        cut.Find("#balanceGoalSelect").Change(nameof(BalanceGoal.ThreeToTwo));

        _viewModel.Received(1).SetBalanceGoalAsync(BalanceGoal.ThreeToTwo);
    }

    [Fact]
    public void Render_BalanceGoal_OffersTheSuggestionsAndOwnRatio()
    {
        var cut = Render<SettingsPage>();

        var options = cut.FindAll("#balanceGoalSelect option").Select(option => option.TextContent.Trim()).ToArray();
        Assert.Contains(Text("BalanceGoalOneToOne"), options);
        Assert.Contains(Text("BalanceGoalTwoToThree"), options);
        Assert.Contains(Text("BalanceGoalThreeToTwo"), options);
        Assert.Contains(Text("BalanceGoalCustom"), options);
    }

    [Fact]
    public void Render_NoCustomGoal_ShowsNoRatioInputs()
    {
        var cut = Render<SettingsPage>();

        Assert.Empty(cut.FindAll("#customForYouInput"));
        Assert.Empty(cut.FindAll("#customToDoInput"));
    }

    [Fact]
    public void Render_CustomGoal_ShowsTheOwnValues()
    {
        _viewModel.BalanceGoal.Returns(BalanceGoal.Custom);
        _viewModel.CustomForYou.Returns(2);
        _viewModel.CustomToDo.Returns(5);

        var cut = Render<SettingsPage>();

        Assert.Equal("2", cut.Find("#customForYouInput").GetAttribute("value"));
        Assert.Equal("5", cut.Find("#customToDoInput").GetAttribute("value"));
    }

    [Fact]
    public void ChangeCustomValues_Input_CallsViewModelWithBothValues()
    {
        _viewModel.BalanceGoal.Returns(BalanceGoal.Custom);
        _viewModel.CustomForYou.Returns(2);
        _viewModel.CustomToDo.Returns(5);
        var cut = Render<SettingsPage>();

        cut.Find("#customForYouInput").Change("3");
        cut.Find("#customToDoInput").Change("4");

        _viewModel.Received(1).SetCustomBalanceAsync(3, 5);
        _viewModel.Received(1).SetCustomBalanceAsync(2, 4);
    }

    [Fact]
    public void ChangeCustomValues_NoNumber_IsIgnored()
    {
        _viewModel.BalanceGoal.Returns(BalanceGoal.Custom);
        var cut = Render<SettingsPage>();

        cut.Find("#customForYouInput").Change("abc");

        _viewModel.DidNotReceive().SetCustomBalanceAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public void Render_Introduction_ShowsAButtonThatOpensItAgain()
    {
        var cut = Render<SettingsPage>();

        Assert.Equal(Text("OnboardingRestart"), cut.Find("#onboardingRestart").TextContent.Trim());
        cut.Find("#onboardingRestart").Click();

        _onboarding.Received(1).Restart();
    }

    [Fact]
    public void ChangeBalanceGoal_UnknownValue_IsIgnored()
    {
        var cut = Render<SettingsPage>();

        cut.Find("#balanceGoalSelect").Change("99");

        _viewModel.DidNotReceive().SetBalanceGoalAsync(Arg.Any<BalanceGoal>());
    }

    [Fact]
    public void Render_Rewarding_ShowsNoExplanationsUnderTheSwitches()
    {
        var cut = Render<SettingsPage>();

        Assert.Empty(cut.FindAll(".settings-toggle-hint"));
        Assert.Contains(Text("BalanceGoalHint"), cut.Markup);
    }

    [Fact]
    public void Render_Page_SetsThePageTitle()
    {
        var head = Render<HeadOutlet>();

        Render<SettingsPage>();

        head.WaitForAssertion(() => Assert.Contains(Text("Settings"), head.Markup));
    }

    [Fact]
    public void ChangeRingtone_NoValue_PassesNullToTheViewModel()
    {
        var cut = Render<SettingsPage>();

        cut.Find("#ringtoneSelect").TriggerEvent("onchange", new ChangeEventArgs { Value = null });

        _viewModel.Received(1).SelectRingtoneAsync(null);
    }

    [Fact]
    public void ChangeBalanceGoal_NoValue_ChangesNothing()
    {
        var cut = Render<SettingsPage>();

        cut.Find("#balanceGoalSelect").TriggerEvent("onchange", new ChangeEventArgs { Value = null });

        _viewModel.DidNotReceive().SetBalanceGoalAsync(Arg.Any<BalanceGoal>());
    }

    [Fact]
    public void ChangeCustomToDo_NoNumber_ChangesNothing()
    {
        _viewModel.BalanceGoal.Returns(BalanceGoal.Custom);
        var cut = Render<SettingsPage>();

        cut.Find("#customToDoInput").Change("abc");

        _viewModel.DidNotReceive().SetCustomBalanceAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public void ChangeCustomValues_NoValue_ChangesNothing()
    {
        _viewModel.BalanceGoal.Returns(BalanceGoal.Custom);
        var cut = Render<SettingsPage>();

        cut.Find("#customForYouInput").TriggerEvent("onchange", new ChangeEventArgs { Value = null });
        cut.Find("#customToDoInput").TriggerEvent("onchange", new ChangeEventArgs { Value = null });

        _viewModel.DidNotReceive().SetCustomBalanceAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public void ChangeLanguage_NoValue_ChangesNothing()
    {
        var cut = Render<SettingsPage>();

        cut.Find("#languageSelect").TriggerEvent("onchange", new ChangeEventArgs { Value = null });

        _localization.DidNotReceive().SetLanguageAsync(Arg.Any<AppLanguage>());
    }

    [Fact]
    public void Render_RingtoneFileWithoutName_ShowsTheFullPathAsLabel()
    {
        _viewModel.AvailableRingtones.Returns([new RingtoneOption(Guid.NewGuid(), "Raw/.mp3")]);
        _viewModel.SelectedRingtonePath.Returns("Raw/.mp3");

        var cut = Render<SettingsPage>();

        var option = Assert.Single(cut.FindAll("#ringtoneSelect option"));
        Assert.Equal("Raw/.mp3", option.TextContent);
    }

    [Fact]
    public void Render_NoRingtoneSelected_DisablesPlay()
    {
        _viewModel.SelectedRingtonePath.Returns((string?)null);

        var cut = Render<SettingsPage>();

        Assert.True(cut.Find(".settings-preview-button").HasAttribute("disabled"));
    }

    [Fact]
    public void Render_NoRingtonesAvailable_ShowsTheEmptyHint()
    {
        _viewModel.AvailableRingtones.Returns([]);

        var cut = Render<SettingsPage>();

        Assert.Equal(Text("NoRingtones"), cut.Find(".settings-empty").TextContent);
        Assert.Empty(cut.FindAll("#ringtoneSelect"));
    }

    private List<Func<bool>> CaptureBackPressHandlers()
    {
        var captured = new List<Func<bool>>();
        var backPress = Services.GetRequiredService<IUiBackPressService>();
        backPress.RegisterHandler(Arg.Do<Func<bool>>(captured.Add));
        return captured;
    }

    private static bool InvokeTopmostBackPressHandler(List<Func<bool>> handlers)
    {
        for (var i = handlers.Count - 1; i >= 0; i--)
        {
            if (handlers[i]())
            {
                return true;
            }
        }

        return false;
    }
}
