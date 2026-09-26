using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Configuration;
using Nibbs.Nps.Integration.Exceptions;

namespace Nibbs.Nps.Integration.Cryptography;

/// <summary>
/// <see cref="INpsKeyProvider"/> that loads the RSA keys from the PEM files that
/// <see cref="NpsOptions.PrivateKeyPath"/> and <see cref="NpsOptions.NibssPublicKeyPath"/>
/// point at. Both options are paths only — inline PEM content is rejected, so key material
/// never travels through configuration.
/// The private key may be PKCS#8 ("BEGIN PRIVATE KEY") or PKCS#1 ("BEGIN RSA PRIVATE KEY");
/// the NIBSS key may be an SPKI/PKCS#1 public key ("BEGIN PUBLIC KEY" / "BEGIN RSA PUBLIC
/// KEY") or an X.509 certificate ("BEGIN CERTIFICATE"), from which the public key is
/// extracted. Keys are read once and cached for the lifetime of the provider, so rotating a
/// key on disk needs a restart.
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
            () => LoadPrivateKey(value.PrivateKeyPath),
            LazyThreadSafetyMode.ExecutionAndPublication);

        _nibssPublicKey = new Lazy<RSA>(
            () => LoadPublicKey(value.NibssPublicKeyPath),
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

    private static RSA LoadPrivateKey(string path)
    {
        var pem = ReadPemFile(path, nameof(NpsOptions.PrivateKeyPath));

        try
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return rsa;
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
            throw new NpsSecurityException(
                "Could not import the institution private key from the file at " +
                $"'{nameof(NpsOptions.PrivateKeyPath)}'. Expected an unencrypted PKCS#8 " +
                "(BEGIN PRIVATE KEY) or PKCS#1 (BEGIN RSA PRIVATE KEY) RSA key.", ex);
        }
    }

    private static RSA LoadPublicKey(string path)
    {
        var pem = ReadPemFile(path, nameof(NpsOptions.NibssPublicKeyPath));

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
                "Could not import the NIBSS public key from the file at " +
                $"'{nameof(NpsOptions.NibssPublicKeyPath)}'. Expected a PEM public key " +
                "(BEGIN PUBLIC KEY / BEGIN RSA PUBLIC KEY) or an X.509 certificate.", ex);
        }
    }

    /// <summary>
    /// Reads the PEM text from the file the setting points at.
    /// </summary>
    /// <remarks>
    /// Deliberately a path and nothing else. Inline PEM is rejected rather than loaded,
    /// because a setting that accepts key material invites key material: it reaches
    /// configuration files that are committed, deployment manifests, and anything that dumps
    /// configuration to a log. Rejecting it turns that mistake into a startup error instead
    /// of a silent leak.
    /// <para>
    /// The path is tried as given — covering absolute paths and paths relative to the
    /// working directory — and then relative to the application directory, so a key deployed
    /// alongside the binaries resolves however the process was launched.
    /// </para>
    /// </remarks>
    private static string ReadPemFile(string path, string optionName)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new NpsIntegrationException(
                $"NpsOptions.{optionName} is not configured. Set it to the path of a PEM file.");

        if (path.Contains("-----BEGIN", StringComparison.Ordinal))
            throw new NpsIntegrationException(
                $"NpsOptions.{optionName} contains inline PEM content, which is not accepted. " +
                "Write the key to a PEM file and set this option to its path, so key material " +
                "never lives in configuration.");

        var resolved = File.Exists(path)
            ? path
            : Path.Combine(AppContext.BaseDirectory, path);

        if (!File.Exists(resolved))
            throw new NpsIntegrationException(
                $"NpsOptions.{optionName} is set to '{path}', but no file exists there " +
                $"(also tried '{resolved}').");

        return File.ReadAllText(resolved);
    }
}
