using Bunit;
using Bunit.TestDoubles;
using FokusApp.Components.Onboarding;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace FocusAppTests.Components;

public class OnboardingDemoTests : ComponentTestContext
{
    private readonly BunitJSModuleInterop _module;

    public OnboardingDemoTests()
    {
        _module = JSInterop.SetupModule("./js/onboardingStage.js");
        _module.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public async Task PlayDayDemoAsync_Completes_LeavesTheTasksWithTheTaxesDone()
    {
        var cut = RenderWithoutPauses();

        await cut.InvokeAsync(() => cut.Instance.PlayDayDemoAsync());

        var items = cut.FindAll(".todo-book-task-item");
        Assert.Equal(2, items.Count);
        Assert.Contains(Text("OnboardingDemoWalk"), items[0].TextContent);
        Assert.Contains(Text("OnboardingDemoTaxes"), items[1].TextContent);
        Assert.Contains("is-done", items[1].ClassName);
        Assert.DoesNotContain("is-done", items[0].ClassName);
        Assert.Empty(cut.FindAll(".demo-editor"));
        Assert.Empty(cut.FindAll(".demo-timer-panel"));
    }

    [Fact]
    public async Task PlayDayDemoAsync_Completes_GoesToTheTimerPageAndBackToTheDayPage()
    {
        var cut = RenderWithoutPauses();
        var navigation = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();

        await cut.InvokeAsync(() => cut.Instance.PlayDayDemoAsync());

        Assert.Contains(navigation.History, entry => entry.Uri.EndsWith("/focus-timer"));
        Assert.Equal("/", new Uri(navigation.Uri).AbsolutePath);
    }

    [Fact]
    public async Task PlayDayDemoAsync_Completes_TapsTypesAndPressesWithTheLeaf()
    {
        var cut = RenderWithoutPauses();

        await cut.InvokeAsync(() => cut.Instance.PlayDayDemoAsync());

        var clicked = _module.Invocations
            .Where(call => call.Identifier == "pointerClick")
            .Select(call => (string)call.Arguments[1]!)
            .ToList();
        Assert.Contains(".stage-demo .demo-note", clicked);
        Assert.Contains(".stage-demo .demo-focus [data-onboarding=\"timer-display\"]", clicked);
        Assert.Contains(".stage-demo .demo-break [data-onboarding=\"timer-increase\"]", clicked);
        Assert.Contains(".stage-demo .demo-save", clicked);
        Assert.Contains(".stage-demo [data-onboarding=\"todo-action-start\"]", clicked);
        Assert.Contains(".stage-demo .demo-timer-start", clicked);
        Assert.Equal(2, clicked.Count(selector => selector == ".stage-demo .demo-note"));
        Assert.Equal(2, clicked.Count(selector => selector == ".stage-demo .demo-save"));
        Assert.Single(clicked, selector => selector == ".stage-demo .demo-focus [data-onboarding=\"timer-display\"]");
        Assert.Equal(8, clicked.Count(selector => selector == ".stage-demo .demo-focus [data-onboarding=\"timer-increase\"]"));
        Assert.Single(clicked, selector => selector == ".stage-demo .demo-break [data-onboarding=\"timer-increase\"]");
    }

    [Fact]
    public async Task PlayDayDemoAsync_Completes_NeverGivesBackARealPageThatIsBeingLeft()
    {
        var cut = RenderWithoutPauses();

        await cut.InvokeAsync(() => cut.Instance.PlayDayDemoAsync());

        var modes = _module.Invocations
            .Where(call => call.Identifier == "setDemoMode")
            .Select(call => $"{call.Arguments[0]}:{call.Arguments[1]}")
            .ToList();
        Assert.Contains("lists:True", modes);
        Assert.Contains("timer:True", modes);
        Assert.DoesNotContain("lists:False", modes);
        Assert.DoesNotContain("timer:False", modes);
    }

    [Fact]
    public async Task PlayDayDemoAsync_Completes_HidesTheRealTimerPageBeforeItOpensAndTheRealDayPageBeforeItReturns()
    {
        var cut = RenderWithoutPauses();
        var navigation = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();

        await cut.InvokeAsync(() => cut.Instance.PlayDayDemoAsync());

        var timerHidden = _module.Invocations.ToList()
            .FindIndex(call => call.Identifier == "setDemoMode" && (string)call.Arguments[0]! == "timer");
        var firstWait = _module.Invocations.ToList().FindIndex(call => call.Identifier == "waitFor");
        Assert.InRange(timerHidden, 0, firstWait - 1);

        var waits = _module.Invocations.Where(call => call.Identifier == "waitFor").Select(call => (string)call.Arguments[0]!).ToList();
        Assert.Equal(["[data-onboarding=\"timer-clock\"]", "[data-onboarding=\"your-day-list\"]"], waits);
        Assert.Contains(navigation.History, entry => entry.Uri.EndsWith("/focus-timer"));
    }

    [Fact]
    public async Task PlayDayDemoAsync_Ringtone_ShowsTheHeadphonesAndTakesThemOffAgain()
    {
        var changes = new List<bool>();
        var cut = RenderWithoutPauses(changes.Add);

        await cut.InvokeAsync(() => cut.Instance.PlayDayDemoAsync());

        Assert.Equal([true, false], changes);
    }

    [Fact]
    public async Task ShowDayResultAsync_Anytime_ShowsTheWalkAndTheDoneTaxes()
    {
        var cut = RenderWithoutPauses();

        await cut.InvokeAsync(() => cut.Instance.ShowDayResultAsync());

        var items = cut.FindAll(".todo-book-task-item");
        Assert.Equal(2, items.Count);
        Assert.Single(cut.FindAll(".todo-book-task-item.is-done"));
        Assert.Contains(_module.Invocations, call => call.Identifier == "placeOver");
    }

    [Fact]
    public async Task SetPrivateAsync_PrivateAndBack_SwapsTheListsAndTheSwitch()
    {
        var cut = RenderWithoutPauses();
        await cut.InvokeAsync(() => cut.Instance.ShowDayResultAsync());
        await cut.InvokeAsync(() => cut.Instance.ShowScopeAsync());
        Assert.Single(cut.FindAll(".scope-toggle-thumb.is-work"));

        await cut.InvokeAsync(() => cut.Instance.SetPrivateAsync(true));

        Assert.Single(cut.FindAll(".scope-toggle-thumb.is-private"));
        var texts = cut.FindAll(".todo-book-task-note").Select(note => note.TextContent).ToList();
        Assert.Equal(
            [Text("OnboardingDemoDateNight"), Text("OnboardingDemoMeditate"), Text("OnboardingDemoCleanFlat")],
            texts);

        await cut.InvokeAsync(() => cut.Instance.SetPrivateAsync(false));

        Assert.Single(cut.FindAll(".scope-toggle-thumb.is-work"));
        texts = cut.FindAll(".todo-book-task-note").Select(note => note.TextContent).ToList();
        Assert.Equal([Text("OnboardingDemoWalk"), Text("OnboardingDemoTaxes")], texts);
        Assert.Single(cut.FindAll(".todo-book-task-item.is-done"));
    }

    [Fact]
    public async Task HideAllAsync_AfterTheDemo_RemovesEveryReplicaAndGivesTheRealElementsBack()
    {
        var cut = RenderWithoutPauses();
        await cut.InvokeAsync(() => cut.Instance.ShowDayResultAsync());
        await cut.InvokeAsync(() => cut.Instance.ShowScopeAsync());

        await cut.InvokeAsync(() => cut.Instance.HideAllAsync());

        Assert.Empty(cut.FindAll(".demo-lists"));
        Assert.Empty(cut.FindAll(".demo-scope"));
        Assert.Contains(_module.Invocations, call => call.Identifier == "resetDemo");
    }

    private IRenderedComponent<OnboardingDemo> RenderWithoutPauses(Action<bool>? onHeadphones = null)
    {
        return Render<OnboardingDemo>(parameters => parameters
            .Add(demo => demo.TimeScale, 0d)
            .Add(demo => demo.OnHeadphonesChanged, EventCallback.Factory.Create<bool>(this, onHeadphones ?? (_ => { }))));
    }
}
