using Integration.BusinessLogics.Nps.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Messages;

namespace Integration.BusinessLogics.Nps.Commands;

/// <summary>Sends a name enquiry (acmt.023) for an account held at another NPS participant.</summary>
/// <param name="Request">The account to verify and the institution holding it.</param>
public sealed record SendNameEnquiryCommand(NpsIdVerificationRequest Request) : IRequest<NpsDispatchResult>;

public sealed class SendNameEnquiryCommandHandler(
    INpsMessageFactory messageFactory,
    INpsApiClient apiClient,
    ILogger<SendNameEnquiryCommandHandler> logger)
    : IRequestHandler<SendNameEnquiryCommand, NpsDispatchResult>
{
    public Task<NpsDispatchResult> Handle(SendNameEnquiryCommand command, CancellationToken cancellationToken)
    {
        var document = messageFactory.CreateIdVerificationRequest(command.Request);
        var messageId = document.VerificationRequest!.Assignment!.MessageId!;

        return NpsDispatcher.DispatchAsync(
            logger,
            "acmt.023 name enquiry",
            messageId,
            () => apiClient.SendIdVerificationRequestAsync(document, cancellationToken),
            cancellationToken);
    }
}
