using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CarContractAgreedPriceAndWitnesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.CarSaleContracts', 'IsAgreedPrice') IS NULL
                    ALTER TABLE [dbo].[CarSaleContracts] ADD [IsAgreedPrice] bit NOT NULL CONSTRAINT [DF_CarSaleContracts_IsAgreedPrice] DEFAULT CAST(0 AS bit);

                IF COL_LENGTH('dbo.CarSaleContracts', 'WitnessOneName') IS NULL
                    ALTER TABLE [dbo].[CarSaleContracts] ADD [WitnessOneName] nvarchar(200) NOT NULL CONSTRAINT [DF_CarSaleContracts_WitnessOneName] DEFAULT N'';

                IF COL_LENGTH('dbo.CarSaleContracts', 'WitnessTwoName') IS NULL
                    ALTER TABLE [dbo].[CarSaleContracts] ADD [WitnessTwoName] nvarchar(200) NOT NULL CONSTRAINT [DF_CarSaleContracts_WitnessTwoName] DEFAULT N'';
                """);
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
