using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CLS.Budget.EfCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditCardDecisionEngineFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinimumPaymentFloor",
                table: "CreditCardDetail",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumPaymentPercentage",
                table: "CreditCardDetail",
                type: "numeric(8,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PromotionalAnnualPercentageRate",
                table: "CreditCardDetail",
                type: "numeric(8,4)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PromotionalRateExpirationDate",
                table: "CreditCardDetail",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinimumPaymentFloor",
                table: "CreditCardDetail");

            migrationBuilder.DropColumn(
                name: "MinimumPaymentPercentage",
                table: "CreditCardDetail");

            migrationBuilder.DropColumn(
                name: "PromotionalAnnualPercentageRate",
                table: "CreditCardDetail");

            migrationBuilder.DropColumn(
                name: "PromotionalRateExpirationDate",
                table: "CreditCardDetail");
        }
    }
}
