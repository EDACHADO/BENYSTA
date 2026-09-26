using Integration.Models;
using Integration.Models.Nps;
using Nibbs.Nps.Integration.Messages.Acmt;
using Nibbs.Nps.Integration.Messages.Common;

namespace Integration.BusinessLogics.Nps.Logging;

/// <summary>
/// Projects an acmt.023 identification verification request onto
/// <see cref="NameEnquiryLog"/> rows — one per Vrfctn entry.
/// </summary>
internal static class NameEnquiryLogMapper
{
    /// <summary>
    /// Builds one row per account in the enquiry. An acmt.023 may carry up to 10 Vrfctn
    /// entries; entries without an account number are skipped, since there is nothing an
    /// acmt.024 could later be matched against.
    /// </summary>
    public static List<NameEnquiryLog> FromAcmt023(
        Acmt023Document? document,
        NpsLogDirection direction,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(document);

        var request = document.VerificationRequest;
        var assignment = request?.Assignment;
        var rows = new List<NameEnquiryLog>();

        if (request?.Verifications is not { Count: > 0 } verifications)
            return rows;

        // Assgne holds the institution that owns the account under enquiry; Assgnr holds
        // the institution asking. On an inbound message Assgne is us.
        var accountBankCode = MemberId(assignment?.Assignee?.Agent);
        var requestingBankCode = MemberId(assignment?.Assigner?.Agent);
        var requestingPartyName = assignment?.Assigner?.Party?.Name
                                  ?? assignment?.Creator?.Party?.Name;

        foreach (var verification in verifications)
        {
            var accountNumber = verification?.PartyAndAccountId?.Account?.Id?.Iban;
            if (string.IsNullOrWhiteSpace(accountNumber))
                continue;

            rows.Add(new NameEnquiryLog
            {
                NameEnquiryLogId = EntityId.New(),
                Direction = direction,
                Status = NpsLogStatus.Pending,

                RequestUniqueId = NpsLogMapping.Fit(assignment?.MessageId, NpsLogColumn.MessageId),
                VerificationId = NpsLogMapping.Fit(verification?.Id, NpsLogColumn.MessageId),
                RequestCreationDateTime = NpsLogMapping.Fit(assignment?.CreationDateTime, NpsLogColumn.WireDateTime),

                BankCode = NpsLogMapping.Fit(accountBankCode, NpsLogColumn.BankCode),
                AccountNumber = NpsLogMapping.Fit(accountNumber, NpsLogColumn.AccountNumber),
                RequestedPartyName = NpsLogMapping.Fit(
                    verification?.PartyAndAccountId?.Party?.Name, NpsLogColumn.PartyName),

                RequestingBankCode = NpsLogMapping.Fit(requestingBankCode, NpsLogColumn.BankCode),
                RequestingPartyName = NpsLogMapping.Fit(requestingPartyName, NpsLogColumn.PartyName),

                RequestSentAt = nowUtc,
            });
        }

        return rows;
    }

    /// <summary>
    /// Compact projection of an inbound acmt.023 for the RequestJson column. An inbound
    /// message has no API request body and the plaintext XML is deliberately not stored.
    /// </summary>
    public static string? InboundRequestJson(Acmt023Document? document, string accountNumber)
    {
        var request = document?.VerificationRequest;
        var assignment = request?.Assignment;

        return NpsLogMapping.ToJson(new
        {
            messageId = assignment?.MessageId,
            creationDateTime = assignment?.CreationDateTime,
            creatorName = assignment?.Creator?.Party?.Name,
            assignerBankCode = MemberId(assignment?.Assigner?.Agent),
            assignerName = assignment?.Assigner?.Party?.Name,
            assigneeBankCode = MemberId(assignment?.Assignee?.Agent),
            accountNumber,

            // The enquiry may have covered several accounts; this row is one of them.
            accountsInEnquiry = request?.Verifications?.Count ?? 0,
            verificationId = request?.Verifications?
                .FirstOrDefault(v => v?.PartyAndAccountId?.Account?.Id?.Iban == accountNumber)?.Id,
            requestedPartyName = request?.Verifications?
                .FirstOrDefault(v => v?.PartyAndAccountId?.Account?.Id?.Iban == accountNumber)
                ?.PartyAndAccountId?.Party?.Name,
        });
    }

    private static string? MemberId(BranchAndFinancialInstitution? agent)
        => agent?.FinancialInstitutionId?.ClearingSystemMemberId?.MemberId
           ?? agent?.FinancialInstitutionId?.Bicfi;
}
