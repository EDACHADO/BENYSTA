using Integration.BusinessLogics.Nps.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Messages;

namespace Integration.BusinessLogics.Nps.Commands;

/// <summary>Sends a payment status query (pacs.028) for a previously sent credit transfer.</summary>
/// <param name="Request">Identifiers of the original payment being queried.</param>
public sealed record SendPaymentStatusQueryCommand(NpsPaymentStatusQuery Request) : IRequest<NpsDispatchResult>;

public sealed class SendPaymentStatusQueryCommandHandler(
    INpsMessageFactory messageFactory,
    INpsApiClient apiClient,
    ILogger<SendPaymentStatusQueryCommandHandler> logger)
    : IRequestHandler<SendPaymentStatusQueryCommand, NpsDispatchResult>
{
    public Task<NpsDispatchResult> Handle(SendPaymentStatusQueryCommand command, CancellationToken cancellationToken)
    {
        var document = messageFactory.CreatePaymentStatusRequest(command.Request);
        var messageId = document.StatusRequest!.GroupHeader!.MessageId!;

        return NpsDispatcher.DispatchAsync(
            logger,
            "pacs.028 payment status query",
            messageId,
            () => apiClient.SendPaymentStatusRequestAsync(document, cancellationToken),
            cancellationToken);
    }
}
