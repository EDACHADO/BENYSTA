namespace Integration.BusinessLogics.Nps.Models;

/// <summary>How the NPS switch answered a dispatched message.</summary>
public enum NpsDispatchStatus
{
    /// <summary>The switch acknowledged receipt (HTTP 2xx). The business outcome arrives later via webhook.</summary>
    Accepted,

    /// <summary>The switch rejected the message at validation level (admi.002).</summary>
    Rejected,

    /// <summary>The switch returned HTTP 400 without a body — it could not decrypt the message (key/algorithm issue).</summary>
    DecryptionFailure,

    /// <summary>The switch could not be reached or returned an unexpected error.</summary>
    DispatchFailed,
}

/// <summary>
/// Outcome of dispatching an ISO 20022 message to the NIBSS NPS switch,
/// as returned by the send commands (credit transfer, name enquiry, status query/report).
/// </summary>
public sealed record NpsDispatchResult
{
    /// <summary>How the switch answered.</summary>
    public required NpsDispatchStatus Status { get; init; }

    /// <summary>The generated ISO 20022 message identifier (GrpHdr/MsgId); correlate webhook callbacks with this.</summary>
    public required string MessageId { get; init; }

    /// <summary>The HTTP status code the switch returned, when it answered at all.</summary>
    public int? NibssStatusCode { get; init; }

    /// <summary>Whether the switch acknowledged receipt (HTTP 2xx). This is not the business outcome.</summary>
    public bool AcknowledgedBySwitch { get; init; }

    /// <summary>The admi.002 rejection reason code, e.g. "DT01", "AC01" (only for <see cref="NpsDispatchStatus.Rejected"/>).</summary>
    public string? ReasonCode { get; init; }

    /// <summary>The MsgId the admi.002 rejection refers to (only for <see cref="NpsDispatchStatus.Rejected"/>).</summary>
    public string? RejectedMessageId { get; init; }

    /// <summary>Human-readable description of the rejection or failure.</summary>
    public string? Error { get; init; }

    public static NpsDispatchResult Accepted(string messageId, int nibssStatusCode, bool acknowledged) => new()
    {
        Status = NpsDispatchStatus.Accepted,
        MessageId = messageId,
        NibssStatusCode = nibssStatusCode,
        AcknowledgedBySwitch = acknowledged,
    };

    public static NpsDispatchResult Rejected(string messageId, string? reasonCode, string? rejectedMessageId, string error) => new()
    {
        Status = NpsDispatchStatus.Rejected,
        MessageId = messageId,
        ReasonCode = reasonCode,
        RejectedMessageId = rejectedMessageId ?? messageId,
        Error = error,
    };

    public static NpsDispatchResult DecryptionFailure(string messageId) => new()
    {
        Status = NpsDispatchStatus.DecryptionFailure,
        MessageId = messageId,
        NibssStatusCode = 400,
        Error = "NPS returned HTTP 400 without a body, which per the integration guide means the switch " +
                "could not decrypt the message. Verify the encryption keys and algorithm.",
    };

    public static NpsDispatchResult Failed(string messageId, string error) => new()
    {
        Status = NpsDispatchStatus.DispatchFailed,
        MessageId = messageId,
        Error = error,
    };
}
