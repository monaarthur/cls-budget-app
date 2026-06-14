using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetIncome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BudgetIncome",
                columns: table => new
                {
                    BudgetIncomeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BudgetId = table.Column<int>(type: "integer", nullable: false),
                    IncomeSourceId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetIncome", x => x.BudgetIncomeId);
                    table.ForeignKey(
                        name: "FK_BudgetIncome_Budget_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budget",
                        principalColumn: "BudgetId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetIncome_IncomeSource_IncomeSourceId",
                        column: x => x.IncomeSourceId,
                        principalTable: "IncomeSource",
                        principalColumn: "IncomeSourceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "IncomeSource",
                keyColumn: "IncomeSourceId",
                keyValue: 1,
                column: "Name",
                value: "Job Income");

            migrationBuilder.InsertData(
                table: "IncomeSource",
                columns: new[] { "IncomeSourceId", "IsActive", "Name" },
                values: new object[,]
                {
                    { 2, true, "Credit Cards" },
                    { 3, true, "Business Income" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetIncome_BudgetId_IncomeSourceId_ReceivedDate",
                table: "BudgetIncome",
                columns: new[] { "BudgetId", "IncomeSourceId", "ReceivedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetIncome_IncomeSourceId",
                table: "BudgetIncome",
                column: "IncomeSourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetIncome");

            migrationBuilder.DeleteData(
                table: "IncomeSource",
                keyColumn: "IncomeSourceId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "IncomeSource",
                keyColumn: "IncomeSourceId",
                keyValue: 3);

            migrationBuilder.UpdateData(
                table: "IncomeSource",
                keyColumn: "IncomeSourceId",
                keyValue: 1,
                column: "Name",
                value: "Primary income");
        }
    }
}
