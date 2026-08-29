using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoPayoffConfigName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AutoPayoffConfig_TenantId",
                table: "AutoPayoffConfig");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "AutoPayoffConfig",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "AutoPayoff");

            migrationBuilder.CreateIndex(
                name: "IX_AutoPayoffConfig_TenantId_Name",
                table: "AutoPayoffConfig",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AutoPayoffConfig_TenantId_Name",
                table: "AutoPayoffConfig");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "AutoPayoffConfig");

            migrationBuilder.CreateIndex(
                name: "IX_AutoPayoffConfig_TenantId",
                table: "AutoPayoffConfig",
                column: "TenantId",
                unique: true);
        }
    }
}
