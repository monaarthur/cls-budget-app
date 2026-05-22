using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetAccountIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountIds",
                table: "Budget",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Budget" b
                SET "AccountIds" = sub.ids
                FROM (
                    SELECT "BudgetId",
                           '[' || string_agg("AccountId"::text, ',' ORDER BY "AccountId") || ']' AS ids
                    FROM "BudgetPayments"
                    GROUP BY "BudgetId"
                ) sub
                WHERE b."BudgetId" = sub."BudgetId"
                  AND b."AccountIds" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountIds",
                table: "Budget");
        }
    }
}
