using Integration.BusinessLogics.Nps.Logging;
using Integration.BusinessLogics.Nps.Models;
using Integration.Infrastructures.Abstractions;
using Integration.Models.Nps;
using Mediator;
using Microsoft.Extensions.Logging;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.RequestModels;

namespace Integration.BusinessLogics.Nps.Commands;

/// <summary>Sends a name enquiry (acmt.023) for an account held at another NPS participant.</summary>
/// <param name="Request">The account to verify and the institution holding it.</param>
public sealed record SendNameEnquiryCommand(NpsIdVerificationRequest Request) : IRequest<NpsDispatchResult>;

public sealed class SendNameEnquiryCommandHandler(
    INpsMessageFactory messageFactory,
    INpsApiClient apiClient,
    INameEnquiryLogStore logStore,
    IDateTimeService clock,
    ILogger<SendNameEnquiryCommandHandler> logger)
    : IRequestHandler<SendNameEnquiryCommand, NpsDispatchResult>
{
    public async ValueTask<NpsDispatchResult> Handle(SendNameEnquiryCommand command, CancellationToken cancellationToken)
    {
        var document = messageFactory.CreateIdVerificationRequest(command.Request);
        var messageId = document.VerificationRequest!.Assignment!.MessageId!;

        // Log before dispatching. A name enquiry is what a transfer is authorised against,
        // so an unrecorded enquiry would leave the resulting payment unexplainable.
        var sentAt = clock.NowUTC;
        var rows = NameEnquiryLogMapper.FromAcmt023(document, NpsLogDirection.Outbound, sentAt);

        var requestJson = NpsLogMapping.ToJson(command.Request);
        foreach (var row in rows)
            row.RequestJson = requestJson;

        await logStore.AddRangeAsync(rows, cancellationToken);

        var result = await NpsDispatcher.DispatchAsync(
            logger,
            "acmt.023 name enquiry",
            messageId,
            () => apiClient.SendIdVerificationRequestAsync(document, cancellationToken),
            cancellationToken);

        // The message has already gone, so a logging failure must not fault the request.
        try
        {
            var status = NpsLogMapping.FromDispatch(result);
            var summary = NpsLogMapping.DispatchSummary(result);
            var error = NpsLogMapping.DispatchError(result);
            var respondedAt = clock.NowUTC;

            foreach (var row in rows)
            {
                row.Status = status;
                row.NibbsHttpStatusCode = result.NibssStatusCode;
                row.AcknowledgedBySwitch = result.AcknowledgedBySwitch;
                row.RejectionReasonCode = NpsLogMapping.Fit(result.ReasonCode, NpsLogColumn.Code);
                row.ErrorMessage = error;
                row.NibbsResponse = summary;
                row.NibbsRespondedAt = respondedAt;
            }

            await logStore.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "acmt.023 {MessageId} was dispatched ({DispatchStatus}) but the outcome could not be " +
                "written to NameEnquiryLog; the row(s) remain Pending.",
                messageId, result.Status);
        }

        return result;
    }
}
