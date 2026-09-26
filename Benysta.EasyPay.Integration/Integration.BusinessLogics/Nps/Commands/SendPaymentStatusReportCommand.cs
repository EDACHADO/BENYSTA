using Integration.BusinessLogics.Nps.Logging;
using Integration.BusinessLogics.Nps.Models;
using Integration.Infrastructures.Abstractions;
using Integration.Models.Nps;
using Mediator;
using Microsoft.Extensions.Logging;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.RequestModels;

namespace Integration.BusinessLogics.Nps.Commands;

/// <summary>Sends a payment status report (pacs.002) answering an inbound credit transfer.</summary>
/// <param name="Request">The original payment identifiers and the ACSC/RJCT decision.</param>
public sealed record SendPaymentStatusReportCommand(NpsPaymentStatusReportRequest Request) : IRequest<NpsDispatchResult>;

public sealed class SendPaymentStatusReportCommandHandler(
    INpsMessageFactory messageFactory,
    INpsApiClient apiClient,
    ISingleTransferLogStore logStore,
    IDateTimeService clock,
    ILogger<SendPaymentStatusReportCommandHandler> logger)
    : IRequestHandler<SendPaymentStatusReportCommand, NpsDispatchResult>
{
    public async ValueTask<NpsDispatchResult> Handle(SendPaymentStatusReportCommand command, CancellationToken cancellationToken)
    {
        var document = messageFactory.CreatePaymentStatusReport(command.Request);
        var messageId = document.StatusReport!.GroupHeader!.MessageId!;

        var result = await NpsDispatcher.DispatchAsync(
            logger,
            "pacs.002 payment status report",
            messageId,
            () => apiClient.SendPaymentStatusReportAsync(document, cancellationToken),
            cancellationToken);

        // This is certification Phase 3. The report is not a payment of its own, so it does
        // not get a row: it completes the inbound pacs.008 row that this answers.
        try
        {
            await RecordAgainstInboundPaymentAsync(command.Request, messageId, result, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "pacs.002 {MessageId} answering inbound payment {OriginalMessageId} was dispatched " +
                "({DispatchStatus}) but the inbound log row could not be updated.",
                messageId, command.Request.OriginalMessageId, result.Status);
        }

        return result;
    }

    private async Task RecordAgainstInboundPaymentAsync(
        NpsPaymentStatusReportRequest request,
        string reportMessageId,
        NpsDispatchResult result,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OriginalMessageId))
        {
            logger.LogWarning(
                "pacs.002 {MessageId} carries no OrgnlMsgId, so it cannot be tied to an inbound payment.",
                reportMessageId);
            return;
        }

        var log = await logStore.FindInboundByMessageIdAsync(request.OriginalMessageId, cancellationToken);
        if (log is null)
        {
            logger.LogWarning(
                "pacs.002 {MessageId} answers inbound payment {OriginalMessageId}, which has no log row. " +
                "Either it was never received on the webhook or it failed to log at the time.",
                reportMessageId, request.OriginalMessageId);
            return;
        }

        log.ResponseMessageId = NpsLogMapping.Fit(reportMessageId, NpsLogColumn.MessageId);
        log.TransactionStatus = NpsLogMapping.Fit(request.Status, NpsLogColumn.ShortCode);
        log.StatusId = NpsLogMapping.Fit(request.StatusId, NpsLogColumn.ShortCode);
        log.StatusReasonCode = NpsLogMapping.Fit(request.ReasonCode, NpsLogColumn.Code);
        log.StatusReasonInformation = NpsLogMapping.Fit(request.ReasonInformation, NpsLogColumn.ReasonInformation);

        log.NibbsHttpStatusCode = result.NibssStatusCode;
        log.AcknowledgedBySwitch = result.AcknowledgedBySwitch;
        log.RejectionReasonCode = NpsLogMapping.Fit(result.ReasonCode, NpsLogColumn.Code);
        log.ErrorMessage = NpsLogMapping.DispatchError(result);
        log.NibbsResponse = NpsLogMapping.DispatchSummary(result);
        log.NibbsRespondedAt = clock.NowUTC;

        // On an inbound row the "webhook response" is the answer we produced.
        log.WebhookResponse = NpsLogMapping.ToJson(new
        {
            reportMessageId,
            originalMessageId = request.OriginalMessageId,
            originalMessageNameId = request.OriginalMessageNameId,
            originalInstructionId = request.OriginalInstructionId,
            originalEndToEndId = request.OriginalEndToEndId,
            originalTransactionId = request.OriginalTransactionId,
            transactionStatus = request.Status,
            statusId = request.StatusId,
            reasonCode = request.ReasonCode,
            reasonInformation = request.ReasonInformation,
        });

        // Only once the switch has taken the report does our decision become the outcome;
        // if the dispatch failed, the inbound payment is still effectively unanswered.
        log.Status = result.Status == NpsDispatchStatus.Accepted && result.AcknowledgedBySwitch
            ? NpsLogMapping.FromTransactionStatus(request.Status)
            : NpsLogMapping.FromDispatch(result);

        await logStore.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Answered inbound payment {OriginalMessageId} with pacs.002 {ReportMessageId}: {Status} -> {LogStatus}.",
            request.OriginalMessageId, reportMessageId, request.Status, log.Status);
    }
}
