using Integration.BusinessLogics.Nps.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.RequestModels;

namespace Integration.BusinessLogics.Nps.Commands;

/// <summary>
/// Sends a customer credit transfer initiation (pain.001) to the NIBSS Institution
/// service. The business outcome arrives asynchronously as a pain.002 webhook.
/// </summary>
/// <param name="Request">The initiation details (debtor, creditor, amount, KYC supplementary data).</param>
public sealed record SendCreditTransferInitiationCommand(NpsCreditTransferInitiationRequest Request) : IRequest<NpsDispatchResult>;

public sealed class SendCreditTransferInitiationCommandHandler(
    INpsMessageFactory messageFactory,
    INpsApiClient apiClient,
    ILogger<SendCreditTransferInitiationCommandHandler> logger)
    : IRequestHandler<SendCreditTransferInitiationCommand, NpsDispatchResult>
{
    public Task<NpsDispatchResult> Handle(SendCreditTransferInitiationCommand command, CancellationToken cancellationToken)
    {
        var document = messageFactory.CreateCreditTransferInitiation(command.Request);
        var messageId = document.CreditTransferInitiation!.GroupHeader!.MessageId!;

        return NpsDispatcher.DispatchAsync(
            logger,
            "pain.001 credit transfer initiation",
            messageId,
            () => apiClient.SendCreditTransferInitiationAsync(document, cancellationToken),
            cancellationToken);
    }
}
