using System.Net;
using Nibbs.Nps.Integration.Exceptions;
using Nibbs.Nps.Integration.Messages.Admi;

namespace Nibbs.Nps.Integration.Client;

/// <summary>
/// Outcome of posting a message to the NPS switch.
/// A success only acknowledges receipt — business outcomes (e.g. pacs.002) arrive
/// asynchronously on your institution's inbound callback URL.
/// </summary>
public class NpsResponse
{
    public HttpStatusCode StatusCode { get; init; }

    /// <summary>True when the switch acknowledged the message (HTTP 2xx).</summary>
    public bool IsSuccess { get; init; }

    /// <summary>The response body exactly as received (possibly signed/encrypted XML).</summary>
    public string RawBody { get; init; }

    /// <summary>The decrypted response body, when a body was present and could be unprotected.</summary>
    public string PlainBody { get; init; }

    /// <summary>
    /// The admi.002 Message Reject details when the switch returned a validation
    /// rejection (HTTP 400 with a response body).
    /// </summary>
    public Admi002Document Rejection { get; init; }

    /// <summary>
    /// True for an HTTP 400 without a response body — per the guide this means the
    /// switch could not decrypt the message (an encryption/key issue).
    /// </summary>
    public bool IsDecryptionFailure => StatusCode == HttpStatusCode.BadRequest && string.IsNullOrEmpty(RawBody);

    /// <summary>Throws <see cref="NpsMessageRejectedException"/> when the switch rejected the message.</summary>
    public NpsResponse EnsureAccepted()
    {
        if (Rejection is not null)
            throw new NpsMessageRejectedException(Rejection);
        if (!IsSuccess)
            throw new NpsIntegrationException(
                IsDecryptionFailure
                    ? "NPS returned HTTP 400 without a body: the switch could not decrypt the message. Verify the encryption keys and algorithm."
                    : $"NPS returned HTTP {(int)StatusCode}.");
        return this;
    }
}
