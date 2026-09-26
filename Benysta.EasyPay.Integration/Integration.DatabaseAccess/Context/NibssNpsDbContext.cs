using Integration.Infrastructures.Abstractions;
using Integration.Models;
using Integration.Models.Nps;
using Integration.Models.AbstractModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data;

namespace Integration.DatabaseAccess.Context;

public class NibssNpsDbContext : DbContext
{
    private readonly IDateTimeService _dateTimeService;
    private readonly List<DatabaseAuditLog> _auditEntries;
    private readonly AuditLogProcessor _auditLogProcessor;

    public NibssNpsDbContext(DbContextOptions<NibssNpsDbContext> options,
        IDateTimeService dateTimeService, List<DatabaseAuditLog> auditEntries, AuditLogProcessor auditLogProcessor) : base(options)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        _dateTimeService = dateTimeService;
        _auditEntries = auditEntries;
        _auditLogProcessor = auditLogProcessor;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        optionsBuilder.AddInterceptors(new AuditInterceptor(_auditEntries, _auditLogProcessor, _dateTimeService));
    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(NibssNpsDbContext).Assembly);

        // Apply configuration for all entities that inherit from BaseAudit
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseAudit).IsAssignableFrom(entityType.ClrType))
            {
                // BaseAudit.RowVersion is deliberately left unmapped on PostgreSQL.
                //
                // Mapping it as a rowversion (IsRowVersion) yields a bytea column with
                // ValueGenerated.OnAddOrUpdate, but PostgreSQL has nothing that populates
                // such a column: it stays NULL, and EF then emits
                // "WHERE ... AND RowVersion = @p" on every UPDATE, which can never match —
                // so the first update of any row throws DbUpdateConcurrencyException.
                //
                // The Npgsql-native token is the system xmin column, but the provider's
                // UseXminAsConcurrencyToken() was removed in 10.0.3 and the migrations
                // differ no longer skips an "xmin" column, so a shadow mapping emits
                // CREATE TABLE ("xmin" xid ...) which PostgreSQL rejects (42701: conflicts
                // with a system column name).
                //
                // These tables therefore carry no EF concurrency token. Concurrent webhook
                // redeliveries must be serialized with a guarded UPDATE (filter on the
                // expected current Status) rather than an optimistic token.
                builder.Entity(entityType.ClrType).Ignore(nameof(BaseAudit.RowVersion));
            }
        }

        // Iterate through all EF Entity types
        //DbContextHelper.TemporalTableAutomaticBuilder(builder); //Add Query Filter for SoftDelete
        DbContextHelper.SoftDeleteAutomaticBuilder(builder); //Add Query Filter for SoftDelete

        DbContextHelper.UniqueKeyAutomaticBuilder(builder); // Unique key and composite Key automation

        builder.HasDefaultSchema("Core");
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        ApplyUtcDateTimeConversion();
        foreach (var entry in ChangeTracker.Entries<BaseAudit>().ToList())
        {
            if (entry.State.Equals(EntityState.Added))
            {
                entry.Entity.CreatedBy = "SYSTEM";
                entry.Entity.DateCreated = _dateTimeService.NowUTC;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyUtcDateTimeConversion()
    {
        var entities = ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entityEntry in entities)
        {
            foreach (var propertyEntry in entityEntry.Properties)
            {
                if (propertyEntry.CurrentValue is DateTime dateTime)
                {
                    if (propertyEntry.Metadata.IsNullable && (dateTime.Kind == DateTimeKind.Unspecified || dateTime.Kind != DateTimeKind.Utc))
                    {
                        propertyEntry.CurrentValue = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                    }
                    else if (dateTime.Kind != DateTimeKind.Utc)
                    {
                        propertyEntry.CurrentValue = dateTime.ToUniversalTime();
                    }
                }
            }
        }
    }
    #region Core and Core Model
    public DbSet<DatabaseAuditLog> DatabaseAuditLogs { get; set; }
    #endregion

    #region NIBSS NPS request/response logs
    /// <summary>acmt.023 name enquiries and the acmt.024 reports that answer them.</summary>
    public DbSet<NameEnquiryLog> NameEnquiryLogs { get; set; }

    /// <summary>pacs.008 single credit transfers and their pacs.002 status reports.</summary>
    public DbSet<SingleTransferLog> SingleTransferLogs { get; set; }

    /// <summary>pain.001 bulk credit transfer initiations and their pain.002 status reports.</summary>
    public DbSet<BulkTransferLog> BulkTransferLogs { get; set; }

    /// <summary>Individual transactions inside a bulk batch, one per pain.002 TxInfAndSts entry.</summary>
    public DbSet<BulkTransferItemLog> BulkTransferItemLogs { get; set; }
    #endregion
}