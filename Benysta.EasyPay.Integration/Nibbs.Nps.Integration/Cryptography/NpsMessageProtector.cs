using System.Security.Cryptography.Xml;
using System.Xml;
using Microsoft.Extensions.Options;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Configuration;
using Nibbs.Nps.Integration.Exceptions;
using Nibbs.Nps.Integration.Serialization;

namespace Nibbs.Nps.Integration.Cryptography;

public sealed class NpsMessageProtector : INpsMessageProtector
{
    private readonly INpsKeyProvider _keys;
    private readonly NpsOptions _options;

    public NpsMessageProtector(INpsKeyProvider keys, IOptions<NpsOptions> options)
    {
        _keys = keys;
        _options = options.Value;
    }

    public string Protect(string plainXml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainXml);

        var document = NpsXmlSerializer.LoadXml(plainXml);

        NpsXmlSigner.Sign(document, _keys.GetInstitutionPrivateKey());

        var businessElement = FindBusinessElement(document)
            ?? throw new NpsSecurityException("Could not locate the business element to encrypt.");
        NpsXmlEncryptor.EncryptContent(businessElement, _keys.GetNibssPublicKey(), _options.UseGcmEncryption);

        return NpsXmlSerializer.ToXmlString(document);
    }

    public string Unprotect(string protectedXml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedXml);

        var document = NpsXmlSerializer.LoadXml(protectedXml);

        NpsXmlEncryptor.DecryptDocument(document, _keys.GetInstitutionPrivateKey());

        if (_options.ValidateInboundSignatures &&
            HasSignature(document) &&
            !NpsXmlSigner.Validate(document, _keys.GetNibssPublicKey()))
        {
            throw new NpsSecurityException("Inbound NPS message signature validation failed.");
        }

        NpsXmlSigner.RemoveSignature(document);
        return NpsXmlSerializer.ToXmlString(document);
    }

    /// <summary>
    /// The business element is the first element child of the ISO Document root that is
    /// not the XMLDSIG Signature (e.g. FIToFICstmrCdtTrf, IdVrfctnReq, MsgRjct).
    /// </summary>
    private static XmlElement FindBusinessElement(XmlDocument document)
        => document.DocumentElement?
            .ChildNodes
            .OfType<XmlElement>()
            .FirstOrDefault(e => e.NamespaceURI != SignedXml.XmlDsigNamespaceUrl);

    private static bool HasSignature(XmlDocument document)
        => document.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl).Count > 0;
}
