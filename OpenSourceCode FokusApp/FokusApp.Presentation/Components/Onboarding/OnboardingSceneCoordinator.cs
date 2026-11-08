using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;

namespace FokusApp.Components.Onboarding;

public sealed class OnboardingSceneCoordinator
{
    private static readonly string[] DayPageRoutes = ["", "dein-tag", "calender"];

    private readonly IOnboardingViewModel _viewModel;
    private readonly IOnboardingStageEffects _effects;
    private readonly IAppErrorCaptureService _errorCapture;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private OnboardingScenePhase? _presentedPhase;
    private bool _isEntered;
    private bool _isLocked;
    private bool _isOnTimerPage;
    private bool _isReducedMotion;
    private bool _hasPlayedScope;

    public OnboardingSceneCoordinator(
        IOnboardingViewModel viewModel,
        IOnboardingStageEffects effects,
        IAppErrorCaptureService errorCapture)
    {
        _viewModel = viewModel;
        _effects = effects;
        _errorCapture = errorCapture;
    }

    public SceneBubble Bubble { get; private set; }

    public bool IsAwake { get; private set; }

    public bool IsBroken { get; private set; }

    public bool IsNextVisible { get; private set; }

    public bool IsNextDelayed { get; private set; }

    public bool IsRingVisible { get; private set; }

    public bool HasHeadphones { get; set; }

    public async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await _gate.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested && await ReconcileOnceAsync())
            {
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await RecoverAsync(ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<bool> ReconcileOnceAsync()
    {
        if (!_viewModel.IsOpen || !_viewModel.IsPlantOnStage)
        {
            IsBroken = false;

            if (_isEntered)
            {
                await TearDownAsync();
            }

            return false;
        }

        if (IsBroken)
        {
            return false;
        }

        if (!_isEntered)
        {
            await EnterAsync();
            return true;
        }

        if (!_viewModel.IsSceneActive)
        {
            if (_isLocked || Bubble != SceneBubble.None || IsNextVisible || IsRingVisible)
            {
                await LeaveSceneAsync();
                return true;
            }

            return false;
        }

        var phase = _viewModel.ScenePhase;
        if (_presentedPhase == phase)
        {
            return false;
        }

        _presentedPhase = phase;
        await SetLockedAsync(true);

        switch (phase)
        {
            case OnboardingScenePhase.Question:
                await ShowBubbleAsync(SceneBubble.Question);
                await _effects.FocusYesButtonAsync();
                break;
            case OnboardingScenePhase.TimerDemo:
                await PlayTimerDemoAsync();
                break;
            case OnboardingScenePhase.Tip:
                await ShowTipAsync();
                break;
            case OnboardingScenePhase.Returning:
                await PlayReturnAsync();
                break;
            case OnboardingScenePhase.DayIntro:
                await ShowDayIntroAsync();
                break;
            case OnboardingScenePhase.DayDemo:
                await PlayDayDemoAsync();
                break;
            case OnboardingScenePhase.ScopeDemo:
                await PlayScopeDemoAsync();
                break;
        }

        return true;
    }

    private async Task EnterAsync()
    {
        _isEntered = true;
        _presentedPhase = null;
        _isReducedMotion = await _effects.IsReducedMotionAsync();

        await SetLockedAsync(true);
        await ShowDayPageAsync();
        await _effects.RenderAsync();

        await _effects.AnchorPlantAtHeaderAsync();
        await PauseAsync(500);
        await _effects.RestartPlantAnimationAsync("shake");
        await PauseAsync(300);
        IsAwake = true;
        await _effects.RenderAsync();
        await PauseAsync(500);

        await _effects.MovePlantAsync("floor", 360, 1.3, 1100, "cubic-bezier(0.5, 0, 0.9, 0.55)");
        await LandAsync();
    }

    private async Task PlayTimerDemoAsync()
    {
        await ShowBubbleAsync(SceneBubble.None);

        await FlyToMenuAndNavigateAsync("/focus-timer", "[data-onboarding=\"timer-clock\"]");
        _isOnTimerPage = true;
        await DropToFloorAsync();

        IsRingVisible = true;
        Bubble = SceneBubble.Explain;
        IsNextVisible = true;
        IsNextDelayed = !_isReducedMotion;
        await _effects.RenderAsync();

        await _effects.ShowTimerRingDemoAsync();
    }

    private async Task ShowTipAsync()
    {
        IsNextVisible = false;
        IsRingVisible = false;
        await ShowBubbleAsync(SceneBubble.None);
        await _effects.ReleaseRingAsync();

        if (_isOnTimerPage)
        {
            await _effects.SetBlurAsync(true);
            await PauseAsync(400);
        }

        Bubble = SceneBubble.Tip;
        IsNextVisible = true;
        IsNextDelayed = false;
        await _effects.RenderAsync();
        await _effects.FocusNextButtonAsync();
    }

    private async Task PlayReturnAsync()
    {
        IsNextVisible = false;
        await ShowBubbleAsync(SceneBubble.None);
        await _effects.SetBlurAsync(false);

        await FlyToMenuAndNavigateAsync("/", "[data-onboarding=\"your-day-list\"]");
        _isOnTimerPage = false;
        await DropToFloorAsync();

        _viewModel.CompleteScenePhase();
    }

    private async Task ShowDayIntroAsync()
    {
        Bubble = SceneBubble.DayIntro;
        IsNextVisible = true;
        IsNextDelayed = false;
        await _effects.RenderAsync();
        await _effects.FocusNextButtonAsync();
    }

    private async Task PlayDayDemoAsync()
    {
        IsNextVisible = false;
        await ShowBubbleAsync(SceneBubble.None);

        if (_isReducedMotion)
        {
            await _effects.ShowDemoResultAsync();
        }
        else
        {
            await _effects.PlayDemoAsync();
        }

        _viewModel.CompleteScenePhase();
    }

    private async Task PlayScopeDemoAsync()
    {
        IsNextVisible = false;
        await ShowBubbleAsync(SceneBubble.Scope);

        if (_hasPlayedScope || _isReducedMotion)
        {
            await _effects.ShowDemoResultAsync();
        }
        else
        {
            await _effects.ShowDemoScopeAsync();
            await PauseAsync(900);
            await _effects.MovePlantAsync("scope", 0, 0.9, 750, "cubic-bezier(0.2, 0.7, 0.3, 1)");
            await _effects.RestartPlantAnimationAsync("squash");
            await _effects.SetDemoPrivateAsync(true);
            await PauseAsync(2000);
            await _effects.RestartPlantAnimationAsync("squash");
            await _effects.SetDemoPrivateAsync(false);
            await PauseAsync(300);
            await _effects.MovePlantAsync("floor", 0, 1.3, 800, "cubic-bezier(0.5, 0, 0.9, 0.55)");
            await LandAsync();
        }

        _hasPlayedScope = true;
        IsNextVisible = true;
        IsNextDelayed = false;
        await _effects.RenderAsync();
        await _effects.FocusNextButtonAsync();
    }

    private async Task LeaveSceneAsync()
    {
        _presentedPhase = null;
        Bubble = SceneBubble.None;
        IsNextVisible = false;
        IsRingVisible = false;
        _isOnTimerPage = false;
        HasHeadphones = false;

        await _effects.ReleaseRingAsync();
        await _effects.SetBlurAsync(false);
        await _effects.HideDemoAsync();

        await SetLockedAsync(false);
        await _effects.RenderAsync();
    }

    private async Task TearDownAsync()
    {
        await _effects.TeardownAsync();

        _isEntered = false;
        IsAwake = false;
        _isLocked = false;
        IsNextVisible = false;
        IsRingVisible = false;
        _isOnTimerPage = false;
        HasHeadphones = false;
        _hasPlayedScope = false;
        Bubble = SceneBubble.None;
        _presentedPhase = null;
        await _effects.RenderAsync();
    }

    private async Task RecoverAsync(Exception exception)
    {
        IsBroken = true;
        _isEntered = false;
        _isLocked = false;
        IsNextVisible = false;
        IsRingVisible = false;
        _isOnTimerPage = false;
        HasHeadphones = false;
        _hasPlayedScope = false;
        Bubble = SceneBubble.None;

        await _effects.TryTeardownAsync();

        await _errorCapture.CaptureUiErrorAsync(exception, $"{nameof(OnboardingSceneCoordinator)}.{nameof(RecoverAsync)}");

        if (_viewModel.IsSceneActive)
        {
            _viewModel.FinishScene();
        }

        await _effects.StateHasChangedAsync();
    }

    private async Task FlyToMenuAndNavigateAsync(string route, string readySelector)
    {
        await _effects.MovePlantAsync("menu", 12, 0.8, 1300, "ease-in-out");
        await _effects.PressMenuAsync(true);
        await PauseAsync(350);

        _effects.NavigateTo(route);
        await _effects.WaitForSelectorAsync(readySelector, 2000);

        await PauseAsync(450);
        await _effects.PressMenuAsync(false);
    }

    private async Task DropToFloorAsync()
    {
        await _effects.MovePlantAsync("floor", 0, 1.3, 900, "cubic-bezier(0.5, 0, 0.9, 0.55)");
        await LandAsync();
    }

    private async Task LandAsync()
    {
        await _effects.DockPlantAtFloorAsync();
        await _effects.RestartPlantAnimationAsync("squash");
        await PauseAsync(450);
    }

    private async Task ShowDayPageAsync()
    {
        var path = _effects.GetRelativePath();
        if (!DayPageRoutes.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            _effects.NavigateTo("/");
            await PauseAsync(400);
        }
    }

    private async Task SetLockedAsync(bool locked)
    {
        if (_isLocked == locked)
        {
            return;
        }

        _isLocked = locked;
        await _effects.LockAppAsync(locked);
    }

    private async Task ShowBubbleAsync(SceneBubble bubble)
    {
        Bubble = bubble;
        await _effects.RenderAsync();
    }

    private Task PauseAsync(int milliseconds) =>
        _isReducedMotion ? Task.CompletedTask : _effects.DelayAsync(milliseconds);
}
