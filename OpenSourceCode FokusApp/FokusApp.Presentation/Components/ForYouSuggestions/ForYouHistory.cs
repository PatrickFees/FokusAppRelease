using FokusApp.Core.Extensions;
using FokusApp.Core.Models;

namespace FokusApp.Components.ForYouSuggestions;

public static class ForYouHistory
{

    public static IReadOnlyList<string> Build(IEnumerable<TaskDto> tasks, DateTime today)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        var known = new HashSet<string>(StringComparer.Ordinal);

        return tasks
            .Where(task => task is { IsPositive: true, IsTemplate: false } && IsPrevious(task, today))
            .OrderByDescending(task => task.CompletedAt ?? task.TaskDate ?? task.CreationDate)
            .Select(task => task.Note?.Trim() ?? string.Empty)
            .Where(note => note.Length > 0 && known.Add(NormalizeKey(note)))
            .ToList();
    }

    public static string NormalizeKey(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return string.Concat(text.Where(character => !char.IsWhiteSpace(character))).ToLowerInvariant();
    }

    private static bool IsPrevious(TaskDto task, DateTime today) =>
        task.IsDone || TaskExtension.IsPastOpenTask(task, today);
}
