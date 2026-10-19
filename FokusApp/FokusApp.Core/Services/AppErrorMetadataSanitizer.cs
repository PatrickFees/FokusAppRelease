using System.Text.Json;

namespace FokusApp.Core.Services;

public static class AppErrorMetadataSanitizer
{
    private static readonly string[] SensitiveMetadataTokens =
    [
        "note",
        "task",
        "content",
        "email",
        "phone",
        "token",
        "password",
        "secret",
        "user"
    ];

    public static IReadOnlyDictionary<string, string>? Sanitize(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return null;
        }

        var safeMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in metadata)
        {
            var normalizedKey = pair.Key?.Trim();
            var normalizedValue = pair.Value?.Trim();

            if (string.IsNullOrWhiteSpace(normalizedKey) || string.IsNullOrWhiteSpace(normalizedValue))
            {
                continue;
            }

            if (ContainsSensitiveToken(normalizedKey))
            {
                continue;
            }

            safeMetadata[normalizedKey] = normalizedValue;
        }

        return safeMetadata.Count == 0 ? null : safeMetadata;
    }

    public static string? Serialize(IReadOnlyDictionary<string, string>? metadata)
    {
        var safeMetadata = Sanitize(metadata);
        return safeMetadata is null ? null : JsonSerializer.Serialize(safeMetadata);
    }

    private static bool ContainsSensitiveToken(string key)
    {
        var loweredKey = key.ToLowerInvariant();
        return SensitiveMetadataTokens.Any(token => loweredKey.Contains(token, StringComparison.Ordinal));
    }
}
