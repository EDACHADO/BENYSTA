namespace Integration.BusinessLogics.Nps.Models;

/// <summary>
/// Outcome of processing an inbound NPS webhook payload
/// (see <see cref="Commands.ProcessInboundNpsMessageCommand"/>).
/// </summary>
/// <param name="Accepted">
/// True when the payload was decrypted, signature-verified and dispatched to the registered
/// message handlers; false when decryption or signature validation failed (answer HTTP 400).
/// </param>
/// <param name="MessageType">The detected ISO 20022 message type, e.g. "Pacs008".</param>
/// <param name="HandlerCount">How many registered message handlers were invoked.</param>
/// <param name="Error">Why the payload was not accepted, when <paramref name="Accepted"/> is false.</param>
public sealed record NpsWebhookProcessingResult(
    bool Accepted,
    string? MessageType,
    int HandlerCount,
    string? Error);
