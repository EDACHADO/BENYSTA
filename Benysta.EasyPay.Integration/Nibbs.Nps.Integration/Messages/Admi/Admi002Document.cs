using System.Xml.Serialization;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Constants;

namespace Nibbs.Nps.Integration.Messages.Admi;

/// <summary>
/// admi.002 — Message Reject. Returned by NPS (with HTTP 400) when a submitted message
/// fails schema validation, contains invalid/missing fields, violates formatting rules
/// or could not be processed.
/// </summary>
[XmlRoot("Document", Namespace = NpsXmlNamespaces.Admi002)]
public class Admi002Document : INpsDocument
{
    [XmlIgnore]
    public NpsMessageType MessageType => NpsMessageType.Admi002;

    [XmlElement("MsgRjct", Namespace = "")]
    public MessageReject MessageReject { get; set; }
}

public class MessageReject
{
    [XmlElement("RltdRef")]
    public RelatedReference RelatedReference { get; set; }

    [XmlElement("Rsn")]
    public RejectionReason Reason { get; set; }
}

public class RelatedReference
{
    /// <summary>Ref — MsgId of the original message being rejected.</summary>
    [XmlElement("Ref")]
    public string Reference { get; set; }
}

public class RejectionReason
{
    /// <summary>RjctgPtyRsn — rejection reason code (e.g. DT01, AC01) per the scheme dictionary.</summary>
    [XmlElement("RjctgPtyRsn")]
    public string RejectingPartyReason { get; set; }

    /// <summary>RjctnDtTm — date-time of rejection, ISO-8601.</summary>
    [XmlElement("RjctnDtTm")]
    public string RejectionDateTime { get; set; }

    /// <summary>RsnDesc — human-readable description of the rejection reason.</summary>
    [XmlElement("RsnDesc")]
    public string ReasonDescription { get; set; }
}
