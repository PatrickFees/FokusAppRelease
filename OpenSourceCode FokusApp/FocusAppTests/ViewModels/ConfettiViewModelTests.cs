using FokusApp.Components.SharedUI;
using FokusApp.Core.Models;
using FokusApp.Core.Services.Interfaces;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FocusAppTests.ViewModels;

public class ConfettiViewModelTests
{
    private readonly ITaskCacheService _taskCache = Substitute.For<ITaskCacheService>();
    private readonly IRewardingSettingsService _rewardingSettings = TestSettings.Create();
    private readonly FakeTimeProvider _clock = TestTasks.CreateClock();
    private readonly ConfettiViewModel _viewModel;

    public ConfettiViewModelTests()
    {
        _viewModel = new ConfettiViewModel(_taskCache, _rewardingSettings, _clock);
    }

    [Fact]
    public void Constructor_NoCompletion_ShowsNoConfetti()
    {
        Assert.False(_viewModel.IsVisible);
        Assert.Equal(0, _viewModel.BurstNumber);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TaskCompleted_ForYouOrToDo_ShowsConfettiTheSameWay(bool isPositive)
    {
        RaiseCompleted(isPositive);

        Assert.True(_viewModel.IsVisible);
        Assert.Equal(1, _viewModel.BurstNumber);
    }

    [Fact]
    public void TaskCompleted_Completion_NotifiesVisibilityAndBurst()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        RaiseCompleted(isPositive: false);

        Assert.Contains(nameof(IConfettiViewModel.IsVisible), changed);
        Assert.Contains(nameof(IConfettiViewModel.BurstNumber), changed);
    }

    [Fact]
    public void IsVisible_AfterTwoSeconds_Disappears()
    {
        RaiseCompleted(isPositive: true);

        _clock.Advance(TimeSpan.FromSeconds(1.9));
        Assert.True(_viewModel.IsVisible);
        _clock.Advance(TimeSpan.FromSeconds(0.2));

        Assert.False(_viewModel.IsVisible);
    }

    [Fact]
    public void IsVisible_Disappearing_NotifiesPropertyChanged()
    {
        RaiseCompleted(isPositive: true);
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        _clock.Advance(TimeSpan.FromSeconds(2.1));

        Assert.Contains(nameof(IConfettiViewModel.IsVisible), changed);
    }

    [Fact]
    public void TaskCompleted_SecondCompletionWhileVisible_StartsNewBurstAndExtendsTime()
    {
        RaiseCompleted(isPositive: true);
        _clock.Advance(TimeSpan.FromSeconds(1.5));

        RaiseCompleted(isPositive: false);
        _clock.Advance(TimeSpan.FromSeconds(1.5));

        Assert.Equal(2, _viewModel.BurstNumber);
        Assert.True(_viewModel.IsVisible);
        _clock.Advance(TimeSpan.FromSeconds(0.6));
        Assert.False(_viewModel.IsVisible);
    }

    [Fact]
    public void TaskCompleted_CompletionAfterConfettiEnded_ShowsConfettiAgain()
    {
        RaiseCompleted(isPositive: true);
        _clock.Advance(TimeSpan.FromSeconds(3));

        RaiseCompleted(isPositive: true);

        Assert.True(_viewModel.IsVisible);
        Assert.Equal(2, _viewModel.BurstNumber);
    }

    [Fact]
    public void TaskCompleted_MicroFeedbackOff_ShowsNoConfetti()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { MicroFeedbackEnabled = false });

        RaiseCompleted(isPositive: true);

        Assert.False(_viewModel.IsVisible);
        Assert.Equal(0, _viewModel.BurstNumber);
    }

    [Fact]
    public void TaskCompleted_MicroFeedbackSwitchedOffAndOnAgain_FollowsTheCurrentSetting()
    {
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default with { MicroFeedbackEnabled = false });
        RaiseCompleted(isPositive: false);
        _rewardingSettings.Rewarding.Returns(AppRewardingSettings.Default);

        RaiseCompleted(isPositive: false);

        Assert.True(_viewModel.IsVisible);
        Assert.Equal(1, _viewModel.BurstNumber);
    }

    [Fact]
    public void TaskListChanges_WithoutCompletion_ShowNoConfetti()
    {
        _taskCache.NormalTasksChanged += Raise.EventWith(EventArgs.Empty);
        _taskCache.TemplatesChanged += Raise.EventWith(EventArgs.Empty);
        _taskCache.DoneTasksChanged += Raise.EventWith(EventArgs.Empty);
        _taskCache.TasksLoaded += Raise.EventWith(EventArgs.Empty);

        Assert.False(_viewModel.IsVisible);
        Assert.Equal(0, _viewModel.BurstNumber);
    }

    [Fact]
    public void Dispose_AfterDispose_IgnoresCompletions()
    {
        _viewModel.Dispose();

        RaiseCompleted(isPositive: true);

        Assert.False(_viewModel.IsVisible);
    }

    [Fact]
    public void Dispose_WithoutAnyBurst_CompletesWithoutError()
    {
        var exception = Record.Exception(() => _viewModel.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_WhileConfettiIsVisible_StopsTheHideTimerAndIgnoresLaterCompletions()
    {
        RaiseCompleted(isPositive: true);

        _viewModel.Dispose();
        _clock.Advance(TimeSpan.FromSeconds(5));
        RaiseCompleted(isPositive: true);

        Assert.True(_viewModel.IsVisible);
        Assert.Equal(1, _viewModel.BurstNumber);
    }

    private void RaiseCompleted(bool isPositive)
    {
        _taskCache.TaskCompleted += Raise.EventWith(new TaskCompletedEventArgs(Guid.NewGuid(), isPositive));
    }
}
