using Integration.BusinessLogics.Nps.Logging;
using Integration.Infrastructures.Abstractions;
using Integration.Models.Nps;
using Microsoft.Extensions.Logging;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Inbound;
using Nibbs.Nps.Integration.Messages.Pacs;

namespace Integration.BusinessLogics.Nps.Handlers;

/// <summary>
/// Records every pacs.008 credit transfer NIBSS pushes to this institution.
/// </summary>
/// <remarks>
/// This is certification Phase 2 — NIBSS sends inbound pacs.008 traffic and the
/// institution is expected to observe it in its logs within 24-48 hours. The row this
/// writes is also what Phase 3 updates when the answering pacs.002 is dispatched.
/// <para>
/// Logging only. Deciding whether to accept the payment (and posting to the core banking
/// ledger) is a separate concern; this handler deliberately does not send the pacs.002,
/// so that a reply is never emitted for a payment that was not first recorded.
/// </para>
/// </remarks>
public sealed class InboundCreditTransferLogHandler(
    ISingleTransferLogStore logStore,
    IDateTimeService clock,
    ILogger<InboundCreditTransferLogHandler> logger)
    : INpsMessageHandler<Pacs008Document>
{
    public async Task HandleAsync(
        Pacs008Document document,
        NpsInboundMessage context,
        CancellationToken cancellationToken = default)
    {
        var messageId = document?.CreditTransfer?.GroupHeader?.MessageId;
        if (string.IsNullOrWhiteSpace(messageId))
        {
            logger.LogWarning(
                "Inbound pacs.008 carried no GrpHdr/MsgId; it cannot be logged or correlated. " +
                "Nothing will be able to answer it with a pacs.002.");
            return;
        }

        // NPS may redeliver a message. The unique index on (Direction, RequestUniqueId)
        // would reject the duplicate insert anyway; checking first makes a redelivery a
        // no-op rather than an error that would push a 500 back to the switch.
        if (await logStore.ExistsAsync(NpsLogDirection.Inbound, messageId, cancellationToken))
        {
            logger.LogInformation(
                "Inbound pacs.008 {MessageId} is already logged; treating this delivery as a redelivery.",
                messageId);
            return;
        }

        var receivedAt = clock.NowUTC;

        var log = SingleTransferLogMapper.FromPacs008(document, NpsLogDirection.Inbound, receivedAt);
        log.RequestJson = SingleTransferLogMapper.InboundRequestJson(document);

        // On an inbound row the request timestamps describe the message we received;
        // NibbsRespondedAt is later filled in with when we dispatched our pacs.002.
        log.RequestSentAt = receivedAt;

        // A failure here deliberately propagates. NpsWebhookProcessor surfaces handler
        // faults (they are our fault, not the sender's), so the switch sees a non-200 and
        // redelivers, rather than us silently accepting a payment we did not record.
        await logStore.AddAsync(log, cancellationToken);

        logger.LogInformation(
            "Logged inbound pacs.008 {MessageId} (TxId {TransactionId}) from {DebtorBankCode}: " +
            "{Currency} {Amount} to {CreditorAccountNumber}.",
            messageId, log.TransactionId, log.DebtorBankCode, log.Currency, log.Amount,
            log.CreditorAccountNumber);
    }
}
