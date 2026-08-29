using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoPayoffConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutoPayoffConfig",
                columns: table => new
                {
                    AutoPayoffConfigId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExcludeStatusIdsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ExcludeCategoryIdsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ExcludeAccountIdsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    TargetCategoryIdsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    TargetByPayoffDate = table.Column<bool>(type: "boolean", nullable: false),
                    ExtraMonthlyAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoPayoffConfig", x => x.AutoPayoffConfigId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutoPayoffConfig_TenantId",
                table: "AutoPayoffConfig",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoPayoffConfig");
        }
    }
}
