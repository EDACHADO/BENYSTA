using System.Globalization;

namespace Nibbs.Nps.Integration.Helpers;

/// <summary>
/// Formatting helpers matching the value formats mandated by the NPS integration guide.
/// </summary>
public static class NpsFormats
{
    /// <summary>ISO-8601 date-time with milliseconds and UTC designator, e.g. 2025-02-25T09:52:22.954Z.</summary>
    public static string DateTimeUtc(DateTime value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    /// <summary>ISO-8601 date-time preserving the supplied offset, e.g. 2026-04-14T11:24:03.138+01:00.</summary>
    public static string DateTimeWithOffset(DateTimeOffset value) =>
        value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture);

    /// <summary>ISO-8601 date, e.g. 2025-02-25.</summary>
    public static string Date(DateTime value) =>
        value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Amount with exactly two decimal places, e.g. 78000.00.</summary>
    public static string Amount(decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);
}
