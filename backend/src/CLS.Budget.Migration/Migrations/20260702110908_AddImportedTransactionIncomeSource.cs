using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddImportedTransactionIncomeSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IncomeSourceId",
                table: "ImportedTransaction",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportedTransaction_IncomeSourceId",
                table: "ImportedTransaction",
                column: "IncomeSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_ImportedTransaction_IncomeSource_IncomeSourceId",
                table: "ImportedTransaction",
                column: "IncomeSourceId",
                principalTable: "IncomeSource",
                principalColumn: "IncomeSourceId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImportedTransaction_IncomeSource_IncomeSourceId",
                table: "ImportedTransaction");

            migrationBuilder.DropIndex(
                name: "IX_ImportedTransaction_IncomeSourceId",
                table: "ImportedTransaction");

            migrationBuilder.DropColumn(
                name: "IncomeSourceId",
                table: "ImportedTransaction");
        }
    }
}
