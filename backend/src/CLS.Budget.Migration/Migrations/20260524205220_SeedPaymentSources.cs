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
            migrationBuilder.Sql("""
                INSERT INTO "PaymentSource" ("PaymentSourceId", "Description", "Name")
                VALUES
                    (1, 'Hudson Valley Credit Union checking', 'HVCU'),
                    (2, 'HVCU credit card used for payments', 'HVCU CC'),
                    (3, 'Alternate / American Express funding account', 'AE')
                ON CONFLICT ("PaymentSourceId") DO NOTHING;
                """);
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
