using Integration.Models;
using Integration.Models.Nps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Integration.DatabaseAccess.Configurations;

public sealed class BulkTransferLogConfiguration : IEntityTypeConfiguration<BulkTransferLog>
{
    public void Configure(EntityTypeBuilder<BulkTransferLog> builder)
    {
        builder.ToTable("BulkTransferLogs");

        builder.HasKey(x => x.BulkTransferLogId);

        // ULID text key. ValueGeneratedNever because the application supplies it (EntityId.New);
        // collation "C" so the index orders by code point, which is what makes the ULID
        // timestamp prefix give creation-ordered keys under any database locale.
        builder.Property(x => x.BulkTransferLogId)
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
        builder.Property(x => x.PaymentInformationId).HasMaxLength(NpsLogColumn.MessageId).IsRequired();
        builder.Property(x => x.ResponseMessageId).HasMaxLength(NpsLogColumn.MessageId);

        builder.Property(x => x.ControlSum).HasPrecision(18, 2);
        builder.Property(x => x.SuccessfulAmount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(NpsLogColumn.Currency);
        builder.Property(x => x.RequestedExecutionDate).HasMaxLength(NpsLogColumn.WireDateTime);
        builder.Property(x => x.ChargeBearer).HasMaxLength(NpsLogColumn.ShortCode);

        builder.Property(x => x.InitiatingPartyName).HasMaxLength(NpsLogColumn.PartyName);
        builder.Property(x => x.ForwardingAgentBic).HasMaxLength(NpsLogColumn.BankCode);

        builder.Property(x => x.DebtorBankCode).HasMaxLength(NpsLogColumn.BankCode);
        builder.Property(x => x.DebtorAccountNumber).HasMaxLength(NpsLogColumn.AccountNumber);
        builder.Property(x => x.DebtorAccountName).HasMaxLength(NpsLogColumn.PartyName);
        builder.Property(x => x.DebtorName).HasMaxLength(NpsLogColumn.PartyName);

        builder.Property(x => x.ChannelCode).HasMaxLength(NpsLogColumn.ShortCode);
        builder.Property(x => x.TransactionLocation).HasMaxLength(NpsLogColumn.Location);
        builder.Property(x => x.MandateCode).HasMaxLength(NpsLogColumn.Code);

        builder.Property(x => x.RejectionReasonCode).HasMaxLength(NpsLogColumn.Code);
        builder.Property(x => x.ErrorMessage).HasMaxLength(NpsLogColumn.ErrorMessage);
        builder.Property(x => x.GroupStatus).HasMaxLength(NpsLogColumn.ShortCode);

        builder.HasIndex(x => new { x.Direction, x.RequestUniqueId })
            .IsUnique()
            .HasDatabaseName("UX_BulkTransferLogs_Direction_RequestUniqueId");

        // pain.002 correlates on OrgnlPmtInfId, which is the only batch identifier present
        // when the report omits the group header id.
        builder.HasIndex(x => new { x.Direction, x.PaymentInformationId })
            .IsUnique()
            .HasDatabaseName("UX_BulkTransferLogs_Direction_PaymentInformationId");

        builder.HasIndex(x => new { x.Status, x.DateCreated })
            .HasDatabaseName("IX_BulkTransferLogs_Status_DateCreated");

        builder.HasIndex(x => new { x.DebtorAccountNumber, x.DateCreated })
            .HasDatabaseName("IX_BulkTransferLogs_DebtorAccountNumber_DateCreated");

        builder.HasMany(x => x.Items)
            .WithOne(x => x.BulkTransfer)
            .HasForeignKey(x => x.BulkTransferLogId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
