using FokusApp.Core.Models;

namespace FokusApp.Core.Services.Interfaces;

public interface IRewardingSettingsService
{
    event EventHandler? RewardingChanged;

    AppRewardingSettings Rewarding { get; }

    Task SetRewardingAsync(AppRewardingSettings rewarding);
}
