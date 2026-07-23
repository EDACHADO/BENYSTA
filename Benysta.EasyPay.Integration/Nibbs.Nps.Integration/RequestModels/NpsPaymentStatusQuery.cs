namespace Nibbs.Nps.Integration.RequestModels;

/// <summary>Input for building a pacs.028 status enquiry.</summary>
public class NpsPaymentStatusQuery
{
    public string MessageId { get; set; }

    /// <summary>NPS member id of the counterparty institution on the original payment.</summary>
    public string CounterpartyId { get; set; }

    /// <summary>GrpHdr/MsgId of the original message.</summary>
    public string OriginalMessageId { get; set; }

    public string OriginalMessageNameId { get; set; } = "pacs.008.001.12";

    /// <summary>GrpHdr/CreDtTm of the original message.</summary>
    public string OriginalCreationDateTime { get; set; }

    /// <summary>CdtTrfTxInf/TxId of the original message.</summary>
    public string OriginalTransactionId { get; set; }

    /// <summary>IntrBkSttlmDt of the original payment (ISO-8601 date).</summary>
    public string OriginalSettlementDate { get; set; }
}
