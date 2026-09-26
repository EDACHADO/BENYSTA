using Integration.Models.AbstractModel;

namespace Integration.Models.Nps;

/// <summary>
/// Request/response log for the NPS Identification Verification (name enquiry) flow:
/// an acmt.023 request and the acmt.024 report that answers it.
/// </summary>
/// <remarks>
/// Correlation: <see cref="RequestUniqueId"/> is the acmt.023 <c>Assgnmt/MsgId</c>. The
/// acmt.024 echoes it back in <c>OrgnlAssgnmt/MsgId</c> and in every <c>Rpt/OrgnlId</c>,
/// so the webhook handler finds this row by (Direction, RequestUniqueId).
/// <para>
/// On <see cref="NpsLogDirection.Outbound"/> we asked another participant to verify an
/// account; on <see cref="NpsLogDirection.Inbound"/> NIBSS pushed us an acmt.023 and the
/// response columns hold the acmt.024 we replied with.
/// </para>
/// </remarks>
public class NameEnquiryLog : BaseAudit
{
    /// <summary>ULID primary key. See <see cref="EntityId"/>.</summary>
    public string NameEnquiryLogId { get; set; }

    public NpsLogDirection Direction { get; set; }

    #region Request identity

    /// <summary>acmt.023 <c>Assgnmt/MsgId</c> — the 35-character correlation key.</summary>
    public string RequestUniqueId { get; set; }

    /// <summary>
    /// acmt.023 <c>Vrfctn/Id</c>. Same value as the MsgId in the NIBSS samples, but kept
    /// separately because the acmt.024 reports quote it per verified account.
    /// </summary>
    public string VerificationId { get; set; }

    /// <summary>
    /// acmt.023 <c>Assgnmt/CreDtTm</c>, stored as the exact string that went on the wire.
    /// The acmt.024 must echo it verbatim in <c>OrgnlAssgnmt/CreDtTm</c>, so a round-tripped
    /// DateTime is not good enough here.
    /// </summary>
    public string RequestCreationDateTime { get; set; }

    #endregion

    #region Account under enquiry

    /// <summary>
    /// NPS member id of the institution holding the account — the acmt.023 <c>Assgne</c>
    /// agent (<c>ClrSysMmbId/MmbId</c>).
    /// </summary>
    public string BankCode { get; set; }

    /// <summary>The NUBAN being verified (acmt.023 <c>Vrfctn/PtyAndAcctId/Acct/Id/Othr/Id</c>).</summary>
    public string AccountNumber { get; set; }

    /// <summary>Optional expected account name supplied on the request (<c>PtyAndAcctId/Pty/Nm</c>).</summary>
    public string RequestedPartyName { get; set; }

    #endregion

    #region Counterparty

    /// <summary>NPS member id of the institution that raised the enquiry (<c>Assgnr</c>).</summary>
    public string RequestingBankCode { get; set; }

    /// <summary>Name of the institution that raised the enquiry (<c>Assgnr/Pty/Nm</c>).</summary>
    public string RequestingPartyName { get; set; }

    #endregion

    #region Payloads

    /// <summary>The API request body as submitted by the caller, serialized to JSON.</summary>
    public string RequestJson { get; set; }

    /// <summary>
    /// Compact JSON summary of the switch's synchronous reply (HTTP status, acknowledgement
    /// flag, admi.002 reason code). The ISO 20022 XML itself is deliberately not stored.
    /// </summary>
    public string NibbsResponse { get; set; }

    /// <summary>Compact JSON summary of the acmt.024 webhook, parsed into its business fields.</summary>
    public string WebhookResponse { get; set; }

    #endregion

    #region Synchronous dispatch outcome

    public NpsLogStatus Status { get; set; } = NpsLogStatus.Pending;

    /// <summary>HTTP status code the switch returned, when it answered at all.</summary>
    public int? NibbsHttpStatusCode { get; set; }

    /// <summary>Whether the switch acknowledged receipt (HTTP 2xx). Not a business outcome.</summary>
    public bool AcknowledgedBySwitch { get; set; }

    /// <summary>admi.002 rejection reason code, e.g. "DT01" or "AC01".</summary>
    public string RejectionReasonCode { get; set; }

    /// <summary>Human-readable dispatch or rejection failure detail.</summary>
    public string ErrorMessage { get; set; }

    #endregion

    #region acmt.024 outcome

    /// <summary>acmt.024 <c>Assgnmt/MsgId</c> — the responder's own message id.</summary>
    public string ResponseMessageId { get; set; }

    /// <summary>acmt.024 <c>Rpt/Vrfctn</c> — null until the report arrives.</summary>
    public bool? Verified { get; set; }

    /// <summary>Resolved name on the account (<c>UpdtdPtyAndAcctId/Pty/Nm</c>).</summary>
    public string ResolvedAccountName { get; set; }

    /// <summary>SplmtryData KYC: 1 individual, 2 corporate.</summary>
    public string AccountDesignation { get; set; }

    /// <summary>SplmtryData KYC: BVN, NIN, JTBTIN, FIRSTIN or RC Number.</summary>
    public string IdType { get; set; }

    /// <summary>SplmtryData KYC identifier value.</summary>
    public string IdValue { get; set; }

    /// <summary>SplmtryData KYC account tier.</summary>
    public string AccountTier { get; set; }

    /// <summary>Risk rating shared in the supplementary block.</summary>
    public string RiskRating { get; set; }

    #endregion

    #region Timings

    public DateTime? RequestSentAt { get; set; }
    public DateTime? NibbsRespondedAt { get; set; }
    public DateTime? WebhookReceivedAt { get; set; }

    /// <summary>
    /// How many webhook callbacks have landed on this row. NPS may redeliver, so the
    /// handler should treat anything above 1 as a duplicate rather than a state change.
    /// </summary>
    public int WebhookCount { get; set; }

    #endregion
}
