namespace FokusApp.Components.Onboarding;

public interface IOnboardingStageEffects
{
    Task<bool> IsReducedMotionAsync();

    string GetRelativePath();

    void NavigateTo(string route);

    Task<bool> WaitForSelectorAsync(string selector, int timeoutMilliseconds);

    Task AnchorPlantAtHeaderAsync();

    Task MovePlantAsync(string kind, int rotateDegrees, double scale, int durationMilliseconds, string easing);

    Task DockPlantAtFloorAsync();

    Task RestartPlantAnimationAsync(string className);

    Task PressMenuAsync(bool pressed);

    Task LockAppAsync(bool locked);

    Task SetBlurAsync(bool blurred);

    Task ShowTimerRingDemoAsync();

    Task ReleaseRingAsync();

    Task TeardownAsync();

    Task TryTeardownAsync();

    Task FocusYesButtonAsync();

    Task FocusNextButtonAsync();

    Task PlayDemoAsync();

    Task ShowDemoResultAsync();

    Task ShowDemoScopeAsync();

    Task SetDemoPrivateAsync(bool isPrivate);

    Task HideDemoAsync();

    Task DelayAsync(int milliseconds);

    Task RenderAsync();

    Task StateHasChangedAsync();
}
