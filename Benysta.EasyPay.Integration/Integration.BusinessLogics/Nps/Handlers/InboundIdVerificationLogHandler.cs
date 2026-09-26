using Integration.BusinessLogics.Nps.Logging;
using Integration.Infrastructures.Abstractions;
using Integration.Models.Nps;
using Microsoft.Extensions.Logging;
using Nibbs.Nps.Integration.Abstractions;
using Nibbs.Nps.Integration.Inbound;
using Nibbs.Nps.Integration.Messages.Acmt;

namespace Integration.BusinessLogics.Nps.Handlers;

/// <summary>
/// Records every acmt.023 name enquiry NIBSS pushes to this institution — one row per
/// account in the enquiry.
/// </summary>
/// <remarks>
/// Must run before <see cref="InboundIdVerificationRequestHandler"/>, which answers the
/// enquiry with an acmt.024: that reply is recorded against the row this handler creates,
/// so if the order were reversed there would be nothing to update.
/// <c>AddNpsIdentificationVerificationFlow</c> registers this handler first to guarantee
/// that regardless of the order the startup extensions are called in.
/// </remarks>
public sealed class InboundIdVerificationLogHandler(
    INameEnquiryLogStore logStore,
    IDateTimeService clock,
    ILogger<InboundIdVerificationLogHandler> logger)
    : INpsMessageHandler<Acmt023Document>
{
    public async Task HandleAsync(
        Acmt023Document document,
        NpsInboundMessage context,
        CancellationToken cancellationToken = default)
    {
        var messageId = document?.VerificationRequest?.Assignment?.MessageId;
        if (string.IsNullOrWhiteSpace(messageId))
        {
            logger.LogWarning(
                "Inbound acmt.023 carried no Assgnmt/MsgId; it cannot be logged or correlated.");
            return;
        }

        if (await logStore.ExistsAsync(NpsLogDirection.Inbound, messageId, cancellationToken))
        {
            logger.LogInformation(
                "Inbound acmt.023 {MessageId} is already logged; treating this delivery as a redelivery.",
                messageId);
            return;
        }

        var receivedAt = clock.NowUTC;
        var rows = NameEnquiryLogMapper.FromAcmt023(document, NpsLogDirection.Inbound, receivedAt);

        if (rows.Count == 0)
        {
            logger.LogWarning(
                "Inbound acmt.023 {MessageId} carried no verification entry with an account number; " +
                "nothing to log.",
                messageId);
            return;
        }

        foreach (var row in rows)
            row.RequestJson = NameEnquiryLogMapper.InboundRequestJson(document, row.AccountNumber);

        // Propagates on failure: the switch then sees a non-200 and redelivers, rather than
        // us answering an enquiry we never recorded.
        await logStore.AddRangeAsync(rows, cancellationToken);

        logger.LogInformation(
            "Logged inbound acmt.023 {MessageId} from {RequestingBankCode} covering {AccountCount} account(s).",
            messageId, rows[0].RequestingBankCode, rows.Count);
    }
}
