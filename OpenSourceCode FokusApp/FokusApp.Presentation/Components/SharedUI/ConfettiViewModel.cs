using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Components.SharedUI;

public sealed class ConfettiViewModel : ObservableObject, IConfettiViewModel, IDisposable
{
    private static readonly TimeSpan VisibleDuration = TimeSpan.FromSeconds(2);

    private readonly ITaskCacheService _taskCacheService;
    private readonly IRewardingSettingsService _rewardingSettingsService;
    private readonly TimeProvider _timeProvider;
    private readonly Lock _burstLock = new();

    private ITimer? _hideTimer;

    public ConfettiViewModel(
        ITaskCacheService taskCacheService,
        IRewardingSettingsService rewardingSettingsService,
        TimeProvider timeProvider)
    {
        _taskCacheService = taskCacheService;
        _rewardingSettingsService = rewardingSettingsService;
        _timeProvider = timeProvider;

        _taskCacheService.TaskCompleted += OnTaskCompleted;
    }

    public bool IsVisible { get; private set; }

    public int BurstNumber { get; private set; }

    public void Dispose()
    {
        _taskCacheService.TaskCompleted -= OnTaskCompleted;

        lock (_burstLock)
        {
            _hideTimer?.Dispose();
            _hideTimer = null;
        }
    }

    public void ShowBurst()
    {
        if (!_rewardingSettingsService.Rewarding.MicroFeedbackEnabled)
        {
            return;
        }

        lock (_burstLock)
        {
            _hideTimer?.Dispose();
            BurstNumber++;
            IsVisible = true;
            _hideTimer = _timeProvider.CreateTimer(_ => Hide(), null, VisibleDuration, Timeout.InfiniteTimeSpan);
        }

        OnPropertyChanged(nameof(BurstNumber));
        OnPropertyChanged(nameof(IsVisible));
    }

    private void OnTaskCompleted(object? sender, TaskCompletedEventArgs e)
    {
        ShowBurst();
    }

    private void Hide()
    {
        lock (_burstLock)
        {
            if (!IsVisible)
            {
                return;
            }

            IsVisible = false;
        }

        OnPropertyChanged(nameof(IsVisible));
    }
}
