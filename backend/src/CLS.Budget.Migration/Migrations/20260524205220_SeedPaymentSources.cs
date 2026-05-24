using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class SeedPaymentSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PaymentSource",
                columns: new[] { "PaymentSourceId", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Hudson Valley Credit Union checking", "HVCU" },
                    { 2, "HVCU credit card used for payments", "HVCU CC" },
                    { 3, "Alternate / American Express funding account", "AE" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PaymentSource",
                keyColumn: "PaymentSourceId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "PaymentSource",
                keyColumn: "PaymentSourceId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "PaymentSource",
                keyColumn: "PaymentSourceId",
                keyValue: 3);
        }
    }
}
