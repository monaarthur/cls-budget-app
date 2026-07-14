using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class SeedScheduledOnlineBudgetPaymentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "BudgetPaymentStatus",
                columns: new[] { "BudgetPaymentStatusId", "Description", "Name" },
                values: new object[] { 7, "Scheduled for online payment", "Scheduled Online" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BudgetPaymentStatus",
                keyColumn: "BudgetPaymentStatusId",
                keyValue: 7);
        }
    }
}
