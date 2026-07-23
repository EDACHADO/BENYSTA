using Microsoft.Extensions.Options;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Configuration;
using Nibbs.Nps.Integration.Helpers;
using Nibbs.Nps.Integration.Messages.Acmt;
using Nibbs.Nps.Integration.Messages.Common;
using Nibbs.Nps.Integration.Messages.Pacs;
using Nibbs.Nps.Integration.Messages.Pain;
using Nibbs.Nps.Integration.RequestModels;

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

    public Acmt024Document CreateIdVerificationReport(NpsIdVerificationReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = DateTime.UtcNow;
        var messageId = request.MessageId ?? NpsMessageIdGenerator.NewMessageId(options.Value.SourceId, now);
        var source = options.Value.SourceId;

        return new Acmt024Document
        {
            VerificationReport = new IdentificationVerificationReport
            {
                Assignment = new IdentificationAssignment
                {
                    MessageId = messageId,
                    CreationDateTime = NpsFormats.DateTimeUtc(now),
                    Assigner = new PartyOrAgent
                    {
                        Agent = BranchAndFinancialInstitution.ForMember(source, source),
                    },
                    Assignee = new PartyOrAgent
                    {
                        Party = string.IsNullOrEmpty(request.RequestingPartyName)
                            ? null
                            : new Party { Name = request.RequestingPartyName },
                        Agent = BranchAndFinancialInstitution.ForMember(request.RequestingAgentId, request.RequestingAgentId),
                    },
                },
                OriginalAssignment = new OriginalAssignment
                {
                    MessageId = request.OriginalMessageId,
                    CreationDateTime = request.OriginalCreationDateTime,
                },
                Reports =
                [
                    new VerificationReport
                    {
                        OriginalId = request.OriginalMessageId,
                        Verification = request.Verified,
                        OriginalPartyAndAccountId = new PartyAndAccountIdentification
                        {
                            Account = new CashAccount { Id = new AccountIdentification { Iban = request.AccountNumber } },
                        },
                        UpdatedPartyAndAccountId = string.IsNullOrEmpty(request.AccountName)
                            ? null
                            : new PartyAndAccountIdentification { Party = new Party { Name = request.AccountName } },
                    },
                ],
                SupplementaryData = request.AccountHolderInfo is null && string.IsNullOrEmpty(request.RiskRating)
                    ? null
                    : new SupplementaryData
                    {
                        Envelope = new SupplementaryDataEnvelope
                        {
                            CustomData = new CustomData
                            {
                                CreditorInfo = request.AccountHolderInfo,
                                TransactionInfo = string.IsNullOrEmpty(request.RiskRating)
                                    ? null
                                    : new TransactionInfo { RiskRating = request.RiskRating },
                            },
                        },
                    },
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

    public Pain001Document CreateCreditTransferInitiation(NpsCreditTransferInitiationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = DateTime.UtcNow;
        var messageId = request.MessageId ?? NpsMessageIdGenerator.NewMessageId(options.Value.SourceId, now);
        var source = options.Value.SourceId;
        var debtorAgent = request.DebtorAgentId ?? source;
        var amount = NpsFormats.Amount(request.Amount);

        return new Pain001Document
        {
            CreditTransferInitiation = new CustomerCreditTransferInitiation
            {
                GroupHeader = new Pain001GroupHeader
                {
                    MessageId = messageId,
                    CreationDateTime = NpsFormats.DateTimeUtc(now),
                    NumberOfTransactions = "1",
                    ControlSum = amount,
                    InitiatingParty = new InitiatingParty
                    {
                        Name = request.InitiatingPartyName ?? options.Value.InstitutionName,
                        Id = new PartyIdentificationChoice
                        {
                            OrganisationId = new OrganisationIdentification
                            {
                                Other = new GenericIdentification
                                {
                                    SchemeName = new ProprietaryChoice { Code = source },
                                },
                            },
                        },
                    },
                    ForwardingAgent = string.IsNullOrEmpty(request.ForwardingAgentBic)
                        ? null
                        : new BranchAndFinancialInstitution
                        {
                            FinancialInstitutionId = new FinancialInstitutionIdentification { Bicfi = request.ForwardingAgentBic },
                        },
                },
                PaymentInformation = new PaymentInstruction
                {
                    PaymentInformationId = request.PaymentInformationId ?? messageId,
                    BatchBooking = request.BatchBooking,
                    NumberOfTransactions = "1",
                    ControlSum = amount,
                    RequestedExecutionDate = new RequestedExecutionDate
                    {
                        Date = NpsFormats.Date(request.RequestedExecutionDate ?? now),
                    },
                    Debtor = new Party { Name = request.DebtorName },
                    DebtorAccount = CashAccount.ForAccount(request.DebtorAccountNumber, request.DebtorAccountName ?? request.DebtorName),
                    DebtorAgent = BranchAndFinancialInstitution.ForMember(debtorAgent, debtorAgent),
                    ChargeBearer = request.ChargeBearer,
                    Transactions =
                    [
                        new CreditTransferTransactionInformation
                        {
                            PaymentId = new Pain001PaymentIdentification { EndToEndId = request.EndToEndId ?? messageId },
                            Amount = new AmountChoice
                            {
                                InstructedAmount = new CurrencyAndAmount { Currency = request.Currency, Value = amount },
                            },
                            CreditorAgent = BranchAndFinancialInstitution.ForMember(request.CreditorAgentId, request.CreditorAgentId),
                            Creditor = new Party { Name = request.CreditorName },
                            CreditorAccount = CashAccount.ForAccount(request.CreditorAccountNumber, request.CreditorAccountName ?? request.CreditorName),
                            RemittanceInformation = string.IsNullOrEmpty(request.Narration)
                                ? null
                                : new RemittanceInformation { Unstructured = request.Narration },
                        },
                    ],
                },
                SupplementaryData = new SupplementaryData
                {
                    Envelope = new SupplementaryDataEnvelope
                    {
                        CustomData = new CustomData
                        {
                            CreditorInfo = request.CreditorInfo,
                            TransactionInfo = new TransactionInfo
                            {
                                TransactionLocation = request.TransactionLocation,
                                ChannelCode = request.ChannelCode,
                                FixedCollectionAmount = request.FixedCollectionAmount ? "true" : "false",
                                MandateCode = request.MandateCode,
                            },
                        },
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