using System.Xml.Serialization;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Constants;
using Nibbs.Nps.Integration.Messages.Common;

namespace Nibbs.Nps.Integration.Messages.Acmt;

/// <summary>
/// acmt.023 — Identification Verification Request (name enquiry). Sent by the debtor's
/// bank to validate beneficiary account details before initiating a payment.
/// The responding bank answers with acmt.024.
/// </summary>
[XmlRoot("Document", Namespace = NpsXmlNamespaces.Acmt023)]
public class Acmt023Document : INpsDocument
{
    [XmlIgnore]
    public NpsMessageType MessageType => NpsMessageType.Acmt023;

    [XmlElement("IdVrfctnReq", Namespace = "")]
    public IdentificationVerificationRequest VerificationRequest { get; set; }
}

public class IdentificationVerificationRequest
{
    [XmlElement("Assgnmt")]
    public IdentificationAssignment Assignment { get; set; }

    /// <summary>Vrfctn — account(s) to verify (up to 10 per the NPS name enquiry service).</summary>
    [XmlElement("Vrfctn")]
    public List<IdentificationVerification> Verifications { get; set; }
}

public class IdentificationAssignment
{
    /// <summary>MsgId — unique 35-character message reference.</summary>
    [XmlElement("MsgId")]
    public string MessageId { get; set; }

    /// <summary>CreDtTm — creation date-time, ISO-8601.</summary>
    [XmlElement("CreDtTm")]
    public string CreationDateTime { get; set; }

    /// <summary>Cretr — creating party (sending institution name).</summary>
    [XmlElement("Cretr")]
    public PartyOrAgent Creator { get; set; }

    /// <summary>Assgnr — assigning party/agent (the requesting institution).</summary>
    [XmlElement("Assgnr")]
    public PartyOrAgent Assigner { get; set; }

    /// <summary>Assgne — assignee agent (the institution holding the account to verify).</summary>
    [XmlElement("Assgne")]
    public PartyOrAgent Assignee { get; set; }
}

/// <summary>Pty/Agt choice used by Cretr, Assgnr and Assgne.</summary>
public class PartyOrAgent
{
    [XmlElement("Pty")]
    public Party Party { get; set; }

    [XmlElement("Agt")]
    public BranchAndFinancialInstitution Agent { get; set; }
}

public class IdentificationVerification
{
    /// <summary>Id — verification identification; same as MsgId in the samples.</summary>
    [XmlElement("Id")]
    public string Id { get; set; }

    [XmlElement("PtyAndAcctId")]
    public PartyAndAccountIdentification PartyAndAccountId { get; set; }
}

public class PartyAndAccountIdentification
{
    /// <summary>Pty — optional name of the party to be verified.</summary>
    [XmlElement("Pty")]
    public Party Party { get; set; }

    /// <summary>Acct — account of the party to be verified.</summary>
    [XmlElement("Acct")]
    public CashAccount Account { get; set; }
}