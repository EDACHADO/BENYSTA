using Nibbs.Nps.Integration.Constants;

namespace Nibbs.Nps.Integration.Abstractions;

/// <summary>
/// Marker for the root Document type of an ISO 20022 NPS message.
/// </summary>
public interface INpsDocument
{
    /// <summary>The NPS message type this document represents.</summary>
    NpsMessageType MessageType { get; }
}
