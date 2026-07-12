using System.Xml.Serialization;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Constants;
using Nibbs.Nps.Integration.Messages.Common;

namespace Nibbs.Nps.Integration.Messages.Pacs;

/// <summary>
/// pacs.002 — FI to FI Payment Status Report. Communicates the outcome (ACSC/RJCT)
/// of a previously received pacs.008/pacs.003, or answers a pacs.028 status request.
/// </summary>
[XmlRoot("Document", Namespace = NpsXmlNamespaces.Pacs002)]
public class Pacs002Document : INpsDocument
{
    [XmlIgnore]
    public NpsMessageType MessageType => NpsMessageType.Pacs002;

    [XmlElement("FIToFIPmtStsRpt", Namespace = "")]
    public FIToFIPaymentStatusReport StatusReport { get; set; }
}

public class FIToFIPaymentStatusReport
{
    [XmlElement("GrpHdr")]
    public StatusGroupHeader GroupHeader { get; set; }

    [XmlElement("OrgnlGrpInfAndSts")]
    public OriginalGroupInformationAndStatus OriginalGroupInformation { get; set; }

    [XmlElement("TxInfAndSts")]
    public TransactionInformationAndStatus TransactionInformation { get; set; }
}

/// <summary>Group header shared by pacs.002 and pacs.028.</summary>
public class StatusGroupHeader
{
    [XmlElement("MsgId")]
    public string MessageId { get; set; }

    [XmlElement("CreDtTm")]
    public string CreationDateTime { get; set; }

    [XmlElement("InstgAgt")]
    public BranchAndFinancialInstitution InstructingAgent { get; set; }

    [XmlElement("InstdAgt")]
    public BranchAndFinancialInstitution InstructedAgent { get; set; }
}

public class OriginalGroupInformationAndStatus
{
    /// <summary>OrgnlMsgId — MsgId of the original pacs.008 (GrpHdr/MsgId).</summary>
    [XmlElement("OrgnlMsgId")]
    public string OriginalMessageId { get; set; }

    /// <summary>OrgnlMsgNmId — e.g. "pacs.008.001.12".</summary>
    [XmlElement("OrgnlMsgNmId")]
    public string OriginalMessageNameId { get; set; }

    /// <summary>OrgnlCreDtTm — CreDtTm of the original message.</summary>
    [XmlElement("OrgnlCreDtTm")]
    public string OriginalCreationDateTime { get; set; }

    /// <summary>GrpSts — group status, e.g. ACSC or RJCT. See <see cref="TransactionStatus"/>.</summary>
    [XmlElement("GrpSts")]
    public string GroupStatus { get; set; }
}

public class TransactionInformationAndStatus
{
    /// <summary>StsId — AUTH / NAUTH, set by the reporting bank.</summary>
    [XmlElement("StsId")]
    public string StatusId { get; set; }

    [XmlElement("OrgnlInstrId")]
    public string OriginalInstructionId { get; set; }

    [XmlElement("OrgnlEndToEndId")]
    public string OriginalEndToEndId { get; set; }

    [XmlElement("OrgnlTxId")]
    public string OriginalTransactionId { get; set; }

    /// <summary>TxSts — transaction status (present on NPS-generated reports).</summary>
    [XmlElement("TxSts")]
    public string TransactionStatus { get; set; }

    /// <summary>StsRsnInf — status reason (mandatory when the payment is not accepted).</summary>
    [XmlElement("StsRsnInf")]
    public StatusReasonInformation StatusReason { get; set; }

    [XmlElement("InstgAgt")]
    public BranchAndFinancialInstitution InstructingAgent { get; set; }

    [XmlElement("InstdAgt")]
    public BranchAndFinancialInstitution InstructedAgent { get; set; }

    [XmlElement("OrgnlTxRef")]
    public OriginalTransactionReference OriginalTransactionReference { get; set; }
}

public class StatusReasonInformation
{
    [XmlElement("Rsn")]
    public ProprietaryChoice Reason { get; set; }

    [XmlElement("AddtlInf")]
    public string AdditionalInformation { get; set; }
}

public class OriginalTransactionReference
{
    /// <summary>IntrBkSttlmDt — settlement date of the original payment.</summary>
    [XmlElement("IntrBkSttlmDt")]
    public string InterbankSettlementDate { get; set; }
}
