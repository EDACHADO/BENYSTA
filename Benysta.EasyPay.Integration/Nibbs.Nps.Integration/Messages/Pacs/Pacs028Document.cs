using System.Xml.Serialization;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Constants;
using Nibbs.Nps.Integration.Messages.Common;

namespace Nibbs.Nps.Integration.Messages.Pacs;

/// <summary>
/// pacs.028 — FI to FI Payment Status Request. Used to query the final status of a
/// previously submitted pacs.008/pacs.002 when the expected response was not
/// received within the timeout defined in the Operations Guide. NPS answers with pacs.002
/// (or HTTP 404 when the original transaction is not found).
/// </summary>
[XmlRoot("Document", Namespace = NpsXmlNamespaces.Pacs028)]
public class Pacs028Document : INpsDocument
{
    [XmlIgnore]
    public NpsMessageType MessageType => NpsMessageType.Pacs028;

    [XmlElement("FIToFIPmtStsReq", Namespace = "")]
    public FIToFIPaymentStatusRequest StatusRequest { get; set; }
}

public class FIToFIPaymentStatusRequest
{
    [XmlElement("GrpHdr")]
    public StatusGroupHeader GroupHeader { get; set; }

    [XmlElement("OrgnlGrpInf")]
    public OriginalGroupInformation OriginalGroupInformation { get; set; }

    [XmlElement("TxInf")]
    public StatusRequestTransactionInformation TransactionInformation { get; set; }
}

public class OriginalGroupInformation
{
    /// <summary>OrgnlMsgId — as taken from the original pacs.008 GrpHdr/MsgId.</summary>
    [XmlElement("OrgnlMsgId")]
    public string OriginalMessageId { get; set; }

    /// <summary>OrgnlMsgNmId — e.g. "pacs.008.001.12".</summary>
    [XmlElement("OrgnlMsgNmId")]
    public string OriginalMessageNameId { get; set; }

    /// <summary>OrgnlCreDtTm — as taken from the original pacs.008 GrpHdr/CreDtTm.</summary>
    [XmlElement("OrgnlCreDtTm")]
    public string OriginalCreationDateTime { get; set; }
}

public class StatusRequestTransactionInformation
{
    /// <summary>StsReqId — status request id; same value as the message's MsgId.</summary>
    [XmlElement("StsReqId")]
    public string StatusRequestId { get; set; }

    /// <summary>OrgnlTxId — as taken from the original pacs.008 CdtTrfTxInf/TxId.</summary>
    [XmlElement("OrgnlTxId")]
    public string OriginalTransactionId { get; set; }

    [XmlElement("InstgAgt")]
    public BranchAndFinancialInstitution InstructingAgent { get; set; }

    [XmlElement("InstdAgt")]
    public BranchAndFinancialInstitution InstructedAgent { get; set; }

    [XmlElement("OrgnlTxRef")]
    public OriginalTransactionReference OriginalTransactionReference { get; set; }
}
