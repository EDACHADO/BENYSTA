using Integration.DatabaseAccess.Context;
using Integration.Infrastructures.Abstractions;
using Integration.Models;
using Integration.Models.Nps;
using Microsoft.EntityFrameworkCore;

namespace Integration.DatabaseAccess.Concrete;

/// <summary>
/// EF Core implementation of <see cref="INameEnquiryLogStore"/>.
/// </summary>
/// <remarks>
/// Uses <see cref="NibssNpsDbContext"/> directly rather than <c>IRepo&lt;NameEnquiryLog&gt;</c>
/// for the same reason as <see cref="SingleTransferLogStore"/>: <c>BaseRepo.SaveContextAsync</c>
/// turns a failed write into a return value instead of an exception.
/// </remarks>
public sealed class NameEnquiryLogStore(NibssNpsDbContext db) : INameEnquiryLogStore
{
    public async Task AddRangeAsync(
        IReadOnlyCollection<NameEnquiryLog> logs,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(logs);
        if (logs.Count == 0)
            return;

        foreach (var log in logs)
        {
            if (string.IsNullOrEmpty(log.NameEnquiryLogId))
                log.NameEnquiryLogId = EntityId.New();
        }

        await db.NameEnquiryLogs.AddRangeAsync(logs, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<NameEnquiryLog> FindAsync(
        NpsLogDirection direction,
        string messageId,
        string accountNumber = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            return null;

        if (!string.IsNullOrWhiteSpace(accountNumber))
        {
            return await db.NameEnquiryLogs.FirstOrDefaultAsync(
                x => x.Direction == direction
                     && x.RequestUniqueId == messageId
                     && x.AccountNumber == accountNumber,
                cancellationToken);
        }

        // Without an account number the report is only unambiguous when the enquiry covered
        // a single account. Guessing between siblings would attach a verification result to
        // the wrong account, so return nothing instead.
        var candidates = await db.NameEnquiryLogs
            .Where(x => x.Direction == direction && x.RequestUniqueId == messageId)
            .Take(2)
            .ToListAsync(cancellationToken);

        return candidates.Count == 1 ? candidates[0] : null;
    }

    public Task<bool> ExistsAsync(
        NpsLogDirection direction,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            return Task.FromResult(false);

        return db.NameEnquiryLogs
            .AnyAsync(x => x.Direction == direction && x.RequestUniqueId == messageId, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);
}
