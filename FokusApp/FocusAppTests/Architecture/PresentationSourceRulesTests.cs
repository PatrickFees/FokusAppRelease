using System.Text.RegularExpressions;

namespace FocusAppTests.Architecture;

/// <summary>
/// Prüft Regeln aus CodingGuideline.md am Quelltext von FokusApp.Presentation
/// (die Dateien werden über FocusAppTests.csproj in den Ausgabeordner kopiert).
/// </summary>
public class PresentationSourceRulesTests
{
    private static readonly string SourceRoot = Path.Combine(AppContext.BaseDirectory, "PresentationSources");

    [Fact]
    public void PresentationSources_AreAvailable()
    {
        var files = ReadSourceFiles();

        Assert.NotEmpty(files);
    }

    [Fact]
    public void ViewModels_DoNotReadSystemClockDirectly()
    {
        var violations = FindViolations(@"\bDateTime(Offset)?\.(Now|Today|UtcNow)\b");

        Assert.Empty(violations);
    }

    [Fact]
    public void ViewModels_DoNotUseStringLocalizerDirectly()
    {
        var violations = FindViolations(@"\bIStringLocalizer\b");

        Assert.Empty(violations);
    }

    private static List<string> FindViolations(string pattern)
    {
        var regex = new Regex(pattern);
        var violations = new List<string>();

        foreach (var file in ReadSourceFiles())
        {
            var lines = File.ReadAllLines(file);

            for (var index = 0; index < lines.Length; index++)
            {
                var trimmedLine = lines[index].TrimStart();
                var isComment = trimmedLine.StartsWith("//");

                if (!isComment && regex.IsMatch(lines[index]))
                {
                    var relativePath = Path.GetRelativePath(SourceRoot, file);
                    violations.Add($"{relativePath}:{index + 1}");
                }
            }
        }

        return violations;
    }

    private static string[] ReadSourceFiles()
    {
        if (!Directory.Exists(SourceRoot))
        {
            return [];
        }

        return Directory.GetFiles(SourceRoot, "*.cs", SearchOption.AllDirectories);
    }
}
