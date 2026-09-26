namespace Integration.Models.Nps;

/// <summary>
/// Lifecycle of a logged NPS exchange, from dispatch through to the asynchronous
/// outcome. Per the integration guide an HTTP 2xx from the switch only acknowledges
/// receipt, so <see cref="Acknowledged"/> is not a business outcome — the row stays
/// there until the matching webhook (acmt.024 / pacs.002 / pain.002) arrives.
/// </summary>
public enum NpsLogStatus
{
    /// <summary>Row written, message not yet handed to the switch.</summary>
    Pending = 1,

    /// <summary>The switch returned HTTP 2xx. Awaiting the webhook carrying the real outcome.</summary>
    Acknowledged = 2,

    /// <summary>The switch rejected the message at validation level (admi.002).</summary>
    Rejected = 3,

    /// <summary>HTTP 400 with no body — the switch could not decrypt the message.</summary>
    DecryptionFailed = 4,

    /// <summary>The switch could not be reached, timed out, or returned an unexpected error.</summary>
    DispatchFailed = 5,

    /// <summary>Webhook confirmed success (acmt.024 Vrfctn true, or TxSts ACSC).</summary>
    Successful = 6,

    /// <summary>Webhook reported failure (acmt.024 Vrfctn false, or TxSts RJCT).</summary>
    Failed = 7,

    /// <summary>Bulk only — some transactions in the batch succeeded and some did not.</summary>
    PartiallySuccessful = 8,

    /// <summary>No webhook arrived within the reconciliation window; resolve via pacs.028.</summary>
    Expired = 9,

    /// <summary>
    /// The webhook reported ACCP/ACSP/ACTC — accepted for processing but not yet settled.
    /// A further report is expected, so this is not a terminal state.
    /// </summary>
    AcceptedForProcessing = 10
}
