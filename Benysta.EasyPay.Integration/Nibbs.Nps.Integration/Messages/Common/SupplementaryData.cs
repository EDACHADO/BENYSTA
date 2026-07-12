using System.Xml.Serialization;

namespace Nibbs.Nps.Integration.Messages.Common;

/// <summary>
/// SplmtryData — NPS proprietary supplementary block carrying KYC and transaction metadata.
/// </summary>
public class SupplementaryData
{
    /// <summary>PlcAndNm — fixed value "AdditionalVerificationDetails" on NPS.</summary>
    [XmlElement("PlcAndNm")]
    public string PlaceAndName { get; set; } = "AdditionalVerificationDetails";

    [XmlElement("Envlp")]
    public SupplementaryDataEnvelope Envelope { get; set; }
}

public class SupplementaryDataEnvelope
{
    [XmlElement("CustomData")]
    public CustomData CustomData { get; set; }
}

/// <summary>NPS custom data: debtor/creditor KYC details and transaction metadata.</summary>
public class CustomData
{
    [XmlElement("DebtorInfo")]
    public PartyVerificationInfo DebtorInfo { get; set; }

    [XmlElement("DebtorMetadata")]
    public PartyMetadata DebtorMetadata { get; set; }

    [XmlElement("CreditorInfo")]
    public PartyVerificationInfo CreditorInfo { get; set; }

    [XmlElement("CreditorMetadata")]
    public PartyMetadata CreditorMetadata { get; set; }

    [XmlElement("TransactionInfo")]
    public TransactionInfo TransactionInfo { get; set; }
}

/// <summary>KYC identification details of a debtor or creditor.</summary>
public class PartyVerificationInfo
{
    /// <summary>1 = individual, 2 = corporate. See <see cref="Constants.AccountDesignation"/>.</summary>
    [XmlElement("AccountDesignation")]
    public string AccountDesignation { get; set; }

    /// <summary>BVN, NIN, JTBTIN, FIRSTIN or RC Number. See <see cref="Constants.NpsIdType"/>.</summary>
    [XmlElement("IdType")]
    public string IdType { get; set; }

    [XmlElement("IdValue")]
    public string IdValue { get; set; }

    [XmlElement("AccountTier")]
    public string AccountTier { get; set; }
}

/// <summary>Free-form metadata container (DebtorMetadata / CreditorMetadata).</summary>
public class PartyMetadata
{
    [XmlAnyElement]
    public System.Xml.XmlElement[] Items { get; set; }
}

/// <summary>Transaction metadata carried in the supplementary block.</summary>
public class TransactionInfo
{
    /// <summary>Geo-coordinates or location reference of the transaction.</summary>
    [XmlElement("TransactionLocation")]
    public string TransactionLocation { get; set; }

    /// <summary>MsgId of the preceding acmt.023 name enquiry, when one was performed.</summary>
    [XmlElement("NameEnquiryMsgId")]
    public string NameEnquiryMessageId { get; set; }

    /// <summary>Channel code. See <see cref="Constants.NpsChannelCode"/>.</summary>
    [XmlElement("ChannelCode")]
    public string ChannelCode { get; set; }

    [XmlElement("RiskRating")]
    public string RiskRating { get; set; }

    /// <summary>pain.001/pain.008 only — fixed collection amount indicator ("true"/"false").</summary>
    [XmlElement("FixedCollectionAmount")]
    public string FixedCollectionAmount { get; set; }

    /// <summary>pain.001/pain.008 only — mandate reference code.</summary>
    [XmlElement("MandateCode")]
    public string MandateCode { get; set; }
}
