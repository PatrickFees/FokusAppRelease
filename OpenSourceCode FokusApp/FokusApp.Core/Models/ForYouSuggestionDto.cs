using FokusApp.Core.DataBase.Entities;

namespace FokusApp.Core.Models;

public sealed class ForYouSuggestionDto
{
    public const int MaximumTextLength = ForYouSuggestionEntity.MaximumTextLength;

    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? PoolKey { get; set; }
    public bool IsDeleted { get; set; }

    public static ForYouSuggestionDto FromEntity(ForYouSuggestionEntity suggestion) => new()
    {
        Id = suggestion.Id,
        Text = suggestion.Text,
        PoolKey = suggestion.PoolKey,
        IsDeleted = suggestion.IsDeleted
    };
}
