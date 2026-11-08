using CommunityToolkit.Mvvm.ComponentModel;
using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Core.Services.Interfaces;
using FokusApp.Services;

namespace FokusApp.Components.ForYouSuggestions;

public sealed class ForYouSuggestionsViewModel : ObservableObject, IForYouSuggestionsViewModel, IDisposable
{
    private readonly IForYouSuggestionService _suggestionService;
    private readonly IAppStartupStateService _appStartupStateService;
    private readonly IAppErrorCaptureService _appErrorCaptureService;
    private readonly IUiTextProvider _textProvider;

    private IReadOnlyList<ForYouSuggestionManageItem> _suggestions = [];
    private string _newText = string.Empty;
    private string? _editingKey;
    private string _editText = string.Empty;
    private bool _isRejected;

    public ForYouSuggestionsViewModel(
        IForYouSuggestionService suggestionService,
        IAppStartupStateService appStartupStateService,
        IAppErrorCaptureService appErrorCaptureService,
        IUiTextProvider textProvider)
    {
        _suggestionService = suggestionService;
        _appStartupStateService = appStartupStateService;
        _appErrorCaptureService = appErrorCaptureService;
        _textProvider = textProvider;

        _suggestionService.SuggestionsChanged += OnSuggestionsChanged;
    }

    public IReadOnlyList<ForYouSuggestionManageItem> Suggestions => _suggestions;

    public int BuiltInCount => _suggestions.Count(item => item.IsBuiltIn);

    public int MaximumTextLength => ForYouSuggestionDto.MaximumTextLength;

    public string NewText => _newText;

    public string? EditingKey => _editingKey;

    public string EditText => _editText;

    public bool IsRejected => _isRejected;

    public async Task InitializeAsync()
    {
        await _appStartupStateService.WaitForReadyAsync();
        await LoadAsync();
    }

    public void SetNewText(string? text)
    {
        _newText = text ?? string.Empty;
        SetRejected(false);
        OnPropertyChanged(nameof(NewText));
    }

    public void SetEditText(string? text)
    {
        _editText = text ?? string.Empty;
        SetRejected(false);
        OnPropertyChanged(nameof(EditText));
    }

    public async Task AddAsync()
    {
        try
        {
            var added = await _suggestionService.AddAsync(_newText);
            if (added)
            {
                _newText = string.Empty;
                OnPropertyChanged(nameof(NewText));
            }

            SetRejected(!added);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(ForYouSuggestionsViewModel)}.{nameof(AddAsync)}");
        }
    }

    public void StartEdit(string key)
    {
        var suggestion = _suggestions.FirstOrDefault(item => item.Key == key);
        if (suggestion is null)
        {
            return;
        }

        _editingKey = key;
        _editText = suggestion.Text;
        SetRejected(false);
        OnPropertyChanged(nameof(EditingKey));
        OnPropertyChanged(nameof(EditText));
    }

    public void CancelEdit()
    {
        _editingKey = null;
        _editText = string.Empty;
        SetRejected(false);
        OnPropertyChanged(nameof(EditingKey));
        OnPropertyChanged(nameof(EditText));
    }

    public async Task SaveEditAsync()
    {
        var suggestion = _suggestions.FirstOrDefault(item => item.Key == _editingKey);
        if (suggestion is null)
        {
            return;
        }

        try
        {
            var saved = suggestion.IsBuiltIn
                ? await _suggestionService.UpdateBuiltInAsync(suggestion.Key, _editText)
                : await _suggestionService.UpdateAsync(Guid.Parse(suggestion.Key), _editText);
            if (saved)
            {
                CancelEdit();
            }
            else
            {
                SetRejected(true);
            }
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(ForYouSuggestionsViewModel)}.{nameof(SaveEditAsync)}");
        }
    }

    public async Task DeleteAsync(string key)
    {
        var suggestion = _suggestions.FirstOrDefault(item => item.Key == key);
        if (suggestion is null)
        {
            return;
        }

        try
        {
            if (_editingKey == key)
            {
                CancelEdit();
            }

            if (suggestion.IsBuiltIn)
            {
                await _suggestionService.DeleteBuiltInAsync(key);
            }
            else
            {
                await _suggestionService.DeleteAsync(Guid.Parse(key));
            }
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(ForYouSuggestionsViewModel)}.{nameof(DeleteAsync)}");
        }
    }

    public void Dispose()
    {
        _suggestionService.SuggestionsChanged -= OnSuggestionsChanged;
    }

    private async Task LoadAsync()
    {
        try
        {
            var stored = await _suggestionService.GetStoredSuggestionsAsync();
            _suggestions = BuildSuggestions(stored);
        }
        catch (Exception ex)
        {
            await _appErrorCaptureService.CaptureUiErrorAsync(ex, $"{nameof(ForYouSuggestionsViewModel)}.{nameof(LoadAsync)}");
        }

        OnPropertyChanged(nameof(Suggestions));
        OnPropertyChanged(nameof(BuiltInCount));
    }

    private List<ForYouSuggestionManageItem> BuildSuggestions(IReadOnlyList<ForYouSuggestionDto> stored)
    {
        var items = stored
            .Where(suggestion => suggestion.PoolKey is null)
            .Select(suggestion => new ForYouSuggestionManageItem(suggestion.Id.ToString(), suggestion.Text, false))
            .ToList();

        foreach (var entry in ForYouSuggestionPool.Entries)
        {
            var change = stored.FirstOrDefault(suggestion => suggestion.PoolKey == entry.TextKey);
            if (change is not { IsDeleted: true })
            {
                items.Add(new ForYouSuggestionManageItem(entry.TextKey, change?.Text ?? _textProvider.GetText(entry.TextKey), true));
            }
        }

        return items;
    }

    private void SetRejected(bool value)
    {
        if (_isRejected == value)
        {
            return;
        }

        _isRejected = value;
        OnPropertyChanged(nameof(IsRejected));
    }

    private void OnSuggestionsChanged(object? sender, EventArgs e)
    {
        _ = LoadAsync();
    }
}
