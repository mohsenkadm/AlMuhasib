using AlMuhasib.Infrastructure.Data.Customs;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Customs.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CustomsDbContext))]
    [Migration("20261027220000_CustomsVehicleClearanceFees")]
    public partial class CustomsVehicleClearanceFees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Destination",
                table: "CustomsVehicles",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "SealAmount",
                table: "CustomsVehicles",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RadiationAmount",
                table: "CustomsVehicles",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DutyAmount",
                table: "CustomsVehicles",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QualityControlAmount",
                table: "CustomsVehicles",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LandTransportAmount",
                table: "CustomsVehicles",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ClearanceFeeAmount",
                table: "CustomsVehicles",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpensesAmount",
                table: "CustomsVehicles",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Destination", table: "CustomsVehicles");
            migrationBuilder.DropColumn(name: "SealAmount", table: "CustomsVehicles");
            migrationBuilder.DropColumn(name: "RadiationAmount", table: "CustomsVehicles");
            migrationBuilder.DropColumn(name: "DutyAmount", table: "CustomsVehicles");
            migrationBuilder.DropColumn(name: "QualityControlAmount", table: "CustomsVehicles");
            migrationBuilder.DropColumn(name: "LandTransportAmount", table: "CustomsVehicles");
            migrationBuilder.DropColumn(name: "ClearanceFeeAmount", table: "CustomsVehicles");
            migrationBuilder.DropColumn(name: "ExpensesAmount", table: "CustomsVehicles");
        }
    }
}
