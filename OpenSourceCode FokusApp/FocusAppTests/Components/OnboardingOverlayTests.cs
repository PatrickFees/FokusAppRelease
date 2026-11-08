using Bunit;
using FokusApp.Components.Onboarding;
using FokusApp.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FocusAppTests.Components;

public class OnboardingOverlayTests : ComponentTestContext
{
    private readonly IOnboardingViewModel _viewModel = Substitute.For<IOnboardingViewModel>();

    public OnboardingOverlayTests()
    {
        _viewModel.IsOpen.Returns(true);
        _viewModel.Step.Returns(OnboardingStep.Language);
        _viewModel.PageNumber.Returns(1);
        _viewModel.PageCount.Returns(9);
        _viewModel.BalanceGoalOptions.Returns([BalanceGoal.OneToOne, BalanceGoal.TwoToThree, BalanceGoal.ThreeToTwo]);
        Services.AddSingleton(_viewModel);
    }

    [Fact]
    public void Render_InitializesViewModel()
    {
        Render<OnboardingOverlay>();

        _viewModel.Received(1).InitializeAsync();
    }

    [Fact]
    public void Render_Closed_ShowsNothing()
    {
        _viewModel.IsOpen.Returns(false);

        var cut = Render<OnboardingOverlay>();

        Assert.Empty(cut.FindAll(".onboarding-dialog"));
        Assert.Empty(cut.FindAll(".onboarding-backdrop"));
    }

    [Fact]
    public void Render_Dialog_IsAnAccessibleModal()
    {
        var cut = Render<OnboardingOverlay>();

        var dialog = cut.Find(".onboarding-dialog");
        Assert.Equal("dialog", dialog.GetAttribute("role"));
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
    }

    [Fact]
    public void Render_LanguagePage_OffersOneButtonPerLanguageWithoutNextOrBack()
    {
        _viewModel.LanguageOptions.Returns([AppLanguage.German, AppLanguage.English]);
        _viewModel.Language.Returns(AppLanguage.English);

        var cut = Render<OnboardingOverlay>();

        var languages = cut.FindAll(".onboarding-language");
        Assert.Equal(2, languages.Count);
        Assert.Equal("Deutsch", languages[0].TextContent.Trim());
        Assert.Equal("English", languages[1].TextContent.Trim());
        Assert.Equal("true", languages[1].GetAttribute("aria-checked"));
        Assert.Equal(Text("OnboardingProgress", 1, 9), cut.Find(".onboarding-progress").TextContent.Trim());
        Assert.Empty(cut.FindAll(".onboarding-back"));
        Assert.Empty(cut.FindAll(".onboarding-next"));
        Assert.Empty(cut.FindAll(".onboarding-skip"));
    }

    [Fact]
    public void Click_Language_SelectsIt()
    {
        _viewModel.LanguageOptions.Returns([AppLanguage.German, AppLanguage.English]);
        var cut = Render<OnboardingOverlay>();

        cut.FindAll(".onboarding-language")[1].Click();

        _viewModel.Received(1).SelectLanguageAsync(AppLanguage.English);
    }

    [Fact]
    public void Render_WelcomePage_ShowsTheGreetingWithNextAndBack()
    {
        _viewModel.Step.Returns(OnboardingStep.Welcome);
        _viewModel.CanGoBack.Returns(true);

        var cut = Render<OnboardingOverlay>();

        Assert.Contains(Text("OnboardingWelcomeText"), cut.Markup);
        Assert.Contains(Text("OnboardingWelcomeHint"), cut.Markup);
        Assert.Single(cut.FindAll(".onboarding-back"));
        Assert.Single(cut.FindAll(".onboarding-next"));
        Assert.Empty(cut.FindAll(".onboarding-skip"));
    }

    [Fact]
    public void Render_WhyPage_ListsTheReasonsAndPsychologicalFoundations()
    {
        _viewModel.Step.Returns(OnboardingStep.Why);
        _viewModel.CanGoBack.Returns(true);

        var cut = Render<OnboardingOverlay>();

        Assert.Equal(Text("OnboardingWhyTitle"), cut.Find("h2").TextContent.Trim());
        Assert.Equal(6, cut.FindAll("li").Count);
        Assert.Contains(Text("OnboardingWhyReward"), cut.Markup);
        Assert.Single(cut.FindAll(".onboarding-back"));
        Assert.Single(cut.FindAll(".onboarding-next"));
    }

    [Fact]
    public void Render_PomodoroScene_ShowsNoDialogAndNoBackdrop()
    {
        _viewModel.Step.Returns(OnboardingStep.Scene);
        _viewModel.IsSceneActive.Returns(true);

        var cut = Render<OnboardingOverlay>();

        Assert.Empty(cut.FindAll(".onboarding-dialog"));
        Assert.Empty(cut.FindAll(".onboarding-backdrop"));
    }

    [Fact]
    public void Render_ThemePage_OffersTheThemesWithTheSelectedOneMarked()
    {
        _viewModel.Step.Returns(OnboardingStep.Theme);
        _viewModel.CanGoBack.Returns(true);
        _viewModel.ThemeOptions.Returns([AppColorTheme.Blue, AppColorTheme.Green]);
        _viewModel.Theme.Returns(AppColorTheme.Green);

        var cut = Render<OnboardingOverlay>();

        var themes = cut.FindAll(".onboarding-theme[data-theme-option]");
        Assert.Equal(2, themes.Count);
        Assert.Equal("false", themes[0].GetAttribute("aria-checked"));
        Assert.Equal("true", themes[1].GetAttribute("aria-checked"));
        Assert.Single(cut.FindAll(".onboarding-next"));
    }

    [Fact]
    public void Click_Theme_SelectsItAtOnce()
    {
        _viewModel.Step.Returns(OnboardingStep.Theme);
        _viewModel.ThemeOptions.Returns([AppColorTheme.Blue, AppColorTheme.Green]);
        var cut = Render<OnboardingOverlay>();

        cut.FindAll(".onboarding-theme[data-theme-option]")[1].Click();

        _viewModel.Received(1).SetThemeAsync(AppColorTheme.Green);
    }

    [Fact]
    public void Render_ThemePage_OffersLightAndDarkWithTheSelectedOneMarked()
    {
        _viewModel.Step.Returns(OnboardingStep.Theme);
        _viewModel.ThemeOptions.Returns([AppColorTheme.Blue, AppColorTheme.Green]);
        _viewModel.ColorModeOptions.Returns([AppColorMode.Light, AppColorMode.Dark]);
        _viewModel.ColorMode.Returns(AppColorMode.Dark);

        var cut = Render<OnboardingOverlay>();

        var modes = cut.FindAll(".onboarding-theme[data-color-mode-option]");
        Assert.Equal(2, modes.Count);
        Assert.Equal("false", modes[0].GetAttribute("aria-checked"));
        Assert.Equal("true", modes[1].GetAttribute("aria-checked"));
    }

    [Fact]
    public void Click_ColorMode_SelectsItAtOnce()
    {
        _viewModel.Step.Returns(OnboardingStep.Theme);
        _viewModel.ThemeOptions.Returns([AppColorTheme.Blue, AppColorTheme.Green]);
        _viewModel.ColorModeOptions.Returns([AppColorMode.Light, AppColorMode.Dark]);
        var cut = Render<OnboardingOverlay>();

        cut.Find(".onboarding-theme[data-color-mode-option=Dark]").Click();

        _viewModel.Received(1).SetColorModeAsync(AppColorMode.Dark);
    }

    [Fact]
    public void Click_NextAndBack_CallTheViewModel()
    {
        _viewModel.Step.Returns(OnboardingStep.Welcome);
        _viewModel.CanGoBack.Returns(true);
        var cut = Render<OnboardingOverlay>();

        cut.Find(".onboarding-next").Click();
        cut.Find(".onboarding-back").Click();

        _viewModel.Received(1).Next();
        _viewModel.Received(1).Back();
    }

    [Fact]
    public void Render_Revisit_StillOffersNoClose()
    {
        _viewModel.Step.Returns(OnboardingStep.Welcome);
        _viewModel.IsRevisit.Returns(true);

        var cut = Render<OnboardingOverlay>();

        Assert.Empty(cut.FindAll(".onboarding-skip"));
        Assert.DoesNotContain("Schließen", cut.Markup);
    }

    [Fact]
    public void Render_FeaturesPage_ShowsTheSwitchesReflectingTheCurrentState()
    {
        _viewModel.Step.Returns(OnboardingStep.Features);
        _viewModel.CanGoBack.Returns(true);
        _viewModel.MicroFeedbackEnabled.Returns(true);
        _viewModel.ReviewsEnabled.Returns(false);
        _viewModel.CompanionEnabled.Returns(true);

        var cut = Render<OnboardingOverlay>();

        Assert.True(cut.Find("#onboardingMicroFeedback").HasAttribute("checked"));
        Assert.False(cut.Find("#onboardingReviews").HasAttribute("checked"));
        Assert.True(cut.Find("#onboardingCompanion").HasAttribute("checked"));
        Assert.Single(cut.FindAll(".onboarding-back"));
    }

    [Fact]
    public void Change_Switches_TakeEffectAtOnce()
    {
        _viewModel.Step.Returns(OnboardingStep.Features);
        var cut = Render<OnboardingOverlay>();

        cut.Find("#onboardingMicroFeedback").Change(false);
        cut.Find("#onboardingReviews").Change(false);
        cut.Find("#onboardingCompanion").Change(false);

        _viewModel.Received(1).SetMicroFeedbackEnabledAsync(false);
        _viewModel.Received(1).SetReviewsEnabledAsync(false);
        _viewModel.Received(1).SetCompanionEnabledAsync(false);
    }

    [Fact]
    public void Render_BalancePage_OffersThePresetsWithTheSelectedOneMarked()
    {
        _viewModel.Step.Returns(OnboardingStep.Balance);
        _viewModel.IsLastStep.Returns(true);
        _viewModel.BalanceGoal.Returns(BalanceGoal.TwoToThree);

        var cut = Render<OnboardingOverlay>();

        var goals = cut.FindAll(".onboarding-goal");
        Assert.Equal(3, goals.Count);
        Assert.Equal(Text("BalanceGoalOneToOne"), goals[0].TextContent.Trim());
        Assert.Equal("false", goals[0].GetAttribute("aria-checked"));
        Assert.Equal("true", goals[1].GetAttribute("aria-checked"));
        Assert.Contains("selected", goals[1].ClassName);
        Assert.Single(cut.FindAll(".onboarding-finish"));
        Assert.Empty(cut.FindAll(".onboarding-next"));
    }

    [Fact]
    public void Click_Goal_SelectsItAtOnce()
    {
        _viewModel.Step.Returns(OnboardingStep.Balance);
        var cut = Render<OnboardingOverlay>();

        cut.FindAll(".onboarding-goal")[2].Click();

        _viewModel.Received(1).SetBalanceGoalAsync(BalanceGoal.ThreeToTwo);
    }

    [Fact]
    public void Click_Finish_FinishesTheIntroduction()
    {
        _viewModel.Step.Returns(OnboardingStep.Balance);
        _viewModel.IsLastStep.Returns(true);
        var cut = Render<OnboardingOverlay>();

        cut.Find(".onboarding-finish").Click();

        _viewModel.Received(1).FinishAsync();
    }

    [Fact]
    public void Render_Invite_ShowsOnlyTheQuietInvitationWithTwoChoices()
    {
        _viewModel.Step.Returns(OnboardingStep.Invite);

        var cut = Render<OnboardingOverlay>();

        Assert.Equal(Text("OnboardingInviteTitle"), cut.Find("h2").TextContent.Trim());
        Assert.Equal(Text("OnboardingInviteText"), cut.Find(".onboarding-text").TextContent.Trim());
        Assert.Empty(cut.FindAll(".onboarding-next"));
        Assert.Empty(cut.FindAll(".onboarding-skip"));

        cut.Find(".onboarding-decline").Click();
        cut.Find(".onboarding-accept").Click();

        _viewModel.Received(1).DeclineInviteAsync();
        _viewModel.Received(1).AcceptInvite();
    }

    [Fact]
    public void Texts_AllOnboardingKeys_ExistInTheResources()
    {
        string[] keys =
        [
            "OnboardingTitle", "OnboardingNext", "OnboardingBack", "OnboardingFinish",
            "OnboardingWelcomeText", "OnboardingWelcomeHint", "OnboardingWhyTitle",
            "OnboardingWhyTraining", "OnboardingWhyPlan", "OnboardingWhyActivation", "OnboardingWhySelfDetermination",
            "OnboardingWhySelfCare", "OnboardingWhyReward", "OnboardingSceneMinutes", "OnboardingSceneQuestion", "OnboardingSceneYes",
            "OnboardingSceneNo", "OnboardingSceneExplain", "OnboardingSceneTip", "OnboardingSceneTipExample",
            "OnboardingSceneLabel", "OnboardingSceneDayIntro", "OnboardingSceneScope", "OnboardingDemoWalk",
            "OnboardingDemoTaxes", "OnboardingDemoDateNight", "OnboardingDemoMeditate", "OnboardingDemoCleanFlat",
            "OnboardingFeaturesTitle", "OnboardingFeaturesText", "OnboardingBalanceTitle",
            "OnboardingBalanceText", "OnboardingBalanceMore", "OnboardingInviteLater", "OnboardingInviteLook",
            "OnboardingSettingsTitle", "OnboardingSettingsDescription"
        ];

        foreach (var key in keys)
        {
            Assert.False(string.IsNullOrWhiteSpace(Text(key)));
        }
    }
}
