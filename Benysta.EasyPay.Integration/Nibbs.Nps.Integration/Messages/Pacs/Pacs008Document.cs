using System.Xml.Serialization;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Constants;
using Nibbs.Nps.Integration.Messages.Common;

namespace Nibbs.Nps.Integration.Messages.Pacs;

/// <summary>
/// pacs.008 — FI to FI Customer Credit Transfer. Sent by the originator (debtor) bank
/// to NPS to move funds to the beneficiary (creditor) bank.
/// </summary>
[XmlRoot("Document", Namespace = NpsXmlNamespaces.Pacs008)]
public class Pacs008Document : INpsDocument
{
    [XmlIgnore]
    public NpsMessageType MessageType => NpsMessageType.Pacs008;

    [XmlElement("FIToFICstmrCdtTrf", Namespace = "")]
    public FIToFICustomerCreditTransfer CreditTransfer { get; set; }
}

public class FIToFICustomerCreditTransfer
{
    [XmlElement("GrpHdr")]
    public Pacs008GroupHeader GroupHeader { get; set; }

    [XmlElement("CdtTrfTxInf")]
    public CreditTransferTransaction Transaction { get; set; }

    [XmlElement("SplmtryData")]
    public SupplementaryData SupplementaryData { get; set; }
}

public class Pacs008GroupHeader
{
    /// <summary>MsgId — unique 35-character message reference.</summary>
    [XmlElement("MsgId")]
    public string MessageId { get; set; }

    /// <summary>CreDtTm — creation date-time, ISO-8601.</summary>
    [XmlElement("CreDtTm")]
    public string CreationDateTime { get; set; }

    /// <summary>BtchBookg — single transfer, fixed "false" on NPS.</summary>
    [XmlElement("BtchBookg")]
    public bool BatchBooking { get; set; }

    /// <summary>NbOfTxs — fixed value 1 (higher values are rejected).</summary>
    [XmlElement("NbOfTxs")]
    public string NumberOfTransactions { get; set; } = "1";

    [XmlElement("SttlmInf")]
    public SettlementInformation SettlementInformation { get; set; } = new();

    /// <summary>InstgAgt — sending participant.</summary>
    [XmlElement("InstgAgt")]
    public BranchAndFinancialInstitution InstructingAgent { get; set; }

    /// <summary>InstdAgt — receiving participant.</summary>
    [XmlElement("InstdAgt")]
    public BranchAndFinancialInstitution InstructedAgent { get; set; }
}

public class CreditTransferTransaction
{
    [XmlElement("PmtId")]
    public PaymentIdentification PaymentId { get; set; }

    [XmlElement("PmtTpInf")]
    public PaymentTypeInformation PaymentTypeInformation { get; set; } = new();

    /// <summary>IntrBkSttlmAmt — payment amount and currency.</summary>
    [XmlElement("IntrBkSttlmAmt")]
    public CurrencyAndAmount InterbankSettlementAmount { get; set; }

    /// <summary>IntrBkSttlmDt — requested settlement date (ISO-8601 date).</summary>
    [XmlElement("IntrBkSttlmDt")]
    public string InterbankSettlementDate { get; set; }

    /// <summary>ChrgBr — charges bearer, fixed SLEV.</summary>
    [XmlElement("ChrgBr")]
    public string ChargeBearer { get; set; } = Constants.ChargeBearer.FollowingServiceLevel;

    [XmlElement("InstgAgt")]
    public BranchAndFinancialInstitution InstructingAgent { get; set; }

    [XmlElement("InstdAgt")]
    public BranchAndFinancialInstitution InstructedAgent { get; set; }

    /// <summary>Dbtr — debtor (sender) details.</summary>
    [XmlElement("Dbtr")]
    public Party Debtor { get; set; }

    [XmlElement("DbtrAcct")]
    public CashAccount DebtorAccount { get; set; }

    [XmlElement("DbtrAgt")]
    public BranchAndFinancialInstitution DebtorAgent { get; set; }

    [XmlElement("CdtrAgt")]
    public BranchAndFinancialInstitution CreditorAgent { get; set; }

    /// <summary>Cdtr — creditor (beneficiary) details.</summary>
    [XmlElement("Cdtr")]
    public Party Creditor { get; set; }

    [XmlElement("CdtrAcct")]
    public CashAccount CreditorAccount { get; set; }

    [XmlElement("InstrForNxtAgt")]
    public List<InstructionForNextAgent> InstructionsForNextAgent { get; set; }

    [XmlElement("RmtInf")]
    public RemittanceInformation RemittanceInformation { get; set; }
}

public class PaymentIdentification
{
    /// <summary>InstrId — instruction identifier (internal sender reference or same as TxId).</summary>
    [XmlElement("InstrId")]
    public string InstructionId { get; set; }

    /// <summary>EndToEndId — end-to-end reference; not verified for uniqueness.</summary>
    [XmlElement("EndToEndId")]
    public string EndToEndId { get; set; }

    /// <summary>TxId — unique transaction identifier; same as MsgId when no internal id exists.</summary>
    [XmlElement("TxId")]
    public string TransactionId { get; set; }
}
