using System.Xml.Serialization;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Constants;
using Nibbs.Nps.Integration.Messages.Common;

namespace Nibbs.Nps.Integration.Messages.Pain;

/// <summary>
/// pain.001 — Customer Credit Transfer Initiation. Submitted to the NIBSS Institution
/// (directly over NPS or via the API Gateway) to execute a customer-to-institution
/// credit transfer. The final outcome is delivered asynchronously as pain.002.
/// </summary>
[XmlRoot("Document", Namespace = NpsXmlNamespaces.Pain001)]
public class Pain001Document : INpsDocument
{
    [XmlIgnore]
    public NpsMessageType MessageType => NpsMessageType.Pain001;

    [XmlElement("CstmrCdtTrfInitn", Namespace = "")]
    public CustomerCreditTransferInitiation CreditTransferInitiation { get; set; }
}

public class CustomerCreditTransferInitiation
{
    [XmlElement("GrpHdr")]
    public Pain001GroupHeader GroupHeader { get; set; }

    [XmlElement("PmtInf")]
    public PaymentInstruction PaymentInformation { get; set; }

    [XmlElement("SplmtryData")]
    public SupplementaryData SupplementaryData { get; set; }
}

public class Pain001GroupHeader
{
    [XmlElement("MsgId")]
    public string MessageId { get; set; }

    [XmlElement("CreDtTm")]
    public string CreationDateTime { get; set; }

    [XmlElement("NbOfTxs")]
    public string NumberOfTransactions { get; set; } = "1";

    /// <summary>CtrlSum — total of all transaction amounts, 2 decimal places.</summary>
    [XmlElement("CtrlSum")]
    public string ControlSum { get; set; }

    [XmlElement("InitgPty")]
    public InitiatingParty InitiatingParty { get; set; }

    [XmlElement("FwdgAgt")]
    public BranchAndFinancialInstitution ForwardingAgent { get; set; }
}

public class InitiatingParty
{
    [XmlElement("Nm")]
    public string Name { get; set; }

    [XmlElement("Id")]
    public PartyIdentificationChoice Id { get; set; }
}

public class PartyIdentificationChoice
{
    [XmlElement("OrgId")]
    public OrganisationIdentification OrganisationId { get; set; }
}

public class OrganisationIdentification
{
    [XmlElement("Othr")]
    public GenericIdentification Other { get; set; }
}

public class GenericIdentification
{
    [XmlElement("Id")]
    public string Id { get; set; }

    [XmlElement("SchmeNm")]
    public ProprietaryChoice SchemeName { get; set; }
}

public class PaymentInstruction
{
    [XmlElement("PmtInfId")]
    public string PaymentInformationId { get; set; }

    /// <summary>PmtMtd — payment method, fixed "TRF".</summary>
    [XmlElement("PmtMtd")]
    public string PaymentMethod { get; set; } = "TRF";

    [XmlElement("BtchBookg")]
    public bool BatchBooking { get; set; }

    [XmlElement("NbOfTxs")]
    public string NumberOfTransactions { get; set; } = "1";

    [XmlElement("CtrlSum")]
    public string ControlSum { get; set; }

    [XmlElement("ReqdExctnDt")]
    public RequestedExecutionDate RequestedExecutionDate { get; set; }

    [XmlElement("Dbtr")]
    public Party Debtor { get; set; }

    [XmlElement("DbtrAcct")]
    public CashAccount DebtorAccount { get; set; }

    [XmlElement("DbtrAgt")]
    public BranchAndFinancialInstitution DebtorAgent { get; set; }

    [XmlElement("ChrgBr")]
    public string ChargeBearer { get; set; } = Constants.ChargeBearer.FollowingServiceLevel;

    [XmlElement("CdtTrfTxInf")]
    public List<CreditTransferTransactionInformation> Transactions { get; set; }
}

public class RequestedExecutionDate
{
    /// <summary>Dt — requested execution date, ISO-8601 date.</summary>
    [XmlElement("Dt")]
    public string Date { get; set; }
}

public class CreditTransferTransactionInformation
{
    [XmlElement("PmtId")]
    public Pain001PaymentIdentification PaymentId { get; set; }

    [XmlElement("Amt")]
    public AmountChoice Amount { get; set; }

    [XmlElement("CdtrAgt")]
    public BranchAndFinancialInstitution CreditorAgent { get; set; }

    [XmlElement("Cdtr")]
    public Party Creditor { get; set; }

    [XmlElement("CdtrAcct")]
    public CashAccount CreditorAccount { get; set; }

    [XmlElement("RmtInf")]
    public RemittanceInformation RemittanceInformation { get; set; }
}

public class Pain001PaymentIdentification
{
    [XmlElement("EndToEndId")]
    public string EndToEndId { get; set; }
}

public class AmountChoice
{
    /// <summary>InstdAmt — instructed amount and currency.</summary>
    [XmlElement("InstdAmt")]
    public CurrencyAndAmount InstructedAmount { get; set; }
}
