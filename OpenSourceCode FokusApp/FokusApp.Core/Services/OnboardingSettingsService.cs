using FokusApp.Core.Enums;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class OnboardingSettingsService : IOnboardingSettingsService
{
    private readonly ISettingStore _settingStore;

    public OnboardingSettingsService(ISettingStore settingStore)
    {
        _settingStore = settingStore;
    }

    public OnboardingState OnboardingState => _settingStore.Read(setting => setting is null
        ? OnboardingState.Completed
        : NormalizeOnboardingState(setting.OnboardingState));

    public DateTime? CompanionHiddenDay => _settingStore.Read(setting => setting?.CompanionHiddenDay);

    public async Task SetOnboardingStateAsync(OnboardingState state)
    {
        await _settingStore.UpdateAsync(current =>
        {
            if (current.OnboardingState == (int)state)
            {
                return null;
            }

            var updated = SettingSnapshot.Copy(current);
            updated.OnboardingState = (int)state;
            return updated;
        }).ConfigureAwait(false);
    }

    public async Task SetCompanionHiddenDayAsync(DateTime? day)
    {
        var normalized = day?.Date;
        await _settingStore.UpdateAsync(current =>
        {
            if (current.CompanionHiddenDay == normalized)
            {
                return null;
            }

            var updated = SettingSnapshot.Copy(current);
            updated.CompanionHiddenDay = normalized;
            return updated;
        }).ConfigureAwait(false);
    }

    private static OnboardingState NormalizeOnboardingState(int state) =>
        Enum.IsDefined((OnboardingState)state) ? (OnboardingState)state : OnboardingState.Completed;
}
