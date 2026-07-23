using Integration.BusinessLogics.Nps.Abstractions;
using Integration.BusinessLogics.Nps.Commands;
using Integration.BusinessLogics.Nps.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Inbound;
using Nibbs.Nps.Integration.Messages.Acmt;
using Nibbs.Nps.Integration.RequestModels;

namespace Integration.BusinessLogics.Nps.Handlers;

/// <summary>
/// Answers inbound acmt.023 name enquiries: resolves each requested account through
/// <see cref="IAccountVerificationService"/> and dispatches an acmt.024 identification
/// verification report back to the requesting institution, completing the
/// Identification Verification flow described in the NPS integration guide.
/// </summary>
public sealed class InboundIdVerificationRequestHandler(
    IAccountVerificationService accountVerification,
    IMediator mediator,
    ILogger<InboundIdVerificationRequestHandler> logger)
    : INpsMessageHandler<Acmt023Document>
{
    public async Task HandleAsync(
        Acmt023Document document,
        NpsInboundMessage context,
        CancellationToken cancellationToken = default)
    {
        var request = document.VerificationRequest;
        var assignment = request?.Assignment;
        if (assignment is null || request?.Verifications is not { Count: > 0 } verifications)
        {
            logger.LogWarning("Inbound acmt.023 {MessageId} carried no verification entries; nothing to answer.",
                assignment?.MessageId);
            return;
        }

        // The requester (Assgnr) of the acmt.023 becomes the assignee of our acmt.024.
        var requesterAgentId = assignment.Assigner?.Agent?.FinancialInstitutionId?.ClearingSystemMemberId?.MemberId;
        var requesterName = assignment.Assigner?.Party?.Name;
        if (string.IsNullOrEmpty(requesterAgentId))
        {
            logger.LogWarning("Inbound acmt.023 {MessageId} has no Assgnr agent member id; cannot address the acmt.024.",
                assignment.MessageId);
            return;
        }

        foreach (var verification in verifications)
        {
            var accountNumber = verification.PartyAndAccountId?.Account?.Id?.Iban;
            if (string.IsNullOrEmpty(accountNumber))
            {
                logger.LogWarning("Inbound acmt.023 {MessageId} verification entry has no account number; skipping.",
                    assignment.MessageId);
                continue;
            }

            var result = await accountVerification.VerifyAccountAsync(
                accountNumber, verification.PartyAndAccountId?.Party?.Name, cancellationToken);

            logger.LogInformation(
                "Answering acmt.023 {MessageId} from {Requester}: account {AccountNumber} verified={Verified}.",
                assignment.MessageId, requesterAgentId, accountNumber, result.Verified);

            var dispatch = await mediator.Send(new SendIdVerificationReportCommand(new NpsIdVerificationReportRequest
            {
                OriginalMessageId = assignment.MessageId,
                OriginalCreationDateTime = assignment.CreationDateTime,
                RequestingAgentId = requesterAgentId,
                RequestingPartyName = requesterName,
                Verified = result.Verified,
                AccountNumber = accountNumber,
                AccountName = result.AccountName,
                AccountHolderInfo = result.AccountHolderInfo,
                RiskRating = result.RiskRating,
            }), cancellationToken);

            if (dispatch.Status != NpsDispatchStatus.Accepted)
            {
                logger.LogError(
                    "acmt.024 for inbound acmt.023 {MessageId} was not accepted by the switch: {Status} {Error}",
                    assignment.MessageId, dispatch.Status, dispatch.Error);
            }
        }
    }
}
