using System.Xml.Serialization;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Constants;
using Nibbs.Nps.Integration.Messages.Common;

namespace Nibbs.Nps.Integration.Messages.Acmt;

/// <summary>
/// acmt.024 — Identification Verification Report. Sent by the account-holding bank
/// in response to an acmt.023 request, carrying the verification result and the
/// account name plus supplementary KYC details.
/// </summary>
[XmlRoot("Document", Namespace = NpsXmlNamespaces.Acmt024)]
public class Acmt024Document : INpsDocument
{
    [XmlIgnore]
    public NpsMessageType MessageType => NpsMessageType.Acmt024;

    [XmlElement("IdVrfctnRpt", Namespace = "")]
    public IdentificationVerificationReport VerificationReport { get; set; }
}

public class IdentificationVerificationReport
{
    [XmlElement("Assgnmt")]
    public IdentificationAssignment Assignment { get; set; }

    /// <summary>OrgnlAssgnmt — MsgId/CreDtTm of the acmt.023 request being answered.</summary>
    [XmlElement("OrgnlAssgnmt")]
    public OriginalAssignment OriginalAssignment { get; set; }

    [XmlElement("Rpt")]
    public List<VerificationReport> Reports { get; set; }

    [XmlElement("SplmtryData")]
    public SupplementaryData SupplementaryData { get; set; }
}

public class OriginalAssignment
{
    [XmlElement("MsgId")]
    public string MessageId { get; set; }

    [XmlElement("CreDtTm")]
    public string CreationDateTime { get; set; }
}

public class VerificationReport
{
    /// <summary>OrgnlId — MsgId of the acmt.023 request received.</summary>
    [XmlElement("OrgnlId")]
    public string OriginalId { get; set; }

    /// <summary>Vrfctn — verification outcome (true/false).</summary>
    [XmlElement("Vrfctn")]
    public bool Verification { get; set; }

    /// <summary>OrgnlPtyAndAcctId — the account that was verified.</summary>
    [XmlElement("OrgnlPtyAndAcctId")]
    public PartyAndAccountIdentification OriginalPartyAndAccountId { get; set; }

    /// <summary>UpdtdPtyAndAcctId — the resolved name on the account.</summary>
    [XmlElement("UpdtdPtyAndAcctId")]
    public PartyAndAccountIdentification UpdatedPartyAndAccountId { get; set; }
}
