namespace Nibbs.Nps.Integration.Abstractions;

/// <summary>
/// Applies and removes NPS message protection.
/// Outbound: sign the plaintext document (enveloped XMLDSIG), then encrypt the content
/// of the business root element — the signature stays outside the encrypted block.
/// Inbound: decrypt the payload first, then validate the signature against the
/// restored plaintext (the two steps must be handled together per the guide).
/// </summary>
public interface INpsMessageProtector
{
    /// <summary>Signs and encrypts a raw plaintext ISO 20022 message for transmission.</summary>
    string Protect(string plainXml);

    /// <summary>
    /// Decrypts an inbound message and validates its signature.
    /// Returns the plaintext XML.
    /// </summary>
    string Unprotect(string protectedXml);
}
