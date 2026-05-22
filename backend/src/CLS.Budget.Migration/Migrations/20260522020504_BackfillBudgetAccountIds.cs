using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class BackfillBudgetAccountIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                  AND (b."AccountIds" IS NULL OR b."AccountIds" = '');

                UPDATE "Budget" b
                SET "AccountIds" = t."AccountIds"
                FROM "BudgetTemplates" t
                WHERE b."BudgetTemplateId" = t."BudgetTemplateId"
                  AND (b."AccountIds" IS NULL OR b."AccountIds" = '')
                  AND t."AccountIds" IS NOT NULL
                  AND t."AccountIds" <> '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
