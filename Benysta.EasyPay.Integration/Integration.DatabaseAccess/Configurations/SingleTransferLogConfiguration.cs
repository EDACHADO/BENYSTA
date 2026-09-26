using Integration.Models;
using Integration.Models.Nps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Integration.DatabaseAccess.Configurations;

public sealed class SingleTransferLogConfiguration : IEntityTypeConfiguration<SingleTransferLog>
{
    public void Configure(EntityTypeBuilder<SingleTransferLog> builder)
    {
        builder.ToTable("SingleTransferLogs");

        builder.HasKey(x => x.SingleTransferLogId);

        // ULID text key. ValueGeneratedNever because the application supplies it (EntityId.New);
        // collation "C" so the index orders by code point, which is what makes the ULID
        // timestamp prefix give creation-ordered keys under any database locale.
        builder.Property(x => x.SingleTransferLogId)
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
        builder.Property(x => x.RequestCreationDateTime).HasMaxLength(NpsLogColumn.WireDateTime);
        builder.Property(x => x.InstructionId).HasMaxLength(NpsLogColumn.MessageId);
        builder.Property(x => x.EndToEndId).HasMaxLength(NpsLogColumn.MessageId);
        builder.Property(x => x.TransactionId).HasMaxLength(NpsLogColumn.MessageId);
        builder.Property(x => x.ResponseMessageId).HasMaxLength(NpsLogColumn.MessageId);
        builder.Property(x => x.LastStatusQueryMessageId).HasMaxLength(NpsLogColumn.MessageId);
        builder.Property(x => x.NameEnquiryMessageId).HasMaxLength(NpsLogColumn.MessageId);

        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(NpsLogColumn.Currency);
        builder.Property(x => x.SettlementDate).HasMaxLength(NpsLogColumn.WireDateTime);
        builder.Property(x => x.TransactionTypeCode).HasMaxLength(NpsLogColumn.ShortCode);

        builder.Property(x => x.DebtorBankCode).HasMaxLength(NpsLogColumn.BankCode);
        builder.Property(x => x.DebtorAccountNumber).HasMaxLength(NpsLogColumn.AccountNumber);
        builder.Property(x => x.DebtorAccountName).HasMaxLength(NpsLogColumn.PartyName);
        builder.Property(x => x.DebtorName).HasMaxLength(NpsLogColumn.PartyName);

        builder.Property(x => x.CreditorBankCode).HasMaxLength(NpsLogColumn.BankCode);
        builder.Property(x => x.CreditorAccountNumber).HasMaxLength(NpsLogColumn.AccountNumber);
        builder.Property(x => x.CreditorAccountName).HasMaxLength(NpsLogColumn.PartyName);
        builder.Property(x => x.CreditorName).HasMaxLength(NpsLogColumn.PartyName);

        builder.Property(x => x.Narration).HasMaxLength(NpsLogColumn.Narration);
        builder.Property(x => x.ChannelCode).HasMaxLength(NpsLogColumn.ShortCode);
        builder.Property(x => x.TransactionLocation).HasMaxLength(NpsLogColumn.Location);
        builder.Property(x => x.RiskRating).HasMaxLength(NpsLogColumn.ShortCode);

        builder.Property(x => x.RejectionReasonCode).HasMaxLength(NpsLogColumn.Code);
        builder.Property(x => x.ErrorMessage).HasMaxLength(NpsLogColumn.ErrorMessage);

        builder.Property(x => x.GroupStatus).HasMaxLength(NpsLogColumn.ShortCode);
        builder.Property(x => x.TransactionStatus).HasMaxLength(NpsLogColumn.ShortCode);
        builder.Property(x => x.StatusId).HasMaxLength(NpsLogColumn.ShortCode);
        builder.Property(x => x.StatusReasonCode).HasMaxLength(NpsLogColumn.Code);
        builder.Property(x => x.StatusReasonInformation).HasMaxLength(NpsLogColumn.ReasonInformation);

        builder.HasIndex(x => new { x.Direction, x.RequestUniqueId })
            .IsUnique()
            .HasDatabaseName("UX_SingleTransferLogs_Direction_RequestUniqueId");

        // TxId is the identifier the guide guarantees unique, and the one pacs.002 quotes as
        // OrgnlTxId — the primary webhook lookup after the group MsgId.
        builder.HasIndex(x => new { x.Direction, x.TransactionId })
            .IsUnique()
            .HasDatabaseName("UX_SingleTransferLogs_Direction_TransactionId");

        // Deliberately NOT unique: the guide states EndToEndId is not verified for
        // uniqueness, so a unique index here would reject legitimate traffic.
        builder.HasIndex(x => x.EndToEndId)
            .HasDatabaseName("IX_SingleTransferLogs_EndToEndId");

        builder.HasIndex(x => x.InstructionId)
            .HasDatabaseName("IX_SingleTransferLogs_InstructionId");

        // Reconciliation sweep: payments acknowledged but with no pacs.002 yet.
        builder.HasIndex(x => new { x.Status, x.DateCreated })
            .HasDatabaseName("IX_SingleTransferLogs_Status_DateCreated");

        // Customer-facing statement lookup.
        builder.HasIndex(x => new { x.DebtorAccountNumber, x.DateCreated })
            .HasDatabaseName("IX_SingleTransferLogs_DebtorAccountNumber_DateCreated");

        // Traces a payment back to the name enquiry that preceded it.
        builder.HasIndex(x => x.NameEnquiryMessageId)
            .HasDatabaseName("IX_SingleTransferLogs_NameEnquiryMessageId");
    }
}
