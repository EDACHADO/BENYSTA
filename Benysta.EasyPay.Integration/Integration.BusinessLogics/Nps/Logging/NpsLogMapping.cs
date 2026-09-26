using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Integration.BusinessLogics.Nps.Models;
using Integration.Models.Nps;
using Nibbs.Nps.Integration.Constants;

namespace Integration.BusinessLogics.Nps.Logging;

/// <summary>
/// Shared mapping between the NPS wire vocabulary and the request/response log tables:
/// status-code translation, the compact JSON summaries stored in the NibbsResponse and
/// WebhookResponse columns, and length-safe writes.
/// </summary>
internal static class NpsLogMapping
{
    private static readonly JsonSerializerOptions Compact = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public static string? ToJson<T>(T value)
        => value is null ? null : JsonSerializer.Serialize(value, Compact);

    /// <summary>
    /// Trims a value to the column width. Log writes must never fail on an over-long
    /// value — an exception message or a non-conforming inbound field would otherwise
    /// abort the insert and lose the record entirely.
    /// </summary>
    public static string? Fit(string? value, int maxLength)
        => value is null || value.Length <= maxLength ? value : value[..maxLength];

    /// <summary>Parses an ISO 20022 amount string. Always invariant — never the current culture.</summary>
    public static decimal ParseAmount(string? value)
        => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : 0m;

    /// <summary>How the switch answered the dispatch, mapped onto the log lifecycle.</summary>
    /// <remarks>
    /// Takes the whole result rather than just the status, because
    /// <see cref="NpsDispatchStatus.Accepted"/> only means "the switch answered and it was
    /// not an admi.002 rejection or an empty-body 400" — it does not mean HTTP 2xx. That is
    /// carried by <see cref="NpsDispatchResult.AcknowledgedBySwitch"/>, and without checking
    /// it a 4xx/5xx with an unparseable body would be recorded as Acknowledged, leaving the
    /// row waiting forever for a webhook that is never coming.
    /// </remarks>
    public static NpsLogStatus FromDispatch(NpsDispatchResult result) => result.Status switch
    {
        NpsDispatchStatus.Accepted => result.AcknowledgedBySwitch
            ? NpsLogStatus.Acknowledged
            : NpsLogStatus.DispatchFailed,
        NpsDispatchStatus.Rejected => NpsLogStatus.Rejected,
        NpsDispatchStatus.DecryptionFailure => NpsLogStatus.DecryptionFailed,
        NpsDispatchStatus.DispatchFailed => NpsLogStatus.DispatchFailed,
        _ => NpsLogStatus.Pending,
    };

    /// <summary>
    /// The failure detail to persist. The dispatcher leaves <c>Error</c> null for an
    /// unacknowledged "Accepted", so a reason is synthesized rather than storing nothing.
    /// </summary>
    public static string? DispatchError(NpsDispatchResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.Error))
            return Fit(result.Error, NpsLogColumn.ErrorMessage);

        if (result.Status == NpsDispatchStatus.Accepted && !result.AcknowledgedBySwitch)
            return Fit(
                $"NPS returned HTTP {result.NibssStatusCode} with a body that was not an admi.002 " +
                "rejection. The message was not accepted and no status report will follow.",
                NpsLogColumn.ErrorMessage);

        return null;
    }

    /// <summary>
    /// Maps an ISO 20022 TxSts/GrpSts onto the log lifecycle. ACSC is the only settled
    /// success; the ACCP/ACSP/ACTC family means accepted-but-not-settled, so a further
    /// report is still expected.
    /// </summary>
    public static NpsLogStatus FromTransactionStatus(string? status) => status?.Trim().ToUpperInvariant() switch
    {
        TransactionStatus.AcceptedSettlementCompleted => NpsLogStatus.Successful,
        TransactionStatus.Rejected => NpsLogStatus.Failed,
        TransactionStatus.PartiallyAccepted => NpsLogStatus.PartiallySuccessful,
        TransactionStatus.AcceptedCustomerProfile
            or TransactionStatus.AcceptedSettlementInProcess
            or TransactionStatus.AcceptedTechnicalValidation
            or TransactionStatus.Pending => NpsLogStatus.AcceptedForProcessing,

        // An unrecognised code must not be silently treated as success.
        _ => NpsLogStatus.AcceptedForProcessing,
    };

    /// <summary>
    /// Whether the row has reached an outcome that a later callback must not overwrite.
    /// Guards against a redelivered or out-of-order report regressing a settled payment.
    /// </summary>
    public static bool IsTerminal(NpsLogStatus status) => status
        is NpsLogStatus.Successful
        or NpsLogStatus.Failed
        or NpsLogStatus.PartiallySuccessful
        or NpsLogStatus.Rejected;

    /// <summary>
    /// The compact JSON written to NibbsResponse: the switch's synchronous answer, which
    /// only acknowledges receipt. Parsed fields only — no ISO 20022 XML is retained.
    /// </summary>
    public static string? DispatchSummary(NpsDispatchResult result) => ToJson(new
    {
        dispatchStatus = result.Status.ToString(),
        httpStatusCode = result.NibssStatusCode,
        acknowledgedBySwitch = result.AcknowledgedBySwitch,
        messageId = result.MessageId,
        reasonCode = result.ReasonCode,
        rejectedMessageId = result.RejectedMessageId,
        error = Fit(result.Error, NpsLogColumn.ErrorMessage),
    });
}
