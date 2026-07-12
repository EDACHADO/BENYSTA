using Integration.BusinessLogics.Nps.Models;
using Microsoft.Extensions.Logging;
using Nibbs.Nps.Integration.Client;
using Nibbs.Nps.Integration.Exceptions;

namespace Integration.BusinessLogics.Nps;

/// <summary>
/// Shared dispatch pipeline for the NPS send commands: posts a built message to the
/// switch and maps the response and the integration exceptions to a
/// <see cref="NpsDispatchResult"/> (accepted, admi.002 rejection, decryption failure
/// or transport failure).
/// </summary>
internal static class NpsDispatcher
{
    public static async Task<NpsDispatchResult> DispatchAsync(
        ILogger logger,
        string messageDescription,
        string messageId,
        Func<Task<NpsResponse>> send,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Dispatching {MessageDescription} {MessageId} to NIBSS NPS", messageDescription, messageId);

        NpsResponse response;
        try
        {
            response = await send();
        }
        catch (NpsMessageRejectedException ex)
        {
            logger.LogWarning(ex,
                "NIBSS NPS rejected {MessageDescription} {MessageId} with reason {ReasonCode}",
                messageDescription, messageId, ex.ReasonCode);

            return NpsDispatchResult.Rejected(messageId, ex.ReasonCode, ex.OriginalMessageId, ex.Message);
        }
        catch (Exception ex) when (
            ex is NpsIntegrationException or HttpRequestException ||
            // HttpClient timeout — as opposed to the caller cancelling the request.
            (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogError(ex,
                "Failed to dispatch {MessageDescription} {MessageId} to NIBSS NPS",
                messageDescription, messageId);

            return NpsDispatchResult.Failed(messageId, ex.Message);
        }

        if (response.Rejection is not null)
        {
            var reason = response.Rejection.MessageReject?.Reason;
            var reasonCode = reason?.RejectingPartyReason;

            logger.LogWarning(
                "NIBSS NPS rejected {MessageDescription} {MessageId} with reason {ReasonCode}: {ReasonDescription}",
                messageDescription, messageId, reasonCode, reason?.ReasonDescription);

            return NpsDispatchResult.Rejected(
                messageId,
                reasonCode,
                response.Rejection.MessageReject?.RelatedReference?.Reference,
                $"NPS rejected the message with reason {reasonCode}: {reason?.ReasonDescription}");
        }

        if (response.IsDecryptionFailure)
        {
            logger.LogError(
                "NIBSS NPS could not decrypt {MessageDescription} {MessageId} (HTTP 400 without a body)",
                messageDescription, messageId);

            return NpsDispatchResult.DecryptionFailure(messageId);
        }

        logger.LogInformation(
            "NIBSS NPS returned HTTP {StatusCode} for {MessageDescription} {MessageId} (accepted: {Accepted})",
            (int)response.StatusCode, messageDescription, messageId, response.IsSuccess);

        // A 2xx from the switch only acknowledges receipt; the business outcome
        // (e.g. pacs.002 / acmt.024) arrives asynchronously via webhook.
        return NpsDispatchResult.Accepted(messageId, (int)response.StatusCode, response.IsSuccess);
    }
}
