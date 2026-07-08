using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Car.Migrations
{
    /// <inheritdoc />
    public partial class CarContractAgreedPriceAndWitnesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAgreedPrice",
                table: "CarSaleContracts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WitnessOneName",
                table: "CarSaleContracts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WitnessTwoName",
                table: "CarSaleContracts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAgreedPrice",
                table: "CarSaleContracts");

            migrationBuilder.DropColumn(
                name: "WitnessOneName",
                table: "CarSaleContracts");

            migrationBuilder.DropColumn(
                name: "WitnessTwoName",
                table: "CarSaleContracts");
        }
    }
}
