using Bunit;
using FokusApp.Components.ForYouSuggestions;
using FokusApp.Components.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FocusAppTests.Components;

public class ForYouSuggestionsSettingsTests : ComponentTestContext
{
    private readonly IForYouSuggestionsViewModel _viewModel = Substitute.For<IForYouSuggestionsViewModel>();
    private const string FirstKey = "erste";
    private const string SecondKey = "ForYouPoolMovement1";

    public ForYouSuggestionsSettingsTests()
    {
        _viewModel.MaximumTextLength.Returns(120);
        _viewModel.BuiltInCount.Returns(20);
        _viewModel.NewText.Returns(string.Empty);
        _viewModel.Suggestions.Returns(new List<ForYouSuggestionManageItem>());
        Services.AddSingleton(_viewModel);
    }

    [Fact]
    public void Render_InitializesViewModel()
    {
        Render<ForYouSuggestionsSettings>();

        _viewModel.Received(1).InitializeAsync();
    }

    [Fact]
    public void Render_NoSuggestions_ShowsAFriendlyEmptyNoteAndTheBuiltInCount()
    {
        var cut = Render<ForYouSuggestionsSettings>();

        Assert.Equal(Text("ForYouSuggestionsEmpty"), cut.Find(".suggestions-empty").TextContent.Trim());
        Assert.Contains("20", cut.Find(".suggestions-description").TextContent);
        Assert.Empty(cut.FindAll(".suggestions-item"));
    }

    [Fact]
    public void Render_Suggestions_AreListedAsSwipeCardsWithEditAndDelete()
    {
        SetSuggestions();

        var cut = Render<ForYouSuggestionsSettings>();

        var items = cut.FindAll(".suggestions-item");
        Assert.Equal(2, items.Count);
        Assert.Equal("Lesen", items[0].QuerySelector(".suggestions-text")!.TextContent.Trim());
        Assert.NotNull(items[0].QuerySelector(".suggestions-edit"));
        Assert.NotNull(items[0].QuerySelector(".suggestions-delete"));
        Assert.Empty(cut.FindAll(".suggestions-empty"));
    }

    [Fact]
    public void Swipe_LeftThenRight_ShowsAndHidesTheActions()
    {
        SetSuggestions();
        var cut = Render<ForYouSuggestionsSettings>();
        var card = cut.FindAll(".suggestion-card")[1];

        card.PointerDown(new PointerEventArgs { ClientX = 250 });
        card.PointerUp(new PointerEventArgs { ClientX = 150 });
        Assert.Contains("show-actions", cut.FindAll(".suggestion-card")[1].ClassName);

        cut.FindAll(".suggestion-card")[1].PointerDown(new PointerEventArgs { ClientX = 150 });
        cut.FindAll(".suggestion-card")[1].PointerUp(new PointerEventArgs { ClientX = 250 });
        Assert.DoesNotContain("show-actions", cut.FindAll(".suggestion-card")[1].ClassName);
    }

    [Fact]
    public void Render_Suggestions_ShowNoPriorityLabel()
    {
        SetSuggestions();

        var cut = Render<ForYouSuggestionsSettings>();

        Assert.Empty(cut.FindAll(".priority-label"));
    }

    [Fact]
    public void Input_NewText_IsPassedToTheViewModel()
    {
        var cut = Render<ForYouSuggestionsSettings>();

        cut.Find("#newSuggestionInput").Input("Malen");

        _viewModel.Received(1).SetNewText("Malen");
        Assert.Equal("120", cut.Find("#newSuggestionInput").GetAttribute("maxlength"));
    }

    [Fact]
    public void Click_Add_AddsTheSuggestion()
    {
        var cut = Render<ForYouSuggestionsSettings>();

        cut.Find(".suggestions-add-button").Click();

        _viewModel.Received(1).AddAsync();
    }

    [Fact]
    public void KeyDown_EnterInTheField_AddsTheSuggestion()
    {
        var cut = Render<ForYouSuggestionsSettings>();

        cut.Find("#newSuggestionInput").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        cut.Find("#newSuggestionInput").KeyDown(new KeyboardEventArgs { Key = "a" });

        _viewModel.Received(1).AddAsync();
    }

    [Fact]
    public void Render_Rejected_ShowsAFriendlyWarning()
    {
        _viewModel.IsRejected.Returns(true);

        var cut = Render<ForYouSuggestionsSettings>();

        Assert.Equal(Text("ForYouSuggestionsRejected"), cut.Find(".suggestions-rejected").TextContent.Trim());
    }

    [Fact]
    public void Render_NotRejected_ShowsNoWarning()
    {
        var cut = Render<ForYouSuggestionsSettings>();

        Assert.Empty(cut.FindAll(".suggestions-rejected"));
    }

    [Fact]
    public void Click_Edit_StartsEditingThatSuggestion()
    {
        SetSuggestions();
        var cut = Render<ForYouSuggestionsSettings>();

        cut.FindAll(".suggestions-item")[1].QuerySelector(".suggestions-edit")!.Click();

        _viewModel.Received(1).StartEdit(SecondKey);
    }

    [Fact]
    public void Click_Delete_DeletesThatSuggestion()
    {
        SetSuggestions();
        var cut = Render<ForYouSuggestionsSettings>();

        cut.FindAll(".suggestions-item")[0].QuerySelector(".suggestions-delete")!.Click();

        _viewModel.Received(1).DeleteAsync(FirstKey);
    }

    [Fact]
    public void Render_Editing_ShowsAnEditFieldWithSaveAndCancelInsteadOfTheText()
    {
        SetSuggestions();
        _viewModel.EditingKey.Returns(FirstKey);
        _viewModel.EditText.Returns("Lesen");

        var cut = Render<ForYouSuggestionsSettings>();

        var editing = cut.FindAll(".suggestions-item")[0];
        Assert.Equal("Lesen", editing.QuerySelector(".suggestions-edit-input")!.GetAttribute("value"));
        Assert.Null(editing.QuerySelector(".suggestions-text"));
        Assert.NotNull(editing.QuerySelector(".suggestions-save"));
        Assert.NotNull(editing.QuerySelector(".suggestions-cancel"));
        Assert.NotNull(cut.FindAll(".suggestions-item")[1].QuerySelector(".suggestions-text"));
    }

    [Fact]
    public void Editing_InputSaveAndCancel_CallTheViewModel()
    {
        SetSuggestions();
        _viewModel.EditingKey.Returns(FirstKey);
        var cut = Render<ForYouSuggestionsSettings>();

        cut.Find(".suggestions-edit-input").Input("Zeitung lesen");
        cut.Find(".suggestions-save").Click();
        cut.Find(".suggestions-cancel").Click();

        _viewModel.Received(1).SetEditText("Zeitung lesen");
        _viewModel.Received(1).SaveEditAsync();
        _viewModel.Received(1).CancelEdit();
    }

    [Fact]
    public void Input_NoValue_IsPassedAsNullToTheViewModel()
    {
        var cut = Render<ForYouSuggestionsSettings>();

        cut.Find("#newSuggestionInput").TriggerEvent("oninput", new ChangeEventArgs { Value = null });

        _viewModel.Received(1).SetNewText(null);
    }

    [Fact]
    public void Swipe_ShortMove_KeepsTheActionsHidden()
    {
        SetSuggestions();
        var cut = Render<ForYouSuggestionsSettings>();

        cut.FindAll(".suggestion-card")[1].PointerDown(new PointerEventArgs { ClientX = 250 });
        cut.FindAll(".suggestion-card")[1].PointerUp(new PointerEventArgs { ClientX = 240 });

        Assert.DoesNotContain("show-actions", cut.FindAll(".suggestion-card")[1].ClassName);
    }

    [Fact]
    public void PointerUp_WithoutPointerDown_KeepsTheActionsHidden()
    {
        SetSuggestions();
        var cut = Render<ForYouSuggestionsSettings>();

        cut.FindAll(".suggestion-card")[1].PointerUp(new PointerEventArgs { ClientX = 100 });

        Assert.DoesNotContain("show-actions", cut.FindAll(".suggestion-card")[1].ClassName);
    }

    [Fact]
    public void Editing_InputWithoutValue_IsPassedAsNullToTheViewModel()
    {
        SetSuggestions();
        _viewModel.EditingKey.Returns(FirstKey);
        var cut = Render<ForYouSuggestionsSettings>();

        cut.Find(".suggestions-edit-input").TriggerEvent("oninput", new ChangeEventArgs { Value = null });

        _viewModel.Received(1).SetEditText(null);
    }

    private void SetSuggestions()
    {
        _viewModel.Suggestions.Returns(new List<ForYouSuggestionManageItem>
        {
            new(FirstKey, "Lesen", false),
            new(SecondKey, "Malen", true)
        });
    }
}
