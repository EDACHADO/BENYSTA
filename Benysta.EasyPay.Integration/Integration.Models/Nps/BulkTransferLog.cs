using Integration.Models.AbstractModel;

namespace Integration.Models.Nps;

/// <summary>
/// Request/response log for a bulk customer credit transfer: a pain.001 initiation and
/// the pain.002 customer payment status report that reports its outcome.
/// </summary>
/// <remarks>
/// Correlation happens at two levels, because pain.002 reports per transaction:
/// <list type="bullet">
/// <item>Batch — <see cref="RequestUniqueId"/> (<c>GrpHdr/MsgId</c>) and
/// <see cref="PaymentInformationId"/> (<c>PmtInfId</c>), echoed in the pain.002
/// <c>OrgnlGrpInfAndSts/OrgnlMsgId</c> and <c>OrgnlPmtInfAndSts/OrgnlPmtInfId</c>.</item>
/// <item>Transaction — each <see cref="BulkTransferItemLog.EndToEndId"/> is echoed in one
/// <c>OrgnlPmtInfAndSts/TxInfAndSts/OrgnlEndToEndId</c> entry, each carrying its own
/// <c>TxSts</c>. A single pain.002 therefore updates this row plus N item rows.</item>
/// </list>
/// </remarks>
public class BulkTransferLog : BaseAudit
{
    /// <summary>ULID primary key. See <see cref="EntityId"/>.</summary>
    public string BulkTransferLogId { get; set; }

    public NpsLogDirection Direction { get; set; }

    #region Request identity

    /// <summary>pain.001 <c>GrpHdr/MsgId</c> — the 35-character correlation key.</summary>
    public string RequestUniqueId { get; set; }

    /// <summary>pain.001 <c>GrpHdr/CreDtTm</c> as the exact wire string.</summary>
    public string RequestCreationDateTime { get; set; }

    /// <summary>pain.001 <c>PmtInf/PmtInfId</c> — the second correlation key on the pain.002.</summary>
    public string PaymentInformationId { get; set; }

    #endregion

    #region Batch totals

    /// <summary>pain.001 <c>NbOfTxs</c> — how many transactions were submitted.</summary>
    public int NumberOfTransactions { get; set; }

    /// <summary>pain.001 <c>CtrlSum</c> — total of all instructed amounts.</summary>
    public decimal ControlSum { get; set; }

    public string Currency { get; set; } = "NGN";

    /// <summary>pain.001 <c>BtchBookg</c> — batch booking indicator.</summary>
    public bool BatchBooking { get; set; }

    /// <summary>pain.001 <c>ReqdExctnDt/Dt</c> as the wire string.</summary>
    public string RequestedExecutionDate { get; set; }

    /// <summary>pain.001 <c>ChrgBr</c> — SLEV unless otherwise agreed.</summary>
    public string ChargeBearer { get; set; }

    #endregion

    #region Initiating party and debtor

    /// <summary>pain.001 <c>InitgPty/Nm</c>.</summary>
    public string InitiatingPartyName { get; set; }

    /// <summary>pain.001 <c>FwdgAgt/FinInstnId/BICFI</c>; omitted from the message when null.</summary>
    public string ForwardingAgentBic { get; set; }

    public string DebtorBankCode { get; set; }
    public string DebtorAccountNumber { get; set; }
    public string DebtorAccountName { get; set; }
    public string DebtorName { get; set; }

    #endregion

    #region Supplementary data

    public string ChannelCode { get; set; }

    public string TransactionLocation { get; set; }

    /// <summary>pain.001 SplmtryData <c>FixedCollectionAmount</c>.</summary>
    public bool FixedCollectionAmount { get; set; }

    /// <summary>pain.001 SplmtryData <c>MandateCode</c>.</summary>
    public string MandateCode { get; set; }

    #endregion

    #region Payloads

    /// <summary>The API request body as submitted by the caller, serialized to JSON.</summary>
    public string RequestJson { get; set; }

    /// <summary>Compact JSON summary of the switch's synchronous reply.</summary>
    public string NibbsResponse { get; set; }

    /// <summary>
    /// Compact JSON summary of the batch-level part of the pain.002. Per-transaction
    /// outcomes live on <see cref="BulkTransferItemLog.WebhookResponse"/>.
    /// </summary>
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

    #region pain.002 outcome

    /// <summary>pain.002 <c>GrpHdr/MsgId</c>.</summary>
    public string ResponseMessageId { get; set; }

    /// <summary>pain.002 <c>OrgnlGrpInfAndSts/GrpSts</c>.</summary>
    public string GroupStatus { get; set; }

    /// <summary>
    /// Rolled up from the item rows as pain.002 transactions land, so batch progress can be
    /// read without aggregating the child table.
    /// </summary>
    public int SuccessfulTransactionCount { get; set; }

    public int FailedTransactionCount { get; set; }

    public int PendingTransactionCount { get; set; }

    /// <summary>Total value of the transactions that reached ACSC.</summary>
    public decimal SuccessfulAmount { get; set; }

    #endregion

    #region Timings

    public DateTime? RequestSentAt { get; set; }
    public DateTime? NibbsRespondedAt { get; set; }

    /// <summary>When the first pain.002 for this batch arrived.</summary>
    public DateTime? WebhookReceivedAt { get; set; }

    /// <summary>
    /// How many pain.002 callbacks have landed. NPS may report a batch across several
    /// reports, so unlike the single-transfer log this can legitimately exceed 1.
    /// </summary>
    public int WebhookCount { get; set; }

    #endregion

    /// <summary>The individual credit transfers in the batch, one per <c>CdtTrfTxInf</c>.</summary>
    public ICollection<BulkTransferItemLog> Items { get; set; } = [];
}
