using Integration.Models;
using Integration.Models.Nps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Integration.DatabaseAccess.Configurations;

public sealed class NameEnquiryLogConfiguration : IEntityTypeConfiguration<NameEnquiryLog>
{
    public void Configure(EntityTypeBuilder<NameEnquiryLog> builder)
    {
        builder.ToTable("NameEnquiryLogs");

        builder.HasKey(x => x.NameEnquiryLogId);

        // ULID text key. ValueGeneratedNever because the application supplies it (EntityId.New);
        // collation "C" so the index orders by code point, which is what makes the ULID
        // timestamp prefix give creation-ordered keys under any database locale.
        builder.Property(x => x.NameEnquiryLogId)
            .HasMaxLength(EntityId.Length)
            .UseCollation(EntityId.Collation)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.Direction)
            .HasConversion<string>()
            .HasMaxLength(NpsLogColumn.EnumName)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(NpsLogColumn.EnumName)
            .IsRequired();

        builder.Property(x => x.RequestUniqueId).HasMaxLength(NpsLogColumn.MessageId).IsRequired();
        builder.Property(x => x.VerificationId).HasMaxLength(NpsLogColumn.MessageId);
        builder.Property(x => x.RequestCreationDateTime).HasMaxLength(NpsLogColumn.WireDateTime);
        builder.Property(x => x.ResponseMessageId).HasMaxLength(NpsLogColumn.MessageId);

        builder.Property(x => x.BankCode).HasMaxLength(NpsLogColumn.BankCode).IsRequired();
        builder.Property(x => x.AccountNumber).HasMaxLength(NpsLogColumn.AccountNumber).IsRequired();
        builder.Property(x => x.RequestedPartyName).HasMaxLength(NpsLogColumn.PartyName);
        builder.Property(x => x.ResolvedAccountName).HasMaxLength(NpsLogColumn.PartyName);

        builder.Property(x => x.RequestingBankCode).HasMaxLength(NpsLogColumn.BankCode);
        builder.Property(x => x.RequestingPartyName).HasMaxLength(NpsLogColumn.PartyName);

        builder.Property(x => x.RejectionReasonCode).HasMaxLength(NpsLogColumn.Code);
        builder.Property(x => x.ErrorMessage).HasMaxLength(NpsLogColumn.ErrorMessage);

        builder.Property(x => x.AccountDesignation).HasMaxLength(NpsLogColumn.ShortCode);
        builder.Property(x => x.IdType).HasMaxLength(NpsLogColumn.ShortCode);
        builder.Property(x => x.IdValue).HasMaxLength(NpsLogColumn.Code);
        builder.Property(x => x.AccountTier).HasMaxLength(NpsLogColumn.ShortCode);
        builder.Property(x => x.RiskRating).HasMaxLength(NpsLogColumn.ShortCode);

        // One row per account per enquiry, not per enquiry: a single acmt.023 may carry up
        // to 10 Vrfctn entries, and the acmt.024 answers each one separately. The MsgId is
        // scoped by direction because an inbound enquiry from another participant could in
        // principle reuse a value we generated for an outbound one.
        builder.HasIndex(x => new { x.Direction, x.RequestUniqueId, x.AccountNumber })
            .IsUnique()
            .HasDatabaseName("UX_NameEnquiryLogs_Direction_RequestUniqueId_AccountNumber");

        // Correlating an acmt.024 starts from the MsgId alone, before the per-report
        // account number narrows it to a row.
        builder.HasIndex(x => new { x.Direction, x.RequestUniqueId })
            .HasDatabaseName("IX_NameEnquiryLogs_Direction_RequestUniqueId");

        // The acmt.024 reports quote Vrfctn/Id per account, so the handler may match on it
        // when OrgnlAssgnmt/MsgId is absent. Not unique: NIBSS does not guarantee it.
        builder.HasIndex(x => x.VerificationId)
            .HasDatabaseName("IX_NameEnquiryLogs_VerificationId");

        // Operational lookup: "every enquiry against this account".
        builder.HasIndex(x => new { x.BankCode, x.AccountNumber })
            .HasDatabaseName("IX_NameEnquiryLogs_BankCode_AccountNumber");

        // Reconciliation sweep: rows still awaiting an acmt.024.
        builder.HasIndex(x => new { x.Status, x.DateCreated })
            .HasDatabaseName("IX_NameEnquiryLogs_Status_DateCreated");
    }
}
