using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "PaySchedule",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CreditCardDetail",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "BudgetPayments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "BudgetIncome",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Budget",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Accounts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.UpdateData(
                table: "PaySchedule",
                keyColumn: "PayScheduleId",
                keyValue: 1,
                column: "TenantId",
                value: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.CreateIndex(
                name: "IX_PaySchedule_TenantId",
                table: "PaySchedule",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCardDetail_TenantId",
                table: "CreditCardDetail",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetPayments_TenantId",
                table: "BudgetPayments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetIncome_TenantId",
                table: "BudgetIncome",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Budget_TenantId",
                table: "Budget",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_TenantId",
                table: "Accounts",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaySchedule_TenantId",
                table: "PaySchedule");

            migrationBuilder.DropIndex(
                name: "IX_CreditCardDetail_TenantId",
                table: "CreditCardDetail");

            migrationBuilder.DropIndex(
                name: "IX_BudgetPayments_TenantId",
                table: "BudgetPayments");

            migrationBuilder.DropIndex(
                name: "IX_BudgetIncome_TenantId",
                table: "BudgetIncome");

            migrationBuilder.DropIndex(
                name: "IX_Budget_TenantId",
                table: "Budget");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_TenantId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PaySchedule");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CreditCardDetail");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "BudgetPayments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "BudgetIncome");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Budget");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Accounts");
        }
    }
}
