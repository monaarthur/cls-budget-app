using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionImportIncomeSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IncomeSourceId",
                table: "TransactionImport",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionImport_IncomeSourceId",
                table: "TransactionImport",
                column: "IncomeSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionImport_IncomeSource_IncomeSourceId",
                table: "TransactionImport",
                column: "IncomeSourceId",
                principalTable: "IncomeSource",
                principalColumn: "IncomeSourceId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionImport_IncomeSource_IncomeSourceId",
                table: "TransactionImport");

            migrationBuilder.DropIndex(
                name: "IX_TransactionImport_IncomeSourceId",
                table: "TransactionImport");

            migrationBuilder.DropColumn(
                name: "IncomeSourceId",
                table: "TransactionImport");
        }
    }
}
