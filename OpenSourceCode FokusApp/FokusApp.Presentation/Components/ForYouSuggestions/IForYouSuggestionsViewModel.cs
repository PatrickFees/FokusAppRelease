using System.ComponentModel;

namespace FokusApp.Components.ForYouSuggestions;

public sealed record ForYouSuggestionManageItem(string Key, string Text, bool IsBuiltIn);

public interface IForYouSuggestionsViewModel : INotifyPropertyChanged
{

    IReadOnlyList<ForYouSuggestionManageItem> Suggestions { get; }

    int BuiltInCount { get; }

    int MaximumTextLength { get; }

    string NewText { get; }

    string? EditingKey { get; }

    string EditText { get; }

    bool IsRejected { get; }

    Task InitializeAsync();

    void SetNewText(string? text);

    void SetEditText(string? text);

    Task AddAsync();

    void StartEdit(string key);

    void CancelEdit();

    Task SaveEditAsync();

    Task DeleteAsync(string key);
}
