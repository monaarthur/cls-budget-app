using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class SeedAccountCategoryAndBudgetTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountCategories",
                columns: table => new
                {
                    AccountCategoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountCategories", x => x.AccountCategoryId);
                });

            migrationBuilder.CreateTable(
                name: "BudgetTemplates",
                columns: table => new
                {
                    BudgetTemplateId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AccountIds = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetTemplates", x => x.BudgetTemplateId);
                });

            migrationBuilder.CreateTable(
                name: "PaymentSource",
                columns: table => new
                {
                    PaymentSourceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentSource", x => x.PaymentSourceId);
                });

            migrationBuilder.CreateTable(
                name: "Budget",
                columns: table => new
                {
                    BudgetId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartPeriod = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndPeriod = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BudgetTemplateId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Budget", x => x.BudgetId);
                    table.ForeignKey(
                        name: "FK_Budget_BudgetTemplates_BudgetTemplateId",
                        column: x => x.BudgetTemplateId,
                        principalTable: "BudgetTemplates",
                        principalColumn: "BudgetTemplateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BudgetPayments",
                columns: table => new
                {
                    BudgetPaymentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BudgetId = table.Column<int>(type: "integer", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    PaymentMade = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    IsCleared = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClearedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentSourceId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetPayments", x => x.BudgetPaymentId);
                    table.ForeignKey(
                        name: "FK_BudgetPayments_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BudgetPayments_Budget_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budget",
                        principalColumn: "BudgetId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetPayments_PaymentSource_PaymentSourceId",
                        column: x => x.PaymentSourceId,
                        principalTable: "PaymentSource",
                        principalColumn: "PaymentSourceId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "AccountCategories",
                columns: new[] { "AccountCategoryId", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Revolving credit accounts", "Credit Card" },
                    { 2, "Installment loans", "Loan" },
                    { 3, "Home mortgage accounts", "Mortgage" },
                    { 4, "Utility and service bills", "Utility" },
                    { 5, "Recurring subscriptions", "Subscription" },
                    { 6, "Savings accounts", "Savings" },
                    { 7, "Checking accounts", "Checking" }
                });

            migrationBuilder.InsertData(
                table: "BudgetTemplates",
                columns: new[] { "BudgetTemplateId", "AccountIds", "Description", "Name" },
                values: new object[] { 1, null, "Default template for monthly budget planning", "Monthly Household Budget" });

            migrationBuilder.CreateIndex(
                name: "IX_Budget_BudgetTemplateId",
                table: "Budget",
                column: "BudgetTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPayments_AccountId",
                table: "BudgetPayments",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPayments_BudgetId_AccountId_PaymentDate",
                table: "BudgetPayments",
                columns: new[] { "BudgetId", "AccountId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPayments_PaymentSourceId",
                table: "BudgetPayments",
                column: "PaymentSourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountCategories");

            migrationBuilder.DropTable(
                name: "BudgetPayments");

            migrationBuilder.DropTable(
                name: "Budget");

            migrationBuilder.DropTable(
                name: "PaymentSource");

            migrationBuilder.DropTable(
                name: "BudgetTemplates");
        }
    }
}
