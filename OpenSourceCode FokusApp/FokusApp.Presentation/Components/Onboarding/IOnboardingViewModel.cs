using System.ComponentModel;
using FokusApp.Components.PopUps.CalendarImport;
using FokusApp.Core.Enums;

namespace FokusApp.Components.Onboarding;

public enum OnboardingStep
{

    Invite,

    Language,

    Welcome,

    Scene,

    Why,

    Theme,

    Features,

    Balance,

    CalendarImport,

    TimerNotification
}

public enum OnboardingScenePhase
{

    Question,

    TimerDemo,

    Tip,

    Returning,

    DayIntro,

    DayDemo,

    ScopeDemo
}

public interface IOnboardingViewModel : INotifyPropertyChanged
{
    bool IsOpen { get; }

    OnboardingStep Step { get; }

    bool IsSceneActive { get; }

    OnboardingScenePhase ScenePhase { get; }

    bool IsPlantOnStage { get; }

    int PageNumber { get; }

    int PageCount { get; }

    bool CanGoBack { get; }

    bool IsLastStep { get; }

    bool IsRevisit { get; }

    IReadOnlyList<AppLanguage> LanguageOptions { get; }

    AppLanguage Language { get; }

    IReadOnlyList<AppColorTheme> ThemeOptions { get; }

    AppColorTheme Theme { get; }

    IReadOnlyList<AppColorMode> ColorModeOptions { get; }

    AppColorMode ColorMode { get; }

    bool MicroFeedbackEnabled { get; }
    bool ReviewsEnabled { get; }
    bool CompanionEnabled { get; }

    IReadOnlyList<BalanceGoal> BalanceGoalOptions { get; }

    BalanceGoal BalanceGoal { get; }

    bool IsCalendarImportAvailable { get; }

    bool IsCalendarImportLoading { get; }

    IReadOnlyList<CalendarOptionModel> CalendarImportCalendars { get; }

    bool CanChangeCalendarImportScope { get; }

    bool IsCalendarImportWorkTask { get; }

    bool CalendarSyncEnabled { get; }

    bool TimerAlertSoundEnabled { get; }
    bool TimerAlertVibrationEnabled { get; }
    bool TimerAlertAdaptToRingerModeEnabled { get; }
    bool IsTimerAlertVibrationAvailable { get; }
    bool IsTimerAlertAdaptToRingerModeAvailable { get; }

    Task InitializeAsync();

    void Restart();

    void Next();

    void Back();

    void AnswerPomodoroQuestion(bool knowsPrinciple);

    void CompleteScenePhase();

    void FinishScene();

    Task FinishAsync();

    Task DeclineInviteAsync();

    void AcceptInvite();

    Task SelectLanguageAsync(AppLanguage language);

    Task SetThemeAsync(AppColorTheme theme);
    Task SetColorModeAsync(AppColorMode mode);
    Task SetMicroFeedbackEnabledAsync(bool enabled);
    Task SetReviewsEnabledAsync(bool enabled);
    Task SetCompanionEnabledAsync(bool enabled);
    Task SetBalanceGoalAsync(BalanceGoal goal);

    void ToggleCalendarImportCalendar(string calendarId);
    void SetCalendarImportIsWorkTask(bool isWorkTask);
    Task SetCalendarSyncEnabledAsync(bool enabled);

    Task ContinueFromCalendarImportAsync();

    Task SetTimerAlertSoundEnabledAsync(bool enabled);
    Task SetTimerAlertVibrationEnabledAsync(bool enabled);
    Task SetTimerAlertAdaptToRingerModeEnabledAsync(bool enabled);
}
