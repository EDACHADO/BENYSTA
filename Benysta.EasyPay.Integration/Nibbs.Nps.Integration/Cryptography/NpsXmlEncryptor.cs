using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using Nibbs.Nps.Integration.Constants;
using Nibbs.Nps.Integration.Exceptions;

namespace Nibbs.Nps.Integration.Cryptography;

/// <summary>
/// Encrypts and decrypts the business payload of NPS ISO 20022 messages per the
/// W3C XML Encryption profile used by the switch: the CONTENT of the business root
/// element (e.g. FIToFICstmrCdtTrf, IdVrfctnReq) is replaced with an EncryptedData
/// element carrying an AES-256 session key wrapped with RSA-OAEP.
/// Outbound encryption supports AES-256-CBC (the guide's .NET sample) and AES-256-GCM;
/// inbound decryption transparently handles both (NIBSS responses use GCM).
/// </summary>
public static class NpsXmlEncryptor
{
    private const int GcmNonceSize = 12;
    private const int GcmTagSize = 16;

    /// <summary>
    /// Encrypts the content of <paramref name="element"/> in place using an AES-256
    /// session key wrapped with the recipient's RSA public key (RSA-OAEP).
    /// </summary>
    public static void EncryptContent(XmlElement element, RSA recipientPublicKey, bool useGcm = false)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(recipientPublicKey);

        try
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;

            byte[] cipherValue;
            string encryptionAlgorithm;
            if (useGcm)
            {
                cipherValue = EncryptContentGcm(element, aes.Key);
                encryptionAlgorithm = NpsXmlNamespaces.Aes256Gcm;
            }
            else
            {
                var encryptedXml = new EncryptedXml();
                cipherValue = encryptedXml.EncryptData(element, aes, content: true);
                encryptionAlgorithm = EncryptedXml.XmlEncAES256Url;
            }

            var wrappedKey = EncryptedXml.EncryptKey(aes.Key, recipientPublicKey, useOAEP: true);

            var encryptedData = new EncryptedData
            {
                Type = EncryptedXml.XmlEncElementContentUrl,
                EncryptionMethod = new EncryptionMethod(encryptionAlgorithm),
            };

            var encryptedKey = new EncryptedKey
            {
                CipherData = new CipherData(wrappedKey),
                EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncRSAOAEPUrl),
            };

            var keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoEncryptedKey(encryptedKey));
            encryptedData.KeyInfo = keyInfo;
            encryptedData.CipherData.CipherValue = cipherValue;

            EncryptedXml.ReplaceElement(element, encryptedData, content: true);
        }
        catch (CryptographicException ex)
        {
            throw new NpsSecurityException("Failed to encrypt the NPS message payload.", ex);
        }
    }

    /// <summary>
    /// Decrypts every EncryptedData element in <paramref name="document"/> in place,
    /// unwrapping the AES session key with <paramref name="recipientPrivateKey"/>.
    /// Returns false when the document contains no EncryptedData element.
    /// </summary>
    public static bool DecryptDocument(XmlDocument document, RSA recipientPrivateKey)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(recipientPrivateKey);

        var encryptedNodes = document
            .GetElementsByTagName("EncryptedData", EncryptedXml.XmlEncNamespaceUrl)
            .Cast<XmlElement>()
            .ToList();
        if (encryptedNodes.Count == 0)
            return false;

        foreach (var encryptedElement in encryptedNodes)
            DecryptElement(encryptedElement, recipientPrivateKey);

        return true;
    }

    private static void DecryptElement(XmlElement encryptedElement, RSA privateKey)
    {
        try
        {
            var algorithm = SelectSingle(encryptedElement, "EncryptionMethod")?.GetAttribute("Algorithm")
                            ?? string.Empty;
            var wrappedKey = ReadCipherValue(SelectSingle(encryptedElement, "EncryptedKey")
                             ?? throw new NpsSecurityException("EncryptedKey not found inside EncryptedData."));
            var cipherValue = ReadCipherValue(encryptedElement);

            var sessionKey = UnwrapSessionKey(wrappedKey, privateKey);
            try
            {
                var plaintext = algorithm switch
                {
                    NpsXmlNamespaces.Aes256Gcm => DecryptGcm(cipherValue, sessionKey),
                    _ => DecryptCbc(cipherValue, sessionKey),
                };

                ReplaceWithPlaintext(encryptedElement, plaintext);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(sessionKey);
            }
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException or XmlException)
        {
            throw new NpsSecurityException("Failed to decrypt the NPS message payload.", ex);
        }
    }

    private static byte[] EncryptContentGcm(XmlElement element, byte[] key)
    {
        var contentBytes = Encoding.UTF8.GetBytes(element.InnerXml);
        var nonce = RandomNumberGenerator.GetBytes(GcmNonceSize);
        var ciphertext = new byte[contentBytes.Length];
        var tag = new byte[GcmTagSize];

        using (var gcm = new AesGcm(key, GcmTagSize))
            gcm.Encrypt(nonce, contentBytes, ciphertext, tag);

        // W3C XML Encryption 1.1 layout: IV || ciphertext || authentication tag.
        var result = new byte[nonce.Length + ciphertext.Length + tag.Length];
        nonce.CopyTo(result, 0);
        ciphertext.CopyTo(result, nonce.Length);
        tag.CopyTo(result, nonce.Length + ciphertext.Length);
        return result;
    }

    private static byte[] UnwrapSessionKey(byte[] wrappedKey, RSA privateKey)
    {
        // The rsa-oaep-mgf1p algorithm is OAEP with SHA-1; the guide's prose mentions
        // SHA-256, so both are attempted.
        try
        {
            return privateKey.Decrypt(wrappedKey, RSAEncryptionPadding.OaepSHA1);
        }
        catch (CryptographicException)
        {
            return privateKey.Decrypt(wrappedKey, RSAEncryptionPadding.OaepSHA256);
        }
    }

    private static byte[] DecryptGcm(byte[] cipherValue, byte[] key)
    {
        if (cipherValue.Length < GcmNonceSize + GcmTagSize)
            throw new NpsSecurityException("AES-GCM cipher value is too short.");

        var nonce = cipherValue.AsSpan(0, GcmNonceSize);
        var tag = cipherValue.AsSpan(cipherValue.Length - GcmTagSize, GcmTagSize);
        var ciphertext = cipherValue.AsSpan(GcmNonceSize, cipherValue.Length - GcmNonceSize - GcmTagSize);

        var plaintext = new byte[ciphertext.Length];
        using var gcm = new AesGcm(key, GcmTagSize);
        gcm.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    private static byte[] DecryptCbc(byte[] cipherValue, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        // XML Encryption padding sets only the final byte to the pad length; decrypt
        // without padding and strip manually.
        aes.Padding = PaddingMode.None;

        var blockSize = aes.BlockSize / 8;
        if (cipherValue.Length < blockSize * 2)
            throw new NpsSecurityException("AES-CBC cipher value is too short.");

        aes.IV = cipherValue.AsSpan(0, blockSize).ToArray();
        using var decryptor = aes.CreateDecryptor();
        var plaintext = decryptor.TransformFinalBlock(cipherValue, blockSize, cipherValue.Length - blockSize);

        var padLength = plaintext[^1];
        if (padLength < 1 || padLength > blockSize || padLength > plaintext.Length)
            throw new NpsSecurityException("Invalid XML Encryption block padding.");

        return plaintext[..^padLength];
    }

    private static void ReplaceWithPlaintext(XmlElement encryptedElement, byte[] plaintext)
    {
        var owner = encryptedElement.OwnerDocument
                    ?? throw new NpsSecurityException("EncryptedData element has no owner document.");
        var parent = encryptedElement.ParentNode
                     ?? throw new NpsSecurityException("EncryptedData element has no parent node.");

        var fragment = owner.CreateDocumentFragment();
        fragment.InnerXml = Encoding.UTF8.GetString(plaintext);
        parent.ReplaceChild(fragment, encryptedElement);
    }

    private static XmlElement? SelectSingle(XmlElement scope, string localName)
        => scope.GetElementsByTagName(localName, EncryptedXml.XmlEncNamespaceUrl)
            .Cast<XmlElement>()
            .FirstOrDefault();

    private static byte[] ReadCipherValue(XmlElement scope)
    {
        // The first CipherValue directly under this scope's CipherData. For the outer
        // EncryptedData, skip the one nested inside EncryptedKey.
        foreach (XmlElement cipherValue in scope.GetElementsByTagName("CipherValue", EncryptedXml.XmlEncNamespaceUrl))
        {
            var withinEncryptedKey = IsWithin(cipherValue, "EncryptedKey", stopAt: scope);
            var scopeIsEncryptedKey = scope.LocalName == "EncryptedKey";
            if (scopeIsEncryptedKey || !withinEncryptedKey)
                return Convert.FromBase64String(NormalizeBase64(cipherValue.InnerText));
        }

        throw new NpsSecurityException($"CipherValue not found under {scope.LocalName}.");
    }

    private static bool IsWithin(XmlElement node, string ancestorLocalName, XmlElement stopAt)
    {
        for (var current = node.ParentNode; current is not null && current != stopAt; current = current.ParentNode)
        {
            if (current is XmlElement e &&
                e.LocalName == ancestorLocalName &&
                e.NamespaceURI == EncryptedXml.XmlEncNamespaceUrl)
                return true;
        }
        return false;
    }

    private static string NormalizeBase64(string value)
        => value.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace(" ", string.Empty)
            .Replace("\t", string.Empty);
}
