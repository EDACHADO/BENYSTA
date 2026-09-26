using Integration.Models;
using Integration.Models.Nps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Integration.DatabaseAccess.Configurations;

public sealed class BulkTransferItemLogConfiguration : IEntityTypeConfiguration<BulkTransferItemLog>
{
    public void Configure(EntityTypeBuilder<BulkTransferItemLog> builder)
    {
        builder.ToTable("BulkTransferItemLogs");

        builder.HasKey(x => x.BulkTransferItemLogId);

        // ULID text key. ValueGeneratedNever because the application supplies it (EntityId.New);
        // collation "C" so the index orders by code point, which is what makes the ULID
        // timestamp prefix give creation-ordered keys under any database locale.
        builder.Property(x => x.BulkTransferItemLogId)
            .HasMaxLength(EntityId.Length)
            .UseCollation(EntityId.Collation)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(NpsLogColumn.EnumName)
            .IsRequired();

        builder.Property(x => x.BulkTransferLogId)
            .HasMaxLength(EntityId.Length)
            .UseCollation(EntityId.Collation)
            .IsRequired();

        builder.Property(x => x.EndToEndId).HasMaxLength(NpsLogColumn.MessageId).IsRequired();
        builder.Property(x => x.InstructionId).HasMaxLength(NpsLogColumn.MessageId);
        builder.Property(x => x.NameEnquiryMessageId).HasMaxLength(NpsLogColumn.MessageId);

        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(NpsLogColumn.Currency);

        builder.Property(x => x.CreditorBankCode).HasMaxLength(NpsLogColumn.BankCode).IsRequired();
        builder.Property(x => x.CreditorAccountNumber).HasMaxLength(NpsLogColumn.AccountNumber).IsRequired();
        builder.Property(x => x.CreditorAccountName).HasMaxLength(NpsLogColumn.PartyName);
        builder.Property(x => x.CreditorName).HasMaxLength(NpsLogColumn.PartyName);
        builder.Property(x => x.Narration).HasMaxLength(NpsLogColumn.Narration);

        builder.Property(x => x.AccountDesignation).HasMaxLength(NpsLogColumn.ShortCode);
        builder.Property(x => x.IdType).HasMaxLength(NpsLogColumn.ShortCode);
        builder.Property(x => x.IdValue).HasMaxLength(NpsLogColumn.Code);
        builder.Property(x => x.AccountTier).HasMaxLength(NpsLogColumn.ShortCode);

        builder.Property(x => x.TransactionStatus).HasMaxLength(NpsLogColumn.ShortCode);
        builder.Property(x => x.StatusId).HasMaxLength(NpsLogColumn.ShortCode);
        builder.Property(x => x.StatusReasonCode).HasMaxLength(NpsLogColumn.Code);
        builder.Property(x => x.StatusReasonInformation).HasMaxLength(NpsLogColumn.ReasonInformation);

        // EndToEndId is the ONLY per-transaction identifier a pain.002 quotes, so it has to
        // resolve to exactly one item row across the whole table — not just within a batch.
        builder.HasIndex(x => x.EndToEndId)
            .IsUnique()
            .HasDatabaseName("UX_BulkTransferItemLogs_EndToEndId");

        builder.HasIndex(x => new { x.BulkTransferLogId, x.ItemSequence })
            .IsUnique()
            .HasDatabaseName("UX_BulkTransferItemLogs_BulkTransferLogId_ItemSequence");

        // Reconciliation sweep across batches: items still awaiting a TxInfAndSts entry.
        builder.HasIndex(x => new { x.Status, x.DateCreated })
            .HasDatabaseName("IX_BulkTransferItemLogs_Status_DateCreated");

        builder.HasIndex(x => new { x.CreditorBankCode, x.CreditorAccountNumber })
            .HasDatabaseName("IX_BulkTransferItemLogs_CreditorBankCode_CreditorAccountNumber");
    }
}
