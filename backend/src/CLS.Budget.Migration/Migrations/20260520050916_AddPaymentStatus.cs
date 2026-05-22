using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentStatus",
                columns: table => new
                {
                    PaymentStatusId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentStatus", x => x.PaymentStatusId);
                });

            migrationBuilder.InsertData(
                table: "PaymentStatus",
                columns: new[] { "PaymentStatusId", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Not yet paid", "Pending" },
                    { 2, "Scheduled for payment", "Scheduled" },
                    { 3, "Payment completed", "Paid" },
                    { 4, "Payment attempt failed", "Failed" },
                    { 5, "Past due and not paid", "Overdue" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentStatus_Name",
                table: "PaymentStatus",
                column: "Name",
                unique: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatusId",
                table: "BudgetPayments",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "BudgetPayments"
                SET "PaymentStatusId" = CASE WHEN "IsPaid" THEN 3 ELSE 1 END
                """);

            migrationBuilder.AlterColumn<int>(
                name: "PaymentStatusId",
                table: "BudgetPayments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "BudgetPayments");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPayments_PaymentStatusId",
                table: "BudgetPayments",
                column: "PaymentStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetPayments_PaymentStatus_PaymentStatusId",
                table: "BudgetPayments",
                column: "PaymentStatusId",
                principalTable: "PaymentStatus",
                principalColumn: "PaymentStatusId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetPayments_PaymentStatus_PaymentStatusId",
                table: "BudgetPayments");

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "BudgetPayments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE "BudgetPayments"
                SET "IsPaid" = ("PaymentStatusId" = 3)
                """);

            migrationBuilder.DropIndex(
                name: "IX_BudgetPayments_PaymentStatusId",
                table: "BudgetPayments");

            migrationBuilder.DropColumn(
                name: "PaymentStatusId",
                table: "BudgetPayments");

            migrationBuilder.DropTable(
                name: "PaymentStatus");
        }
    }
}
