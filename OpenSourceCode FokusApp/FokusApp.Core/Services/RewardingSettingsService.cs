using FokusApp.Core.DataBase.Entities;
using FokusApp.Core.Enums;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Core.Services;

public sealed class RewardingSettingsService : IRewardingSettingsService
{
    public event EventHandler? RewardingChanged;

    private readonly ISettingStore _settingStore;

    public RewardingSettingsService(ISettingStore settingStore)
    {
        _settingStore = settingStore;
    }

    public AppRewardingSettings Rewarding => _settingStore.Read(ToRewarding);

    public async Task SetRewardingAsync(AppRewardingSettings rewarding)
    {
        ArgumentNullException.ThrowIfNull(rewarding);

        var normalized = rewarding with
        {
            BalanceGoal = NormalizeBalanceGoal((int)rewarding.BalanceGoal),
            CustomForYou = AppRewardingSettings.ClampCustomPart(rewarding.CustomForYou),
            CustomToDo = AppRewardingSettings.ClampCustomPart(rewarding.CustomToDo)
        };

        var changed = await _settingStore.UpdateAsync(current =>
        {
            if (ToRewarding(current) == normalized)
            {
                return null;
            }

            var updated = SettingSnapshot.Copy(current);
            updated.MicroFeedbackEnabled = normalized.MicroFeedbackEnabled;
            updated.ReviewsEnabled = normalized.ReviewsEnabled;
            updated.CompanionEnabled = normalized.CompanionEnabled;
            updated.BalanceGoal = (int)normalized.BalanceGoal;
            updated.CustomForYou = normalized.CustomForYou;
            updated.CustomToDo = normalized.CustomToDo;
            return updated;
        }).ConfigureAwait(false);

        if (changed)
        {
            RewardingChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static AppRewardingSettings ToRewarding(SettingEntity? setting)
    {
        if (setting is null)
        {
            return AppRewardingSettings.Default;
        }

        var (goal, forYou, toDo) = AppRewardingSettings.ReadStoredGoal(
            setting.BalanceGoal,
            setting.CustomForYou,
            setting.CustomToDo);

        return new AppRewardingSettings(
            setting.MicroFeedbackEnabled,
            setting.ReviewsEnabled,
            setting.CompanionEnabled,
            goal,
            forYou,
            toDo);
    }

    private static BalanceGoal NormalizeBalanceGoal(int goal) => Enum.IsDefined((BalanceGoal)goal)
        ? (BalanceGoal)goal
        : BalanceGoal.OneToOne;
}
