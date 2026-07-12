using Integration.BusinessLogics.Nps.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Nibbs.Nps.Integration.Exceptions;
using Nibbs.Nps.Integration.Inbound;

namespace Integration.BusinessLogics.Nps.Commands;

/// <summary>
/// Processes an inbound ISO 20022 message pushed by NIBSS NPS to one of this
/// institution's callback URLs: decrypts and signature-verifies the raw body and
/// dispatches the typed document to the registered <c>INpsMessageHandler&lt;TDocument&gt;</c>
/// implementations.
/// </summary>
/// <param name="RawXml">The signed+encrypted XML exactly as received from NPS.</param>
/// <param name="Source">Where the payload arrived (e.g. the request path), for logging.</param>
public sealed record ProcessInboundNpsMessageCommand(string RawXml, string? Source = null)
    : IRequest<NpsWebhookProcessingResult>;

public sealed class ProcessInboundNpsMessageCommandHandler(
    INpsWebhookProcessor webhookProcessor,
    ILogger<ProcessInboundNpsMessageCommandHandler> logger)
    : IRequestHandler<ProcessInboundNpsMessageCommand, NpsWebhookProcessingResult>
{
    public async Task<NpsWebhookProcessingResult> Handle(
        ProcessInboundNpsMessageCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await webhookProcessor.ProcessAsync(command.RawXml, cancellationToken);

            logger.LogInformation(
                "Accepted inbound NPS {MessageType} message on {Source} " +
                "({PlainXmlLength} chars decrypted, {HandlerCount} handler(s) invoked).",
                result.MessageType, command.Source, result.PlainXml.Length, result.HandlerCount);

            return new NpsWebhookProcessingResult(
                Accepted: true,
                MessageType: result.MessageType.ToString(),
                HandlerCount: result.HandlerCount,
                Error: null);
        }
        catch (NpsSecurityException ex)
        {
            // Mirror how NPS itself answers undecryptable/invalid messages: the caller
            // should respond HTTP 400. Handler failures (NpsIntegrationException) are
            // deliberately not swallowed — those are our fault, not the sender's.
            logger.LogWarning(ex,
                "Rejected inbound NPS webhook call on {Source}: decryption or signature validation failed.",
                command.Source);

            return new NpsWebhookProcessingResult(
                Accepted: false,
                MessageType: null,
                HandlerCount: 0,
                Error: "Message could not be decrypted or its signature is invalid.");
        }
    }
}
