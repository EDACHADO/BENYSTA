using Nibbs.Nps.Integration.Constants;

namespace Nibbs.Nps.Integration.RequestModels;

/// <summary>Input for building a pacs.002 answering an inbound pacs.008.</summary>
public class NpsPaymentStatusReportRequest
{
    public string MessageId { get; set; }

    /// <summary>NPS member id of the institution that sent the original payment.</summary>
    public string OriginalSenderId { get; set; }

    /// <summary>GrpHdr/MsgId of the original pacs.008.</summary>
    public string OriginalMessageId { get; set; } 

    public string OriginalMessageNameId { get; set; } = "pacs.008.001.12";

    /// <summary>GrpHdr/CreDtTm of the original pacs.008.</summary>
    public string OriginalCreationDateTime { get; set; }

    public string OriginalInstructionId { get; set; }
    public string OriginalEndToEndId { get; set; }
    public string OriginalTransactionId { get; set; }

    /// <summary>IntrBkSttlmDt of the original payment (ISO-8601 date).</summary>
    public string OriginalSettlementDate { get; set; }

    /// <summary>ACSC to approve, RJCT to decline. See <see cref="TransactionStatus"/>.</summary>
    public string Status { get; set; } = TransactionStatus.AcceptedSettlementCompleted;

    /// <summary>AUTH / NAUTH. See <see cref="Constants.StatusId"/>.</summary>
    public string StatusId { get; set; } = Constants.StatusId.Authorized;

    /// <summary>Reject reason code (registered in the NPS dictionaries), required for RJCT.</summary>
    public string ReasonCode { get; set; }

    public string ReasonInformation { get; set; }
}
