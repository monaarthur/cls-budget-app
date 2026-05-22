using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class RenamePaymentStatusToBudgetPaymentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetPayments_PaymentStatus_PaymentStatusId",
                table: "BudgetPayments");

            migrationBuilder.RenameTable(
                name: "PaymentStatus",
                newName: "BudgetPaymentStatus");

            migrationBuilder.RenameColumn(
                name: "PaymentStatusId",
                table: "BudgetPaymentStatus",
                newName: "BudgetPaymentStatusId");

            migrationBuilder.RenameColumn(
                name: "PaymentStatusId",
                table: "BudgetPayments",
                newName: "BudgetPaymentStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentStatus_Name",
                table: "BudgetPaymentStatus",
                newName: "IX_BudgetPaymentStatus_Name");

            migrationBuilder.RenameIndex(
                name: "IX_BudgetPayments_PaymentStatusId",
                table: "BudgetPayments",
                newName: "IX_BudgetPayments_BudgetPaymentStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetPayments_BudgetPaymentStatus_BudgetPaymentStatusId",
                table: "BudgetPayments",
                column: "BudgetPaymentStatusId",
                principalTable: "BudgetPaymentStatus",
                principalColumn: "BudgetPaymentStatusId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetPayments_BudgetPaymentStatus_BudgetPaymentStatusId",
                table: "BudgetPayments");

            migrationBuilder.RenameIndex(
                name: "IX_BudgetPayments_BudgetPaymentStatusId",
                table: "BudgetPayments",
                newName: "IX_BudgetPayments_PaymentStatusId");

            migrationBuilder.RenameIndex(
                name: "IX_BudgetPaymentStatus_Name",
                table: "BudgetPaymentStatus",
                newName: "IX_PaymentStatus_Name");

            migrationBuilder.RenameColumn(
                name: "BudgetPaymentStatusId",
                table: "BudgetPayments",
                newName: "PaymentStatusId");

            migrationBuilder.RenameColumn(
                name: "BudgetPaymentStatusId",
                table: "BudgetPaymentStatus",
                newName: "PaymentStatusId");

            migrationBuilder.RenameTable(
                name: "BudgetPaymentStatus",
                newName: "PaymentStatus");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetPayments_PaymentStatus_PaymentStatusId",
                table: "BudgetPayments",
                column: "PaymentStatusId",
                principalTable: "PaymentStatus",
                principalColumn: "PaymentStatusId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
