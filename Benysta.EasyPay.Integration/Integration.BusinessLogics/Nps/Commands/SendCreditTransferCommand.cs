using Integration.BusinessLogics.Nps.Logging;
using Integration.BusinessLogics.Nps.Models;
using Integration.Infrastructures.Abstractions;
using Integration.Models.Nps;
using Mediator;
using Microsoft.Extensions.Logging;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.RequestModels;

namespace Integration.BusinessLogics.Nps.Commands;

/// <summary>Sends a credit transfer (pacs.008) to another NPS participant.</summary>
/// <param name="Request">The transfer details (amount, debtor, creditor, beneficiary institution).</param>
public sealed record SendCreditTransferCommand(NpsCreditTransferRequest Request) : IRequest<NpsDispatchResult>;

public sealed class SendCreditTransferCommandHandler(
    INpsMessageFactory messageFactory,
    INpsApiClient apiClient,
    ISingleTransferLogStore logStore,
    IDateTimeService clock,
    ILogger<SendCreditTransferCommandHandler> logger)
    : IRequestHandler<SendCreditTransferCommand, NpsDispatchResult>
{
    public async ValueTask<NpsDispatchResult> Handle(SendCreditTransferCommand command, CancellationToken cancellationToken)
    {
        var document = messageFactory.CreateCreditTransfer(command.Request);
        var messageId = document.CreditTransfer!.GroupHeader!.MessageId!;

        // Log before dispatching, and let a failure here propagate: a payment we cannot
        // record must not be sent, or it becomes money in flight with no local trace.
        var sentAt = clock.NowUTC;
        var log = SingleTransferLogMapper.FromPacs008(document, NpsLogDirection.Outbound, sentAt);
        log.RequestJson = NpsLogMapping.ToJson(command.Request);
        log.RequestSentAt = sentAt;
        await logStore.AddAsync(log, cancellationToken);

        var result = await NpsDispatcher.DispatchAsync(
            logger,
            "pacs.008 credit transfer",
            messageId,
            () => apiClient.SendCreditTransferAsync(document, cancellationToken),
            cancellationToken);

        // The message is already gone, so a logging failure must not turn a dispatched
        // payment into a faulted request. Reconciliation (pacs.028) resolves rows that
        // are still Pending.
        try
        {
            log.Status = NpsLogMapping.FromDispatch(result);
            log.NibbsHttpStatusCode = result.NibssStatusCode;
            log.AcknowledgedBySwitch = result.AcknowledgedBySwitch;
            log.RejectionReasonCode = NpsLogMapping.Fit(result.ReasonCode, NpsLogColumn.Code);
            log.ErrorMessage = NpsLogMapping.DispatchError(result);
            log.NibbsResponse = NpsLogMapping.DispatchSummary(result);
            log.NibbsRespondedAt = clock.NowUTC;

            await logStore.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "pacs.008 {MessageId} was dispatched ({DispatchStatus}) but the outcome could not be " +
                "written to SingleTransferLog {LogId}; the row remains Pending for reconciliation.",
                messageId, result.Status, log.SingleTransferLogId);
        }

        return result;
    }
}
