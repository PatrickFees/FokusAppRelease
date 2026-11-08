using FokusApp.Core.Services;
using System.Text;

namespace FocusAppTests.Services;

public class IcsTextFormatterTests
{
    [Theory]
    [InlineData("Simple text", "Simple text")]
    [InlineData("a, b; c\\d", "a\\, b\\; c\\\\d")]
    [InlineData("line1\nline2", "line1\\nline2")]
    [InlineData("line1\r\nline2", "line1\\nline2")]
    public void EscapeText_EscapesSpecialCharacters(string input, string expected)
    {
        Assert.Equal(expected, IcsTextFormatter.EscapeText(input));
    }

    [Fact]
    public void FoldLine_ShortLine_IsUnchanged()
    {
        var line = "SUMMARY:Short title";

        Assert.Equal(line, IcsTextFormatter.FoldLine(line));
    }

    [Fact]
    public void FoldLine_LongAsciiLine_FoldsAt75OctetsWithContinuation()
    {
        var line = "SUMMARY:" + new string('A', 100);

        var folded = IcsTextFormatter.FoldLine(line);
        var segments = folded.Split("\r\n");

        Assert.True(Encoding.UTF8.GetByteCount(segments[0]) <= 75);
        Assert.Equal(2, segments.Length);
        Assert.StartsWith(" ", segments[1]);
        Assert.Equal(line, string.Concat(segments[0], segments[1].TrimStart(' ')));
    }

    [Fact]
    public void FoldLine_LongLineWithMultiByteCharacters_NeverSplitsAUtf8Sequence()
    {
        var line = "SUMMARY:" + string.Concat(Enumerable.Repeat("ä€", 40));

        var folded = IcsTextFormatter.FoldLine(line);

        foreach (var segment in folded.Split("\r\n"))
        {
            var bytes = Encoding.UTF8.GetBytes(segment.TrimStart(' '));
            Assert.Equal(segment.TrimStart(' '), Encoding.UTF8.GetString(bytes));
            Assert.True(bytes.Length <= 75);
        }

        var reassembled = string.Concat(folded.Split("\r\n").Select((s, i) => i == 0 ? s : s.TrimStart(' ')));
        Assert.Equal(line, reassembled);
    }

    [Fact]
    public void FormatDateTimeLocal_ReturnsFloatingLocalFormat()
    {
        var result = IcsTextFormatter.FormatDateTimeLocal(new DateTime(2026, 3, 10), new TimeSpan(14, 30, 0));

        Assert.Equal("20260310T143000", result);
    }

    [Fact]
    public void FormatDateOnly_ReturnsDateOnlyFormat()
    {
        var result = IcsTextFormatter.FormatDateOnly(new DateTime(2026, 3, 10));

        Assert.Equal("20260310", result);
    }

    [Fact]
    public void FormatUtcNow_ReturnsUtcFormatWithZSuffix()
    {
        var now = new DateTimeOffset(2026, 3, 10, 14, 30, 0, TimeSpan.Zero);

        var result = IcsTextFormatter.FormatUtcNow(now);

        Assert.Equal("20260310T143000Z", result);
    }
}
