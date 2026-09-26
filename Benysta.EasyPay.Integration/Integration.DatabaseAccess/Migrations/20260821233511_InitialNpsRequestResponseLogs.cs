using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Integration.DatabaseAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialNpsRequestResponseLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Core");

            migrationBuilder.CreateTable(
                name: "BulkTransferLogs",
                schema: "Core",
                columns: table => new
                {
                    BulkTransferLogId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false, collation: "C"),
                    Direction = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    RequestUniqueId = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    RequestCreationDateTime = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    PaymentInformationId = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    NumberOfTransactions = table.Column<int>(type: "integer", nullable: false),
                    ControlSum = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    BatchBooking = table.Column<bool>(type: "boolean", nullable: false),
                    RequestedExecutionDate = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    ChargeBearer = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    InitiatingPartyName = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    ForwardingAgentBic = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    DebtorBankCode = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    DebtorAccountNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DebtorAccountName = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    DebtorName = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    ChannelCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    TransactionLocation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FixedCollectionAmount = table.Column<bool>(type: "boolean", nullable: false),
                    MandateCode = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    RequestJson = table.Column<string>(type: "text", nullable: true),
                    NibbsResponse = table.Column<string>(type: "text", nullable: true),
                    WebhookResponse = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    NibbsHttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    AcknowledgedBySwitch = table.Column<bool>(type: "boolean", nullable: false),
                    RejectionReasonCode = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResponseMessageId = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    GroupStatus = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SuccessfulTransactionCount = table.Column<int>(type: "integer", nullable: false),
                    FailedTransactionCount = table.Column<int>(type: "integer", nullable: false),
                    PendingTransactionCount = table.Column<int>(type: "integer", nullable: false),
                    SuccessfulAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RequestSentAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NibbsRespondedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    WebhookReceivedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    WebhookCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(75)", maxLength: 75, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SoftDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkTransferLogs", x => x.BulkTransferLogId);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseAuditLogs",
                schema: "Core",
                columns: table => new
                {
                    DatabaseAuditLogId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false, collation: "C"),
                    TableName = table.Column<string>(type: "text", nullable: true),
                    RecordId = table.Column<string>(type: "text", nullable: true),
                    OperationType = table.Column<string>(type: "text", nullable: true),
                    MetaData = table.Column<string>(type: "text", nullable: true),
                    StartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Succeded = table.Column<bool>(type: "boolean", nullable: false),
                    Author = table.Column<string>(type: "text", nullable: true),
                    DateModified = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseAuditLogs", x => x.DatabaseAuditLogId);
                });

            migrationBuilder.CreateTable(
                name: "NameEnquiryLogs",
                schema: "Core",
                columns: table => new
                {
                    NameEnquiryLogId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false, collation: "C"),
                    Direction = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    RequestUniqueId = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    VerificationId = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    RequestCreationDateTime = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    BankCode = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    AccountNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequestedPartyName = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    RequestingBankCode = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    RequestingPartyName = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    RequestJson = table.Column<string>(type: "text", nullable: true),
                    NibbsResponse = table.Column<string>(type: "text", nullable: true),
                    WebhookResponse = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    NibbsHttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    AcknowledgedBySwitch = table.Column<bool>(type: "boolean", nullable: false),
                    RejectionReasonCode = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResponseMessageId = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    Verified = table.Column<bool>(type: "boolean", nullable: true),
                    ResolvedAccountName = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    AccountDesignation = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IdType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IdValue = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    AccountTier = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    RiskRating = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    RequestSentAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NibbsRespondedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    WebhookReceivedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    WebhookCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(75)", maxLength: 75, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SoftDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NameEnquiryLogs", x => x.NameEnquiryLogId);
                });

            migrationBuilder.CreateTable(
                name: "SingleTransferLogs",
                schema: "Core",
                columns: table => new
                {
                    SingleTransferLogId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false, collation: "C"),
                    Direction = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    RequestUniqueId = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    RequestCreationDateTime = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    InstructionId = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    EndToEndId = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    TransactionId = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    SettlementDate = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    TransactionTypeCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    DebtorBankCode = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    DebtorAccountNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DebtorAccountName = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    DebtorName = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    CreditorBankCode = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    CreditorAccountNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreditorAccountName = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    CreditorName = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    Narration = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    ChannelCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    TransactionLocation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NameEnquiryMessageId = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    RiskRating = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    RequestJson = table.Column<string>(type: "text", nullable: true),
                    NibbsResponse = table.Column<string>(type: "text", nullable: true),
                    WebhookResponse = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    NibbsHttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    AcknowledgedBySwitch = table.Column<bool>(type: "boolean", nullable: false),
                    RejectionReasonCode = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResponseMessageId = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    GroupStatus = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    TransactionStatus = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    StatusId = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    StatusReasonCode = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    StatusReasonInformation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StatusQueryCount = table.Column<int>(type: "integer", nullable: false),
                    LastStatusQueryAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastStatusQueryMessageId = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    RequestSentAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    NibbsRespondedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    WebhookReceivedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    WebhookCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(75)", maxLength: 75, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SoftDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SingleTransferLogs", x => x.SingleTransferLogId);
                });

            migrationBuilder.CreateTable(
                name: "BulkTransferItemLogs",
                schema: "Core",
                columns: table => new
                {
                    BulkTransferItemLogId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false, collation: "C"),
                    BulkTransferLogId = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false, collation: "C"),
                    ItemSequence = table.Column<int>(type: "integer", nullable: false),
                    EndToEndId = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    InstructionId = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    CreditorBankCode = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    CreditorAccountNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreditorAccountName = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    CreditorName = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    Narration = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    AccountDesignation = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IdType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IdValue = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    AccountTier = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    NameEnquiryMessageId = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    Status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    TransactionStatus = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    StatusId = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    StatusReasonCode = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    StatusReasonInformation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WebhookResponse = table.Column<string>(type: "text", nullable: true),
                    WebhookReceivedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    WebhookCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(75)", maxLength: 75, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SoftDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkTransferItemLogs", x => x.BulkTransferItemLogId);
                    table.ForeignKey(
                        name: "FK_BulkTransferItemLogs_BulkTransferLogs_BulkTransferLogId",
                        column: x => x.BulkTransferLogId,
                        principalSchema: "Core",
                        principalTable: "BulkTransferLogs",
                        principalColumn: "BulkTransferLogId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BulkTransferItemLogs_CreditorBankCode_CreditorAccountNumber",
                schema: "Core",
                table: "BulkTransferItemLogs",
                columns: new[] { "CreditorBankCode", "CreditorAccountNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_BulkTransferItemLogs_SoftDeleted",
                schema: "Core",
                table: "BulkTransferItemLogs",
                column: "SoftDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BulkTransferItemLogs_Status_DateCreated",
                schema: "Core",
                table: "BulkTransferItemLogs",
                columns: new[] { "Status", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "UX_BulkTransferItemLogs_BulkTransferLogId_ItemSequence",
                schema: "Core",
                table: "BulkTransferItemLogs",
                columns: new[] { "BulkTransferLogId", "ItemSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_BulkTransferItemLogs_EndToEndId",
                schema: "Core",
                table: "BulkTransferItemLogs",
                column: "EndToEndId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BulkTransferLogs_DebtorAccountNumber_DateCreated",
                schema: "Core",
                table: "BulkTransferLogs",
                columns: new[] { "DebtorAccountNumber", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "IX_BulkTransferLogs_SoftDeleted",
                schema: "Core",
                table: "BulkTransferLogs",
                column: "SoftDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_BulkTransferLogs_Status_DateCreated",
                schema: "Core",
                table: "BulkTransferLogs",
                columns: new[] { "Status", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "UX_BulkTransferLogs_Direction_PaymentInformationId",
                schema: "Core",
                table: "BulkTransferLogs",
                columns: new[] { "Direction", "PaymentInformationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_BulkTransferLogs_Direction_RequestUniqueId",
                schema: "Core",
                table: "BulkTransferLogs",
                columns: new[] { "Direction", "RequestUniqueId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseAuditLogs_StartTime",
                schema: "Core",
                table: "DatabaseAuditLogs",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseAuditLogs_TableName_RecordId",
                schema: "Core",
                table: "DatabaseAuditLogs",
                columns: new[] { "TableName", "RecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_NameEnquiryLogs_BankCode_AccountNumber",
                schema: "Core",
                table: "NameEnquiryLogs",
                columns: new[] { "BankCode", "AccountNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_NameEnquiryLogs_Direction_RequestUniqueId",
                schema: "Core",
                table: "NameEnquiryLogs",
                columns: new[] { "Direction", "RequestUniqueId" });

            migrationBuilder.CreateIndex(
                name: "IX_NameEnquiryLogs_SoftDeleted",
                schema: "Core",
                table: "NameEnquiryLogs",
                column: "SoftDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_NameEnquiryLogs_Status_DateCreated",
                schema: "Core",
                table: "NameEnquiryLogs",
                columns: new[] { "Status", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "IX_NameEnquiryLogs_VerificationId",
                schema: "Core",
                table: "NameEnquiryLogs",
                column: "VerificationId");

            migrationBuilder.CreateIndex(
                name: "UX_NameEnquiryLogs_Direction_RequestUniqueId_AccountNumber",
                schema: "Core",
                table: "NameEnquiryLogs",
                columns: new[] { "Direction", "RequestUniqueId", "AccountNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SingleTransferLogs_DebtorAccountNumber_DateCreated",
                schema: "Core",
                table: "SingleTransferLogs",
                columns: new[] { "DebtorAccountNumber", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "IX_SingleTransferLogs_EndToEndId",
                schema: "Core",
                table: "SingleTransferLogs",
                column: "EndToEndId");

            migrationBuilder.CreateIndex(
                name: "IX_SingleTransferLogs_InstructionId",
                schema: "Core",
                table: "SingleTransferLogs",
                column: "InstructionId");

            migrationBuilder.CreateIndex(
                name: "IX_SingleTransferLogs_NameEnquiryMessageId",
                schema: "Core",
                table: "SingleTransferLogs",
                column: "NameEnquiryMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_SingleTransferLogs_SoftDeleted",
                schema: "Core",
                table: "SingleTransferLogs",
                column: "SoftDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SingleTransferLogs_Status_DateCreated",
                schema: "Core",
                table: "SingleTransferLogs",
                columns: new[] { "Status", "DateCreated" });

            migrationBuilder.CreateIndex(
                name: "UX_SingleTransferLogs_Direction_RequestUniqueId",
                schema: "Core",
                table: "SingleTransferLogs",
                columns: new[] { "Direction", "RequestUniqueId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_SingleTransferLogs_Direction_TransactionId",
                schema: "Core",
                table: "SingleTransferLogs",
                columns: new[] { "Direction", "TransactionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BulkTransferItemLogs",
                schema: "Core");

            migrationBuilder.DropTable(
                name: "DatabaseAuditLogs",
                schema: "Core");

            migrationBuilder.DropTable(
                name: "NameEnquiryLogs",
                schema: "Core");

            migrationBuilder.DropTable(
                name: "SingleTransferLogs",
                schema: "Core");

            migrationBuilder.DropTable(
                name: "BulkTransferLogs",
                schema: "Core");
        }
    }
}
