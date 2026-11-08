using System.Globalization;
using System.Text;

namespace FokusApp.Core.Services;

public static class IcsTextFormatter
{
    private const int MaxLineOctets = 75;

    public static string EscapeText(string value) => value
        .Replace("\\", "\\\\")
        .Replace(";", "\\;")
        .Replace(",", "\\,")
        .Replace("\r\n", "\\n")
        .Replace("\n", "\\n");

    public static string FoldLine(string contentLine)
    {
        var bytes = Encoding.UTF8.GetBytes(contentLine);
        if (bytes.Length <= MaxLineOctets)
        {
            return contentLine;
        }

        var builder = new StringBuilder();
        var offset = 0;
        var isFirstChunk = true;
        while (offset < bytes.Length)
        {
            var chunkLimit = isFirstChunk ? MaxLineOctets : MaxLineOctets - 1;
            var chunkLength = Math.Min(chunkLimit, bytes.Length - offset);

            while (chunkLength > 0 && offset + chunkLength < bytes.Length && IsUtf8ContinuationByte(bytes[offset + chunkLength]))
            {
                chunkLength--;
            }

            if (!isFirstChunk)
            {
                builder.Append("\r\n ");
            }

            builder.Append(Encoding.UTF8.GetString(bytes, offset, chunkLength));
            offset += chunkLength;
            isFirstChunk = false;
        }

        return builder.ToString();
    }

    private static bool IsUtf8ContinuationByte(byte value) => (value & 0xC0) == 0x80;

    public static string FormatDateTimeLocal(DateTime date, TimeSpan time) =>
        (date.Date + time).ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture);

    public static string FormatDateOnly(DateTime date) =>
        date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

    public static string FormatUtcNow(DateTimeOffset now) =>
        now.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
}
