using FokusApp.Components.ForYouSuggestions;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using static FocusAppTests.TestSupport.WaitForAsyncHelper;

namespace FocusAppTests.ViewModels;

public class ForYouSuggestionsViewModelTests
{
    private readonly IForYouSuggestionService _service = Substitute.For<IForYouSuggestionService>();
    private readonly IAppStartupStateService _startupState = Substitute.For<IAppStartupStateService>();
    private readonly IAppErrorCaptureService _errorCapture = Substitute.For<IAppErrorCaptureService>();
    private readonly ForYouSuggestionsViewModel _viewModel;

    public ForYouSuggestionsViewModelTests()
    {
        _service.GetStoredSuggestionsAsync().Returns(new List<ForYouSuggestionDto>());
        _service.AddAsync(Arg.Any<string>()).Returns(true);
        _service.UpdateAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(true);
        _viewModel = new ForYouSuggestionsViewModel(_service, _startupState, _errorCapture, new KeyTextProvider());
    }

    [Fact]
    public async Task InitializeAsync_AppDataLoading_WaitsForStartupAndLoadsTheOwnSuggestions()
    {
        var suggestion = Own("Lesen");
        _service.GetStoredSuggestionsAsync().Returns(new List<ForYouSuggestionDto> { suggestion });

        await _viewModel.InitializeAsync();

        await _startupState.Received(1).WaitForReadyAsync(Arg.Any<CancellationToken>());
        var own = _viewModel.Suggestions[0];
        Assert.Equal(new ForYouSuggestionManageItem(suggestion.Id.ToString(), "Lesen", false), own);
        Assert.Equal(1 + ForYouSuggestionPool.Entries.Count, _viewModel.Suggestions.Count);
    }

    [Fact]
    public async Task InitializeAsync_NothingStored_ListsTheBuiltInSuggestionsAfterTheOwnOnes()
    {
        await _viewModel.InitializeAsync();

        var expectedTextKeys = ForYouSuggestionPool.Entries.Select(entry => entry.TextKey);
        var actualKeys = _viewModel.Suggestions.Select(item => item.Key);
        Assert.Equal(expectedTextKeys, actualKeys);
        Assert.All(_viewModel.Suggestions, item => Assert.True(item.IsBuiltIn));
        Assert.Equal(ForYouSuggestionPool.Entries.Count, _viewModel.BuiltInCount);
    }

    [Fact]
    public async Task InitializeAsync_EditedAndDeletedBuiltIns_ShowTheNewTextAndHideTheDeletedOne()
    {
        var entries = ForYouSuggestionPool.Entries;
        _service.GetStoredSuggestionsAsync().Returns(new List<ForYouSuggestionDto>
        {
            new() { Id = Guid.NewGuid(), Text = "Anders", PoolKey = entries[0].TextKey },
            new() { Id = Guid.NewGuid(), Text = string.Empty, PoolKey = entries[1].TextKey, IsDeleted = true }
        });

        await _viewModel.InitializeAsync();

        Assert.Equal("Anders", _viewModel.Suggestions[0].Text);
        Assert.DoesNotContain(_viewModel.Suggestions, item => item.Key == entries[1].TextKey);
        Assert.Equal(entries.Count - 1, _viewModel.BuiltInCount);
    }

    [Fact]
    public void MaximumTextLength_Always_ExposesTheTextLimit()
    {
        Assert.Equal(ForYouSuggestionDto.MaximumTextLength, _viewModel.MaximumTextLength);
    }

    [Fact]
    public async Task AddAsync_NewText_AddsClearsTheFieldAndShowsNoWarning()
    {
        _viewModel.SetNewText("Lesen");

        await _viewModel.AddAsync();

        await _service.Received(1).AddAsync("Lesen");
        Assert.Equal(string.Empty, _viewModel.NewText);
        Assert.False(_viewModel.IsRejected);
    }

    [Fact]
    public async Task AddAsync_Refused_KeepsTheTextAndShowsAFriendlyWarning()
    {
        _service.AddAsync(Arg.Any<string>()).Returns(false);
        _viewModel.SetNewText("Lesen");

        await _viewModel.AddAsync();

        Assert.Equal("Lesen", _viewModel.NewText);
        Assert.True(_viewModel.IsRejected);
    }

    [Fact]
    public async Task SetNewText_AfterARefusal_HidesTheWarning()
    {
        _service.AddAsync(Arg.Any<string>()).Returns(false);
        _viewModel.SetNewText("Lesen");
        await _viewModel.AddAsync();

        _viewModel.SetNewText("Lesen!");

        Assert.False(_viewModel.IsRejected);
    }

    [Fact]
    public async Task AddAsync_Fails_CapturesTheError()
    {
        var failure = new InvalidOperationException("Datenbank gesperrt");
        _service.AddAsync(Arg.Any<string>()).ThrowsAsync(failure);

        await _viewModel.AddAsync();

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null && options.Source == "ForYouSuggestionsViewModel.AddAsync" && options.IsUiFailure));
    }

    [Fact]
    public async Task StartEdit_OwnSuggestion_FillsTheEditField()
    {
        var suggestion = Own("Lesen");
        _service.GetStoredSuggestionsAsync().Returns(new List<ForYouSuggestionDto> { suggestion });
        await _viewModel.InitializeAsync();

        _viewModel.StartEdit(suggestion.Id.ToString());

        Assert.Equal(suggestion.Id.ToString(), _viewModel.EditingKey);
        Assert.Equal("Lesen", _viewModel.EditText);
    }

    [Fact]
    public void StartEdit_UnknownSuggestion_DoesNothing()
    {
        _viewModel.StartEdit("unbekannt");

        Assert.Null(_viewModel.EditingKey);
    }

    [Fact]
    public async Task SaveEditAsync_ChangedText_UpdatesAndLeavesEditing()
    {
        var suggestion = Own("Lesen");
        _service.GetStoredSuggestionsAsync().Returns(new List<ForYouSuggestionDto> { suggestion });
        await _viewModel.InitializeAsync();
        _viewModel.StartEdit(suggestion.Id.ToString());
        _viewModel.SetEditText("Zeitung lesen");

        await _viewModel.SaveEditAsync();

        await _service.Received(1).UpdateAsync(suggestion.Id, "Zeitung lesen");
        Assert.Null(_viewModel.EditingKey);
        Assert.False(_viewModel.IsRejected);
    }

    [Fact]
    public async Task SaveEditAsync_Refused_StaysInEditingAndShowsAWarning()
    {
        var suggestion = Own("Lesen");
        _service.GetStoredSuggestionsAsync().Returns(new List<ForYouSuggestionDto> { suggestion });
        _service.UpdateAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(false);
        await _viewModel.InitializeAsync();
        _viewModel.StartEdit(suggestion.Id.ToString());

        await _viewModel.SaveEditAsync();

        Assert.Equal(suggestion.Id.ToString(), _viewModel.EditingKey);
        Assert.True(_viewModel.IsRejected);
    }

    [Fact]
    public async Task SaveEditAsync_NothingBeingEdited_DoesNothing()
    {
        await _viewModel.SaveEditAsync();

        await _service.DidNotReceiveWithAnyArgs().UpdateAsync(default, default!);
    }

    [Fact]
    public async Task CancelEdit_WhileEditing_LeavesEditingWithoutSaving()
    {
        var suggestion = Own("Lesen");
        _service.GetStoredSuggestionsAsync().Returns(new List<ForYouSuggestionDto> { suggestion });
        await _viewModel.InitializeAsync();
        _viewModel.StartEdit(suggestion.Id.ToString());

        _viewModel.CancelEdit();

        Assert.Null(_viewModel.EditingKey);
        await _service.DidNotReceiveWithAnyArgs().UpdateAsync(default, default!);
    }

    [Fact]
    public async Task DeleteAsync_OwnSuggestion_DeletesIt()
    {
        var suggestion = Own("Lesen");
        _service.GetStoredSuggestionsAsync().Returns(new List<ForYouSuggestionDto> { suggestion });
        await _viewModel.InitializeAsync();

        await _viewModel.DeleteAsync(suggestion.Id.ToString());

        await _service.Received(1).DeleteAsync(suggestion.Id);
        await _service.DidNotReceiveWithAnyArgs().DeleteBuiltInAsync(default!);
    }

    [Fact]
    public async Task DeleteAsync_BuiltInSuggestion_RemovesItByItsKey()
    {
        var key = ForYouSuggestionPool.Entries[2].TextKey;
        await _viewModel.InitializeAsync();

        await _viewModel.DeleteAsync(key);

        await _service.Received(1).DeleteBuiltInAsync(key);
        await _service.DidNotReceiveWithAnyArgs().DeleteAsync(default);
    }

    [Fact]
    public async Task DeleteAsync_UnknownKey_DoesNothing()
    {
        await _viewModel.InitializeAsync();

        await _viewModel.DeleteAsync(Guid.NewGuid().ToString());

        await _service.DidNotReceiveWithAnyArgs().DeleteAsync(default);
        await _service.DidNotReceiveWithAnyArgs().DeleteBuiltInAsync(default!);
    }

    [Fact]
    public async Task SaveEditAsync_BuiltInSuggestion_UpdatesItByItsKey()
    {
        var key = ForYouSuggestionPool.Entries[0].TextKey;
        _service.UpdateBuiltInAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        await _viewModel.InitializeAsync();
        _viewModel.StartEdit(key);
        _viewModel.SetEditText("Langer Spaziergang");

        await _viewModel.SaveEditAsync();

        await _service.Received(1).UpdateBuiltInAsync(key, "Langer Spaziergang");
        await _service.DidNotReceiveWithAnyArgs().UpdateAsync(default, default!);
        Assert.Null(_viewModel.EditingKey);
    }

    [Fact]
    public async Task DeleteAsync_SuggestionBeingEdited_LeavesEditing()
    {
        var suggestion = Own("Lesen");
        _service.GetStoredSuggestionsAsync().Returns(new List<ForYouSuggestionDto> { suggestion });
        await _viewModel.InitializeAsync();
        _viewModel.StartEdit(suggestion.Id.ToString());

        await _viewModel.DeleteAsync(suggestion.Id.ToString());

        Assert.Null(_viewModel.EditingKey);
    }

    [Fact]
    public async Task SuggestionsChanged_Elsewhere_ReloadsTheList()
    {
        await _viewModel.InitializeAsync();
        var suggestion = Own("Malen");
        _service.GetStoredSuggestionsAsync().Returns(new List<ForYouSuggestionDto> { suggestion });

        _service.SuggestionsChanged += Raise.Event();

        await WaitForAsync(_viewModel, () => _viewModel.Suggestions.Any(item => item.Text == "Malen"));
        Assert.Equal("Malen", _viewModel.Suggestions[0].Text);
    }

    [Fact]
    public async Task Dispose_AfterDispose_IgnoresChanges()
    {
        await _viewModel.InitializeAsync();
        _viewModel.Dispose();

        _service.SuggestionsChanged += Raise.Event();
        await Task.Delay(50);

        await _service.Received(1).GetStoredSuggestionsAsync();
    }

    [Fact]
    public void SetNewText_Null_BecomesEmptyText()
    {
        _viewModel.SetNewText("Etwas");

        _viewModel.SetNewText(null);

        Assert.Equal(string.Empty, _viewModel.NewText);
    }

    [Fact]
    public void SetEditText_Null_BecomesEmptyText()
    {
        _viewModel.SetEditText("Etwas");

        _viewModel.SetEditText(null);

        Assert.Equal(string.Empty, _viewModel.EditText);
    }

    [Fact]
    public async Task SetEditText_AfterARefusal_HidesTheWarning()
    {
        var suggestion = Own("Lesen");
        _service.GetStoredSuggestionsAsync().Returns(new List<ForYouSuggestionDto> { suggestion });
        _service.UpdateAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(false);
        await _viewModel.InitializeAsync();
        _viewModel.StartEdit(suggestion.Id.ToString());
        await _viewModel.SaveEditAsync();
        Assert.True(_viewModel.IsRejected);

        _viewModel.SetEditText("Malen");

        Assert.False(_viewModel.IsRejected);
    }

    [Fact]
    public async Task SaveEditAsync_Fails_CapturesTheErrorAndStaysInEditing()
    {
        var suggestion = Own("Lesen");
        _service.GetStoredSuggestionsAsync().Returns(new List<ForYouSuggestionDto> { suggestion });
        var failure = new InvalidOperationException("Datenbank gesperrt");
        _service.UpdateAsync(Arg.Any<Guid>(), Arg.Any<string>()).ThrowsAsync(failure);
        await _viewModel.InitializeAsync();
        _viewModel.StartEdit(suggestion.Id.ToString());

        await _viewModel.SaveEditAsync();

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "ForYouSuggestionsViewModel.SaveEditAsync" &&
                options.IsUiFailure));
        Assert.Equal(suggestion.Id.ToString(), _viewModel.EditingKey);
    }

    [Fact]
    public async Task DeleteAsync_Fails_CapturesTheError()
    {
        var suggestion = Own("Lesen");
        _service.GetStoredSuggestionsAsync().Returns(new List<ForYouSuggestionDto> { suggestion });
        var failure = new InvalidOperationException("Datenbank gesperrt");
        _service.DeleteAsync(Arg.Any<Guid>()).ThrowsAsync(failure);
        await _viewModel.InitializeAsync();

        await _viewModel.DeleteAsync(suggestion.Id.ToString());

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "ForYouSuggestionsViewModel.DeleteAsync" &&
                options.IsUiFailure));
    }

    [Fact]
    public async Task InitializeAsync_LoadingFails_CapturesTheErrorAndKeepsTheListEmpty()
    {
        var failure = new InvalidOperationException("Datenbank gesperrt");
        _service.GetStoredSuggestionsAsync().ThrowsAsync(failure);

        await _viewModel.InitializeAsync();

        await _errorCapture.Received(1).CaptureAsync(
            failure,
            Arg.Is<AppErrorCaptureOptions?>(options =>
                options != null &&
                options.Source == "ForYouSuggestionsViewModel.LoadAsync" &&
                options.IsUiFailure));
        Assert.Empty(_viewModel.Suggestions);
    }

    [Fact]
    public async Task InitializeAsync_Finished_NotifiesTheListAndTheBuiltInCount()
    {
        var changed = new List<string?>();
        _viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        await _viewModel.InitializeAsync();

        Assert.Contains(nameof(ForYouSuggestionsViewModel.Suggestions), changed);
        Assert.Contains(nameof(ForYouSuggestionsViewModel.BuiltInCount), changed);
    }

    private static ForYouSuggestionDto Own(string text) => new() { Id = Guid.NewGuid(), Text = text };

    private sealed class KeyTextProvider : IUiTextProvider
    {
        public string GetText(string key, params object[] arguments) => key;
    }
}
