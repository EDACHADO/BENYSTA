using Microsoft.Extensions.Options;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Configuration;
using Nibbs.Nps.Integration.Constants;
using Nibbs.Nps.Integration.Helpers;
using Nibbs.Nps.Integration.Messages.Acmt;
using Nibbs.Nps.Integration.Messages.Common;
using Nibbs.Nps.Integration.Messages.Pacs;

namespace Nibbs.Nps.Integration.Messages;

public class NpsMessageFactory(IOptions<NpsOptions> options) : INpsMessageFactory
{


    public Pacs008Document CreateCreditTransfer(NpsCreditTransferRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = DateTime.UtcNow;
        var messageId = request.MessageId ?? NpsMessageIdGenerator.NewMessageId(options.Value.SourceId, now);
        var source = options.Value.SourceId;
        var destination = request.CreditorAgentId;

        return new Pacs008Document
        {
            CreditTransfer = new FIToFICustomerCreditTransfer
            {
                GroupHeader = new Pacs008GroupHeader
                {
                    MessageId = messageId,
                    CreationDateTime = NpsFormats.DateTimeUtc(now),
                    BatchBooking = false,
                    NumberOfTransactions = "1",
                    SettlementInformation = new SettlementInformation(),
                    InstructingAgent = BranchAndFinancialInstitution.ForMember(source, source),
                    InstructedAgent = BranchAndFinancialInstitution.ForMember(destination),
                },
                Transaction = new CreditTransferTransaction
                {
                    PaymentId = new PaymentIdentification
                    {
                        InstructionId = request.InstructionId ?? messageId,
                        EndToEndId = request.EndToEndId ?? messageId,
                        TransactionId = messageId,
                    },
                    PaymentTypeInformation = new PaymentTypeInformation
                    {
                        CategoryPurpose = new ProprietaryChoice { Proprietary = request.TransactionTypeCode },
                    },
                    InterbankSettlementAmount = CurrencyAndAmount.Naira(request.Amount),
                    InterbankSettlementDate = NpsFormats.Date(request.SettlementDate ?? now),
                    InstructingAgent = BranchAndFinancialInstitution.ForMember(source),
                    InstructedAgent = BranchAndFinancialInstitution.ForMember(destination),
                    Debtor = new Party { Name = request.DebtorName },
                    DebtorAccount = CashAccount.ForAccount(request.DebtorAccountNumber, request.DebtorAccountName ?? request.DebtorName),
                    DebtorAgent = BranchAndFinancialInstitution.ForMember(source),
                    CreditorAgent = BranchAndFinancialInstitution.ForMember(destination),
                    Creditor = new Party { Name = request.CreditorName },
                    CreditorAccount = CashAccount.ForAccount(request.CreditorAccountNumber, request.CreditorAccountName ?? request.CreditorName),
                    RemittanceInformation = string.IsNullOrEmpty(request.Narration)
                        ? null
                        : new RemittanceInformation { Unstructured = request.Narration },
                },
                SupplementaryData = new SupplementaryData
                {
                    Envelope = new SupplementaryDataEnvelope
                    {
                        CustomData = new CustomData
                        {
                            DebtorInfo = request.DebtorInfo,
                            CreditorInfo = request.CreditorInfo,
                            TransactionInfo = new TransactionInfo
                            {
                                TransactionLocation = request.TransactionLocation,
                                NameEnquiryMessageId = request.NameEnquiryMessageId ?? string.Empty,
                                ChannelCode = request.ChannelCode,
                                RiskRating = request.RiskRating,
                            },
                        },
                    },
                },
            },
        };
    }

    public Acmt023Document CreateIdVerificationRequest(NpsIdVerificationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = DateTime.UtcNow;
        var messageId = request.MessageId ?? NpsMessageIdGenerator.NewMessageId(options.Value.SourceId, now);
        var source = options.Value.SourceId;

        return new Acmt023Document
        {
            VerificationRequest = new IdentificationVerificationRequest
            {
                Assignment = new IdentificationAssignment
                {
                    MessageId = messageId,
                    CreationDateTime = NpsFormats.DateTimeUtc(now),
                    Creator = new PartyOrAgent { Party = new Party { Name = options.Value.InstitutionName } },
                    Assigner = new PartyOrAgent
                    {
                        Party = new Party { Name = options.Value.InstitutionName },
                        Agent = BranchAndFinancialInstitution.ForMember(source, source),
                    },
                    Assignee = new PartyOrAgent
                    {
                        Agent = BranchAndFinancialInstitution.ForMember(request.AccountAgentId, request.AccountAgentId),
                    },
                },
                Verifications =
                [
                    new IdentificationVerification
                    {
                        Id = messageId,
                        PartyAndAccountId = new PartyAndAccountIdentification
                        {
                            Party = string.IsNullOrEmpty(request.PartyName) ? null : new Party { Name = request.PartyName },
                            Account = new CashAccount { Id = new AccountIdentification { Iban = request.AccountNumber } },
                        },
                    },
                ],
            },
        };
    }

    public Pacs002Document CreatePaymentStatusReport(NpsPaymentStatusReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = DateTime.UtcNow;
        var messageId = request.MessageId ?? NpsMessageIdGenerator.NewMessageId(options.Value.SourceId, now);
        var source = options.Value.SourceId;

        return new Pacs002Document
        {
            StatusReport = new FIToFIPaymentStatusReport
            {
                GroupHeader = new StatusGroupHeader
                {
                    MessageId = messageId,
                    CreationDateTime = NpsFormats.DateTimeUtc(now),
                    InstructingAgent = BranchAndFinancialInstitution.ForMember(source, source),
                    InstructedAgent = BranchAndFinancialInstitution.ForMember(request.OriginalSenderId, request.OriginalSenderId),
                },
                OriginalGroupInformation = new OriginalGroupInformationAndStatus
                {
                    OriginalMessageId = request.OriginalMessageId,
                    OriginalMessageNameId = request.OriginalMessageNameId,
                    OriginalCreationDateTime = request.OriginalCreationDateTime,
                    GroupStatus = request.Status,
                },
                TransactionInformation = new TransactionInformationAndStatus
                {
                    StatusId = request.StatusId,
                    OriginalInstructionId = request.OriginalInstructionId,
                    OriginalEndToEndId = request.OriginalEndToEndId,
                    OriginalTransactionId = request.OriginalTransactionId,
                    StatusReason = string.IsNullOrEmpty(request.ReasonCode)
                        ? null
                        : new StatusReasonInformation
                        {
                            Reason = new ProprietaryChoice { Code = request.ReasonCode },
                            AdditionalInformation = request.ReasonInformation,
                        },
                    InstructingAgent = BranchAndFinancialInstitution.ForMember(source, source),
                    InstructedAgent = BranchAndFinancialInstitution.ForMember(request.OriginalSenderId, request.OriginalSenderId),
                    OriginalTransactionReference = new OriginalTransactionReference
                    {
                        InterbankSettlementDate = request.OriginalSettlementDate,
                    },
                },
            },
        };
    }

    public Pacs028Document CreatePaymentStatusRequest(NpsPaymentStatusQuery request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = DateTime.UtcNow;
        var messageId = request.MessageId ?? NpsMessageIdGenerator.NewMessageId(options.Value.SourceId, now);
        var source = options.Value.SourceId;

        return new Pacs028Document
        {
            StatusRequest = new FIToFIPaymentStatusRequest
            {
                GroupHeader = new StatusGroupHeader
                {
                    MessageId = messageId,
                    CreationDateTime = NpsFormats.DateTimeUtc(now),
                    InstructingAgent = BranchAndFinancialInstitution.ForMember(source),
                },
                OriginalGroupInformation = new OriginalGroupInformation
                {
                    OriginalMessageId = request.OriginalMessageId,
                    OriginalMessageNameId = request.OriginalMessageNameId,
                    OriginalCreationDateTime = request.OriginalCreationDateTime,
                },
                TransactionInformation = new StatusRequestTransactionInformation
                {
                    StatusRequestId = messageId,
                    OriginalTransactionId = request.OriginalTransactionId,
                    InstructingAgent = BranchAndFinancialInstitution.ForMember(source, source),
                    InstructedAgent = BranchAndFinancialInstitution.ForMember(request.CounterpartyId, request.CounterpartyId),
                    OriginalTransactionReference = new OriginalTransactionReference
                    {
                        InterbankSettlementDate = request.OriginalSettlementDate,
                    },
                },
            },
        };
    }
}

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

/// <summary>Input for building an acmt.023 name enquiry.</summary>
public class NpsIdVerificationRequest
{
    public string MessageId { get; set; }

    /// <summary>NPS member id of the institution holding the account.</summary>
    public string AccountAgentId { get; set; } = string.Empty;

    /// <summary>The 10-digit account number to verify.</summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>Optional expected name of the account holder.</summary>
    public string PartyName { get; set; }
}

/// <summary>Input for building a pacs.002 answering an inbound pacs.008.</summary>
public class NpsPaymentStatusReportRequest
{
    public string MessageId { get; set; }

    /// <summary>NPS member id of the institution that sent the original payment.</summary>
    public string OriginalSenderId { get; set; } = string.Empty;

    /// <summary>GrpHdr/MsgId of the original pacs.008.</summary>
    public string OriginalMessageId { get; set; } = string.Empty;

    public string OriginalMessageNameId { get; set; } = "pacs.008.001.12";

    /// <summary>GrpHdr/CreDtTm of the original pacs.008.</summary>
    public string OriginalCreationDateTime { get; set; } = string.Empty;

    public string OriginalInstructionId { get; set; }
    public string OriginalEndToEndId { get; set; }
    public string OriginalTransactionId { get; set; }

    /// <summary>IntrBkSttlmDt of the original payment (ISO-8601 date).</summary>
    public string OriginalSettlementDate { get; set; }

    /// <summary>ACSC to approve, RJCT to decline. See <see cref="TransactionStatus"/>.</summary>
    public string Status { get; set; } = TransactionStatus.AcceptedSettlementCompleted;

    /// <summary>AUTH / NAUTH. See <see cref="Constants.StatusId"/>.</summary>
    public string StatusId { get; set; } = Constants.StatusId.Authorized;

    /// <summary>Reject reason code (registered in the NPS dictionaries), required for RJCT.</summary>
    public string ReasonCode { get; set; }

    public string ReasonInformation { get; set; }
}

/// <summary>Input for building a pacs.028 status enquiry.</summary>
public class NpsPaymentStatusQuery
{
    public string MessageId { get; set; }

    /// <summary>NPS member id of the counterparty institution on the original payment.</summary>
    public string CounterpartyId { get; set; } = string.Empty;

    /// <summary>GrpHdr/MsgId of the original message.</summary>
    public string OriginalMessageId { get; set; } = string.Empty;

    public string OriginalMessageNameId { get; set; } = "pacs.008.001.12";

    /// <summary>GrpHdr/CreDtTm of the original message.</summary>
    public string OriginalCreationDateTime { get; set; } = string.Empty;

    /// <summary>CdtTrfTxInf/TxId of the original message.</summary>
    public string OriginalTransactionId { get; set; } = string.Empty;

    /// <summary>IntrBkSttlmDt of the original payment (ISO-8601 date).</summary>
    public string OriginalSettlementDate { get; set; }
}
