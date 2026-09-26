using Integration.Models.Nps;

namespace Integration.Infrastructures.Abstractions;

/// <summary>
/// Persistence and correlation lookup for <see cref="SingleTransferLog"/> rows — the
/// pacs.008 / pacs.002 request-response log.
/// </summary>
/// <remarks>
/// Deliberately narrow: it persists and it finds. Mapping an ISO 20022 document onto a
/// log row stays in the business-logic layer, which is the only layer that references
/// the NPS message models. Returned entities are change-tracked, so the caller mutates
/// them and then calls <see cref="SaveChangesAsync"/>.
/// </remarks>
public interface ISingleTransferLogStore
{
    /// <summary>
    /// Inserts a new log row and commits it. Throws rather than reporting failure, because
    /// a payment that could not be logged must not be dispatched.
    /// </summary>
    Task AddAsync(SingleTransferLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the outbound payment a pacs.002 or pacs.028 response refers to, trying the
    /// identifiers in order of the uniqueness NPS guarantees: TxId first (the only one the
    /// guide calls unique), then the group MsgId, then EndToEndId and InstrId as a last
    /// resort. Null when nothing matches.
    /// </summary>
    Task<SingleTransferLog> FindOutboundAsync(
        string transactionId = null,
        string messageId = null,
        string endToEndId = null,
        string instructionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the inbound pacs.008 that our pacs.002 answers, by its group MsgId.
    /// </summary>
    Task<SingleTransferLog> FindInboundByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether a row already exists for this direction and MsgId. Used to make inbound
    /// handling idempotent, since NPS may redeliver a message.
    /// </summary>
    Task<bool> ExistsAsync(
        NpsLogDirection direction,
        string messageId,
        CancellationToken cancellationToken = default);

    /// <summary>Commits pending changes to tracked log rows. Throws on failure.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
