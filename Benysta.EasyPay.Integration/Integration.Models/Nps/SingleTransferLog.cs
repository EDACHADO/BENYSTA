using Integration.Models.AbstractModel;

namespace Integration.Models.Nps;

/// <summary>
/// Request/response log for a single FI-to-FI credit transfer: a pacs.008 and the
/// pacs.002 payment status report that settles it.
/// </summary>
/// <remarks>
/// Correlation: <see cref="RequestUniqueId"/> is the pacs.008 <c>GrpHdr/MsgId</c>, echoed by
/// the pacs.002 in <c>OrgnlGrpInfAndSts/OrgnlMsgId</c>. <see cref="TransactionId"/>,
/// <see cref="EndToEndId"/> and <see cref="InstructionId"/> are echoed in
/// <c>TxInfAndSts/OrgnlTxId</c>, <c>OrgnlEndToEndId</c> and <c>OrgnlInstrId</c>, giving the
/// handler three fallbacks when the group id is absent. Prefer TxId — the guide only
/// guarantees uniqueness there, explicitly not for EndToEndId.
/// <para>
/// On <see cref="NpsLogDirection.Inbound"/> NIBSS pushed us a pacs.008 and the response
/// columns hold the pacs.002 we sent back via /api/nibbs/payment-status-report.
/// </para>
/// </remarks>
public class SingleTransferLog : BaseAudit
{
    /// <summary>ULID primary key. See <see cref="EntityId"/>.</summary>
    public string SingleTransferLogId { get; set; }

    public NpsLogDirection Direction { get; set; }

    #region Request identity

    /// <summary>pacs.008 <c>GrpHdr/MsgId</c> — the 35-character correlation key.</summary>
    public string RequestUniqueId { get; set; }

    /// <summary>
    /// pacs.008 <c>GrpHdr/CreDtTm</c> as the exact wire string. Required verbatim when
    /// raising a pacs.028 status query or answering with a pacs.002.
    /// </summary>
    public string RequestCreationDateTime { get; set; }

    /// <summary>pacs.008 <c>PmtId/InstrId</c>.</summary>
    public string InstructionId { get; set; }

    /// <summary>pacs.008 <c>PmtId/EndToEndId</c>. Not guaranteed unique by NPS.</summary>
    public string EndToEndId { get; set; }

    /// <summary>pacs.008 <c>PmtId/TxId</c> — the unique transaction identifier.</summary>
    public string TransactionId { get; set; }

    #endregion

    #region Money

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "NGN";

    /// <summary>
    /// pacs.008 <c>IntrBkSttlmDt</c> as the wire string; a pacs.028 query must quote it
    /// exactly as sent.
    /// </summary>
    public string SettlementDate { get; set; }

    /// <summary>Transaction type code per the NPS TTC dictionary.</summary>
    public string TransactionTypeCode { get; set; }

    #endregion

    #region Debtor

    public string DebtorBankCode { get; set; }
    public string DebtorAccountNumber { get; set; }
    public string DebtorAccountName { get; set; }
    public string DebtorName { get; set; }

    #endregion

    #region Creditor

    /// <summary>NPS member id of the beneficiary institution (<c>CdtrAgt</c>).</summary>
    public string CreditorBankCode { get; set; }

    public string CreditorAccountNumber { get; set; }
    public string CreditorAccountName { get; set; }
    public string CreditorName { get; set; }

    #endregion

    #region Supplementary data

    public string Narration { get; set; }

    /// <summary>Channel code: 1 bank teller, 2 internet banking, 3 mobile app, and so on.</summary>
    public string ChannelCode { get; set; }

    public string TransactionLocation { get; set; }

    /// <summary>
    /// MsgId of the preceding acmt.023, matching <see cref="NameEnquiryLog.RequestUniqueId"/>.
    /// Left as a loose reference rather than an FK: the enquiry may have been performed
    /// outside this service.
    /// </summary>
    public string NameEnquiryMessageId { get; set; }

    public string RiskRating { get; set; }

    #endregion

    #region Payloads

    /// <summary>The API request body as submitted by the caller, serialized to JSON.</summary>
    public string RequestJson { get; set; }

    /// <summary>Compact JSON summary of the switch's synchronous reply.</summary>
    public string NibbsResponse { get; set; }

    /// <summary>Compact JSON summary of the pacs.002 webhook, parsed into its business fields.</summary>
    public string WebhookResponse { get; set; }

    #endregion

    #region Synchronous dispatch outcome

    public NpsLogStatus Status { get; set; } = NpsLogStatus.Pending;

    public int? NibbsHttpStatusCode { get; set; }

    public bool AcknowledgedBySwitch { get; set; }

    /// <summary>admi.002 rejection reason code.</summary>
    public string RejectionReasonCode { get; set; }

    public string ErrorMessage { get; set; }

    #endregion

    #region pacs.002 outcome

    /// <summary>pacs.002 <c>GrpHdr/MsgId</c>.</summary>
    public string ResponseMessageId { get; set; }

    /// <summary>pacs.002 <c>OrgnlGrpInfAndSts/GrpSts</c>.</summary>
    public string GroupStatus { get; set; }

    /// <summary>pacs.002 <c>TxInfAndSts/TxSts</c> — ACSC on success, RJCT on rejection.</summary>
    public string TransactionStatus { get; set; }

    /// <summary>pacs.002 <c>TxInfAndSts/StsId</c> — AUTH or NAUTH.</summary>
    public string StatusId { get; set; }

    /// <summary>pacs.002 <c>StsRsnInf/Rsn/Prtry</c> — mandatory when the payment is not accepted.</summary>
    public string StatusReasonCode { get; set; }

    /// <summary>pacs.002 <c>StsRsnInf/AddtlInf</c>.</summary>
    public string StatusReasonInformation { get; set; }

    #endregion

    #region pacs.028 status queries

    /// <summary>How many pacs.028 status queries have been raised against this payment.</summary>
    public int StatusQueryCount { get; set; }

    public DateTime? LastStatusQueryAt { get; set; }

    /// <summary>MsgId of the most recent pacs.028 raised for this payment.</summary>
    public string LastStatusQueryMessageId { get; set; }

    #endregion

    #region Timings

    public DateTime? RequestSentAt { get; set; }
    public DateTime? NibbsRespondedAt { get; set; }
    public DateTime? WebhookReceivedAt { get; set; }

    /// <summary>Webhook delivery count; anything above 1 is a redelivery, not a state change.</summary>
    public int WebhookCount { get; set; }

    #endregion
}
