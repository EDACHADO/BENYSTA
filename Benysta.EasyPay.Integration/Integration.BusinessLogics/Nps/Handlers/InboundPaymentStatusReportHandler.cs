using Integration.BusinessLogics.Nps.Logging;
using Integration.Infrastructures.Abstractions;
using Integration.Models.Nps;
using Microsoft.Extensions.Logging;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Inbound;
using Nibbs.Nps.Integration.Messages.Pacs;

namespace Integration.BusinessLogics.Nps.Handlers;

/// <summary>
/// Applies an inbound pacs.002 payment status report to the outbound pacs.008 it reports
/// on, completing that payment's record with the real business outcome.
/// </summary>
/// <remarks>
/// This closes certification Phase 1: the HTTP 2xx from the switch only acknowledged
/// receipt, and it is this callback that says whether the payment settled (ACSC) or was
/// rejected (RJCT). The same handler serves a pacs.002 that answers a pacs.028 status
/// query, since that too reports on one of our outbound payments.
/// </remarks>
public sealed class InboundPaymentStatusReportHandler(
    ISingleTransferLogStore logStore,
    IDateTimeService clock,
    ILogger<InboundPaymentStatusReportHandler> logger)
    : INpsMessageHandler<Pacs002Document>
{
    public async Task HandleAsync(
        Pacs002Document document,
        NpsInboundMessage context,
        CancellationToken cancellationToken = default)
    {
        var report = document?.StatusReport;
        var group = report?.OriginalGroupInformation;
        var transaction = report?.TransactionInformation;

        if (report is null)
        {
            logger.LogWarning("Inbound pacs.002 had no FIToFIPmtStsRpt payload; nothing to apply.");
            return;
        }

        var log = await logStore.FindOutboundAsync(
            transactionId: transaction?.OriginalTransactionId,
            messageId: group?.OriginalMessageId,
            endToEndId: transaction?.OriginalEndToEndId,
            instructionId: transaction?.OriginalInstructionId,
            cancellationToken: cancellationToken);

        if (log is null)
        {
            // Not necessarily an error: the payment may have been initiated before this
            // service owned the flow, or by another channel entirely.
            logger.LogWarning(
                "Inbound pacs.002 {MessageId} could not be matched to an outbound payment " +
                "(OrgnlMsgId {OriginalMessageId}, OrgnlTxId {OriginalTransactionId}, " +
                "OrgnlEndToEndId {OriginalEndToEndId}). No row was updated.",
                report.GroupHeader?.MessageId, group?.OriginalMessageId,
                transaction?.OriginalTransactionId, transaction?.OriginalEndToEndId);
            return;
        }

        // TxSts is the transaction-level truth; GrpSts is the fallback when the report is
        // group-scoped only.
        var statusCode = FirstNonEmpty(transaction?.TransactionStatus, group?.GroupStatus);
        var newStatus = NpsLogMapping.FromTransactionStatus(statusCode);

        log.WebhookCount++;

        if (NpsLogMapping.IsTerminal(log.Status))
        {
            // A settled payment must not be reopened by a late or redelivered report. The
            // count still moves so the redelivery is visible; the original outcome stands.
            if (log.Status == newStatus)
            {
                logger.LogInformation(
                    "pacs.002 for payment {RequestUniqueId} restates the existing outcome {Status}; " +
                    "recorded as delivery {WebhookCount}.",
                    log.RequestUniqueId, log.Status, log.WebhookCount);
            }
            else
            {
                // Worth alerting on: NPS has reported two different outcomes for one payment.
                logger.LogError(
                    "pacs.002 for payment {RequestUniqueId} reports {NewStatus} ({StatusCode}) but the row is " +
                    "already terminal at {ExistingStatus}. The original outcome is kept; investigate manually.",
                    log.RequestUniqueId, newStatus, statusCode, log.Status);
            }

            await logStore.SaveChangesAsync(cancellationToken);
            return;
        }

        log.Status = newStatus;
        log.WebhookReceivedAt = clock.NowUTC;
        log.ResponseMessageId = NpsLogMapping.Fit(report.GroupHeader?.MessageId, NpsLogColumn.MessageId);
        log.GroupStatus = NpsLogMapping.Fit(group?.GroupStatus, NpsLogColumn.ShortCode);
        log.TransactionStatus = NpsLogMapping.Fit(statusCode, NpsLogColumn.ShortCode);
        log.StatusId = NpsLogMapping.Fit(transaction?.StatusId, NpsLogColumn.ShortCode);
        log.StatusReasonCode = NpsLogMapping.Fit(ReasonCode(transaction), NpsLogColumn.Code);
        log.StatusReasonInformation = NpsLogMapping.Fit(
            transaction?.StatusReason?.AdditionalInformation, NpsLogColumn.ReasonInformation);

        log.WebhookResponse = NpsLogMapping.ToJson(new
        {
            messageId = report.GroupHeader?.MessageId,
            creationDateTime = report.GroupHeader?.CreationDateTime,
            originalMessageId = group?.OriginalMessageId,
            originalMessageNameId = group?.OriginalMessageNameId,
            groupStatus = group?.GroupStatus,
            statusId = transaction?.StatusId,
            originalInstructionId = transaction?.OriginalInstructionId,
            originalEndToEndId = transaction?.OriginalEndToEndId,
            originalTransactionId = transaction?.OriginalTransactionId,
            transactionStatus = transaction?.TransactionStatus,
            reasonCode = ReasonCode(transaction),
            additionalInformation = transaction?.StatusReason?.AdditionalInformation,
            settlementDate = transaction?.OriginalTransactionReference?.InterbankSettlementDate,
        });

        await logStore.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Applied pacs.002 {ResponseMessageId} to payment {RequestUniqueId}: {StatusCode} -> {Status}{Reason}.",
            log.ResponseMessageId, log.RequestUniqueId, statusCode, log.Status,
            log.StatusReasonCode is null ? string.Empty : $" (reason {log.StatusReasonCode})");
    }

    private static string? ReasonCode(TransactionInformationAndStatus? transaction)
        => FirstNonEmpty(transaction?.StatusReason?.Reason?.Proprietary, transaction?.StatusReason?.Reason?.Code);

    private static string? FirstNonEmpty(string? first, string? second)
        => string.IsNullOrWhiteSpace(first) ? (string.IsNullOrWhiteSpace(second) ? null : second) : first;
}
