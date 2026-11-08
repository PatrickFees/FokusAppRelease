using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Components.PopUps.CalendarImport;
using FokusApp.Core;
using FokusApp.Core.Enums;
using FokusApp.Core.Interfaces;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Components.Onboarding;

public sealed partial class OnboardingViewModel : ObservableObject, IOnboardingViewModel, IDisposable
{
    private static readonly OnboardingStep[] Pages =
    [
        OnboardingStep.Language,
        OnboardingStep.Welcome,
        OnboardingStep.Scene,
        OnboardingStep.Why,
        OnboardingStep.Theme,
        OnboardingStep.Features,
        OnboardingStep.Balance,
        OnboardingStep.CalendarImport,
        OnboardingStep.TimerNotification
    ];

    private static readonly AppLanguage[] OfferedLanguages = Enum.GetValues<AppLanguage>();

    private static readonly AppColorTheme[] OfferedThemes = Enum.GetValues<AppColorTheme>();

    private static readonly AppColorMode[] OfferedColorModes = Enum.GetValues<AppColorMode>();

    private static readonly BalanceGoal[] OfferedGoals = [BalanceGoal.OneToOne, BalanceGoal.TwoToThree, BalanceGoal.ThreeToTwo];

    private readonly IRewardingSettingsService _rewardingSettingsService;
    private readonly IOnboardingSettingsService _onboardingSettingsService;
    private readonly IAppStartupStateService _appStartupStateService;
    private readonly IAppLocalizationService _localizationService;
    private readonly IAppThemeService _themeService;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly IChangelogSettingsService _changelogSettingsService;
    private readonly IAppEnvironmentInfo _environmentInfo;
    private readonly ICalendarImportService _calendarImportService;
    private readonly ICalendarImportPlatformAdapter _calendarImportPlatformAdapter;
    private readonly ICalendarAutoSyncSettingsService _calendarAutoSyncSettingsService;
    private readonly IScopeService _scopeService;
    private readonly ITimerAlertModeSettingsService _timerAlertModeSettingsService;
    private readonly ITimerAlertEffectService _timerAlertEffectService;
    private readonly TimeProvider _timeProvider;
    private readonly HashSet<string> _selectedCalendarImportIds = [];
    private IReadOnlyList<DeviceCalendarInfo> _availableCalendarsForImport = [];

    private bool _isPlantOnStage;
    private bool _hasVisitedTimer;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSceneActive))]
    [NotifyPropertyChangedFor(nameof(IsPlantOnStage))]
    private bool isOpen;

    [ObservableProperty]
    private bool isRevisit;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSceneActive))]
    [NotifyPropertyChangedFor(nameof(PageNumber))]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    [NotifyPropertyChangedFor(nameof(IsLastStep))]
    private OnboardingStep step = OnboardingStep.Language;

    [ObservableProperty]
    private OnboardingScenePhase scenePhase = OnboardingScenePhase.Question;

    [ObservableProperty]
    private bool isCalendarImportLoading;

    private DateTime _calendarImportFrom;
    private DateTime _calendarImportTo;

    [ObservableProperty]
    private bool canChangeCalendarImportScope;

    [ObservableProperty]
    private bool isCalendarImportWorkTask;

    [ObservableProperty]
    private bool calendarSyncEnabled;

    public OnboardingViewModel(
        IRewardingSettingsService rewardingSettingsService,
        IOnboardingSettingsService onboardingSettingsService,
        IAppStartupStateService appStartupStateService,
        IAppLocalizationService localizationService,
        IAppThemeService themeService,
        IAppErrorCaptureService appErrorCaptureService,
        IChangelogSettingsService changelogSettingsService,
        IAppEnvironmentInfo environmentInfo,
        ICalendarImportService calendarImportService,
        ICalendarImportPlatformAdapter calendarImportPlatformAdapter,
        ICalendarAutoSyncSettingsService calendarAutoSyncSettingsService,
        IScopeService scopeService,
        ITimerAlertModeSettingsService timerAlertModeSettingsService,
        ITimerAlertEffectService timerAlertEffectService,
        TimeProvider timeProvider)
    {
        _rewardingSettingsService = rewardingSettingsService;
        _onboardingSettingsService = onboardingSettingsService;
        _appStartupStateService = appStartupStateService;
        _localizationService = localizationService;
        _themeService = themeService;
        _appErrorCaptureService = appErrorCaptureService;
        _changelogSettingsService = changelogSettingsService;
        _environmentInfo = environmentInfo;
        _calendarImportService = calendarImportService;
        _calendarImportPlatformAdapter = calendarImportPlatformAdapter;
        _calendarAutoSyncSettingsService = calendarAutoSyncSettingsService;
        _scopeService = scopeService;
        _timerAlertModeSettingsService = timerAlertModeSettingsService;
        _timerAlertEffectService = timerAlertEffectService;
        _timeProvider = timeProvider;

        _rewardingSettingsService.RewardingChanged += OnRewardingChanged;
        _localizationService.LanguageChanged += OnRewardingChanged;
        _themeService.ThemeChanged += OnRewardingChanged;
        _timerAlertModeSettingsService.TimerAlertModeChanged += OnRewardingChanged;
    }

    public int PageNumber => Array.IndexOf(Pages, Step) + 1;

    public bool IsSceneActive => IsOpen && Step == OnboardingStep.Scene;

    public bool IsPlantOnStage => IsOpen && _isPlantOnStage;

    public int PageCount => Pages.Length;

    public bool CanGoBack => Step != OnboardingStep.Scene && Array.IndexOf(Pages, Step) > 0;

    public bool IsLastStep => Step == OnboardingStep.TimerNotification;

    public IReadOnlyList<AppLanguage> LanguageOptions => OfferedLanguages;

    public AppLanguage Language => _localizationService.CurrentLanguage;

    public IReadOnlyList<AppColorTheme> ThemeOptions => OfferedThemes;

    public AppColorTheme Theme => _themeService.SelectedTheme;

    public IReadOnlyList<AppColorMode> ColorModeOptions => OfferedColorModes;

    public AppColorMode ColorMode => _themeService.SelectedMode;

    public bool MicroFeedbackEnabled => _rewardingSettingsService.Rewarding.MicroFeedbackEnabled;

    public bool ReviewsEnabled => _rewardingSettingsService.Rewarding.ReviewsEnabled;

    public bool CompanionEnabled => _rewardingSettingsService.Rewarding.CompanionEnabled;

    public IReadOnlyList<BalanceGoal> BalanceGoalOptions => OfferedGoals;

    public BalanceGoal BalanceGoal => _rewardingSettingsService.Rewarding.BalanceGoal;

    public bool IsCalendarImportAvailable => _calendarImportPlatformAdapter.IsAvailable;

    public IReadOnlyList<CalendarOptionModel> CalendarImportCalendars => _availableCalendarsForImport
        .Select(calendar => new CalendarOptionModel(
            calendar.Id, calendar.DisplayName, calendar.AccountName, _selectedCalendarImportIds.Contains(calendar.Id)))
        .ToList();

    public bool TimerAlertSoundEnabled => _timerAlertModeSettingsService.TimerAlertMode.HasFlag(TimerAlertMode.Sound);

    public bool TimerAlertVibrationEnabled => _timerAlertModeSettingsService.TimerAlertMode.HasFlag(TimerAlertMode.Vibration);

    public bool TimerAlertAdaptToRingerModeEnabled => _timerAlertModeSettingsService.TimerAlertMode.HasFlag(TimerAlertMode.AdaptToRingerMode);

    public bool IsTimerAlertVibrationAvailable => _timerAlertEffectService.SupportsNativeEffects;

    public bool IsTimerAlertAdaptToRingerModeAvailable => _timerAlertEffectService.SupportsNativeEffects;

    public void Dispose()
    {
        _rewardingSettingsService.RewardingChanged -= OnRewardingChanged;
        _localizationService.LanguageChanged -= OnRewardingChanged;
        _themeService.ThemeChanged -= OnRewardingChanged;
        _timerAlertModeSettingsService.TimerAlertModeChanged -= OnRewardingChanged;
    }

    public async Task InitializeAsync()
    {
        await _appStartupStateService.WaitForReadyAsync();

        switch (_onboardingSettingsService.OnboardingState)
        {
            case OnboardingState.NotSeen:
                Open(OnboardingStep.Language, revisit: false);
                break;
            case OnboardingState.InvitePending:
                Open(OnboardingStep.Invite, revisit: false);
                break;
        }
    }

    public void Restart() => Open(OnboardingStep.Language, revisit: true);

    public void Next()
    {
        if (Step == OnboardingStep.Scene)
        {
            AdvanceScene();
            return;
        }

        var index = Array.IndexOf(Pages, Step);
        if (index < 0 || index >= Pages.Length - 1)
        {
            return;
        }

        var next = Pages[index + 1];
        if (next == OnboardingStep.Scene)
        {
            StartScene();
            return;
        }

        if (next == OnboardingStep.CalendarImport)
        {
            Step = next;
            _ = OpenCalendarImportStepAsync();
            return;
        }

        Step = next;
    }

    public void Back()
    {
        var index = Array.IndexOf(Pages, Step);
        if (Step == OnboardingStep.Scene || index <= 0)
        {
            return;
        }

        var previous = Pages[index - 1];
        if (previous == OnboardingStep.Scene)
        {
            ReturnToSceneEnd();
            return;
        }

        Step = previous;
    }

    public void AnswerPomodoroQuestion(bool knowsPrinciple)
    {
        if (Step != OnboardingStep.Scene || ScenePhase != OnboardingScenePhase.Question)
        {
            return;
        }

        _hasVisitedTimer = !knowsPrinciple;
        ScenePhase = knowsPrinciple ? OnboardingScenePhase.Tip : OnboardingScenePhase.TimerDemo;
    }

    public void CompleteScenePhase()
    {
        if (Step != OnboardingStep.Scene)
        {
            return;
        }

        switch (ScenePhase)
        {
            case OnboardingScenePhase.Returning:
                ScenePhase = OnboardingScenePhase.DayIntro;
                break;
            case OnboardingScenePhase.DayDemo:
                ScenePhase = OnboardingScenePhase.ScopeDemo;
                break;
        }
    }

    public void FinishScene()
    {
        if (Step != OnboardingStep.Scene)
        {
            return;
        }

        _hasVisitedTimer = false;
        Step = Pages[Array.IndexOf(Pages, OnboardingStep.Scene) + 1];
    }

    public async Task FinishAsync()
    {
        await CompleteAsync();
    }

    public Task DeclineInviteAsync() => CompleteAsync();

    public void AcceptInvite() => Step = OnboardingStep.Language;

    public async Task SelectLanguageAsync(AppLanguage language)
    {
        try
        {
            await _localizationService.SetLanguageAsync(language);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(OnboardingViewModel)}.{nameof(SelectLanguageAsync)}");
        }

        Next();
    }

    public async Task SetThemeAsync(AppColorTheme theme)
    {
        try
        {
            await _themeService.SetThemeAsync(theme);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(OnboardingViewModel)}.{nameof(SetThemeAsync)}");
        }
    }

    public async Task SetColorModeAsync(AppColorMode mode)
    {
        try
        {
            await _themeService.SetColorModeAsync(mode);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(OnboardingViewModel)}.{nameof(SetColorModeAsync)}");
        }
    }

    public Task SetMicroFeedbackEnabledAsync(bool enabled) =>
        UpdateRewardingAsync(rewarding => rewarding with { MicroFeedbackEnabled = enabled }, nameof(MicroFeedbackEnabled));

    public Task SetReviewsEnabledAsync(bool enabled) =>
        UpdateRewardingAsync(rewarding => rewarding with { ReviewsEnabled = enabled }, nameof(ReviewsEnabled));

    public Task SetCompanionEnabledAsync(bool enabled) =>
        UpdateRewardingAsync(rewarding => rewarding with { CompanionEnabled = enabled }, nameof(CompanionEnabled));

    public Task SetBalanceGoalAsync(BalanceGoal goal) =>
        UpdateRewardingAsync(rewarding => rewarding with { BalanceGoal = goal }, nameof(BalanceGoal));

    public void ToggleCalendarImportCalendar(string calendarId)
    {
        if (!_selectedCalendarImportIds.Remove(calendarId))
        {
            _selectedCalendarImportIds.Add(calendarId);
        }

        OnPropertyChanged(nameof(CalendarImportCalendars));

        if (CalendarSyncEnabled)
        {
            _ = PersistCalendarSyncSelectionAsync();
        }
    }

    public void SetCalendarImportIsWorkTask(bool isWorkTask)
    {
        if (!CanChangeCalendarImportScope)
        {
            return;
        }

        IsCalendarImportWorkTask = isWorkTask;
    }

    public async Task SetCalendarSyncEnabledAsync(bool enabled)
    {
        try
        {
            await _calendarAutoSyncSettingsService.SetEnabledAsync(enabled);
            if (enabled)
            {
                await _calendarAutoSyncSettingsService.SetSelectedCalendarIdsAsync(_selectedCalendarImportIds.ToList());
            }
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(OnboardingViewModel)}.{nameof(SetCalendarSyncEnabledAsync)}");
        }

        CalendarSyncEnabled = enabled;
    }

    public async Task ContinueFromCalendarImportAsync()
    {
        if (Step != OnboardingStep.CalendarImport)
        {
            return;
        }

        if (_selectedCalendarImportIds.Count > 0)
        {
            IsCalendarImportLoading = true;

            try
            {
                await _calendarImportService.ImportAsync(
                    _selectedCalendarImportIds.ToList(), _calendarImportFrom, _calendarImportTo, IsCalendarImportWorkTask);
            }
            catch (Exception ex)
            {
                await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(OnboardingViewModel)}.{nameof(ContinueFromCalendarImportAsync)}");
            }
            finally
            {
                IsCalendarImportLoading = false;
            }
        }

        Next();
    }

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
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(OnboardingViewModel)}.{nameof(SetTimerAlertFlagAsync)}");
        }

        OnPropertyChanged(nameof(TimerAlertSoundEnabled));
        OnPropertyChanged(nameof(TimerAlertVibrationEnabled));
        OnPropertyChanged(nameof(TimerAlertAdaptToRingerModeEnabled));
    }

    private async Task OpenCalendarImportStepAsync()
    {
        var today = _timeProvider.GetLocalNow().Date;
        _calendarImportFrom = today;
        _calendarImportTo = today.AddDays(AppConstants.OnboardingCalendarImportHorizonDays);
        IsCalendarImportWorkTask = _scopeService.IsWorkScope;
        CanChangeCalendarImportScope = _scopeService.CanSwitch;
        CalendarSyncEnabled = _calendarAutoSyncSettingsService.Enabled;
        _selectedCalendarImportIds.Clear();
        _availableCalendarsForImport = [];
        IsCalendarImportLoading = true;
        OnPropertyChanged(nameof(CalendarImportCalendars));

        try
        {
            _availableCalendarsForImport = await _calendarImportService.GetAvailableCalendarsAsync();
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(OnboardingViewModel)}.{nameof(OpenCalendarImportStepAsync)}");
        }
        finally
        {
            IsCalendarImportLoading = false;
            OnPropertyChanged(nameof(CalendarImportCalendars));
        }
    }

    private async Task PersistCalendarSyncSelectionAsync()
    {
        try
        {
            await _calendarAutoSyncSettingsService.SetSelectedCalendarIdsAsync(_selectedCalendarImportIds.ToList());
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(OnboardingViewModel)}.{nameof(PersistCalendarSyncSelectionAsync)}");
        }
    }

    private void Open(OnboardingStep step, bool revisit)
    {
        IsOpen = true;
        IsRevisit = revisit;
        _isPlantOnStage = false;
        _hasVisitedTimer = false;
        ScenePhase = OnboardingScenePhase.Question;
        Step = step;
    }

    private void StartScene()
    {
        _isPlantOnStage = true;
        OnPropertyChanged(nameof(IsPlantOnStage));
        _hasVisitedTimer = false;
        ScenePhase = OnboardingScenePhase.Question;
        Step = OnboardingStep.Scene;
    }

    private void ReturnToSceneEnd()
    {
        _hasVisitedTimer = false;
        ScenePhase = OnboardingScenePhase.ScopeDemo;
        Step = OnboardingStep.Scene;
    }

    private void AdvanceScene()
    {
        switch (ScenePhase)
        {
            case OnboardingScenePhase.TimerDemo:
                ScenePhase = OnboardingScenePhase.Tip;
                break;
            case OnboardingScenePhase.Tip when _hasVisitedTimer:
                ScenePhase = OnboardingScenePhase.Returning;
                break;
            case OnboardingScenePhase.Tip:
                ScenePhase = OnboardingScenePhase.DayIntro;
                break;
            case OnboardingScenePhase.DayIntro:
                ScenePhase = OnboardingScenePhase.DayDemo;
                break;
            case OnboardingScenePhase.ScopeDemo:
                FinishScene();
                break;
        }
    }

    private async Task CompleteAsync()
    {
        try
        {
            await _onboardingSettingsService.SetOnboardingStateAsync(OnboardingState.Completed);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(OnboardingViewModel)}.{nameof(CompleteAsync)}");
        }

        if (_environmentInfo.AppVersionNumber is { } currentVersion)
        {
            try
            {
                await _changelogSettingsService.SetLastSeenChangelogVersionAsync(currentVersion);
            }
            catch (Exception ex)
            {
                await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(OnboardingViewModel)}.{nameof(CompleteAsync)}");
            }
        }

        IsOpen = false;
        IsRevisit = false;
        _isPlantOnStage = false;
    }

    private async Task UpdateRewardingAsync(Func<AppRewardingSettings, AppRewardingSettings> change, string propertyName)
    {
        try
        {
            await _rewardingSettingsService.SetRewardingAsync(change(_rewardingSettingsService.Rewarding));
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(OnboardingViewModel)}.{nameof(UpdateRewardingAsync)}");
        }

        OnPropertyChanged(propertyName);
    }

    private void OnRewardingChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(Language));
        OnPropertyChanged(nameof(Theme));
        OnPropertyChanged(nameof(ColorMode));
        OnPropertyChanged(nameof(MicroFeedbackEnabled));
        OnPropertyChanged(nameof(ReviewsEnabled));
        OnPropertyChanged(nameof(CompanionEnabled));
        OnPropertyChanged(nameof(BalanceGoal));
        OnPropertyChanged(nameof(TimerAlertSoundEnabled));
        OnPropertyChanged(nameof(TimerAlertVibrationEnabled));
        OnPropertyChanged(nameof(TimerAlertAdaptToRingerModeEnabled));
    }
}
