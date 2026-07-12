using System.Security.Cryptography;

namespace Nibbs.Nps.Integration.Abstractions;

/// <summary>
/// Supplies the RSA keys used to secure NPS messages:
/// the institution's private key (signing outbound, decrypting inbound) and the
/// NIBSS public key (encrypting outbound, validating inbound signatures).
/// </summary>
public interface INpsKeyProvider
{
    /// <summary>Your institution's RSA-2048 private key.</summary>
    RSA GetInstitutionPrivateKey();

    /// <summary>The NIBSS/NPS RSA public key.</summary>
    RSA GetNibssPublicKey();
}