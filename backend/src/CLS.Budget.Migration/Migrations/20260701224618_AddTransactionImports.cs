using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionImports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransactionImport",
                columns: table => new
                {
                    TransactionImportId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionImport", x => x.TransactionImportId);
                });

            migrationBuilder.CreateTable(
                name: "ImportedTransaction",
                columns: table => new
                {
                    ImportedTransactionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionImportId = table.Column<int>(type: "integer", nullable: false),
                    LineNumber = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CategoryRaw = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AccountCategoryId = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PostingStatusRaw = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BudgetPaymentStatusId = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedTransaction", x => x.ImportedTransactionId);
                    table.ForeignKey(
                        name: "FK_ImportedTransaction_AccountCategories_AccountCategoryId",
                        column: x => x.AccountCategoryId,
                        principalTable: "AccountCategories",
                        principalColumn: "AccountCategoryId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ImportedTransaction_BudgetPaymentStatus_BudgetPaymentStatus~",
                        column: x => x.BudgetPaymentStatusId,
                        principalTable: "BudgetPaymentStatus",
                        principalColumn: "BudgetPaymentStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportedTransaction_TransactionImport_TransactionImportId",
                        column: x => x.TransactionImportId,
                        principalTable: "TransactionImport",
                        principalColumn: "TransactionImportId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportedTransaction_AccountCategoryId",
                table: "ImportedTransaction",
                column: "AccountCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedTransaction_BudgetPaymentStatusId",
                table: "ImportedTransaction",
                column: "BudgetPaymentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedTransaction_TenantId",
                table: "ImportedTransaction",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedTransaction_TransactionImportId_LineNumber",
                table: "ImportedTransaction",
                columns: new[] { "TransactionImportId", "LineNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionImport_TenantId",
                table: "TransactionImport",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportedTransaction");

            migrationBuilder.DropTable(
                name: "TransactionImport");
        }
    }
}
