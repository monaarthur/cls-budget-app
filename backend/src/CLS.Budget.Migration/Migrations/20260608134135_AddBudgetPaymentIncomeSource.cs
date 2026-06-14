using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetPaymentIncomeSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IncomeSourceId",
                table: "BudgetPayments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPayments_IncomeSourceId",
                table: "BudgetPayments",
                column: "IncomeSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetPayments_IncomeSource_IncomeSourceId",
                table: "BudgetPayments",
                column: "IncomeSourceId",
                principalTable: "IncomeSource",
                principalColumn: "IncomeSourceId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetPayments_IncomeSource_IncomeSourceId",
                table: "BudgetPayments");

            migrationBuilder.DropIndex(
                name: "IX_BudgetPayments_IncomeSourceId",
                table: "BudgetPayments");

            migrationBuilder.DropColumn(
                name: "IncomeSourceId",
                table: "BudgetPayments");
        }
    }
}
