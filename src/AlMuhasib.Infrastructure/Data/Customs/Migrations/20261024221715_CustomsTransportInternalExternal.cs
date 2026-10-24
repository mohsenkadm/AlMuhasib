using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Customs.Migrations
{
    /// <inheritdoc />
    public partial class CustomsTransportInternalExternal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DelayPenaltyAmount",
                table: "CustomsVehicles",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TransportKind",
                table: "CustomsInvoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransportRoute",
                table: "CustomsInvoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DelayPenaltyAmount",
                table: "CustomsVehicles");

            migrationBuilder.DropColumn(
                name: "TransportKind",
                table: "CustomsInvoices");

            migrationBuilder.DropColumn(
                name: "TransportRoute",
                table: "CustomsInvoices");
        }
    }
}
