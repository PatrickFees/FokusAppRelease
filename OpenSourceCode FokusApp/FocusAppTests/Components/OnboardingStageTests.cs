using Bunit;
using FokusApp.Components.Onboarding;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;

namespace FocusAppTests.Components;

public class OnboardingStageTests : ComponentTestContext
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly IOnboardingViewModel _viewModel = Substitute.For<IOnboardingViewModel>();
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly BunitJSModuleInterop _module;
    private readonly TaskCompletionSource _scenePhaseCompleted = new();
    private readonly TaskCompletionSource _sceneFinished = new();

    public OnboardingStageTests()
    {
        _viewModel.IsOpen.Returns(true);
        _viewModel.IsPlantOnStage.Returns(true);
        _viewModel.IsSceneActive.Returns(true);
        _viewModel.ScenePhase.Returns(OnboardingScenePhase.Question);
        _viewModel.When(viewModel => viewModel.CompleteScenePhase()).Do(_ => _scenePhaseCompleted.TrySetResult());
        _viewModel.When(viewModel => viewModel.FinishScene()).Do(_ => _sceneFinished.TrySetResult());
        Services.AddSingleton(_viewModel);
        Services.AddSingleton(_errorCapture);

        _module = JSInterop.SetupModule("./js/onboardingStage.js");
        _module.Mode = JSRuntimeMode.Loose;

        _module.Setup<bool>("isReducedMotion").SetResult(true);
    }

    private static async Task WaitForCallAsync(TaskCompletionSource source)
    {
        var completed = await Task.WhenAny(source.Task, Task.Delay(Timeout));
        if (completed != source.Task)
        {
            throw new TimeoutException("Der erwartete Aufruf blieb aus.");
        }
    }

    private static async Task WithinAsync(Action assertion)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (true)
        {
            try
            {
                assertion();
                return;
            }
            catch (Exception) when (DateTime.UtcNow < deadline)
            {
                await Task.Delay(20);
            }
        }
    }

    [Fact]
    public void Render_PlantNotOnStage_ShowsNothing()
    {
        _viewModel.IsPlantOnStage.Returns(false);

        var cut = Render<OnboardingStage>();

        Assert.Empty(cut.FindAll(".stage"));
    }

    [Fact]
    public void Render_Question_AsksWithYesAndNoAndLocksTheApp()
    {
        var cut = Render<OnboardingStage>();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".stage-bubble")), Timeout);
        Assert.Contains(Text("OnboardingSceneQuestion"), cut.Find(".stage-bubble").TextContent);
        Assert.Equal(Text("OnboardingSceneYes"), cut.Find(".stage-yes").TextContent.Trim());
        Assert.Equal(Text("OnboardingSceneNo"), cut.Find(".stage-no").TextContent.Trim());
        Assert.Single(cut.FindAll(".stage-blocker"));
        Assert.Single(cut.FindAll(".stage-plant"));
        Assert.Empty(cut.FindAll(".stage-next"));
        Assert.Contains(_module.Invocations, call => call.Identifier == "lockApp" && call.Arguments[0] is true);
    }

    [Fact]
    public async Task Click_YesAndNo_AnswerTheQuestion()
    {
        var cut = Render<OnboardingStage>();

        await WithinAsync(() => Assert.Contains(JSInterop.Invocations, call => call.Identifier.EndsWith("focus", StringComparison.OrdinalIgnoreCase)));

        cut.Find(".stage-yes").Click();
        cut.Find(".stage-no").Click();

        _viewModel.Received(1).AnswerPomodoroQuestion(true);
        _viewModel.Received(1).AnswerPomodoroQuestion(false);
    }

    [Fact]
    public async Task Render_TimerDemo_OpensTheTimerPageAndExplainsWithTheRing()
    {
        _viewModel.ScenePhase.Returns(OnboardingScenePhase.TimerDemo);

        var cut = Render<OnboardingStage>();

        await WithinAsync(() => Assert.Contains(_module.Invocations, call => call.Identifier == "startRingDemo"));
        Assert.Single(cut.FindAll(".stage-ring"));
        Assert.EndsWith("/focus-timer", Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Contains(Text("OnboardingSceneExplain"), cut.Find(".stage-bubble").TextContent);
        Assert.Equal(Text("OnboardingSceneMinutes"), cut.Find(".stage-ring-minutes").TextContent.Trim());

        cut.Find(".stage-next").Click();

        _viewModel.Received(1).Next();
    }

    [Fact]
    public void Render_Tip_ShowsTheExampleWithNext()
    {
        _viewModel.ScenePhase.Returns(OnboardingScenePhase.Tip);

        var cut = Render<OnboardingStage>();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".stage-next")), Timeout);
        var bubble = cut.Find(".stage-bubble").TextContent;
        Assert.Contains(Text("OnboardingSceneTip"), bubble);
        Assert.Contains(Text("OnboardingSceneTipExample"), bubble);
        Assert.Empty(cut.FindAll(".stage-ring"));
    }

    [Fact]
    public void Render_TipAfterTheTimerPage_BlursThePage()
    {
        _viewModel.ScenePhase.Returns(OnboardingScenePhase.TimerDemo);
        var cut = Render<OnboardingStage>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".stage-ring")), Timeout);

        _viewModel.ScenePhase.Returns(OnboardingScenePhase.Tip);
        _viewModel.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _viewModel, new System.ComponentModel.PropertyChangedEventArgs(nameof(IOnboardingViewModel.ScenePhase)));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".stage-ring")), Timeout);
        Assert.Contains(_module.Invocations, call => call.Identifier == "setBlur" && call.Arguments[0] is true);
        Assert.Contains(Text("OnboardingSceneTipExample"), cut.Find(".stage-bubble").TextContent);
    }

    [Fact]
    public async Task Render_Returning_GoesBackToTheDayPageAndMovesOnToTheDayIntro()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/focus-timer");
        _viewModel.ScenePhase.Returns(OnboardingScenePhase.Returning);

        Render<OnboardingStage>();

        await WaitForCallAsync(_scenePhaseCompleted);
        _viewModel.Received(1).CompleteScenePhase();
        var uri = new Uri(Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Equal("/", uri.AbsolutePath);
    }

    [Fact]
    public async Task Render_DayIntro_ExplainsTheDayPageAndWaitsForNext()
    {
        _viewModel.ScenePhase.Returns(OnboardingScenePhase.DayIntro);

        var cut = Render<OnboardingStage>();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".stage-next")), Timeout);
        await WithinAsync(() => Assert.Contains(JSInterop.Invocations, call => call.Identifier.EndsWith("focus", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains(Text("OnboardingSceneDayIntro"), cut.Find(".stage-bubble").TextContent);
        Assert.Empty(cut.FindAll(".demo-lists"));

        cut.Find(".stage-next").Click();

        _viewModel.Received(1).Next();
    }

    [Fact]
    public async Task Render_DayDemoWithReducedMotion_ShowsOnlyTheResultAndCompletesThePhase()
    {
        _viewModel.ScenePhase.Returns(OnboardingScenePhase.DayDemo);

        var cut = Render<OnboardingStage>();

        await WaitForCallAsync(_scenePhaseCompleted);
        _viewModel.Received(1).CompleteScenePhase();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".todo-book-task-item").Count), Timeout);
        Assert.Contains(Text("OnboardingDemoWalk"), cut.Markup);
        Assert.Single(cut.FindAll(".todo-book-task-item.is-done"));
        Assert.Empty(cut.FindAll(".stage-bubble"));
        Assert.Empty(cut.FindAll(".stage-next"));
    }

    [Fact]
    public async Task Render_ScopeDemoWithReducedMotion_ShowsTheLastBubbleWithNextAndTheResult()
    {
        _viewModel.ScenePhase.Returns(OnboardingScenePhase.ScopeDemo);

        var cut = Render<OnboardingStage>();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".stage-next")), Timeout);
        await WithinAsync(() => Assert.Contains(JSInterop.Invocations, call => call.Identifier.EndsWith("focus", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains(Text("OnboardingSceneScope"), cut.Find(".stage-bubble").TextContent);
        Assert.Equal(2, cut.FindAll(".todo-book-task-item").Count);
        Assert.Empty(cut.FindAll(".demo-scope"));

        cut.Find(".stage-next").Click();

        _viewModel.Received(1).Next();
    }

    [Fact]
    public void Render_AfterTheScene_TakesTheReplicasAway()
    {
        _viewModel.ScenePhase.Returns(OnboardingScenePhase.ScopeDemo);
        var cut = Render<OnboardingStage>();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".todo-book-task-item").Count), Timeout);

        _viewModel.IsSceneActive.Returns(false);
        _viewModel.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _viewModel, new System.ComponentModel.PropertyChangedEventArgs(nameof(IOnboardingViewModel.IsSceneActive)));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".todo-book-task-item")), Timeout);
        Assert.Single(cut.FindAll(".stage-plant"));
        Assert.Contains(_module.Invocations, call => call.Identifier == "resetDemo");
    }

    [Fact]
    public void Render_AfterTheScene_KeepsThePlantButUnlocksTheApp()
    {
        _viewModel.IsSceneActive.Returns(false);

        var cut = Render<OnboardingStage>();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".stage-plant")), Timeout);
        Assert.Empty(cut.FindAll(".stage-blocker"));
        Assert.Empty(cut.FindAll(".stage-bubble"));
        Assert.Empty(cut.FindAll(".stage-next"));
    }

    [Fact]
    public void Render_SceneStartedOnAnotherPage_TakesTheUserToTheDayPage()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/einstellungen");

        var cut = Render<OnboardingStage>();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".stage-bubble")), Timeout);
        Assert.Equal("/", new Uri(Services.GetRequiredService<NavigationManager>().Uri).AbsolutePath);
    }

    [Fact]
    public void BackKey_WhileTheSceneRuns_IsConsumed()
    {
        var handlers = new List<Func<bool>>();
        var backPress = Substitute.For<IUiBackPressService>();
        backPress.RegisterHandler(Arg.Do<Func<bool>>(handlers.Add));
        Services.AddSingleton(backPress);

        Render<OnboardingStage>();

        Assert.Single(handlers);
        Assert.True(handlers[0]());
        _viewModel.IsSceneActive.Returns(false);
        Assert.False(handlers[0]());
    }

    [Fact]
    public async Task Render_AnimationFails_GivesTheAppBackAndContinuesTheIntroduction()
    {
        _module.SetupVoid("anchorAtHeader", _ => true).SetException(new JSException("kaputt"));

        var cut = Render<OnboardingStage>();

        await WaitForCallAsync(_sceneFinished);
        _viewModel.Received(1).FinishScene();
        await _errorCapture.Received(1).CaptureAsync(
            Arg.Is<Exception>(exception => exception is JSException),
            Arg.Any<AppErrorCaptureOptions?>());
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".stage")), Timeout);
        Assert.Contains(_module.Invocations, call => call.Identifier == "teardown");
    }

    [Fact]
    public void Render_ClosedAfterwards_GivesTheAppBack()
    {
        var cut = Render<OnboardingStage>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".stage-bubble")), Timeout);

        _viewModel.IsOpen.Returns(false);
        _viewModel.IsPlantOnStage.Returns(false);
        _viewModel.PropertyChanged += Raise.Event<System.ComponentModel.PropertyChangedEventHandler>(
            _viewModel, new System.ComponentModel.PropertyChangedEventArgs(nameof(IOnboardingViewModel.IsOpen)));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".stage")), Timeout);
        Assert.Contains(_module.Invocations, call => call.Identifier == "teardown");
    }
}
