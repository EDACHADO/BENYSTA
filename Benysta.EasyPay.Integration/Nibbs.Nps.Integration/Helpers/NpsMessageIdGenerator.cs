using System.Security.Cryptography;
using System.Text;

namespace Nibbs.Nps.Integration.Helpers;

/// <summary>
/// Generates NPS message identifiers in the mandated 35-character format:
/// sourceId + yyyyMMddHHmmss + random digits (padded to exactly 35 characters).
/// </summary>
public static class NpsMessageIdGenerator
{
    public const int MessageIdLength = 35;

    /// <summary>
    /// Creates a new 35-character MsgId for the given institution source id (e.g. "999058").
    /// </summary>
    public static string NewMessageId(string sourceId, DateTime? timestampUtc = null)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("Source id is required.", nameof(sourceId));

        var stamp = (timestampUtc ?? DateTime.UtcNow).ToString("yyyyMMddHHmmss");
        var prefix = sourceId + stamp;
        if (prefix.Length >= MessageIdLength)
            throw new ArgumentException(
                $"Source id '{sourceId}' is too long to build a {MessageIdLength}-character MsgId.",
                nameof(sourceId));

        return prefix + RandomDigits(MessageIdLength - prefix.Length);
    }

    private static string RandomDigits(int count)
    {
        var sb = new StringBuilder(count);
        for (var i = 0; i < count; i++)
            sb.Append((char)('0' + RandomNumberGenerator.GetInt32(10)));
        return sb.ToString();
    }
}
