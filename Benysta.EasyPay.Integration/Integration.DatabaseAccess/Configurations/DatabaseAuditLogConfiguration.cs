using Integration.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Integration.DatabaseAccess.Configurations;

/// <summary>
/// Declares the audit log key the same way as the NPS log tables, so every table in the
/// schema has an identical ULID key column.
/// </summary>
public sealed class DatabaseAuditLogConfiguration : IEntityTypeConfiguration<DatabaseAuditLog>
{
    public void Configure(EntityTypeBuilder<DatabaseAuditLog> builder)
    {
        builder.HasKey(x => x.DatabaseAuditLogId);

        builder.Property(x => x.DatabaseAuditLogId)
            .HasMaxLength(EntityId.Length)
            .UseCollation(EntityId.Collation)
            .ValueGeneratedNever()
            .IsRequired();

        // Newest-first is the only way this table is ever read.
        builder.HasIndex(x => x.StartTime)
            .HasDatabaseName("IX_DatabaseAuditLogs_StartTime");

        // "show me everything that happened to this row".
        builder.HasIndex(x => new { x.TableName, x.RecordId })
            .HasDatabaseName("IX_DatabaseAuditLogs_TableName_RecordId");
    }
}
