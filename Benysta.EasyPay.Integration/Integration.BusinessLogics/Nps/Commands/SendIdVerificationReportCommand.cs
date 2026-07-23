using Integration.BusinessLogics.Nps.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.RequestModels;

namespace Integration.BusinessLogics.Nps.Commands;

/// <summary>
/// Sends an identification verification report (acmt.024) answering an inbound
/// acmt.023 name enquiry for an account held at this institution.
/// </summary>
/// <param name="Request">The verification outcome, resolved account name and KYC details.</param>
public sealed record SendIdVerificationReportCommand(NpsIdVerificationReportRequest Request) : IRequest<NpsDispatchResult>;

public sealed class SendIdVerificationReportCommandHandler(
    INpsMessageFactory messageFactory,
    INpsApiClient apiClient,
    ILogger<SendIdVerificationReportCommandHandler> logger)
    : IRequestHandler<SendIdVerificationReportCommand, NpsDispatchResult>
{
    public Task<NpsDispatchResult> Handle(SendIdVerificationReportCommand command, CancellationToken cancellationToken)
    {
        var document = messageFactory.CreateIdVerificationReport(command.Request);
        var messageId = document.VerificationReport!.Assignment!.MessageId!;

        return NpsDispatcher.DispatchAsync(
            logger,
            "acmt.024 identification verification report",
            messageId,
            () => apiClient.SendIdVerificationReportAsync(document, cancellationToken),
            cancellationToken);
    }
}
