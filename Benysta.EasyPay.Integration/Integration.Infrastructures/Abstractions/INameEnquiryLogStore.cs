using Integration.Models.Nps;

namespace Integration.Infrastructures.Abstractions;

/// <summary>
/// Persistence and correlation lookup for <see cref="NameEnquiryLog"/> rows — the
/// acmt.023 / acmt.024 request-response log.
/// </summary>
/// <remarks>
/// The grain is one row per account per enquiry, because an acmt.023 may carry up to 10
/// Vrfctn entries and the acmt.024 reports on each independently. Returned entities are
/// change-tracked; mutate them and then call <see cref="SaveChangesAsync"/>.
/// </remarks>
public interface INameEnquiryLogStore
{
    /// <summary>
    /// Inserts one or more rows for a single enquiry and commits them. Throws rather than
    /// reporting failure — an enquiry that cannot be logged should not be dispatched.
    /// </summary>
    Task AddRangeAsync(IReadOnlyCollection<NameEnquiryLog> logs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the row an acmt.024 report refers to. <paramref name="accountNumber"/> narrows
    /// a multi-account enquiry to the right row; when it is absent, the lookup only succeeds
    /// if the enquiry covered exactly one account.
    /// </summary>
    Task<NameEnquiryLog> FindAsync(
        NpsLogDirection direction,
        string messageId,
        string accountNumber = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether any row already exists for this direction and MsgId. Used to make inbound
    /// handling idempotent, since NPS may redeliver a message.
    /// </summary>
    Task<bool> ExistsAsync(
        NpsLogDirection direction,
        string messageId,
        CancellationToken cancellationToken = default);

    /// <summary>Commits pending changes to tracked log rows. Throws on failure.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
