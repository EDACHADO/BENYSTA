using Integration.BusinessLogics.Nps.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Messages;

namespace Integration.BusinessLogics.Nps.Commands;

/// <summary>Sends a credit transfer (pacs.008) to another NPS participant.</summary>
/// <param name="Request">The transfer details (amount, debtor, creditor, beneficiary institution).</param>
public sealed record SendCreditTransferCommand(NpsCreditTransferRequest Request) : IRequest<NpsDispatchResult>;

public sealed class SendCreditTransferCommandHandler(
    INpsMessageFactory messageFactory,
    INpsApiClient apiClient,
    ILogger<SendCreditTransferCommandHandler> logger)
    : IRequestHandler<SendCreditTransferCommand, NpsDispatchResult>
{
    public Task<NpsDispatchResult> Handle(SendCreditTransferCommand command, CancellationToken cancellationToken)
    {
        var document = messageFactory.CreateCreditTransfer(command.Request);
        var messageId = document.CreditTransfer!.GroupHeader!.MessageId!;

        return NpsDispatcher.DispatchAsync(
            logger,
            "pacs.008 credit transfer",
            messageId,
            () => apiClient.SendCreditTransferAsync(document, cancellationToken),
            cancellationToken);
    }
}
