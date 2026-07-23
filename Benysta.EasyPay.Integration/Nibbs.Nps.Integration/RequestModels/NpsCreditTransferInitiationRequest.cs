using Nibbs.Nps.Integration.Messages.Common;

namespace Nibbs.Nps.Integration.RequestModels;

/// <summary>
/// Input for building a pain.001 Customer Credit Transfer Initiation per the NPS
/// integration guide. The final outcome is delivered asynchronously as pain.002.
/// </summary>
public class NpsCreditTransferInitiationRequest
{
    /// <summary>Optional pre-generated 35-character MsgId; generated when null.</summary>
    public string MessageId { get; set; }

    /// <summary>PmtInfId — payment information identification; defaults to the MsgId.</summary>
    public string PaymentInformationId { get; set; }

    /// <summary>EndToEndId (max 35 characters); defaults to the MsgId.</summary>
    public string EndToEndId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "NGN";

    /// <summary>ReqdExctnDt — requested execution date; defaults to today.</summary>
    public DateTime? RequestedExecutionDate { get; set; }

    /// <summary>BtchBookg — batch booking indicator; false for a single instruction.</summary>
    public bool BatchBooking { get; set; }

    /// <summary>ChrgBr — charge bearer; SLEV per the guide unless agreed otherwise.</summary>
    public string ChargeBearer { get; set; } = Constants.ChargeBearer.FollowingServiceLevel;

    /// <summary>InitgPty/Nm — defaults to the configured institution name.</summary>
    public string InitiatingPartyName { get; set; }

    /// <summary>FwdgAgt/FinInstnId/BICFI — optional; the element is omitted when null.</summary>
    public string ForwardingAgentBic { get; set; }

    public string DebtorName { get; set; } = string.Empty;
    public string DebtorAccountNumber { get; set; } = string.Empty;
    public string DebtorAccountName { get; set; }

    /// <summary>NPS member id of the debtor's institution; defaults to the configured SourceId.</summary>
    public string DebtorAgentId { get; set; }

    /// <summary>NPS member id of the beneficiary institution.</summary>
    public string CreditorAgentId { get; set; } = string.Empty;

    public string CreditorName { get; set; } = string.Empty;
    public string CreditorAccountNumber { get; set; } = string.Empty;
    public string CreditorAccountName { get; set; }

    /// <summary>Narration (RmtInf/Ustrd), max 140 characters; optional.</summary>
    public string Narration { get; set; }

    /// <summary>SplmtryData creditor KYC block (AccountDesignation, IdType, IdValue, AccountTier) — mandatory per the guide.</summary>
    public PartyVerificationInfo CreditorInfo { get; set; }

    /// <summary>Transaction location coordinates or reference — mandatory per the guide.</summary>
    public string TransactionLocation { get; set; }

    /// <summary>Channel code (1 bank teller, 2 internet banking, 3 mobile app, …) — mandatory per the guide.</summary>
    public string ChannelCode { get; set; }

    /// <summary>Fixed collection amount indicator — mandatory per the guide.</summary>
    public bool FixedCollectionAmount { get; set; }

    /// <summary>Mandate reference code; optional.</summary>
    public string MandateCode { get; set; }
}
