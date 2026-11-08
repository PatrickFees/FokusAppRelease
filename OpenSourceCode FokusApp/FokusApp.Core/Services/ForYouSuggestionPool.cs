namespace FokusApp.Core.Services;

public enum ForYouSuggestionCategory
{
    Movement,
    Calm,
    Contact,
    Creativity
}

public sealed record ForYouPoolEntry(string TextKey, ForYouSuggestionCategory Category);

public static class ForYouSuggestionPool
{
    public const int SuggestionsPerCategory = 5;

    public static IReadOnlyList<ForYouPoolEntry> Entries { get; } = Enum.GetValues<ForYouSuggestionCategory>()
        .SelectMany(category => Enumerable.Range(1, SuggestionsPerCategory)
            .Select(number => new ForYouPoolEntry($"ForYouPool{category}{number}", category)))
        .ToList();
}
