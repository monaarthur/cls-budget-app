using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddForecastScenarioTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ForecastScenario",
                columns: table => new
                {
                    ForecastScenarioId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Strategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalMonthlyDebtPayment = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ForecastMonths = table.Column<int>(type: "integer", nullable: false),
                    StartingDebt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MonthlyNetIncome = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MonthlyExpenses = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TargetUtilizationPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    PayOverLimitFirst = table.Column<bool>(type: "boolean", nullable: false),
                    EstimatedDebtFreeDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TotalInterestPaid = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastScenario", x => x.ForecastScenarioId);
                });

            migrationBuilder.CreateTable(
                name: "ForecastMonthlySnapshot",
                columns: table => new
                {
                    ForecastMonthlySnapshotId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ForecastScenarioId = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<DateOnly>(type: "date", nullable: false),
                    MonthIndex = table.Column<int>(type: "integer", nullable: false),
                    StartingDebt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NewCharges = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Interest = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Payments = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EndingDebt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalCreditLimit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OverallUtilizationPercentage = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    AvailableCash = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CardsPaidOffThisMonth = table.Column<int>(type: "integer", nullable: false),
                    CumulativeInterest = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastMonthlySnapshot", x => x.ForecastMonthlySnapshotId);
                    table.ForeignKey(
                        name: "FK_ForecastMonthlySnapshot_ForecastScenario_ForecastScenarioId",
                        column: x => x.ForecastScenarioId,
                        principalTable: "ForecastScenario",
                        principalColumn: "ForecastScenarioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ForecastScenarioCreditCard",
                columns: table => new
                {
                    ForecastScenarioCreditCardId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ForecastScenarioId = table.Column<int>(type: "integer", nullable: false),
                    CreditCardId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartingBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AnnualPercentageRate = table.Column<decimal>(type: "numeric(8,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastScenarioCreditCard", x => x.ForecastScenarioCreditCardId);
                    table.ForeignKey(
                        name: "FK_ForecastScenarioCreditCard_ForecastScenario_ForecastScenari~",
                        column: x => x.ForecastScenarioId,
                        principalTable: "ForecastScenario",
                        principalColumn: "ForecastScenarioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForecastMonthlySnapshot_ForecastScenarioId_MonthIndex",
                table: "ForecastMonthlySnapshot",
                columns: new[] { "ForecastScenarioId", "MonthIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_ForecastMonthlySnapshot_TenantId",
                table: "ForecastMonthlySnapshot",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastScenario_TenantId",
                table: "ForecastScenario",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastScenarioCreditCard_ForecastScenarioId",
                table: "ForecastScenarioCreditCard",
                column: "ForecastScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastScenarioCreditCard_TenantId",
                table: "ForecastScenarioCreditCard",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForecastMonthlySnapshot");

            migrationBuilder.DropTable(
                name: "ForecastScenarioCreditCard");

            migrationBuilder.DropTable(
                name: "ForecastScenario");
        }
    }
}
