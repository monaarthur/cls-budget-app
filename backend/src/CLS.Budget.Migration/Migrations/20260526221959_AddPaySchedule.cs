using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPaySchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PayScheduleId",
                table: "Budget",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IncomeSource",
                columns: table => new
                {
                    IncomeSourceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncomeSource", x => x.IncomeSourceId);
                });

            migrationBuilder.CreateTable(
                name: "PayFrequencyType",
                columns: table => new
                {
                    PayFrequencyTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayFrequencyType", x => x.PayFrequencyTypeId);
                });

            migrationBuilder.CreateTable(
                name: "PaySchedule",
                columns: table => new
                {
                    PayScheduleId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IncomeSourceId = table.Column<int>(type: "integer", nullable: false),
                    PayFrequencyTypeId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AnchorDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    SemiMonthlyDay1 = table.Column<int>(type: "integer", nullable: true),
                    SemiMonthlyDay2 = table.Column<int>(type: "integer", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaySchedule", x => x.PayScheduleId);
                    table.ForeignKey(
                        name: "FK_PaySchedule_IncomeSource_IncomeSourceId",
                        column: x => x.IncomeSourceId,
                        principalTable: "IncomeSource",
                        principalColumn: "IncomeSourceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaySchedule_PayFrequencyType_PayFrequencyTypeId",
                        column: x => x.PayFrequencyTypeId,
                        principalTable: "PayFrequencyType",
                        principalColumn: "PayFrequencyTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "IncomeSource",
                columns: new[] { "IncomeSourceId", "IsActive", "Name" },
                values: new object[] { 1, true, "Primary income" });

            migrationBuilder.InsertData(
                table: "PayFrequencyType",
                columns: new[] { "PayFrequencyTypeId", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Paid every 7 days on a fixed weekday", "Weekly" },
                    { 2, "Paid every 14 days on a fixed weekday", "BiWeekly" },
                    { 3, "Paid twice per month on fixed calendar days", "SemiMonthly" }
                });

            migrationBuilder.InsertData(
                table: "PaySchedule",
                columns: new[] { "PayScheduleId", "AnchorDate", "DayOfWeek", "IncomeSourceId", "IsActive", "IsDefault", "Name", "PayFrequencyTypeId", "SemiMonthlyDay1", "SemiMonthlyDay2" },
                values: new object[] { 1, null, null, 1, true, true, "Twice monthly (1st & 15th)", 3, 1, 15 });

            migrationBuilder.CreateIndex(
                name: "IX_Budget_PayScheduleId",
                table: "Budget",
                column: "PayScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_PaySchedule_IncomeSourceId",
                table: "PaySchedule",
                column: "IncomeSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaySchedule_PayFrequencyTypeId",
                table: "PaySchedule",
                column: "PayFrequencyTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Budget_PaySchedule_PayScheduleId",
                table: "Budget",
                column: "PayScheduleId",
                principalTable: "PaySchedule",
                principalColumn: "PayScheduleId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Budget_PaySchedule_PayScheduleId",
                table: "Budget");

            migrationBuilder.DropTable(
                name: "PaySchedule");

            migrationBuilder.DropTable(
                name: "IncomeSource");

            migrationBuilder.DropTable(
                name: "PayFrequencyType");

            migrationBuilder.DropIndex(
                name: "IX_Budget_PayScheduleId",
                table: "Budget");

            migrationBuilder.DropColumn(
                name: "PayScheduleId",
                table: "Budget");
        }
    }
}
