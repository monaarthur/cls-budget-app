using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedPayoffPlanLoanFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LoanAmount",
                table: "SavedPayoffPlan",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LoanAnnualPercentageRate",
                table: "SavedPayoffPlan",
                type: "numeric(8,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoanAmount",
                table: "SavedPayoffPlan");

            migrationBuilder.DropColumn(
                name: "LoanAnnualPercentageRate",
                table: "SavedPayoffPlan");
        }
    }
}
