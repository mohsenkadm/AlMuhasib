using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncCloudPendingModelChanges : Migration
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

            migrationBuilder.AddColumn<decimal>(
                name: "SaleAmountPaid",
                table: "CarTradeTransactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "SaleDate",
                table: "CarTradeTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalePaymentMode",
                table: "CarTradeTransactions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "SaleRemainingAmount",
                table: "CarTradeTransactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PaymentKind",
                table: "CarTradePayments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSold",
                table: "CarTradeTransactions");

            migrationBuilder.DropColumn(
                name: "SaleAmountPaid",
                table: "CarTradeTransactions");

            migrationBuilder.DropColumn(
                name: "SaleDate",
                table: "CarTradeTransactions");

            migrationBuilder.DropColumn(
                name: "SalePaymentMode",
                table: "CarTradeTransactions");

            migrationBuilder.DropColumn(
                name: "SaleRemainingAmount",
                table: "CarTradeTransactions");

            migrationBuilder.DropColumn(
                name: "PaymentKind",
                table: "CarTradePayments");
        }
    }
}
