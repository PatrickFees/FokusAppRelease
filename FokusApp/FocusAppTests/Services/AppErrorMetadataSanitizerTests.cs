using System.Text.Json;
using FokusApp.Core.Services;

namespace FocusAppTests.Services;

public class AppErrorMetadataSanitizerTests
{
    [Fact]
    public void Sanitize_NullMetadata_ReturnsNull()
    {
        var result = AppErrorMetadataSanitizer.Sanitize(null);

        Assert.Null(result);
    }

    [Fact]
    public void Sanitize_EmptyMetadata_ReturnsNull()
    {
        var metadata = new Dictionary<string, string>();

        var result = AppErrorMetadataSanitizer.Sanitize(metadata);

        Assert.Null(result);
    }

    [Fact]
    public void Sanitize_HarmlessEntries_AreKept()
    {
        var metadata = new Dictionary<string, string>
        {
            ["Page"] = "FocusTimer",
            ["Platform"] = "Android"
        };

        var result = AppErrorMetadataSanitizer.Sanitize(metadata);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("FocusTimer", result["Page"]);
        Assert.Equal("Android", result["Platform"]);
    }

    [Theory]
    [InlineData("note")]
    [InlineData("TaskId")]
    [InlineData("content")]
    [InlineData("Email")]
    [InlineData("phoneNumber")]
    [InlineData("AccessToken")]
    [InlineData("Password")]
    [InlineData("clientSecret")]
    [InlineData("UserName")]
    public void Sanitize_SensitiveKey_IsRemoved(string key)
    {
        var metadata = new Dictionary<string, string>
        {
            [key] = "geheim",
            ["Page"] = "Settings"
        };

        var result = AppErrorMetadataSanitizer.Sanitize(metadata);

        Assert.NotNull(result);
        var containsSensitiveKey = result.ContainsKey(key);
        Assert.Single(result);
        Assert.False(containsSensitiveKey);
    }

    [Fact]
    public void Sanitize_OnlySensitiveEntries_ReturnsNull()
    {
        var metadata = new Dictionary<string, string> { ["Password"] = "geheim" };

        var result = AppErrorMetadataSanitizer.Sanitize(metadata);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("Page", "")]
    [InlineData("Page", "   ")]
    [InlineData("", "Wert")]
    [InlineData("   ", "Wert")]
    public void Sanitize_BlankKeyOrValue_IsRemoved(string key, string value)
    {
        var metadata = new Dictionary<string, string> { [key] = value };

        var result = AppErrorMetadataSanitizer.Sanitize(metadata);

        Assert.Null(result);
    }

    [Fact]
    public void Sanitize_KeyAndValueWithWhitespace_AreTrimmed()
    {
        var metadata = new Dictionary<string, string> { ["  Page  "] = "  Home  " };

        var result = AppErrorMetadataSanitizer.Sanitize(metadata);

        Assert.NotNull(result);
        Assert.Equal("Home", result["Page"]);
    }

    [Fact]
    public void Sanitize_Result_HasCaseInsensitiveKeys()
    {
        var metadata = new Dictionary<string, string> { ["Page"] = "Home" };

        var result = AppErrorMetadataSanitizer.Sanitize(metadata);

        Assert.NotNull(result);
        Assert.Equal("Home", result["page"]);
    }

    [Fact]
    public void Sanitize_KeysDifferingOnlyByCase_AreMergedToOneEntry()
    {
        var metadata = new Dictionary<string, string>
        {
            ["Page"] = "Home",
            ["page"] = "Settings"
        };

        var result = AppErrorMetadataSanitizer.Sanitize(metadata);

        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public void Serialize_NullMetadata_ReturnsNull()
    {
        var json = AppErrorMetadataSanitizer.Serialize(null);

        Assert.Null(json);
    }

    [Fact]
    public void Serialize_OnlySensitiveEntries_ReturnsNull()
    {
        var metadata = new Dictionary<string, string> { ["token"] = "abc" };

        var json = AppErrorMetadataSanitizer.Serialize(metadata);

        Assert.Null(json);
    }

    [Fact]
    public void Serialize_HarmlessAndSensitiveEntries_ReturnsJsonWithoutSensitiveData()
    {
        var metadata = new Dictionary<string, string>
        {
            ["Page"] = "Home",
            ["Password"] = "geheim"
        };

        var json = AppErrorMetadataSanitizer.Serialize(metadata);

        Assert.NotNull(json);
        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        Assert.NotNull(parsed);
        var entry = Assert.Single(parsed);
        Assert.Equal("Home", entry.Value);
        Assert.DoesNotContain("geheim", json);
    }
}
