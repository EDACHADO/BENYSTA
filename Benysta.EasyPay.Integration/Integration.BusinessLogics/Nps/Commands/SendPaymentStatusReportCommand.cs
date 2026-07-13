using Integration.BusinessLogics.Nps.Models;
using MediatR;
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
    ILogger<SendPaymentStatusReportCommandHandler> logger)
    : IRequestHandler<SendPaymentStatusReportCommand, NpsDispatchResult>
{
    public Task<NpsDispatchResult> Handle(SendPaymentStatusReportCommand command, CancellationToken cancellationToken)
    {
        var document = messageFactory.CreatePaymentStatusReport(command.Request);
        var messageId = document.StatusReport!.GroupHeader!.MessageId!;

        return NpsDispatcher.DispatchAsync(
            logger,
            "pacs.002 payment status report",
            messageId,
            () => apiClient.SendPaymentStatusReportAsync(document, cancellationToken),
            cancellationToken);
    }
}
