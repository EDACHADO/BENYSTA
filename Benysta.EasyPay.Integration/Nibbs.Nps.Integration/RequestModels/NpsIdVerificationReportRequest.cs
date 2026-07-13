using Nibbs.Nps.Integration.Messages.Common;

namespace Nibbs.Nps.Integration.RequestModels;

/// <summary>
/// Input for building an acmt.024 Identification Verification Report — the answer to
/// an inbound acmt.023 name enquiry, carrying the verification outcome, the resolved
/// account name and the account holder's KYC details.
/// </summary>
public class NpsIdVerificationReportRequest
{
    /// <summary>Optional pre-generated 35-character MsgId; generated when null.</summary>
    public string MessageId { get; set; }

    /// <summary>MsgId of the acmt.023 request being answered (OrgnlAssgnmt/MsgId and Rpt/OrgnlId).</summary>
    public string OriginalMessageId { get; set; } = string.Empty;

    /// <summary>CreDtTm of the acmt.023 request being answered, exactly as received.</summary>
    public string OriginalCreationDateTime { get; set; } = string.Empty;

    /// <summary>NPS member id of the institution that sent the acmt.023 (becomes the Assgne agent).</summary>
    public string RequestingAgentId { get; set; } = string.Empty;

    /// <summary>Name of the requesting institution (Assgne/Pty/Nm); optional.</summary>
    public string RequestingPartyName { get; set; }

    /// <summary>Vrfctn — whether the account details were verified successfully.</summary>
    public bool Verified { get; set; }

    /// <summary>The account number that was verified (echoed in OrgnlPtyAndAcctId).</summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>The resolved name on the account (UpdtdPtyAndAcctId/Pty/Nm); mandatory when verified.</summary>
    public string AccountName { get; set; }

    /// <summary>
    /// SplmtryData KYC block for the account holder (AccountDesignation, IdType, IdValue,
    /// AccountTier) — mandatory for a successful verification per the guide.
    /// </summary>
    public PartyVerificationInfo AccountHolderInfo { get; set; }

    /// <summary>Optional risk rating shared in the supplementary block.</summary>
    public string RiskRating { get; set; }
}
