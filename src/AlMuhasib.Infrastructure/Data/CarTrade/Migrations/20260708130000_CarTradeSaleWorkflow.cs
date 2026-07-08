using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.CarTrade.Migrations;

/// <inheritdoc />
public partial class CarTradeSaleWorkflow : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsSold",
            table: "CarTradeTransactions",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "SaleDate",
            table: "CarTradeTransactions",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SalePaymentMode",
            table: "CarTradeTransactions",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "FullCash");

        migrationBuilder.AddColumn<decimal>(
            name: "SaleAmountPaid",
            table: "CarTradeTransactions",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "SaleRemainingAmount",
            table: "CarTradeTransactions",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "PaymentKind",
            table: "CarTradePayments",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "Purchase");

        migrationBuilder.CreateIndex(
            name: "IX_CarTradeTransactions_IsSold",
            table: "CarTradeTransactions",
            column: "IsSold");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_CarTradeTransactions_IsSold",
            table: "CarTradeTransactions");

        migrationBuilder.DropColumn(
            name: "IsSold",
            table: "CarTradeTransactions");

        migrationBuilder.DropColumn(
            name: "SaleDate",
            table: "CarTradeTransactions");

        migrationBuilder.DropColumn(
            name: "SalePaymentMode",
            table: "CarTradeTransactions");

        migrationBuilder.DropColumn(
            name: "SaleAmountPaid",
            table: "CarTradeTransactions");

        migrationBuilder.DropColumn(
            name: "SaleRemainingAmount",
            table: "CarTradeTransactions");

        migrationBuilder.DropColumn(
            name: "PaymentKind",
            table: "CarTradePayments");
    }
}
