using Nibbs.Nps.Integration.Messages.Common;

namespace Nibbs.Nps.Integration.RequestModels;

/// <summary>Input for building a pacs.008 credit transfer.</summary>
public class NpsCreditTransferRequest
{
    /// <summary>Optional pre-generated 35-character MsgId; generated when null.</summary>
    public string MessageId { get; set; }

    public string InstructionId { get; set; }
    public string EndToEndId { get; set; }

    public decimal Amount { get; set; }
    public DateTime? SettlementDate { get; set; }

    /// <summary>NPS member id of the beneficiary institution.</summary>
    public string CreditorAgentId { get; set; } = string.Empty;

    public string DebtorName { get; set; } = string.Empty;
    public string DebtorAccountNumber { get; set; } = string.Empty;
    public string DebtorAccountName { get; set; }

    public string CreditorName { get; set; } = string.Empty;
    public string CreditorAccountNumber { get; set; } = string.Empty;
    public string CreditorAccountName { get; set; }

    /// <summary>Narration, max 140 characters.</summary>
    public string Narration { get; set; }

    /// <summary>Transaction type code per the NPS TTC dictionary; default "001".</summary>
    public string TransactionTypeCode { get; set; } = "001";

    public PartyVerificationInfo DebtorInfo { get; set; }
    public PartyVerificationInfo CreditorInfo { get; set; }

    public string TransactionLocation { get; set; }
    public string NameEnquiryMessageId { get; set; }
    public string ChannelCode { get; set; }
    public string RiskRating { get; set; }
}
