namespace Nibbs.Nps.Integration.Configuration;

/// <summary>
/// Options for direct integration with the NPS switch.
/// </summary>
public class NpsOptions
{
    public const string SectionName = "Nps";

    /// <summary>
    /// Base URL of the NPS switch including the /nps segment,
    /// e.g. "https://nps-test.nibss-plc.com.ng:8022/nps" (internet VPN)
    /// or "https://192.234.10.105:8022/nps" (extranet VPN).
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Your institution's NPS member/source identifier (ClrSysMmbId/MmbId), e.g. "999058".
    /// Used as the InstgAgt member id and as the MsgId prefix.
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>Display name of your institution (used e.g. in acmt.023 Assgnr/Pty/Nm).</summary>
    public string InstitutionName { get; set; } = string.Empty;

    /// <summary>
    /// PEM (PKCS#8 or PKCS#1) content or file path of your institution's RSA-2048 private key.
    /// The matching public key must have been shared with NIBSS during onboarding.
    /// </summary>
    public string PrivateKeyPem { get; set; } = string.Empty;

    /// <summary>
    /// PEM content or file path of the NIBSS/NPS RSA public key
    /// (used to encrypt outbound payloads and validate inbound signatures).
    /// </summary>
    public string NibssPublicKeyPem { get; set; } = string.Empty;

    /// <summary>HTTP timeout for calls to the switch.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// When true (default) outbound messages are signed and their business payload encrypted
    /// before transmission. Only disable for schema-validation sandboxes.
    /// </summary>
    public bool ProtectOutboundMessages { get; set; } = true;

    /// <summary>
    /// When true, uses AES-256-GCM for outbound payload encryption; otherwise AES-256-CBC
    /// (the algorithm shown in the guide's .NET encryption sample). NIBSS responses use GCM;
    /// both are always supported for inbound decryption.
    /// </summary>
    public bool UseGcmEncryption { get; set; }

    /// <summary>
    /// When true, inbound message signatures are validated after decryption. Default true.
    /// </summary>
    public bool ValidateInboundSignatures { get; set; } = true;
}
