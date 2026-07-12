using Nibbs.Nps.Integration.Inbound;

namespace Nibbs.Nps.Integration.Abstractions;

/// <summary>
/// Application hook for reacting to a specific inbound NPS message type pushed by
/// NIBSS to your institution's callback URL (e.g. a pacs.008 credit transfer or an
/// acmt.023 name enquiry). Register implementations with
/// <c>services.AddNpsMessageHandler&lt;THandler, TDocument&gt;()</c>; the
/// <see cref="INpsWebhookProcessor"/> resolves and invokes every handler registered
/// for the concrete document type of the decrypted message.
/// </summary>
/// <typeparam name="TDocument">
/// The ISO 20022 document type to handle, e.g. <c>Pacs008Document</c>.
/// </typeparam>
public interface INpsMessageHandler<in TDocument> where TDocument : class, INpsDocument
{
    /// <summary>
    /// Handles a decrypted, signature-verified inbound document.
    /// Keep the work fast — NPS expects an immediate HTTP 200 acknowledgment;
    /// offload long-running processing to a queue or background job.
    /// </summary>
    /// <param name="document">The typed ISO 20022 document.</param>
    /// <param name="context">
    /// The full inbound message (message type, decrypted plaintext XML).
    /// </param>
    /// <param name="cancellationToken">Aborts the handling operation.</param>
    Task HandleAsync(TDocument document, NpsInboundMessage context, CancellationToken cancellationToken = default);
}
