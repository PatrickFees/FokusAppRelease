using FokusApp.Core.Enums;

namespace FokusApp.Core.Services.Interfaces;

public interface IOnboardingSettingsService
{

    OnboardingState OnboardingState { get; }

    DateTime? CompanionHiddenDay { get; }

    Task SetOnboardingStateAsync(OnboardingState state);

    Task SetCompanionHiddenDayAsync(DateTime? day);
}
