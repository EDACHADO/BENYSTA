using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Constants;

namespace Nibbs.Nps.Integration.Inbound;

/// <summary>
/// A decrypted, signature-verified message delivered by NPS to your institution's
/// inbound callback URL (https://&lt;your-base-url&gt;/&lt;npsMessageType&gt;).
/// </summary>
public class NpsInboundMessage
{
    public NpsMessageType MessageType { get; init; }

    /// <summary>The decrypted plaintext ISO 20022 XML.</summary>
    public string PlainXml { get; init; } = string.Empty;

    /// <summary>
    /// The typed document when the message type has a model
    /// (pacs.008/002/028, acmt.023/024, pain.001/002, admi.002); otherwise null.
    /// </summary>
    public INpsDocument? Document { get; init; }

    /// <summary>Returns the typed document, or throws when it is not of the requested type.</summary>
    public TDocument GetDocument<TDocument>() where TDocument : class, INpsDocument
        => Document as TDocument
           ?? throw new InvalidOperationException(
               $"Inbound {MessageType} message is not available as {typeof(TDocument).Name}.");
}
