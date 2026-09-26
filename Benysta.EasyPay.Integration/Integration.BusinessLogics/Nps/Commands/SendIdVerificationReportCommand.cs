using Integration.BusinessLogics.Nps.Logging;
using Integration.BusinessLogics.Nps.Models;
using Integration.Infrastructures.Abstractions;
using Integration.Models.Nps;
using Mediator;
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
    INameEnquiryLogStore logStore,
    IDateTimeService clock,
    ILogger<SendIdVerificationReportCommandHandler> logger)
    : IRequestHandler<SendIdVerificationReportCommand, NpsDispatchResult>
{
    public async ValueTask<NpsDispatchResult> Handle(SendIdVerificationReportCommand command, CancellationToken cancellationToken)
    {
        var document = messageFactory.CreateIdVerificationReport(command.Request);
        var messageId = document.VerificationReport!.Assignment!.MessageId!;

        var result = await NpsDispatcher.DispatchAsync(
            logger,
            "acmt.024 identification verification report",
            messageId,
            () => apiClient.SendIdVerificationReportAsync(document, cancellationToken),
            cancellationToken);

        // The report is not an enquiry of its own, so it gets no row: it completes the
        // inbound acmt.023 row for the account it answers.
        try
        {
            await RecordAgainstInboundEnquiryAsync(command.Request, messageId, result, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "acmt.024 {MessageId} answering inbound enquiry {OriginalMessageId} was dispatched " +
                "({DispatchStatus}) but the inbound log row could not be updated.",
                messageId, command.Request.OriginalMessageId, result.Status);
        }

        return result;
    }

    private async Task RecordAgainstInboundEnquiryAsync(
        NpsIdVerificationReportRequest request,
        string reportMessageId,
        NpsDispatchResult result,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OriginalMessageId))
        {
            logger.LogWarning(
                "acmt.024 {MessageId} carries no OrgnlAssgnmt/MsgId, so it cannot be tied to an " +
                "inbound enquiry.",
                reportMessageId);
            return;
        }

        var log = await logStore.FindAsync(
            NpsLogDirection.Inbound, request.OriginalMessageId, request.AccountNumber, cancellationToken);

        if (log is null)
        {
            logger.LogWarning(
                "acmt.024 {MessageId} answers inbound enquiry {OriginalMessageId} / account " +
                "{AccountNumber}, which has no log row. Either it was never received on the webhook " +
                "or it failed to log at the time.",
                reportMessageId, request.OriginalMessageId, request.AccountNumber);
            return;
        }

        log.ResponseMessageId = NpsLogMapping.Fit(reportMessageId, NpsLogColumn.MessageId);
        log.Verified = request.Verified;
        log.ResolvedAccountName = NpsLogMapping.Fit(request.AccountName, NpsLogColumn.PartyName);

        log.AccountDesignation = NpsLogMapping.Fit(
            request.AccountHolderInfo?.AccountDesignation, NpsLogColumn.ShortCode);
        log.IdType = NpsLogMapping.Fit(request.AccountHolderInfo?.IdType, NpsLogColumn.ShortCode);
        log.IdValue = NpsLogMapping.Fit(request.AccountHolderInfo?.IdValue, NpsLogColumn.Code);
        log.AccountTier = NpsLogMapping.Fit(request.AccountHolderInfo?.AccountTier, NpsLogColumn.ShortCode);
        log.RiskRating = NpsLogMapping.Fit(request.RiskRating, NpsLogColumn.ShortCode);

        log.NibbsHttpStatusCode = result.NibssStatusCode;
        log.AcknowledgedBySwitch = result.AcknowledgedBySwitch;
        log.RejectionReasonCode = NpsLogMapping.Fit(result.ReasonCode, NpsLogColumn.Code);
        log.ErrorMessage = NpsLogMapping.DispatchError(result);
        log.NibbsResponse = NpsLogMapping.DispatchSummary(result);
        log.NibbsRespondedAt = clock.NowUTC;

        // On an inbound row the "webhook response" is the answer we produced.
        log.WebhookResponse = NpsLogMapping.ToJson(new
        {
            reportMessageId,
            originalMessageId = request.OriginalMessageId,
            originalCreationDateTime = request.OriginalCreationDateTime,
            requestingBankCode = request.RequestingAgentId,
            accountNumber = request.AccountNumber,
            verified = request.Verified,
            resolvedAccountName = request.AccountName,
            accountDesignation = request.AccountHolderInfo?.AccountDesignation,
            idType = request.AccountHolderInfo?.IdType,
            accountTier = request.AccountHolderInfo?.AccountTier,
            riskRating = request.RiskRating,
        });

        // Only once the switch has taken the report is the enquiry actually answered.
        log.Status = result.Status == NpsDispatchStatus.Accepted && result.AcknowledgedBySwitch
            ? (request.Verified ? NpsLogStatus.Successful : NpsLogStatus.Failed)
            : NpsLogMapping.FromDispatch(result);

        await logStore.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Answered inbound enquiry {OriginalMessageId} / account {AccountNumber} with acmt.024 " +
            "{ReportMessageId}: Verified={Verified} -> {LogStatus}.",
            request.OriginalMessageId, request.AccountNumber, reportMessageId, request.Verified, log.Status);
    }
}
