using System.Reflection;
using System.Xml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Configuration;
using Nibbs.Nps.Integration.Constants;
using Nibbs.Nps.Integration.Cryptography;
using Nibbs.Nps.Integration.Exceptions;
using Nibbs.Nps.Integration.Serialization;

namespace Nibbs.Nps.Integration.Inbound;

/// <summary>
/// End-to-end pipeline for a webhook request pushed by NIBSS NPS to one of your
/// institution's callback URLs (https://&lt;baseURL&gt;/&lt;npsMessageType&gt;):
/// decrypts and signature-verifies the raw body, materializes the typed ISO 20022
/// document and dispatches it to every registered
/// <see cref="INpsMessageHandler{TDocument}"/> for that document type.
/// </summary>
public interface INpsWebhookProcessor
{
    /// <summary>
    /// Processes a raw inbound webhook body and dispatches the decrypted document
    /// to the registered message handlers.
    /// </summary>
    /// <param name="rawXml">The signed+encrypted XML exactly as received from NPS.</param>
    /// <param name="cancellationToken">Aborts handler execution.</param>
    /// <exception cref="NpsSecurityException">
    /// Decryption or signature validation failed — the caller should answer HTTP 400.
    /// </exception>
    /// <exception cref="NpsIntegrationException">A message handler failed.</exception>
    Task<NpsWebhookResult> ProcessAsync(string rawXml, CancellationToken cancellationToken = default);
}

/// <summary>The outcome of processing an inbound NPS webhook request.</summary>
/// <param name="MessageType">The detected ISO 20022 message type.</param>
/// <param name="PlainXml">The decrypted plaintext ISO 20022 XML.</param>
/// <param name="Document">The typed document, or null when the type has no model.</param>
/// <param name="HandlerCount">How many registered handlers were invoked.</param>
/// <param name="WasEncrypted">Whether the payload arrived encrypted.</param>
/// <param name="SignatureValidated">Whether an XMLDSIG signature was present and verified.</param>
public sealed record NpsWebhookResult(
    NpsMessageType MessageType,
    string PlainXml,
    INpsDocument? Document,
    int HandlerCount,
    bool WasEncrypted,
    bool SignatureValidated);

public class NpsWebhookProcessor : INpsWebhookProcessor
{
    private readonly INpsKeyProvider _keys;
    private readonly NpsOptions _options;
    private readonly IServiceProvider _serviceProvider;

    public NpsWebhookProcessor(
        INpsKeyProvider keys,
        IOptions<NpsOptions> options,
        IServiceProvider serviceProvider)
    {
        _keys = keys;
        _options = options.Value;
        _serviceProvider = serviceProvider;
    }

    public async Task<NpsWebhookResult> ProcessAsync(string rawXml, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawXml);

        // NpsSecurityException from any of the steps below deliberately propagates so
        // the caller can answer HTTP 400, mirroring how NPS treats undecryptable input.

        // 1. Parse the raw body, preserving whitespace exactly — the signature digest
        //    was computed over the sender's byte-precise document.
        XmlDocument document;
        try
        {
            document = NpsXmlSerializer.LoadXml(rawXml);
        }
        catch (XmlException ex)
        {
            throw new NpsSecurityException("The inbound webhook body is not well-formed XML.", ex);
        }

        // 2. Decrypt: locate every xenc:EncryptedData block, unwrap its AES-256 session
        //    key with our institution's RSA private key (RSA-OAEP) and restore the
        //    plaintext content in place. NIBSS uses AES-256-GCM for pushes; AES-256-CBC
        //    is handled as well. Returns false when the message arrived unencrypted
        //    (e.g. schema-validation sandboxes).
        var wasEncrypted = NpsXmlEncryptor.DecryptDocument(document, _keys.GetInstitutionPrivateKey());

        // 3. Verify authenticity and integrity: the enveloped RSA-SHA256 signature sits
        //    OUTSIDE the encrypted block but was computed over the plaintext document,
        //    so it can only be checked now, after decryption ("validation and decryption
        //    must be handled together" per the integration guide).
        var signatureValidated = false;
        if (_options.ValidateInboundSignatures)
        {
            var hasSignature = HasSignature(document);
            if (!hasSignature && wasEncrypted)
                throw new NpsSecurityException(
                    "The encrypted inbound message carries no XMLDSIG signature; refusing to process an unauthenticated payload.");

            if (hasSignature)
            {
                if (!NpsXmlSigner.Validate(document, _keys.GetNibssPublicKey()))
                    throw new NpsSecurityException(
                        "Inbound message signature validation failed: the payload was tampered with or was not signed by NIBSS.");
                signatureValidated = true;
            }
        }

        // 4. Strip the signature and render the business plaintext.
        NpsXmlSigner.RemoveSignature(document);
        var plainXml = NpsXmlSerializer.ToXmlString(document);

        // 5. Detect the ISO 20022 message type from the Document namespace and
        //    materialize the typed model where one exists.
        var messageType = NpsMessageTypeInfo.FromXmlNamespace(document.DocumentElement?.NamespaceURI);
        var typedDocument = NpsInboundMessageProcessor.Materialize(messageType, plainXml);

        // 6. Dispatch to the registered handlers for this document type.
        var handlerCount = 0;
        if (typedDocument is not null)
        {
            var context = new NpsInboundMessage
            {
                MessageType = messageType,
                PlainXml = plainXml,
                Document = typedDocument,
            };
            handlerCount = await DispatchAsync(typedDocument, context, cancellationToken).ConfigureAwait(false);
        }

        return new NpsWebhookResult(messageType, plainXml, typedDocument, handlerCount, wasEncrypted, signatureValidated);
    }

    private static bool HasSignature(XmlDocument document)
        => document.GetElementsByTagName("Signature", NpsXmlNamespaces.XmlDsig).Count > 0;

    /// <summary>
    /// Resolves every <see cref="INpsMessageHandler{TDocument}"/> registered for the
    /// concrete document type and invokes them sequentially.
    /// </summary>
    private async Task<int> DispatchAsync(
        INpsDocument document,
        NpsInboundMessage message,
        CancellationToken cancellationToken)
    {
        var handlerContract = typeof(INpsMessageHandler<>).MakeGenericType(document.GetType());
        var handleAsync = handlerContract.GetMethod(nameof(INpsMessageHandler<INpsDocument>.HandleAsync))
            ?? throw new MissingMethodException(handlerContract.FullName, "HandleAsync");

        var count = 0;
        foreach (var handler in _serviceProvider.GetServices(handlerContract))
        {
            if (handler is null)
                continue;

            try
            {
                await ((Task)handleAsync.Invoke(handler, [document, message, cancellationToken])!)
                    .ConfigureAwait(false);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                throw Wrap(ex.InnerException, handler, message.MessageType);
            }
            catch (Exception ex) when (ex is not OperationCanceledException and not NpsIntegrationException)
            {
                throw Wrap(ex, handler, message.MessageType);
            }

            count++;
        }

        return count;
    }

    private static Exception Wrap(Exception exception, object handler, NpsMessageType messageType)
        => exception switch
        {
            OperationCanceledException or NpsIntegrationException => exception,
            _ => new NpsIntegrationException(
                $"Handler '{handler.GetType().Name}' failed while processing an inbound {messageType} message.",
                exception),
        };
}
