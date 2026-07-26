using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedPayoffPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedPayoffPlan",
                columns: table => new
                {
                    SavedPayoffPlanId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Strategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExtraMonthlyPayment = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalMonthlyDebtPayment = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TargetUtilizationPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    PayOverLimitFirst = table.Column<bool>(type: "boolean", nullable: false),
                    PostUtilizationStrategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EnableCashAdvanceBalanceMoves = table.Column<bool>(type: "boolean", nullable: false),
                    PromotionalTransfersJson = table.Column<string>(type: "text", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedPayoffPlan", x => x.SavedPayoffPlanId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedPayoffPlan_TenantId",
                table: "SavedPayoffPlan",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPayoffPlan_TenantId_CreatedOnUtc",
                table: "SavedPayoffPlan",
                columns: new[] { "TenantId", "CreatedOnUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedPayoffPlan");
        }
    }
}
