using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedPayoffPlanLoanTypeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LoanFixedMonthlyPayment",
                table: "SavedPayoffPlan",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoanInterestOnlyMonths",
                table: "SavedPayoffPlan",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoanTermMonths",
                table: "SavedPayoffPlan",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoanType",
                table: "SavedPayoffPlan",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoanFixedMonthlyPayment",
                table: "SavedPayoffPlan");

            migrationBuilder.DropColumn(
                name: "LoanInterestOnlyMonths",
                table: "SavedPayoffPlan");

            migrationBuilder.DropColumn(
                name: "LoanTermMonths",
                table: "SavedPayoffPlan");

            migrationBuilder.DropColumn(
                name: "LoanType",
                table: "SavedPayoffPlan");
        }
    }
}
