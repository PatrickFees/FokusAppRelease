using System.ComponentModel.DataAnnotations;

namespace FokusApp.Core.DataBase.Entities;

public class ForYouSuggestionEntity
{
    public const int MaximumTextLength = 120;
    public const int MaximumPoolKeyLength = 64;

    [Key]
    public Guid Id { get; set; }

    [MaxLength(MaximumTextLength)]
    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    [MaxLength(MaximumPoolKeyLength)]
    public string? PoolKey { get; set; }

    public bool IsDeleted { get; set; }
}
