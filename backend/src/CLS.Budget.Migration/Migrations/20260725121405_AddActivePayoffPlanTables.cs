using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddActivePayoffPlanTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivePayoffPlan",
                columns: table => new
                {
                    ActivePayoffPlanId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceSavedPayoffPlanId = table.Column<int>(type: "integer", nullable: true),
                    StartedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentVersionNumber = table.Column<int>(type: "integer", nullable: false),
                    StartingDebt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Goal = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Strategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExtraMonthlyPayment = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalMonthlyDebtPayment = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TargetUtilizationPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    PayOverLimitFirst = table.Column<bool>(type: "boolean", nullable: false),
                    PostUtilizationStrategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EnableCashAdvanceBalanceMoves = table.Column<bool>(type: "boolean", nullable: false),
                    LoanAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    LoanAnnualPercentageRate = table.Column<decimal>(type: "numeric(8,4)", nullable: true),
                    LoanApplyStrategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LoanApplyCreditCardIdsJson = table.Column<string>(type: "text", nullable: true),
                    LoanType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LoanTermMonths = table.Column<int>(type: "integer", nullable: true),
                    LoanInterestOnlyMonths = table.Column<int>(type: "integer", nullable: true),
                    LoanFixedMonthlyPayment = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    PromotionalTransfersJson = table.Column<string>(type: "text", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivePayoffPlan", x => x.ActivePayoffPlanId);
                });

            migrationBuilder.CreateTable(
                name: "PayoffPlanEvent",
                columns: table => new
                {
                    PayoffPlanEventId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivePayoffPlanId = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoffPlanEvent", x => x.PayoffPlanEventId);
                    table.ForeignKey(
                        name: "FK_PayoffPlanEvent_ActivePayoffPlan_ActivePayoffPlanId",
                        column: x => x.ActivePayoffPlanId,
                        principalTable: "ActivePayoffPlan",
                        principalColumn: "ActivePayoffPlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayoffPlanVersion",
                columns: table => new
                {
                    PayoffPlanVersionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivePayoffPlanId = table.Column<int>(type: "integer", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Goal = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Strategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExtraMonthlyPayment = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalMonthlyDebtPayment = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TargetUtilizationPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    PayOverLimitFirst = table.Column<bool>(type: "boolean", nullable: false),
                    PostUtilizationStrategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EnableCashAdvanceBalanceMoves = table.Column<bool>(type: "boolean", nullable: false),
                    LoanAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    LoanAnnualPercentageRate = table.Column<decimal>(type: "numeric(8,4)", nullable: true),
                    LoanApplyStrategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LoanApplyCreditCardIdsJson = table.Column<string>(type: "text", nullable: true),
                    LoanType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LoanTermMonths = table.Column<int>(type: "integer", nullable: true),
                    LoanInterestOnlyMonths = table.Column<int>(type: "integer", nullable: true),
                    LoanFixedMonthlyPayment = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    PromotionalTransfersJson = table.Column<string>(type: "text", nullable: true),
                    SnapshotDebt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ProjectedMonthsToPayoff = table.Column<int>(type: "integer", nullable: false),
                    ProjectedTotalInterest = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ProjectedPayoffDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ProjectionIsValid = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoffPlanVersion", x => x.PayoffPlanVersionId);
                    table.ForeignKey(
                        name: "FK_PayoffPlanVersion_ActivePayoffPlan_ActivePayoffPlanId",
                        column: x => x.ActivePayoffPlanId,
                        principalTable: "ActivePayoffPlan",
                        principalColumn: "ActivePayoffPlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayoffPlanPayment",
                columns: table => new
                {
                    PayoffPlanPaymentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivePayoffPlanId = table.Column<int>(type: "integer", nullable: false),
                    PayoffPlanVersionId = table.Column<int>(type: "integer", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsVoided = table.Column<bool>(type: "boolean", nullable: false),
                    VoidedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayoffPlanPayment", x => x.PayoffPlanPaymentId);
                    table.ForeignKey(
                        name: "FK_PayoffPlanPayment_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayoffPlanPayment_ActivePayoffPlan_ActivePayoffPlanId",
                        column: x => x.ActivePayoffPlanId,
                        principalTable: "ActivePayoffPlan",
                        principalColumn: "ActivePayoffPlanId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayoffPlanPayment_PayoffPlanVersion_PayoffPlanVersionId",
                        column: x => x.PayoffPlanVersionId,
                        principalTable: "PayoffPlanVersion",
                        principalColumn: "PayoffPlanVersionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivePayoffPlan_TenantId",
                table: "ActivePayoffPlan",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivePayoffPlan_TenantId_Status",
                table: "ActivePayoffPlan",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PayoffPlanEvent_ActivePayoffPlanId_CreatedOnUtc",
                table: "PayoffPlanEvent",
                columns: new[] { "ActivePayoffPlanId", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PayoffPlanEvent_TenantId",
                table: "PayoffPlanEvent",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoffPlanPayment_AccountId",
                table: "PayoffPlanPayment",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoffPlanPayment_ActivePayoffPlanId_PaymentDate",
                table: "PayoffPlanPayment",
                columns: new[] { "ActivePayoffPlanId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PayoffPlanPayment_PayoffPlanVersionId",
                table: "PayoffPlanPayment",
                column: "PayoffPlanVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoffPlanPayment_TenantId",
                table: "PayoffPlanPayment",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PayoffPlanVersion_ActivePayoffPlanId_VersionNumber",
                table: "PayoffPlanVersion",
                columns: new[] { "ActivePayoffPlanId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayoffPlanVersion_TenantId",
                table: "PayoffPlanVersion",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayoffPlanEvent");

            migrationBuilder.DropTable(
                name: "PayoffPlanPayment");

            migrationBuilder.DropTable(
                name: "PayoffPlanVersion");

            migrationBuilder.DropTable(
                name: "ActivePayoffPlan");
        }
    }
}
