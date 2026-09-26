namespace Integration.Models;

/// <summary>
/// Primary keys for the persisted models: a ULID rendered in its canonical 26-character
/// Crockford base32 form.
/// </summary>
/// <remarks>
/// Chosen over a GUID because the leading 48 bits are a millisecond timestamp, so keys
/// generated over time sort in creation order. That gives B-tree insert locality (new rows
/// land at the right-hand edge of the index rather than scattering) and makes a key
/// readable as an approximate creation time.
/// <para>
/// The canonical form is fixed-width and uses only digits and uppercase letters, so the
/// columns are declared <c>character varying(26)</c>. Sortability only holds under a
/// byte-ordering collation, which is why the key columns are pinned to collation "C".
/// </para>
/// </remarks>
public static class EntityId
{
    /// <summary>Length of the canonical ULID text form.</summary>
    public const int Length = 26;

    /// <summary>
    /// Collation for key columns. The default database collation is locale-aware and does
    /// not necessarily order the base32 alphabet by code point, which would break the
    /// ordering the ULID prefix exists to provide.
    /// </summary>
    public const string Collation = "C";

    /// <summary>Creates a new identifier.</summary>
    public static string New() => Ulid.NewUlid().ToString();
}
