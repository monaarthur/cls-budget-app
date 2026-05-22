using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class SyncCreditCardDetailEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnnualFee",
                table: "CreditCardDetail");

            migrationBuilder.DropColumn(
                name: "CreditLimit",
                table: "CreditCardDetail");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "CreditCardDetail");

            migrationBuilder.DropColumn(
                name: "LastPaymentAmount",
                table: "CreditCardDetail");

            migrationBuilder.DropColumn(
                name: "LastPaymentBalance",
                table: "CreditCardDetail");

            migrationBuilder.DropColumn(
                name: "LastPaymentDate",
                table: "CreditCardDetail");

            migrationBuilder.RenameColumn(
                name: "MinimumPayment",
                table: "CreditCardDetail",
                newName: "Limit");

            migrationBuilder.AddColumn<decimal>(
                name: "CashOutInterestRate",
                table: "CreditCardDetail",
                type: "numeric(8,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CashOutInterestRate",
                table: "CreditCardDetail");

            migrationBuilder.RenameColumn(
                name: "Limit",
                table: "CreditCardDetail",
                newName: "MinimumPayment");

            migrationBuilder.AddColumn<decimal>(
                name: "AnnualFee",
                table: "CreditCardDetail",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditLimit",
                table: "CreditCardDetail",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "CreditCardDetail",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LastPaymentAmount",
                table: "CreditCardDetail",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LastPaymentBalance",
                table: "CreditCardDetail",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPaymentDate",
                table: "CreditCardDetail",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
