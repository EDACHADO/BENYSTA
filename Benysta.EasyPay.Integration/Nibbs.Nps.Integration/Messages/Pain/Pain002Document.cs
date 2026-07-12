using System.Xml.Serialization;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Constants;
using Nibbs.Nps.Integration.Messages.Common;
using Nibbs.Nps.Integration.Messages.Pacs;

namespace Nibbs.Nps.Integration.Messages.Pain;

/// <summary>
/// pain.002 — Customer Payment Status Report. Communicates the combined outcome
/// (Successful / Partial / Failed) of a pain.001 or pain.008 instruction.
/// </summary>
[XmlRoot("Document", Namespace = NpsXmlNamespaces.Pain002)]
public class Pain002Document : INpsDocument
{
    [XmlIgnore]
    public NpsMessageType MessageType => NpsMessageType.Pain002;

    [XmlElement("CstmrPmtStsRpt", Namespace = "")]
    public CustomerPaymentStatusReport StatusReport { get; set; }
}

public class CustomerPaymentStatusReport
{
    [XmlElement("GrpHdr")]
    public Pain002GroupHeader GroupHeader { get; set; }

    [XmlElement("OrgnlGrpInfAndSts")]
    public OriginalGroupInformationAndStatus OriginalGroupInformation { get; set; }

    [XmlElement("OrgnlPmtInfAndSts")]
    public OriginalPaymentInformationAndStatus OriginalPaymentInformation { get; set; }
}

public class Pain002GroupHeader
{
    [XmlElement("MsgId")]
    public string MessageId { get; set; }

    [XmlElement("CreDtTm")]
    public string CreationDateTime { get; set; }

    [XmlElement("InitgPty")]
    public Party InitiatingParty { get; set; }

    [XmlElement("DbtrAgt")]
    public BranchAndFinancialInstitution DebtorAgent { get; set; }
}

public class OriginalPaymentInformationAndStatus
{
    /// <summary>OrgnlPmtInfId — PmtInfId of the original pain.001.</summary>
    [XmlElement("OrgnlPmtInfId")]
    public string OriginalPaymentInformationId { get; set; }

    [XmlElement("TxInfAndSts")]
    public List<PaymentTransactionStatus> Transactions { get; set; }
}

public class PaymentTransactionStatus
{
    [XmlElement("StsId")]
    public string StatusId { get; set; }

    [XmlElement("OrgnlEndToEndId")]
    public string OriginalEndToEndId { get; set; }

    /// <summary>TxSts — e.g. ACSC, ACCP, RJCT. See <see cref="TransactionStatus"/>.</summary>
    [XmlElement("TxSts")]
    public string TransactionStatus { get; set; }

    [XmlElement("StsRsnInf")]
    public StatusReasonInformation StatusReason { get; set; }
}
