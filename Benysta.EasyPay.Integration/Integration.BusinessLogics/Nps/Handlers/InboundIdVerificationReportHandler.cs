using Integration.BusinessLogics.Nps.Logging;
using Integration.Infrastructures.Abstractions;
using Integration.Models.Nps;
using Microsoft.Extensions.Logging;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Inbound;
using Nibbs.Nps.Integration.Messages.Acmt;
using Nibbs.Nps.Integration.Messages.Common;

namespace Integration.BusinessLogics.Nps.Handlers;

/// <summary>
/// Applies an inbound acmt.024 identification verification report to the outbound acmt.023
/// it answers, completing the enquiry with the resolved account name and KYC details.
/// </summary>
/// <remarks>
/// An acmt.024 carries one Rpt entry per account that was enquired about, so a single
/// report may complete several rows. Correlation is by the original MsgId — from
/// OrgnlAssgnmt/MsgId, or Rpt/OrgnlId when the former is absent — narrowed to a row by the
/// account number echoed in OrgnlPtyAndAcctId.
/// </remarks>
public sealed class InboundIdVerificationReportHandler(
    INameEnquiryLogStore logStore,
    IDateTimeService clock,
    ILogger<InboundIdVerificationReportHandler> logger)
    : INpsMessageHandler<Acmt024Document>
{
    public async Task HandleAsync(
        Acmt024Document document,
        NpsInboundMessage context,
        CancellationToken cancellationToken = default)
    {
        var report = document?.VerificationReport;
        if (report is null)
        {
            logger.LogWarning("Inbound acmt.024 had no IdVrfctnRpt payload; nothing to apply.");
            return;
        }

        if (report.Reports is not { Count: > 0 } reports)
        {
            logger.LogWarning(
                "Inbound acmt.024 {MessageId} carried no Rpt entries; nothing to apply.",
                report.Assignment?.MessageId);
            return;
        }

        var kyc = report.SupplementaryData?.Envelope?.CustomData;
        var applied = 0;

        foreach (var entry in reports)
        {
            // OrgnlAssgnmt/MsgId is the documented correlation key; Rpt/OrgnlId repeats it
            // per report and is the fallback when the assignment block is missing.
            var originalMessageId = FirstNonEmpty(report.OriginalAssignment?.MessageId, entry?.OriginalId);
            var accountNumber = entry?.OriginalPartyAndAccountId?.Account?.Id?.Iban;

            var log = await logStore.FindAsync(
                NpsLogDirection.Outbound, originalMessageId, accountNumber, cancellationToken);

            if (log is null)
            {
                logger.LogWarning(
                    "Inbound acmt.024 {MessageId} reports on enquiry {OriginalMessageId} / account " +
                    "{AccountNumber}, which has no outbound log row. Either the enquiry was raised " +
                    "elsewhere, or the account number was not echoed and the enquiry covered several " +
                    "accounts.",
                    report.Assignment?.MessageId, originalMessageId, accountNumber);
                continue;
            }

            ApplyReport(log, report, entry, kyc);
            applied++;
        }

        if (applied > 0)
            await logStore.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Applied acmt.024 {MessageId} to {Applied} of {Total} reported account(s).",
            report.Assignment?.MessageId, applied, reports.Count);
    }

    private void ApplyReport(
        NameEnquiryLog log,
        IdentificationVerificationReport report,
        VerificationReport? entry,
        CustomData? kyc)
    {
        log.WebhookCount++;

        // A verified enquiry is a terminal answer; a redelivered or contradictory report
        // must not silently flip an account name a payment was already authorised against.
        if (NpsLogMapping.IsTerminal(log.Status))
        {
            var restated = log.Verified == entry?.Verification;
            if (restated)
            {
                logger.LogInformation(
                    "acmt.024 for enquiry {RequestUniqueId} / account {AccountNumber} restates " +
                    "Verified={Verified}; recorded as delivery {WebhookCount}.",
                    log.RequestUniqueId, log.AccountNumber, log.Verified, log.WebhookCount);
            }
            else
            {
                logger.LogError(
                    "acmt.024 for enquiry {RequestUniqueId} / account {AccountNumber} reports " +
                    "Verified={NewVerified} but the row is already terminal with Verified={ExistingVerified}. " +
                    "The original answer is kept; investigate manually.",
                    log.RequestUniqueId, log.AccountNumber, entry?.Verification, log.Verified);
            }

            return;
        }

        log.Verified = entry?.Verification;
        log.Status = entry?.Verification == true ? NpsLogStatus.Successful : NpsLogStatus.Failed;
        log.WebhookReceivedAt = clock.NowUTC;
        log.ResponseMessageId = NpsLogMapping.Fit(report.Assignment?.MessageId, NpsLogColumn.MessageId);
        log.ResolvedAccountName = NpsLogMapping.Fit(
            entry?.UpdatedPartyAndAccountId?.Party?.Name, NpsLogColumn.PartyName);

        // The guide carries the account holder's KYC block in the acmt.024 supplementary
        // data under CreditorInfo.
        var holder = kyc?.CreditorInfo ?? kyc?.DebtorInfo;
        log.AccountDesignation = NpsLogMapping.Fit(holder?.AccountDesignation, NpsLogColumn.ShortCode);
        log.IdType = NpsLogMapping.Fit(holder?.IdType, NpsLogColumn.ShortCode);
        log.IdValue = NpsLogMapping.Fit(holder?.IdValue, NpsLogColumn.Code);
        log.AccountTier = NpsLogMapping.Fit(holder?.AccountTier, NpsLogColumn.ShortCode);
        log.RiskRating = NpsLogMapping.Fit(kyc?.TransactionInfo?.RiskRating, NpsLogColumn.ShortCode);

        log.WebhookResponse = NpsLogMapping.ToJson(new
        {
            messageId = report.Assignment?.MessageId,
            creationDateTime = report.Assignment?.CreationDateTime,
            originalMessageId = report.OriginalAssignment?.MessageId,
            originalCreationDateTime = report.OriginalAssignment?.CreationDateTime,
            originalId = entry?.OriginalId,
            verified = entry?.Verification,
            accountNumber = entry?.OriginalPartyAndAccountId?.Account?.Id?.Iban,
            resolvedAccountName = entry?.UpdatedPartyAndAccountId?.Party?.Name,
            accountDesignation = holder?.AccountDesignation,
            idType = holder?.IdType,
            // IdValue (BVN/NIN) is deliberately not repeated here — it already has its own
            // column, and a second copy only widens the PII footprint.
            accountTier = holder?.AccountTier,
            riskRating = kyc?.TransactionInfo?.RiskRating,
        });
    }

    private static string? FirstNonEmpty(string? first, string? second)
        => string.IsNullOrWhiteSpace(first) ? (string.IsNullOrWhiteSpace(second) ? null : second) : first;
}
