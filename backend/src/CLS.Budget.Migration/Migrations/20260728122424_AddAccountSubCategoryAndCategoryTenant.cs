using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountSubCategoryAndCategoryTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountSubCategoryId",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                table: "AccountCategories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "AccountCategories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccountSubCategory",
                columns: table => new
                {
                    AccountSubCategoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountCategoryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountSubCategory", x => x.AccountSubCategoryId);
                    table.ForeignKey(
                        name: "FK_AccountSubCategory_AccountCategories_AccountCategoryId",
                        column: x => x.AccountCategoryId,
                        principalTable: "AccountCategories",
                        principalColumn: "AccountCategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "AccountCategories",
                keyColumn: "AccountCategoryId",
                keyValue: 1,
                columns: new[] { "IsSystem", "TenantId" },
                values: new object[] { true, null });

            migrationBuilder.UpdateData(
                table: "AccountCategories",
                keyColumn: "AccountCategoryId",
                keyValue: 2,
                columns: new[] { "IsSystem", "TenantId" },
                values: new object[] { true, null });

            migrationBuilder.UpdateData(
                table: "AccountCategories",
                keyColumn: "AccountCategoryId",
                keyValue: 3,
                columns: new[] { "IsSystem", "TenantId" },
                values: new object[] { true, null });

            migrationBuilder.UpdateData(
                table: "AccountCategories",
                keyColumn: "AccountCategoryId",
                keyValue: 4,
                columns: new[] { "IsSystem", "TenantId" },
                values: new object[] { true, null });

            migrationBuilder.UpdateData(
                table: "AccountCategories",
                keyColumn: "AccountCategoryId",
                keyValue: 5,
                columns: new[] { "IsSystem", "TenantId" },
                values: new object[] { true, null });

            migrationBuilder.UpdateData(
                table: "AccountCategories",
                keyColumn: "AccountCategoryId",
                keyValue: 6,
                columns: new[] { "IsSystem", "TenantId" },
                values: new object[] { true, null });

            migrationBuilder.UpdateData(
                table: "AccountCategories",
                keyColumn: "AccountCategoryId",
                keyValue: 7,
                columns: new[] { "IsSystem", "TenantId" },
                values: new object[] { true, null });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountCategoryId",
                table: "Accounts",
                column: "AccountCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountSubCategoryId",
                table: "Accounts",
                column: "AccountSubCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountCategories_TenantId",
                table: "AccountCategories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountCategories_TenantId_Name",
                table: "AccountCategories",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountSubCategory_AccountCategoryId",
                table: "AccountSubCategory",
                column: "AccountCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountSubCategory_TenantId",
                table: "AccountSubCategory",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountSubCategory_TenantId_AccountCategoryId_Name",
                table: "AccountSubCategory",
                columns: new[] { "TenantId", "AccountCategoryId", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_AccountCategories_AccountCategoryId",
                table: "Accounts",
                column: "AccountCategoryId",
                principalTable: "AccountCategories",
                principalColumn: "AccountCategoryId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_AccountSubCategory_AccountSubCategoryId",
                table: "Accounts",
                column: "AccountSubCategoryId",
                principalTable: "AccountSubCategory",
                principalColumn: "AccountSubCategoryId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_AccountCategories_AccountCategoryId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_AccountSubCategory_AccountSubCategoryId",
                table: "Accounts");

            migrationBuilder.DropTable(
                name: "AccountSubCategory");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_AccountCategoryId",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_AccountSubCategoryId",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_AccountCategories_TenantId",
                table: "AccountCategories");

            migrationBuilder.DropIndex(
                name: "IX_AccountCategories_TenantId_Name",
                table: "AccountCategories");

            migrationBuilder.DropColumn(
                name: "AccountSubCategoryId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "IsSystem",
                table: "AccountCategories");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AccountCategories");
        }
    }
}
