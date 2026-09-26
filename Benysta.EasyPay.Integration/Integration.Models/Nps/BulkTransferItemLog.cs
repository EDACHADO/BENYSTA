using Integration.Models.AbstractModel;

namespace Integration.Models.Nps;

/// <summary>
/// One credit transfer inside a <see cref="BulkTransferLog"/> batch — a single pain.001
/// <c>CdtTrfTxInf</c> and the <c>TxInfAndSts</c> entry that reports its outcome.
/// </summary>
/// <remarks>
/// <see cref="EndToEndId"/> is the correlation key: it is the only identifier a pain.002
/// quotes per transaction (<c>OrgnlEndToEndId</c>), so it is held unique across the whole
/// table rather than merely unique within the batch. Generate it; do not let the caller
/// supply it, or a repeated value will make the webhook ambiguous.
/// </remarks>
public class BulkTransferItemLog : BaseAudit
{
    /// <summary>ULID primary key. See <see cref="EntityId"/>.</summary>
    public string BulkTransferItemLogId { get; set; }

    /// <summary>FK to the owning <see cref="BulkTransferLog"/>.</summary>
    public string BulkTransferLogId { get; set; }

    public BulkTransferLog BulkTransfer { get; set; }

    /// <summary>Position within the batch as submitted, so the batch can be replayed in order.</summary>
    public int ItemSequence { get; set; }

    #region Transaction identity

    /// <summary>
    /// pain.001 <c>CdtTrfTxInf/PmtId/EndToEndId</c> — the identifier the pain.002 reports
    /// this transaction status against.
    /// </summary>
    public string EndToEndId { get; set; }

    /// <summary>pain.001 <c>CdtTrfTxInf/PmtId/InstrId</c>, when supplied.</summary>
    public string InstructionId { get; set; }

    #endregion

    #region Money and beneficiary

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "NGN";

    /// <summary>NPS member id of the beneficiary institution (<c>CdtrAgt</c>).</summary>
    public string CreditorBankCode { get; set; }

    public string CreditorAccountNumber { get; set; }
    public string CreditorAccountName { get; set; }
    public string CreditorName { get; set; }

    public string Narration { get; set; }

    #endregion

    #region Beneficiary KYC (SplmtryData CreditorInfo)

    public string AccountDesignation { get; set; }
    public string IdType { get; set; }
    public string IdValue { get; set; }
    public string AccountTier { get; set; }

    /// <summary>MsgId of the acmt.023 that verified this beneficiary, when one was performed.</summary>
    public string NameEnquiryMessageId { get; set; }

    #endregion

    #region pain.002 outcome

    public NpsLogStatus Status { get; set; } = NpsLogStatus.Pending;

    /// <summary>pain.002 <c>TxInfAndSts/TxSts</c> — ACSC, ACCP or RJCT.</summary>
    public string TransactionStatus { get; set; }

    /// <summary>pain.002 <c>TxInfAndSts/StsId</c>.</summary>
    public string StatusId { get; set; }

    /// <summary>pain.002 <c>StsRsnInf/Rsn/Prtry</c>.</summary>
    public string StatusReasonCode { get; set; }

    /// <summary>pain.002 <c>StsRsnInf/AddtlInf</c>.</summary>
    public string StatusReasonInformation { get; set; }

    /// <summary>Compact JSON summary of the <c>TxInfAndSts</c> entry for this transaction.</summary>
    public string WebhookResponse { get; set; }

    public DateTime? WebhookReceivedAt { get; set; }

    /// <summary>Webhook delivery count for this transaction; above 1 means a redelivery.</summary>
    public int WebhookCount { get; set; }

    #endregion
}
