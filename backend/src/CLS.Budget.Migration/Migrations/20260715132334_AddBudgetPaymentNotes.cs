using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetPaymentNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "BudgetPayments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "BudgetPayments");
        }
    }
}
