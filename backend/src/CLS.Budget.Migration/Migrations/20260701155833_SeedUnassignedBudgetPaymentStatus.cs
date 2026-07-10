using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class SeedUnassignedBudgetPaymentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "BudgetPaymentStatus",
                columns: new[] { "BudgetPaymentStatusId", "Description", "Name" },
                values: new object[] { 6, "Status not yet chosen", "Unassigned" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "BudgetPaymentStatus",
                keyColumn: "BudgetPaymentStatusId",
                keyValue: 6);
        }
    }
}
