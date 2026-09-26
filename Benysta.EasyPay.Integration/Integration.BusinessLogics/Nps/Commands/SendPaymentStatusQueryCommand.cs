using Integration.BusinessLogics.Nps.Logging;
using Integration.BusinessLogics.Nps.Models;
using Integration.Infrastructures.Abstractions;
using Integration.Models.Nps;
using Mediator;
using Microsoft.Extensions.Logging;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.RequestModels;

namespace Integration.BusinessLogics.Nps.Commands;

/// <summary>Sends a payment status query (pacs.028) for a previously sent credit transfer.</summary>
/// <param name="Request">Identifiers of the original payment being queried.</param>
public sealed record SendPaymentStatusQueryCommand(NpsPaymentStatusQuery Request) : IRequest<NpsDispatchResult>;

public sealed class SendPaymentStatusQueryCommandHandler(
    INpsMessageFactory messageFactory,
    INpsApiClient apiClient,
    ISingleTransferLogStore logStore,
    IDateTimeService clock,
    ILogger<SendPaymentStatusQueryCommandHandler> logger)
    : IRequestHandler<SendPaymentStatusQueryCommand, NpsDispatchResult>
{
    public async ValueTask<NpsDispatchResult> Handle(SendPaymentStatusQueryCommand command, CancellationToken cancellationToken)
    {
        var document = messageFactory.CreatePaymentStatusRequest(command.Request);
        var messageId = document.StatusRequest!.GroupHeader!.MessageId!;

        var result = await NpsDispatcher.DispatchAsync(
            logger,
            "pacs.028 payment status query",
            messageId,
            () => apiClient.SendPaymentStatusRequestAsync(document, cancellationToken),
            cancellationToken);

        // A query has no outcome of its own — the answering pacs.002 lands on the webhook
        // and is applied by InboundPaymentStatusReportHandler. All that is recorded here is
        // that the payment was chased, so repeated chasing is visible.
        try
        {
            var log = await logStore.FindOutboundAsync(
                transactionId: command.Request.OriginalTransactionId,
                messageId: command.Request.OriginalMessageId,
                cancellationToken: cancellationToken);

            if (log is null)
            {
                logger.LogWarning(
                    "pacs.028 {MessageId} queried payment {OriginalMessageId} / TxId {OriginalTransactionId}, " +
                    "which has no outbound log row.",
                    messageId, command.Request.OriginalMessageId, command.Request.OriginalTransactionId);
            }
            else
            {
                log.StatusQueryCount++;
                log.LastStatusQueryAt = clock.NowUTC;
                log.LastStatusQueryMessageId = NpsLogMapping.Fit(messageId, NpsLogColumn.MessageId);
                await logStore.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "pacs.028 {MessageId} was dispatched but the query could not be recorded against payment " +
                "{OriginalMessageId}.",
                messageId, command.Request.OriginalMessageId);
        }

        return result;
    }
}
