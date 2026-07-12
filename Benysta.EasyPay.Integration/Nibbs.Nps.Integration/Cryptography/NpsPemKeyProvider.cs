using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Configuration;
using Nibbs.Nps.Integration.Exceptions;

namespace Nibbs.Nps.Integration.Cryptography;

/// <summary>
/// <see cref="INpsKeyProvider"/> that loads the RSA keys from the PEM material configured
/// in <see cref="NpsOptions"/>. <see cref="NpsOptions.PrivateKeyPem"/> and
/// <see cref="NpsOptions.NibssPublicKeyPem"/> may each hold either the PEM text itself
/// or a path to a PEM file. The private key may be PKCS#8 ("BEGIN PRIVATE KEY") or
/// PKCS#1 ("BEGIN RSA PRIVATE KEY"); the NIBSS key may be an SPKI/PKCS#1 public key
/// ("BEGIN PUBLIC KEY" / "BEGIN RSA PUBLIC KEY") or an X.509 certificate
/// ("BEGIN CERTIFICATE"), from which the public key is extracted.
/// Keys are loaded once and cached for the lifetime of the provider.
/// </summary>
public sealed class NpsPemKeyProvider : INpsKeyProvider, IDisposable
{
    private readonly Lazy<RSA> _institutionPrivateKey;
    private readonly Lazy<RSA> _nibssPublicKey;

    public NpsPemKeyProvider(IOptions<NpsOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var value = options.Value;

        _institutionPrivateKey = new Lazy<RSA>(
            () => LoadPrivateKey(value.PrivateKeyPem),
            LazyThreadSafetyMode.ExecutionAndPublication);

        _nibssPublicKey = new Lazy<RSA>(
            () => LoadPublicKey(value.NibssPublicKeyPem),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public RSA GetInstitutionPrivateKey() => _institutionPrivateKey.Value;

    public RSA GetNibssPublicKey() => _nibssPublicKey.Value;

    public void Dispose()
    {
        if (_institutionPrivateKey.IsValueCreated)
            _institutionPrivateKey.Value.Dispose();
        if (_nibssPublicKey.IsValueCreated)
            _nibssPublicKey.Value.Dispose();
    }

    private static RSA LoadPrivateKey(string pemOrPath)
    {
        var pem = ResolvePem(pemOrPath, nameof(NpsOptions.PrivateKeyPem));

        try
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return rsa;
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
            throw new NpsSecurityException(
                "Could not import the institution private key from " +
                $"'{nameof(NpsOptions.PrivateKeyPem)}'. Expected an unencrypted PKCS#8 " +
                "(BEGIN PRIVATE KEY) or PKCS#1 (BEGIN RSA PRIVATE KEY) RSA key.", ex);
        }
    }

    private static RSA LoadPublicKey(string pemOrPath)
    {
        var pem = ResolvePem(pemOrPath, nameof(NpsOptions.NibssPublicKeyPem));

        try
        {
            if (pem.Contains("-----BEGIN CERTIFICATE-----", StringComparison.Ordinal))
            {
                using var certificate = X509Certificate2.CreateFromPem(pem);
                return certificate.GetRSAPublicKey()
                    ?? throw new NpsSecurityException(
                        "The configured NIBSS certificate does not contain an RSA public key.");
            }

            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return rsa;
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
            throw new NpsSecurityException(
                "Could not import the NIBSS public key from " +
                $"'{nameof(NpsOptions.NibssPublicKeyPem)}'. Expected a PEM public key " +
                "(BEGIN PUBLIC KEY / BEGIN RSA PUBLIC KEY) or an X.509 certificate.", ex);
        }
    }

    /// <summary>
    /// Returns the PEM text for a setting that may hold either inline PEM content
    /// or the path of a PEM file.
    /// </summary>
    private static string ResolvePem(string pemOrPath, string optionName)
    {
        if (string.IsNullOrWhiteSpace(pemOrPath))
            throw new NpsIntegrationException(
                $"NpsOptions.{optionName} is not configured. Provide PEM content or a PEM file path.");

        if (pemOrPath.Contains("-----BEGIN", StringComparison.Ordinal))
            return pemOrPath;

        if (!File.Exists(pemOrPath))
            throw new NpsIntegrationException(
                $"NpsOptions.{optionName} does not contain PEM content and no file exists at '{pemOrPath}'.");

        return File.ReadAllText(pemOrPath);
    }
}
