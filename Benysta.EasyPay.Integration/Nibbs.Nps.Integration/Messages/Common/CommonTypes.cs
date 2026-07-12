using System.Xml.Serialization;

namespace Nibbs.Nps.Integration.Messages.Common;

/// <summary>ClrSysMmbId — clearing system member identification.</summary>
public class ClearingSystemMemberIdentification
{
    /// <summary>MmbId — NPS member identifier of the institution, e.g. "999058".</summary>
    [XmlElement("MmbId")]
    public string MemberId { get; set; }
}

/// <summary>FinInstnId — financial institution identification.</summary>
public class FinancialInstitutionIdentification
{
    /// <summary>BICFI — BIC of the institution. NPS typically carries the member id here too.</summary>
    [XmlElement("BICFI")]
    public string Bicfi { get; set; }

    [XmlElement("ClrSysMmbId")]
    public ClearingSystemMemberIdentification ClearingSystemMemberId { get; set; }

    /// <summary>Emits an empty &lt;BICFI/&gt; element when <see cref="Bicfi"/> is null, matching the NPS samples.</summary>
    public static FinancialInstitutionIdentification ForMember(string memberId, string bicfi = null) => new()
    {
        Bicfi = bicfi,
        ClearingSystemMemberId = new ClearingSystemMemberIdentification { MemberId = memberId },
    };
}

/// <summary>
/// Agent element (InstgAgt / InstdAgt / DbtrAgt / CdtrAgt / Agt / FwdgAgt) wrapping FinInstnId.
/// </summary>
public class BranchAndFinancialInstitution
{
    [XmlElement("FinInstnId")]
    public FinancialInstitutionIdentification FinancialInstitutionId { get; set; }

    public static BranchAndFinancialInstitution ForMember(string memberId, string bicfi = null) => new()
    {
        FinancialInstitutionId = FinancialInstitutionIdentification.ForMember(memberId, bicfi),
    };
}

/// <summary>Simple party (Dbtr / Cdtr / Pty) carrying a name.</summary>
public class Party
{
    [XmlElement("Nm")]
    public string Name { get; set; }
}

/// <summary>Acct / DbtrAcct / CdtrAcct — account with NUBAN carried in the IBAN tag.</summary>
public class CashAccount
{
    [XmlElement("Id")]
    public AccountIdentification Id { get; set; }

    /// <summary>Nm — name on the account.</summary>
    [XmlElement("Nm")]
    public string Name { get; set; }

    public static CashAccount ForAccount(string accountNumber, string accountName = null) => new()
    {
        Id = new AccountIdentification { Iban = accountNumber },
        Name = accountName,
    };
}

/// <summary>Account identification. NPS uses the IBAN tag for the 10-digit NUBAN.</summary>
public class AccountIdentification
{
    /// <summary>IBAN — the 10-digit account number (NUBAN) per the NPS guide.</summary>
    [XmlElement("IBAN")]
    public string Iban { get; set; }
}

/// <summary>Amount element carrying a currency attribute, e.g. IntrBkSttlmAmt / InstdAmt.</summary>
public class CurrencyAndAmount
{
    [XmlAttribute("Ccy")]
    public string Currency { get; set; } = "NGN";

    /// <summary>Amount rendered with two decimal places, e.g. "78000.00".</summary>
    [XmlText]
    public string Value { get; set; }

    public static CurrencyAndAmount Naira(decimal amount) => new()
    {
        Currency = "NGN",
        Value = Helpers.NpsFormats.Amount(amount),
    };
}

/// <summary>SttlmInf — settlement information.</summary>
public class SettlementInformation
{
    /// <summary>SttlmMtd — fixed value CLRG (settlement via clearing).</summary>
    [XmlElement("SttlmMtd")]
    public string SettlementMethod { get; set; } = Constants.SettlementMethod.Clearing;
}

/// <summary>PmtTpInf — payment type information.</summary>
public class PaymentTypeInformation
{
    /// <summary>ClrChanl — fixed value RTNS.</summary>
    [XmlElement("ClrChanl")]
    public string ClearingChannel { get; set; } = Constants.ClearingChannel.RealTimeNetSettlement;

    [XmlElement("SvcLvl")]
    public ProprietaryChoice ServiceLevel { get; set; } = new() { Proprietary = "0100" };

    [XmlElement("LclInstrm")]
    public ProprietaryChoice LocalInstrument { get; set; } = new() { Proprietary = Constants.LocalInstrument.CreditTransferAccountToAccount };

    /// <summary>CtgyPurp — transaction type code as defined in the NPS TTC dictionary.</summary>
    [XmlElement("CtgyPurp")]
    public ProprietaryChoice CategoryPurpose { get; set; } = new() { Proprietary = "001" };
}

/// <summary>Generic Prtry/Cd choice used by SvcLvl, LclInstrm, CtgyPurp, Rsn etc.</summary>
public class ProprietaryChoice
{
    [XmlElement("Cd")]
    public string Code { get; set; }

    [XmlElement("Prtry")]
    public string Proprietary { get; set; }
}

/// <summary>InstrForNxtAgt — instruction for the next agent.</summary>
public class InstructionForNextAgent
{
    [XmlElement("InstrInf")]
    public string InstructionInformation { get; set; }
}

/// <summary>RmtInf — remittance information (narration, max 140 characters).</summary>
public class RemittanceInformation
{
    [XmlElement("Ustrd")]
    public string Unstructured { get; set; }
}
