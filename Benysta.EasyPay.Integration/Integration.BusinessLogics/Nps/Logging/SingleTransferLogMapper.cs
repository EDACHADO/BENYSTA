using Integration.Models;
using Integration.Models.Nps;
using Nibbs.Nps.Integration.Messages.Pacs;

namespace Integration.BusinessLogics.Nps.Logging;

/// <summary>
/// Projects a pacs.008 document onto a <see cref="SingleTransferLog"/> row.
/// </summary>
/// <remarks>
/// The document is the source of truth rather than the API request model, because it is
/// what actually goes on (or came off) the wire — the factory defaults several
/// identifiers, so the request model alone does not tell you what NIBSS saw.
/// The same extraction serves both directions; only <c>Direction</c> and the payload
/// columns differ.
/// </remarks>
internal static class SingleTransferLogMapper
{
    public static SingleTransferLog FromPacs008(
        Pacs008Document? document,
        NpsLogDirection direction,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(document);

        var transfer = document.CreditTransfer;
        var header = transfer?.GroupHeader;
        var transaction = transfer?.Transaction;
        var paymentId = transaction?.PaymentId;
        var amount = transaction?.InterbankSettlementAmount;
        var supplementary = transfer?.SupplementaryData?.Envelope?.CustomData?.TransactionInfo;

        return new SingleTransferLog
        {
            SingleTransferLogId = EntityId.New(),
            Direction = direction,
            Status = NpsLogStatus.Pending,

            RequestUniqueId = NpsLogMapping.Fit(header?.MessageId, NpsLogColumn.MessageId),
            RequestCreationDateTime = NpsLogMapping.Fit(header?.CreationDateTime, NpsLogColumn.WireDateTime),
            InstructionId = NpsLogMapping.Fit(paymentId?.InstructionId, NpsLogColumn.MessageId),
            EndToEndId = NpsLogMapping.Fit(paymentId?.EndToEndId, NpsLogColumn.MessageId),
            TransactionId = NpsLogMapping.Fit(paymentId?.TransactionId, NpsLogColumn.MessageId),

            Amount = NpsLogMapping.ParseAmount(amount?.Value),
            Currency = NpsLogMapping.Fit(amount?.Currency, NpsLogColumn.Currency),
            SettlementDate = NpsLogMapping.Fit(transaction?.InterbankSettlementDate, NpsLogColumn.WireDateTime),
            TransactionTypeCode = NpsLogMapping.Fit(
                transaction?.PaymentTypeInformation?.CategoryPurpose?.Proprietary, NpsLogColumn.ShortCode),

            DebtorBankCode = NpsLogMapping.Fit(MemberId(transaction?.DebtorAgent), NpsLogColumn.BankCode),
            DebtorAccountNumber = NpsLogMapping.Fit(transaction?.DebtorAccount?.Id?.Iban, NpsLogColumn.AccountNumber),
            DebtorAccountName = NpsLogMapping.Fit(transaction?.DebtorAccount?.Name, NpsLogColumn.PartyName),
            DebtorName = NpsLogMapping.Fit(transaction?.Debtor?.Name, NpsLogColumn.PartyName),

            CreditorBankCode = NpsLogMapping.Fit(MemberId(transaction?.CreditorAgent), NpsLogColumn.BankCode),
            CreditorAccountNumber = NpsLogMapping.Fit(transaction?.CreditorAccount?.Id?.Iban, NpsLogColumn.AccountNumber),
            CreditorAccountName = NpsLogMapping.Fit(transaction?.CreditorAccount?.Name, NpsLogColumn.PartyName),
            CreditorName = NpsLogMapping.Fit(transaction?.Creditor?.Name, NpsLogColumn.PartyName),

            Narration = NpsLogMapping.Fit(transaction?.RemittanceInformation?.Unstructured, NpsLogColumn.Narration),
            ChannelCode = NpsLogMapping.Fit(supplementary?.ChannelCode, NpsLogColumn.ShortCode),
            TransactionLocation = NpsLogMapping.Fit(supplementary?.TransactionLocation, NpsLogColumn.Location),
            NameEnquiryMessageId = NpsLogMapping.Fit(
                NullIfEmpty(supplementary?.NameEnquiryMessageId), NpsLogColumn.MessageId),
            RiskRating = NpsLogMapping.Fit(supplementary?.RiskRating, NpsLogColumn.ShortCode),
        };
    }

    /// <summary>
    /// Compact projection of an inbound pacs.008 for the RequestJson column. An inbound
    /// message has no API request body, and the plaintext XML is deliberately not stored,
    /// so this is the single-field record of what NIBSS delivered.
    /// </summary>
    public static string? InboundRequestJson(Pacs008Document? document)
    {
        var transfer = document?.CreditTransfer;
        var transaction = transfer?.Transaction;
        var supplementary = transfer?.SupplementaryData?.Envelope?.CustomData;

        return NpsLogMapping.ToJson(new
        {
            messageId = transfer?.GroupHeader?.MessageId,
            creationDateTime = transfer?.GroupHeader?.CreationDateTime,
            numberOfTransactions = transfer?.GroupHeader?.NumberOfTransactions,
            instructingAgent = MemberId(transfer?.GroupHeader?.InstructingAgent),
            instructedAgent = MemberId(transfer?.GroupHeader?.InstructedAgent),
            instructionId = transaction?.PaymentId?.InstructionId,
            endToEndId = transaction?.PaymentId?.EndToEndId,
            transactionId = transaction?.PaymentId?.TransactionId,
            amount = transaction?.InterbankSettlementAmount?.Value,
            currency = transaction?.InterbankSettlementAmount?.Currency,
            settlementDate = transaction?.InterbankSettlementDate,
            debtor = new
            {
                name = transaction?.Debtor?.Name,
                account = transaction?.DebtorAccount?.Id?.Iban,
                accountName = transaction?.DebtorAccount?.Name,
                bankCode = MemberId(transaction?.DebtorAgent),
                idType = supplementary?.DebtorInfo?.IdType,
                accountDesignation = supplementary?.DebtorInfo?.AccountDesignation,
                accountTier = supplementary?.DebtorInfo?.AccountTier,
            },
            creditor = new
            {
                name = transaction?.Creditor?.Name,
                account = transaction?.CreditorAccount?.Id?.Iban,
                accountName = transaction?.CreditorAccount?.Name,
                bankCode = MemberId(transaction?.CreditorAgent),
                idType = supplementary?.CreditorInfo?.IdType,
                accountDesignation = supplementary?.CreditorInfo?.AccountDesignation,
                accountTier = supplementary?.CreditorInfo?.AccountTier,
            },
            narration = transaction?.RemittanceInformation?.Unstructured,
            transactionInfo = new
            {
                channelCode = supplementary?.TransactionInfo?.ChannelCode,
                transactionLocation = supplementary?.TransactionInfo?.TransactionLocation,
                nameEnquiryMessageId = supplementary?.TransactionInfo?.NameEnquiryMessageId,
                riskRating = supplementary?.TransactionInfo?.RiskRating,
            },
        });
    }

    private static string? MemberId(Nibbs.Nps.Integration.Messages.Common.BranchAndFinancialInstitution? agent)
        => agent?.FinancialInstitutionId?.ClearingSystemMemberId?.MemberId
           ?? agent?.FinancialInstitutionId?.Bicfi;

    // The factory writes string.Empty rather than null for an absent name enquiry id.
    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
