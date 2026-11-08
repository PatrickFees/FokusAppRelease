using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace FocusAppTests.Localization;

public partial class LocalizationResourceTests
{
    private static readonly string GermanPath = ResourcePath("AppStrings.resx");
    private static readonly string EnglishPath = ResourcePath("AppStrings.en.resx");

    [Fact]
    public void Resources_GermanAndEnglish_ContainTheSameKeys()
    {
        var german = ReadEntries(GermanPath);
        var english = ReadEntries(EnglishPath);

        var missingInEnglish = german.Keys.Except(english.Keys).Order().ToList();
        var missingInGerman = english.Keys.Except(german.Keys).Order().ToList();

        Assert.Empty(missingInEnglish);
        Assert.Empty(missingInGerman);
    }

    [Theory]
    [MemberData(nameof(ResourceFiles))]
    public void Resources_File_HasNoDuplicateKeys(string path)
    {
        var names = ReadNames(path);

        var duplicates = names.GroupBy(name => name).Where(group => group.Count() > 1).Select(group => group.Key).ToList();

        Assert.Empty(duplicates);
    }

    [Theory]
    [MemberData(nameof(ResourceFiles))]
    public void Resources_File_HasNoEmptyValues(string path)
    {
        var entries = ReadEntries(path);

        var emptyKeys = entries.Where(entry => string.IsNullOrWhiteSpace(entry.Value)).Select(entry => entry.Key).ToList();

        Assert.Empty(emptyKeys);
    }

    [Fact]
    public void Resources_GermanAndEnglish_UseTheSamePlaceholders()
    {
        var german = ReadEntries(GermanPath);
        var english = ReadEntries(EnglishPath);
        var mismatches = new List<string>();

        foreach (var (key, germanValue) in german)
        {
            if (!english.TryGetValue(key, out var englishValue))
            {
                continue;
            }

            var germanPlaceholders = ExtractPlaceholders(germanValue);
            var englishPlaceholders = ExtractPlaceholders(englishValue);
            if (!germanPlaceholders.SequenceEqual(englishPlaceholders))
            {
                mismatches.Add(key);
            }
        }

        Assert.Empty(mismatches);
    }

    private static string ResourcePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Localization", fileName);

    private static List<string> ReadNames(string path)
    {
        var document = XDocument.Load(path);

        return document.Root!
            .Elements("data")
            .Select(data => data.Attribute("name")!.Value)
            .ToList();
    }

    private static Dictionary<string, string> ReadEntries(string path)
    {
        var document = XDocument.Load(path);

        return document.Root!
            .Elements("data")
            .ToDictionary(
                data => data.Attribute("name")!.Value,
                data => data.Element("value")?.Value ?? string.Empty);
    }

    private static List<string> ExtractPlaceholders(string value)
    {
        return PlaceholderPattern()
            .Matches(value)
            .Select(match => match.Value)
            .Order()
            .ToList();
    }

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex PlaceholderPattern();

    public static TheoryData<string> ResourceFiles => new() { GermanPath, EnglishPath };
}
