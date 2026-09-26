using System.Text.Json;
using Integration.Infrastructures.Abstractions;
using Integration.Models;
using Integration.Models.Nps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Integration.DatabaseAccess.Context;

public class AuditInterceptor(List<DatabaseAuditLog> auditEntries, AuditLogProcessor auditLogProcessor, IDateTimeService dateTimeService) : SaveChangesInterceptor
{
    private readonly List<DatabaseAuditLog> _auditEntries = auditEntries;
    private readonly AuditLogProcessor _auditLogProcessor = auditLogProcessor;
    private readonly IDateTimeService _dateTimeService = dateTimeService;

    /// <summary>
    /// Entity types that are their own audit trail. Auditing them would write a second copy
    /// of every payment payload — including account numbers and BVN/NIN — into
    /// DatabaseAuditLogs for no extra information.
    /// </summary>
    private static readonly HashSet<Type> NotAudited =
    [
        typeof(DatabaseAuditLog),
        typeof(NameEnquiryLog),
        typeof(SingleTransferLog),
        typeof(BulkTransferLog),
        typeof(BulkTransferItemLog),
    ];

    // Both the sync and async save paths are intercepted. Only overriding the async pair
    // would leave SaveChanges() (used by DbContextValidationHelper) unaudited, and only
    // overriding the sync failure hook would leave failed async saves unaudited while
    // leaving their entries in _auditEntries to be misreported on the next successful save.

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Collect(eventData);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Collect(eventData);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Flush(succeeded: true, error: null);
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Flush(succeeded: true, error: null);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        Flush(succeeded: false, error: eventData.Exception?.Message);
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        Flush(succeeded: false, error: eventData.Exception?.Message);
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void Collect(DbContextEventData eventData)
    {
        if (eventData.Context is null)
            return;

        var startTime = _dateTimeService.NowUTC;

        var entries = eventData.Context.ChangeTracker.Entries()
            .Where(x => !NotAudited.Contains(x.Entity.GetType())
                        && x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(x => new DatabaseAuditLog
            {
                DatabaseAuditLogId = EntityId.New(),
                TableName = x.Metadata.GetTableName(),
                RecordId = PrimaryKey(x),
                OperationType = x.State.ToString(),
                MetaData = Describe(x),
                StartTime = startTime,
                EndTime = startTime,
                Author = "SYSTEM",
                DateModified = startTime,
            })
            .ToList();

        if (entries.Count > 0)
            _auditEntries.AddRange(entries);
    }

    /// <summary>
    /// Hands the collected entries to the background writer and clears the buffer.
    /// Clearing unconditionally matters: entries left behind would be re-reported against
    /// the next save on the same scope, with the wrong outcome.
    /// </summary>
    private void Flush(bool succeeded, string error)
    {
        if (_auditEntries.Count == 0)
            return;

        var endTime = _dateTimeService.NowUTC;

        foreach (var auditEntry in _auditEntries)
        {
            auditEntry.EndTime = endTime;
            auditEntry.Succeded = succeeded;
            auditEntry.ErrorMessage = error;
            _auditLogProcessor.Enqueue(auditEntry);
        }

        _auditEntries.Clear();
    }

    private static string PrimaryKey(EntityEntry entry)
    {
        var keys = entry.Properties
            .Where(p => p.Metadata.IsPrimaryKey())
            .Select(p => p.CurrentValue?.ToString())
            .Where(v => !string.IsNullOrEmpty(v))
            .ToList();

        return keys.Count > 0 ? string.Join("|", keys) : "0";
    }

    /// <summary>
    /// A compact JSON change description.
    /// </summary>
    /// <remarks>
    /// Replaces ChangeTracker DebugView.LongView, which was built as a debugger aid: it
    /// renders every property of every entry into an unbounded string on every save, and its
    /// format is not contractual. This records only what changed, and for a modification
    /// records both sides so the entry is actually useful for reconstructing history.
    /// </remarks>
    private static string Describe(EntityEntry entry)
    {
        switch (entry.State)
        {
            case EntityState.Modified:
                var changed = entry.Properties
                    .Where(p => p.IsModified)
                    .ToDictionary(
                        p => p.Metadata.Name,
                        p => (object)new { old = Render(p.OriginalValue), @new = Render(p.CurrentValue) });

                return JsonSerializer.Serialize(new { changed });

            case EntityState.Deleted:
                // Only the key: the row is going away, and the values were already captured
                // when it was written.
                return JsonSerializer.Serialize(new { deleted = PrimaryKey(entry) });

            default:
                var values = entry.Properties
                    .Where(p => p.CurrentValue is not null)
                    .ToDictionary(p => p.Metadata.Name, p => Render(p.CurrentValue));

                return JsonSerializer.Serialize(new { added = values });
        }
    }

    // Byte arrays and other opaque values are summarized rather than embedded.
    private static object Render(object value) => value switch
    {
        null => null,
        byte[] bytes => $"<{bytes.Length} bytes>",
        _ => value,
    };
}

/// <summary>
/// Writes audit entries to the database off the request path, draining a queue the
/// interceptor feeds.
/// </summary>
public class AuditLogProcessor(IServiceProvider serviceProvider, ILogger<AuditLogProcessor> logger)
    : BackgroundService
{
    /// <summary>
    /// Bounded on purpose. An unbounded queue turns a database outage into unbounded memory
    /// growth; dropping the oldest entry keeps the process alive and is visible in the logs.
    /// </summary>
    private const int Capacity = 10_000;

    /// <summary>Rows written per transaction.</summary>
    private const int BatchSize = 100;

    private readonly Channel<DatabaseAuditLog> _channel = Channel.CreateBounded<DatabaseAuditLog>(
        new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });

    private long _dropped;

    public void Enqueue(DatabaseAuditLog auditLog)
    {
        if (auditLog is null)
            return;

        if (!_channel.Writer.TryWrite(auditLog))
            Interlocked.Increment(ref _dropped);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<DatabaseAuditLog>(BatchSize);

        try
        {
            // ReadAllAsync yields as entries arrive; the batch is written whenever it fills
            // or the queue momentarily empties. The previous implementation saved only after
            // the loop, which meant never: the channel is never completed, and cancellation
            // throws straight past the save.
            await foreach (var auditLog in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                batch.Add(auditLog);

                if (batch.Count >= BatchSize || !_channel.Reader.TryPeek(out _))
                    await WriteBatchAsync(batch, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down.
        }
        finally
        {
            if (batch.Count > 0)
            {
                // Drain what is in hand on a best-effort basis, with a fresh token: the
                // stopping token is already cancelled by now.
                using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await WriteBatchAsync(batch, shutdown.Token);
            }

            var dropped = Interlocked.Read(ref _dropped);
            if (dropped > 0)
                logger.LogWarning("{Dropped} audit entries were dropped because the queue was full.", dropped);
        }
    }

    private async Task WriteBatchAsync(List<DatabaseAuditLog> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
            return;

        try
        {
            // A scope per batch, so the DbContext and its change tracker are short-lived.
            // Reusing one context for the lifetime of the process leaked every entry it had
            // ever tracked.
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NibssNpsDbContext>();

            dbContext.Set<DatabaseAuditLog>().AddRange(batch);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Never let a bad batch kill the background service — that would silently stop
            // all auditing for the lifetime of the process.
            logger.LogError(ex, "Failed to write {Count} audit entries; the batch was discarded.", batch.Count);
        }
        finally
        {
            batch.Clear();
        }
    }
}
