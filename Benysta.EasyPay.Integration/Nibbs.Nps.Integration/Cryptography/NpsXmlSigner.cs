using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using Nibbs.Nps.Integration.Constants;
using Nibbs.Nps.Integration.Exceptions;

namespace Nibbs.Nps.Integration.Cryptography;

/// <summary>
/// Signs and validates NPS ISO 20022 messages per the W3C XMLDSIG profile mandated
/// by the integration guide: enveloped RSA-SHA256 signature over the whole document,
/// SHA-256 digest, inclusive C14N canonicalization, appended to the Document element.
/// </summary>
public static class NpsXmlSigner
{
    /// <summary>
    /// Computes an enveloped signature over <paramref name="document"/> and appends the
    /// resulting Signature element to the document root.
    /// </summary>
    public static void Sign(XmlDocument document, RSA privateKey)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(privateKey);
        if (document.DocumentElement is null)
            throw new NpsSecurityException("Cannot sign an empty XML document.");

        try
        {
            var signedXml = new SignedXml(document) { SigningKey = privateKey };

            var reference = new Reference(string.Empty)
            {
                DigestMethod = SignedXml.XmlDsigSHA256Url,
            };
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            signedXml.AddReference(reference);

            signedXml.SignedInfo!.CanonicalizationMethod = SignedXml.XmlDsigCanonicalizationUrl;
            signedXml.SignedInfo.SignatureMethod = NpsXmlNamespaces.RsaSha256;

            signedXml.ComputeSignature();

            var signatureElement = signedXml.GetXml();
            document.DocumentElement.AppendChild(document.ImportNode(signatureElement, deep: true));
        }
        catch (CryptographicException ex)
        {
            throw new NpsSecurityException("Failed to sign the NPS message.", ex);
        }
    }

    /// <summary>
    /// Validates the enveloped Signature element of an inbound message using the
    /// sender's (NIBSS) public key. Returns false when the signature does not verify.
    /// </summary>
    public static bool Validate(XmlDocument document, RSA publicKey)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(publicKey);

        var signatureNodes = document.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);
        if (signatureNodes.Count == 0)
            throw new NpsSecurityException("The message does not contain a Signature element.");

        try
        {
            var signedXml = new SignedXml(document);
            signedXml.LoadXml((XmlElement)signatureNodes[0]!);
            return signedXml.CheckSignature(publicKey);
        }
        catch (CryptographicException ex)
        {
            throw new NpsSecurityException("Failed to validate the message signature.", ex);
        }
    }

    /// <summary>Removes the enveloped Signature element (if any) from the document root.</summary>
    public static void RemoveSignature(XmlDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var signatureNodes = document.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);
        for (var i = signatureNodes.Count - 1; i >= 0; i--)
        {
            var node = signatureNodes[i];
            node?.ParentNode?.RemoveChild(node);
        }
    }
}
