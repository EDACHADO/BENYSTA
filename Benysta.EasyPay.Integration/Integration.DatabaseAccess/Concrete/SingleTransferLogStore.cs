using Integration.DatabaseAccess.Context;
using Integration.Infrastructures.Abstractions;
using Integration.Models;
using Integration.Models.Nps;
using Microsoft.EntityFrameworkCore;

namespace Integration.DatabaseAccess.Concrete;

/// <summary>
/// EF Core implementation of <see cref="ISingleTransferLogStore"/>.
/// </summary>
/// <remarks>
/// Uses <see cref="NibssNpsDbContext"/> directly rather than <c>IRepo&lt;SingleTransferLog&gt;</c>
/// on purpose: <c>BaseRepo.SaveContextAsync</c> catches every exception and returns a
/// <c>ResponseVm</c>, so a failed write would look like a value to inspect rather than an
/// error. A payment log has to fail loudly.
/// <para>
/// Reads are change-tracked (no AsNoTracking) because every caller mutates what it finds
/// and then calls <see cref="SaveChangesAsync"/>.
/// </para>
/// </remarks>
public sealed class SingleTransferLogStore(NibssNpsDbContext db) : ISingleTransferLogStore
{
    public async Task AddAsync(SingleTransferLog log, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(log);

        if (string.IsNullOrEmpty(log.SingleTransferLogId))
            log.SingleTransferLogId = EntityId.New();

        await db.SingleTransferLogs.AddAsync(log, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SingleTransferLog> FindOutboundAsync(
        string transactionId = null,
        string messageId = null,
        string endToEndId = null,
        string instructionId = null,
        CancellationToken cancellationToken = default)
    {
        // TxId is the only identifier the integration guide describes as unique, so it is
        // tried first. Each lookup is a separate query rather than one OR-ed predicate so a
        // weaker identifier can never win over a stronger one.
        if (!string.IsNullOrWhiteSpace(transactionId))
        {
            var byTransaction = await Outbound()
                .FirstOrDefaultAsync(x => x.TransactionId == transactionId, cancellationToken);
            if (byTransaction is not null)
                return byTransaction;
        }

        if (!string.IsNullOrWhiteSpace(messageId))
        {
            var byMessage = await Outbound()
                .FirstOrDefaultAsync(x => x.RequestUniqueId == messageId, cancellationToken);
            if (byMessage is not null)
                return byMessage;
        }

        // EndToEndId is explicitly not verified for uniqueness by NPS, so only accept it
        // when it resolves to exactly one row.
        if (!string.IsNullOrWhiteSpace(endToEndId))
        {
            var candidates = await Outbound()
                .Where(x => x.EndToEndId == endToEndId)
                .Take(2)
                .ToListAsync(cancellationToken);
            if (candidates.Count == 1)
                return candidates[0];
        }

        if (!string.IsNullOrWhiteSpace(instructionId))
        {
            var candidates = await Outbound()
                .Where(x => x.InstructionId == instructionId)
                .Take(2)
                .ToListAsync(cancellationToken);
            if (candidates.Count == 1)
                return candidates[0];
        }

        return null;
    }

    public Task<SingleTransferLog> FindInboundByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            return Task.FromResult<SingleTransferLog>(null);

        return db.SingleTransferLogs
            .FirstOrDefaultAsync(
                x => x.Direction == NpsLogDirection.Inbound && x.RequestUniqueId == messageId,
                cancellationToken);
    }

    public Task<bool> ExistsAsync(
        NpsLogDirection direction,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            return Task.FromResult(false);

        return db.SingleTransferLogs
            .AnyAsync(x => x.Direction == direction && x.RequestUniqueId == messageId, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);

    private IQueryable<SingleTransferLog> Outbound()
        => db.SingleTransferLogs.Where(x => x.Direction == NpsLogDirection.Outbound);
}
