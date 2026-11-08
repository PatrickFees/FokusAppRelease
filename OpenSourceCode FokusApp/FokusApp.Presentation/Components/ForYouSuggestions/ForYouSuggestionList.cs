using FokusApp.Core.Models;
using FokusApp.Core.Services;
using FokusApp.Services;

namespace FokusApp.Components.ForYouSuggestions;

public sealed record ForYouSuggestionItem(
    string Text,
    bool IsOwn,
    ForYouSuggestionCategory? Category,
    Guid? TemplateId = null);

public static class ForYouSuggestionList
{

    public static IReadOnlyList<ForYouSuggestionItem> Build(
        IReadOnlyList<ForYouSuggestionDto> stored,
        IReadOnlyList<TaskDto> templates,
        IUiTextProvider textProvider)
    {
        var known = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
        var items = new List<ForYouSuggestionItem>();

        foreach (var template in templates)
        {
            var note = template.Note?.Trim();
            if (template is { IsPositive: true, IsTemplate: true, IsDone: false, Id: not null } &&
                !string.IsNullOrEmpty(note) &&
                known.Add(note))
            {
                items.Add(new ForYouSuggestionItem(note, false, null, template.Id));
            }
        }

        foreach (var suggestion in stored.Where(suggestion => suggestion.PoolKey is null))
        {
            if (known.Add(suggestion.Text))
            {
                items.Add(new ForYouSuggestionItem(suggestion.Text, true, null));
            }
        }

        foreach (var entry in ForYouSuggestionPool.Entries)
        {
            var change = stored.FirstOrDefault(suggestion => suggestion.PoolKey == entry.TextKey);
            if (change is { IsDeleted: true })
            {
                continue;
            }

            var text = change?.Text ?? textProvider.GetText(entry.TextKey);
            if (known.Add(text))
            {
                items.Add(new ForYouSuggestionItem(text, false, entry.Category));
            }
        }

        return items;
    }
}
