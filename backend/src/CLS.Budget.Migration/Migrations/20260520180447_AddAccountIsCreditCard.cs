using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountIsCreditCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCreditCard",
                table: "Accounts",
                type: "boolean",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Accounts"
                SET "IsCreditCard" = TRUE
                WHERE "AccountCategoryId" = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCreditCard",
                table: "Accounts");
        }
    }
}
