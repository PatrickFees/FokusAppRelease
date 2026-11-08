using FokusApp.Components.Onboarding;
using FokusApp.Core;
using FokusApp.Core.Enums;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FocusAppTests.ViewModels;

public class OnboardingViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly IRewardingSettingsService _rewardingSettings = TestSettings.Create();
    private readonly IOnboardingSettingsService _onboardingSettings = Substitute.For<IOnboardingSettingsService>();
    private readonly IAppStartupStateService _startupState = Substitute.For<IAppStartupStateService>();
    private readonly IAppLocalizationService _localization = Substitute.For<IAppLocalizationService>();
    private readonly IAppThemeService _theme = Substitute.For<IAppThemeService>();
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly IChangelogSettingsService _changelogSettings = Substitute.For<IChangelogSettingsService>();
    private readonly IAppEnvironmentInfo _environmentInfo = Substitute.For<IAppEnvironmentInfo>();
    private readonly ICalendarImportService _calendarImportService = Substitute.For<ICalendarImportService>();
    private readonly ICalendarImportPlatformAdapter _calendarImportPlatformAdapter = Substitute.For<ICalendarImportPlatformAdapter>();
    private readonly ICalendarAutoSyncSettingsService _calendarAutoSyncSettings = Substitute.For<ICalendarAutoSyncSettingsService>();
    private readonly IScopeService _scopeService = Substitute.For<IScopeService>();
    private readonly ITimerAlertModeSettingsService _timerAlertModeSettings = Substitute.For<ITimerAlertModeSettingsService>();
    private readonly ITimerAlertEffectService _timerAlertEffectService = Substitute.For<ITimerAlertEffectService>();
    private readonly OnboardingViewModel _viewModel;

    public OnboardingViewModelTests()
    {
        _onboardingSettings.OnboardingState.Returns(OnboardingState.NotSeen);
        _environmentInfo.AppVersionNumber.Returns("1.5.0");
        _calendarImportService.GetAvailableCalendarsAsync().Returns<IReadOnlyList<DeviceCalendarInfo>>([]);
        _viewModel = new OnboardingViewModel(
            _rewardingSettings, _onboardingSettings, _startupState, _localization, _theme, _errorCapture, _changelogSettings, _environmentInfo,
            _calendarImportService, _calendarImportPlatformAdapter, _calendarAutoSyncSettings, _scopeService,
            _timerAlertModeSettings, _timerAlertEffectService, new FakeTimeProvider(Now));
    }

    [Fact]
    public void Constructor_BeforeInitialization_IsClosed()
    {
        Assert.False(_viewModel.IsOpen);
    }

    [Fact]
    public async Task InitializeAsync_FreshInstall_WaitsForStartupAndOpensTheLanguagePage()
    {
        await _viewModel.InitializeAsync();

        await _startupState.Received(1).WaitForReadyAsync(Arg.Any<CancellationToken>());
        Assert.True(_viewModel.IsOpen);
        Assert.Equal(OnboardingStep.Language, _viewModel.Step);
        Assert.False(_viewModel.IsRevisit);
        Assert.Equal(1, _viewModel.PageNumber);
        Assert.Equal(9, _viewModel.PageCount);
    }

    [Fact]
    public async Task InitializeAsync_ExistingUserWithPendingInvite_OpensTheQuietInvitation()
    {
        _onboardingSettings.OnboardingState.Returns(OnboardingState.InvitePending);

        await _viewModel.InitializeAsync();

        Assert.True(_viewModel.IsOpen);
        Assert.Equal(OnboardingStep.Invite, _viewModel.Step);
    }

    [Fact]
    public async Task InitializeAsync_AlreadyCompleted_StaysClosed()
    {
        _onboardingSettings.OnboardingState.Returns(OnboardingState.Completed);

        await _viewModel.InitializeAsync();

        Assert.False(_viewModel.IsOpen);
    }

    [Fact]
    public async Task Next_FromTheLanguagePage_GoesThroughAllPagesToTheLastPage()
    {
        OnboardingStep[] expected =
        [
            OnboardingStep.Welcome,
            OnboardingStep.Scene,
            OnboardingStep.Why,
            OnboardingStep.Theme,
            OnboardingStep.Features,
            OnboardingStep.Balance,
            OnboardingStep.CalendarImport,
            OnboardingStep.TimerNotification
        ];
        await _viewModel.InitializeAsync();

        foreach (var step in expected)
        {
            GoOneStepForward();

            Assert.Equal(step, _viewModel.Step);
            Assert.Equal(step != OnboardingStep.Scene, _viewModel.CanGoBack);
        }

        Assert.True(_viewModel.IsLastStep);
        Assert.Equal(9, _viewModel.PageNumber);
    }

    [Fact]
    public async Task Next_OnTheLastPage_StaysThere()
    {
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.TimerNotification);

        _viewModel.Next();

        Assert.Equal(OnboardingStep.TimerNotification, _viewModel.Step);
    }

    [Fact]
    public async Task Back_FromTheBalancePage_GoesBackToTheEndOfTheSceneAndNoFurther()
    {
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.Balance);

        _viewModel.Back();
        Assert.Equal(OnboardingStep.Features, _viewModel.Step);
        _viewModel.Back();
        Assert.Equal(OnboardingStep.Theme, _viewModel.Step);
        _viewModel.Back();
        Assert.Equal(OnboardingStep.Why, _viewModel.Step);
        Assert.True(_viewModel.CanGoBack);
        _viewModel.Back();
        Assert.Equal(OnboardingStep.Scene, _viewModel.Step);
        Assert.Equal(OnboardingScenePhase.ScopeDemo, _viewModel.ScenePhase);
        Assert.False(_viewModel.CanGoBack);
        _viewModel.Back();

        Assert.Equal(OnboardingStep.Scene, _viewModel.Step);
    }

    [Fact]
    public async Task Next_FromTheWelcomePage_StartsTheSceneWithThePlantOnTheStage()
    {
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.Welcome);
        Assert.False(_viewModel.IsSceneActive);
        Assert.False(_viewModel.IsPlantOnStage);

        _viewModel.Next();

        Assert.Equal(OnboardingStep.Scene, _viewModel.Step);
        Assert.True(_viewModel.IsSceneActive);
        Assert.True(_viewModel.IsPlantOnStage);
        Assert.Equal(OnboardingScenePhase.Question, _viewModel.ScenePhase);
        Assert.False(_viewModel.CanGoBack);
    }

    [Fact]
    public async Task Next_WhileTheQuestionIsOpen_StaysInTheScene()
    {
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.Scene);

        _viewModel.Next();

        Assert.Equal(OnboardingStep.Scene, _viewModel.Step);
        Assert.Equal(OnboardingScenePhase.Question, _viewModel.ScenePhase);
    }

    [Fact]
    public async Task AnswerPomodoroQuestion_Yes_GoesToTheTipWithoutTheTimerPage()
    {
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.Scene);

        _viewModel.AnswerPomodoroQuestion(true);

        Assert.Equal(OnboardingScenePhase.Tip, _viewModel.ScenePhase);
        _viewModel.Next();
        Assert.Equal(OnboardingStep.Scene, _viewModel.Step);
        Assert.Equal(OnboardingScenePhase.DayIntro, _viewModel.ScenePhase);
        Assert.True(_viewModel.IsSceneActive);
    }

    [Fact]
    public async Task AnswerPomodoroQuestion_No_ExplainsOnTheTimerPageThenTipThenReturns()
    {
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.Scene);

        _viewModel.AnswerPomodoroQuestion(false);
        Assert.Equal(OnboardingScenePhase.TimerDemo, _viewModel.ScenePhase);

        _viewModel.Next();
        Assert.Equal(OnboardingScenePhase.Tip, _viewModel.ScenePhase);

        _viewModel.Next();
        Assert.Equal(OnboardingScenePhase.Returning, _viewModel.ScenePhase);
        Assert.Equal(OnboardingStep.Scene, _viewModel.Step);
        Assert.True(_viewModel.IsSceneActive);

        _viewModel.Next();
        Assert.Equal(OnboardingScenePhase.Returning, _viewModel.ScenePhase);

        _viewModel.CompleteScenePhase();
        Assert.Equal(OnboardingScenePhase.DayIntro, _viewModel.ScenePhase);
        Assert.Equal(OnboardingStep.Scene, _viewModel.Step);
    }

    [Fact]
    public async Task Next_ThroughTheDayPartOfTheScene_LeavesToTheReasonsPageWithThePlantStaying()
    {
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.Scene);
        _viewModel.AnswerPomodoroQuestion(true);
        _viewModel.Next();
        Assert.Equal(OnboardingScenePhase.DayIntro, _viewModel.ScenePhase);

        _viewModel.Next();
        Assert.Equal(OnboardingScenePhase.DayDemo, _viewModel.ScenePhase);

        _viewModel.Next();
        Assert.Equal(OnboardingScenePhase.DayDemo, _viewModel.ScenePhase);

        _viewModel.CompleteScenePhase();
        Assert.Equal(OnboardingScenePhase.ScopeDemo, _viewModel.ScenePhase);
        Assert.True(_viewModel.IsSceneActive);

        _viewModel.Next();
        Assert.Equal(OnboardingStep.Why, _viewModel.Step);
        Assert.False(_viewModel.IsSceneActive);
        Assert.True(_viewModel.IsPlantOnStage);
        Assert.True(_viewModel.CanGoBack);
    }

    [Fact]
    public async Task CompleteScenePhase_InPhasesThatWaitForTheUser_DoesNothing()
    {
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.Scene);

        _viewModel.CompleteScenePhase();
        Assert.Equal(OnboardingScenePhase.Question, _viewModel.ScenePhase);

        _viewModel.AnswerPomodoroQuestion(true);
        _viewModel.CompleteScenePhase();
        Assert.Equal(OnboardingScenePhase.Tip, _viewModel.ScenePhase);

        _viewModel.Next();
        _viewModel.CompleteScenePhase();
        Assert.Equal(OnboardingScenePhase.DayIntro, _viewModel.ScenePhase);
    }

    [Fact]
    public async Task CompleteScenePhase_OnAnotherPage_DoesNothing()
    {
        await _viewModel.InitializeAsync();

        _viewModel.CompleteScenePhase();

        Assert.Equal(OnboardingStep.Language, _viewModel.Step);
    }

    [Fact]
    public async Task FinishScene_InTheMiddleOfTheScene_LeavesToTheReasonsPage()
    {
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.Scene);

        _viewModel.FinishScene();

        Assert.Equal(OnboardingStep.Why, _viewModel.Step);
        Assert.False(_viewModel.IsSceneActive);
        Assert.True(_viewModel.IsPlantOnStage);
    }

    [Fact]
    public async Task AnswerPomodoroQuestion_OutsideTheQuestion_IsIgnored()
    {
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.Scene);
        _viewModel.AnswerPomodoroQuestion(false);

        _viewModel.AnswerPomodoroQuestion(true);

        Assert.Equal(OnboardingScenePhase.TimerDemo, _viewModel.ScenePhase);
    }

    [Fact]
    public async Task AnswerPomodoroQuestion_OnAnotherPage_IsIgnored()
    {
        await _viewModel.InitializeAsync();

        _viewModel.AnswerPomodoroQuestion(true);

        Assert.Equal(OnboardingStep.Language, _viewModel.Step);
        Assert.Equal(OnboardingScenePhase.Question, _viewModel.ScenePhase);
    }

    [Fact]
    public async Task FinishScene_OutsideTheScene_DoesNothing()
    {
        await _viewModel.InitializeAsync();

        _viewModel.FinishScene();

        Assert.Equal(OnboardingStep.Language, _viewModel.Step);
    }

    [Fact]
    public async Task Back_FromThePageAfterTheScene_ShowsTheLastSpeechBubbleAgain()
    {
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.Why);

        _viewModel.Back();

        Assert.Equal(OnboardingStep.Scene, _viewModel.Step);
        Assert.Equal(OnboardingScenePhase.ScopeDemo, _viewModel.ScenePhase);
        Assert.True(_viewModel.IsPlantOnStage);

        _viewModel.Next();

        Assert.Equal(OnboardingStep.Why, _viewModel.Step);
    }

    [Fact]
    public async Task FinishAsync_AfterTheScene_TakesThePlantOffTheStage()
    {
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.Balance);
        Assert.True(_viewModel.IsPlantOnStage);

        await _viewModel.FinishAsync();

        Assert.False(_viewModel.IsPlantOnStage);
        Assert.False(_viewModel.IsSceneActive);
    }

    [Fact]
    public async Task Restart_AfterTheScene_StartsWithoutThePlantOnTheStage()
    {
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.Balance);

        _viewModel.Restart();

        Assert.False(_viewModel.IsPlantOnStage);
        Assert.Equal(OnboardingStep.Language, _viewModel.Step);
        Assert.Equal(OnboardingScenePhase.Question, _viewModel.ScenePhase);
    }

    [Fact]
    public async Task Scene_EveryChange_NotifiesTheSceneProperties()
    {
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.Scene);
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        _viewModel.AnswerPomodoroQuestion(false);

        Assert.Contains(nameof(IOnboardingViewModel.ScenePhase), changed);
        Assert.True(_viewModel.IsSceneActive);
        Assert.True(_viewModel.IsPlantOnStage);
    }

    [Fact]
    public async Task SelectLanguageAsync_Language_IsSetAndContinuesWithTheWelcomePage()
    {
        await _viewModel.InitializeAsync();

        await _viewModel.SelectLanguageAsync(AppLanguage.English);

        await _localization.Received(1).SetLanguageAsync(AppLanguage.English);
        Assert.Equal(OnboardingStep.Welcome, _viewModel.Step);
    }

    [Fact]
    public async Task SelectLanguageAsync_StoringFails_CapturesTheErrorAndStillContinues()
    {
        var failure = new InvalidOperationException("Datenbank gesperrt");
        _localization.SetLanguageAsync(Arg.Any<AppLanguage>()).ThrowsAsync(failure);
        await _viewModel.InitializeAsync();

        await _viewModel.SelectLanguageAsync(AppLanguage.English);

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null && options.Source == "OnboardingViewModel.SelectLanguageAsync" && options.IsUiFailure));
        Assert.Equal(OnboardingStep.Welcome, _viewModel.Step);
    }

    [Fact]
    public async Task SetThemeAsync_Theme_IsSetAtOnce()
    {
        await _viewModel.InitializeAsync();

        await _viewModel.SetThemeAsync(AppColorTheme.Green);

        await _theme.Received(1).SetThemeAsync(AppColorTheme.Green);
        Assert.Equal(OnboardingStep.Language, _viewModel.Step);
    }

    [Fact]
    public async Task SetThemeAsync_StoringFails_CapturesTheError()
    {
        var failure = new InvalidOperationException("Datenbank gesperrt");
        _theme.SetThemeAsync(Arg.Any<AppColorTheme>()).ThrowsAsync(failure);

        await _viewModel.SetThemeAsync(AppColorTheme.Green);

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null && options.Source == "OnboardingViewModel.SetThemeAsync" && options.IsUiFailure));
    }

    [Fact]
    public async Task SetColorModeAsync_Mode_IsSetAtOnce()
    {
        await _viewModel.InitializeAsync();

        await _viewModel.SetColorModeAsync(AppColorMode.Dark);

        await _theme.Received(1).SetColorModeAsync(AppColorMode.Dark);
        Assert.Equal(OnboardingStep.Language, _viewModel.Step);
    }

    [Fact]
    public async Task SetColorModeAsync_StoringFails_CapturesTheError()
    {
        var failure = new InvalidOperationException("Datenbank gesperrt");
        _theme.SetColorModeAsync(Arg.Any<AppColorMode>()).ThrowsAsync(failure);

        await _viewModel.SetColorModeAsync(AppColorMode.Dark);

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null && options.Source == "OnboardingViewModel.SetColorModeAsync" && options.IsUiFailure));
    }

    [Fact]
    public void ColorMode_Always_OffersLightAndDarkWithTheSelectedMode()
    {
        _theme.SelectedMode.Returns(AppColorMode.Dark);

        Assert.Equal(Enum.GetValues<AppColorMode>(), _viewModel.ColorModeOptions);
        Assert.Equal(AppColorMode.Dark, _viewModel.ColorMode);
    }

    [Fact]
    public void ThemeChanged_Elsewhere_NotifiesTheColorMode()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        _theme.ThemeChanged += Raise.EventWith(EventArgs.Empty);

        Assert.Contains(nameof(IOnboardingViewModel.ColorMode), changed);
    }

    [Fact]
    public void Options_Always_OfferEveryLanguageAndTheme()
    {
        _localization.CurrentLanguage.Returns(AppLanguage.English);
        _theme.SelectedTheme.Returns(AppColorTheme.Pink);

        Assert.Equal(Enum.GetValues<AppLanguage>(), _viewModel.LanguageOptions);
        Assert.Equal(Enum.GetValues<AppColorTheme>(), _viewModel.ThemeOptions);
        Assert.Equal(AppLanguage.English, _viewModel.Language);
        Assert.Equal(AppColorTheme.Pink, _viewModel.Theme);
    }

    [Fact]
    public void LanguageOrThemeChanged_Elsewhere_NotifiesTheChoices()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        _localization.LanguageChanged += Raise.EventWith(EventArgs.Empty);
        _theme.ThemeChanged += Raise.EventWith(EventArgs.Empty);

        Assert.Contains(nameof(IOnboardingViewModel.Language), changed);
        Assert.Contains(nameof(IOnboardingViewModel.Theme), changed);
    }

    [Fact]
    public async Task FinishAsync_OnTheLastPage_StoresCompletedAndCloses()
    {
        await _viewModel.InitializeAsync();

        await _viewModel.FinishAsync();

        await _onboardingSettings.Received(1).SetOnboardingStateAsync(OnboardingState.Completed);
        await _changelogSettings.Received(1).SetLastSeenChangelogVersionAsync("1.5.0");
        Assert.False(_viewModel.IsOpen);
    }

    [Fact]
    public void Restart_Anytime_OpensFromTheLanguagePageAsARevisit()
    {
        _viewModel.Restart();

        Assert.True(_viewModel.IsOpen);
        Assert.True(_viewModel.IsRevisit);
        Assert.Equal(OnboardingStep.Language, _viewModel.Step);
    }

    [Fact]
    public async Task DeclineInviteAsync_ExistingUser_CompletesWithoutChangingAnything()
    {
        _onboardingSettings.OnboardingState.Returns(OnboardingState.InvitePending);
        await _viewModel.InitializeAsync();

        await _viewModel.DeclineInviteAsync();

        await _onboardingSettings.Received(1).SetOnboardingStateAsync(OnboardingState.Completed);
        await _rewardingSettings.DidNotReceiveWithAnyArgs().SetRewardingAsync(default!);
        Assert.False(_viewModel.IsOpen);
    }

    [Fact]
    public async Task AcceptInvite_ExistingUser_ContinuesWithTheLanguagePage()
    {
        _onboardingSettings.OnboardingState.Returns(OnboardingState.InvitePending);
        await _viewModel.InitializeAsync();

        _viewModel.AcceptInvite();

        Assert.True(_viewModel.IsOpen);
        Assert.Equal(OnboardingStep.Language, _viewModel.Step);
    }

    [Fact]
    public async Task Setters_Rewarding_TakeEffectAtOnceAndKeepTheOtherValues()
    {
        var current = AppRewardingSettings.Default;
        _rewardingSettings.Rewarding.Returns(current);
        await _viewModel.InitializeAsync();

        await _viewModel.SetMicroFeedbackEnabledAsync(false);
        await _viewModel.SetReviewsEnabledAsync(false);
        await _viewModel.SetCompanionEnabledAsync(false);
        await _viewModel.SetBalanceGoalAsync(BalanceGoal.TwoToThree);

        await _rewardingSettings.Received(1).SetRewardingAsync(current with { MicroFeedbackEnabled = false });
        await _rewardingSettings.Received(1).SetRewardingAsync(current with { ReviewsEnabled = false });
        await _rewardingSettings.Received(1).SetRewardingAsync(current with { CompanionEnabled = false });
        await _rewardingSettings.Received(1).SetRewardingAsync(current with { BalanceGoal = BalanceGoal.TwoToThree });
    }

    [Fact]
    public void BalanceGoalOptions_Always_AreTheRecommendedPresets()
    {
        Assert.Equal([BalanceGoal.OneToOne, BalanceGoal.TwoToThree, BalanceGoal.ThreeToTwo], _viewModel.BalanceGoalOptions);
    }

    [Fact]
    public async Task FinishAsync_StoringFails_CapturesTheErrorAndStillCloses()
    {
        var failure = new InvalidOperationException("Datenbank gesperrt");
        _onboardingSettings.SetOnboardingStateAsync(Arg.Any<OnboardingState>()).ThrowsAsync(failure);
        await _viewModel.InitializeAsync();

        await _viewModel.FinishAsync();

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null && options.Source == "OnboardingViewModel.CompleteAsync" && options.IsUiFailure));
        Assert.False(_viewModel.IsOpen);
    }

    [Fact]
    public async Task RewardingChanged_Elsewhere_NotifiesTheSwitches()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        _rewardingSettings.RewardingChanged += Raise.EventWith(EventArgs.Empty);

        Assert.Contains(nameof(IOnboardingViewModel.CompanionEnabled), changed);
        await Task.CompletedTask;
    }

    [Fact]
    public void Dispose_AfterDispose_IgnoresSettingChanges()
    {
        _viewModel.Dispose();
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        _rewardingSettings.RewardingChanged += Raise.EventWith(EventArgs.Empty);

        Assert.Empty(changed);
    }

    [Fact]
    public void RewardingValues_StoredSettings_AreReadFromTheSettingsService()
    {
        var rewarding = AppRewardingSettings.Default with
        {
            MicroFeedbackEnabled = false,
            ReviewsEnabled = false,
            CompanionEnabled = false,
            BalanceGoal = BalanceGoal.ThreeToTwo
        };
        _rewardingSettings.Rewarding.Returns(rewarding);

        Assert.False(_viewModel.MicroFeedbackEnabled);
        Assert.False(_viewModel.ReviewsEnabled);
        Assert.False(_viewModel.CompanionEnabled);
        Assert.Equal(BalanceGoal.ThreeToTwo, _viewModel.BalanceGoal);
    }

    [Fact]
    public void RewardingValues_DefaultSettings_AreAllOnWithTheRecommendedGoal()
    {
        var defaults = AppRewardingSettings.Default;

        Assert.True(_viewModel.MicroFeedbackEnabled);
        Assert.True(_viewModel.ReviewsEnabled);
        Assert.True(_viewModel.CompanionEnabled);
        Assert.Equal(defaults.BalanceGoal, _viewModel.BalanceGoal);
    }

    [Fact]
    public async Task Setters_StoringFails_CapturesTheErrorForEverySetting()
    {
        var failure = new InvalidOperationException("Datenbank gesperrt");
        _rewardingSettings.SetRewardingAsync(Arg.Any<AppRewardingSettings>()).ThrowsAsync(failure);
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        await _viewModel.SetMicroFeedbackEnabledAsync(false);
        await _viewModel.SetReviewsEnabledAsync(false);
        await _viewModel.SetCompanionEnabledAsync(false);
        await _viewModel.SetBalanceGoalAsync(BalanceGoal.TwoToThree);

        await _errorCapture.Received(4).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "OnboardingViewModel.UpdateRewardingAsync" &&
                options.IsUiFailure));
        Assert.Contains(nameof(IOnboardingViewModel.BalanceGoal), changed);
    }

    [Fact]
    public async Task Back_OnTheLanguagePage_StaysThere()
    {
        await _viewModel.InitializeAsync();

        _viewModel.Back();

        Assert.Equal(OnboardingStep.Language, _viewModel.Step);
        Assert.False(_viewModel.CanGoBack);
    }

    [Fact]
    public async Task Next_OnTheInvitePage_StaysThere()
    {
        _onboardingSettings.OnboardingState.Returns(OnboardingState.InvitePending);
        await _viewModel.InitializeAsync();

        _viewModel.Next();
        _viewModel.Back();

        Assert.Equal(OnboardingStep.Invite, _viewModel.Step);
        Assert.Equal(0, _viewModel.PageNumber);
    }

    [Fact]
    public async Task Next_IntoTheCalendarImportStep_LoadsTheDeviceCalendars()
    {
        _calendarImportPlatformAdapter.IsAvailable.Returns(true);
        _calendarImportService.GetAvailableCalendarsAsync().Returns<IReadOnlyList<DeviceCalendarInfo>>(
            [new DeviceCalendarInfo("cal-1", "Privat", "me@example.com")]);
        _scopeService.IsWorkScope.Returns(true);
        _scopeService.CanSwitch.Returns(true);
        await _viewModel.InitializeAsync();

        GoToStep(OnboardingStep.CalendarImport);

        Assert.False(_viewModel.IsCalendarImportLoading);
        Assert.Single(_viewModel.CalendarImportCalendars);
        Assert.False(_viewModel.CalendarImportCalendars[0].IsSelected);
        Assert.True(_viewModel.IsCalendarImportWorkTask);
        Assert.True(_viewModel.CanChangeCalendarImportScope);
    }

    [Fact]
    public async Task ToggleCalendarImportCalendar_Twice_SelectsThenDeselects()
    {
        _calendarImportService.GetAvailableCalendarsAsync().Returns<IReadOnlyList<DeviceCalendarInfo>>(
            [new DeviceCalendarInfo("cal-1", "Privat", "me@example.com")]);
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.CalendarImport);

        _viewModel.ToggleCalendarImportCalendar("cal-1");
        Assert.True(_viewModel.CalendarImportCalendars[0].IsSelected);

        _viewModel.ToggleCalendarImportCalendar("cal-1");
        Assert.False(_viewModel.CalendarImportCalendars[0].IsSelected);
    }

    [Fact]
    public async Task ContinueFromCalendarImportAsync_WithASelectedCalendar_ImportsThenAdvances()
    {
        _calendarImportService.GetAvailableCalendarsAsync().Returns<IReadOnlyList<DeviceCalendarInfo>>(
            [new DeviceCalendarInfo("cal-1", "Privat", "me@example.com")]);
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.CalendarImport);
        _viewModel.ToggleCalendarImportCalendar("cal-1");

        await _viewModel.ContinueFromCalendarImportAsync();

        await _calendarImportService.Received(1).ImportAsync(
            Arg.Is<IReadOnlyList<string>>(ids => ids.Single() == "cal-1"),
            Now.Date, Now.Date.AddDays(AppConstants.OnboardingCalendarImportHorizonDays), _viewModel.IsCalendarImportWorkTask);
        Assert.Equal(OnboardingStep.TimerNotification, _viewModel.Step);
        Assert.False(_viewModel.IsCalendarImportLoading);
    }

    [Fact]
    public async Task ContinueFromCalendarImportAsync_WithoutASelection_SkipsTheImportButAdvances()
    {
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.CalendarImport);

        await _viewModel.ContinueFromCalendarImportAsync();

        await _calendarImportService.DidNotReceiveWithAnyArgs().ImportAsync(default!, default, default, default);
        Assert.Equal(OnboardingStep.TimerNotification, _viewModel.Step);
    }

    [Fact]
    public async Task SetCalendarSyncEnabledAsync_Enabled_PersistsTheCurrentSelectionAndTheFlag()
    {
        _calendarImportService.GetAvailableCalendarsAsync().Returns<IReadOnlyList<DeviceCalendarInfo>>(
            [new DeviceCalendarInfo("cal-1", "Privat", "me@example.com")]);
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.CalendarImport);
        _viewModel.ToggleCalendarImportCalendar("cal-1");

        await _viewModel.SetCalendarSyncEnabledAsync(true);

        await _calendarAutoSyncSettings.Received(1).SetEnabledAsync(true);
        await _calendarAutoSyncSettings.Received(1).SetSelectedCalendarIdsAsync(
            Arg.Is<IReadOnlyList<string>>(ids => ids.Single() == "cal-1"));
        Assert.True(_viewModel.CalendarSyncEnabled);
    }

    [Fact]
    public async Task ToggleCalendarImportCalendar_WhileSyncIsEnabled_PersistsTheUpdatedSelection()
    {
        _calendarImportService.GetAvailableCalendarsAsync().Returns<IReadOnlyList<DeviceCalendarInfo>>(
            [new DeviceCalendarInfo("cal-1", "Privat", "me@example.com")]);
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.CalendarImport);
        await _viewModel.SetCalendarSyncEnabledAsync(true);

        _viewModel.ToggleCalendarImportCalendar("cal-1");

        await _calendarAutoSyncSettings.Received(1).SetSelectedCalendarIdsAsync(
            Arg.Is<IReadOnlyList<string>>(ids => ids.Single() == "cal-1"));
    }

    [Fact]
    public async Task TimerAlertSetters_TakeEffectAtOnce()
    {
        await _viewModel.InitializeAsync();
        _timerAlertModeSettings.TimerAlertMode.Returns(TimerAlertMode.Sound);

        await _viewModel.SetTimerAlertSoundEnabledAsync(false);
        await _viewModel.SetTimerAlertVibrationEnabledAsync(true);
        await _viewModel.SetTimerAlertAdaptToRingerModeEnabledAsync(true);

        await _timerAlertModeSettings.Received(1).SetTimerAlertModeAsync(TimerAlertMode.None);
        await _timerAlertModeSettings.Received(1).SetTimerAlertModeAsync(TimerAlertMode.Sound | TimerAlertMode.Vibration);
        await _timerAlertModeSettings.Received(1).SetTimerAlertModeAsync(TimerAlertMode.Sound | TimerAlertMode.AdaptToRingerMode);
    }

    [Fact]
    public async Task FinishAsync_OnTheTimerNotificationPage_StoresCompletedAndCloses()
    {
        await _viewModel.InitializeAsync();
        GoToStep(OnboardingStep.TimerNotification);

        await _viewModel.FinishAsync();

        await _onboardingSettings.Received(1).SetOnboardingStateAsync(OnboardingState.Completed);
        Assert.False(_viewModel.IsOpen);
    }

    private void GoToStep(OnboardingStep step)
    {
        while (_viewModel.Step != step)
        {
            GoOneStepForward();
        }
    }

    private void GoOneStepForward()
    {
        if (_viewModel.Step != OnboardingStep.Scene)
        {
            _viewModel.Next();
            return;
        }

        if (_viewModel.ScenePhase == OnboardingScenePhase.Question)
        {
            _viewModel.AnswerPomodoroQuestion(true);
        }

        while (_viewModel.Step == OnboardingStep.Scene)
        {
            if (_viewModel.ScenePhase == OnboardingScenePhase.DayDemo)
            {
                _viewModel.CompleteScenePhase();
            }
            else
            {
                _viewModel.Next();
            }
        }
    }
}
