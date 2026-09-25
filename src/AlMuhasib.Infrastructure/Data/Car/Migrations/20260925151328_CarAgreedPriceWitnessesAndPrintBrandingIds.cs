using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Car.Migrations
{
    /// <inheritdoc />
    public partial class CarAgreedPriceWitnessesAndPrintBrandingIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IsAgreedPrice / witnesses were added to the model/snapshot earlier without a desktop Up().
            // Idempotent SQL keeps existing and fresh databases safe.
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.CarSaleContracts', N'IsAgreedPrice') IS NULL
                    ALTER TABLE [dbo].[CarSaleContracts] ADD [IsAgreedPrice] bit NOT NULL
                        CONSTRAINT [DF_CarSaleContracts_IsAgreedPrice] DEFAULT (CAST(0 AS bit));

                IF COL_LENGTH(N'dbo.CarSaleContracts', N'WitnessOneName') IS NULL
                    ALTER TABLE [dbo].[CarSaleContracts] ADD [WitnessOneName] nvarchar(200) NOT NULL
                        CONSTRAINT [DF_CarSaleContracts_WitnessOneName] DEFAULT (N'');

                IF COL_LENGTH(N'dbo.CarSaleContracts', N'WitnessTwoName') IS NULL
                    ALTER TABLE [dbo].[CarSaleContracts] ADD [WitnessTwoName] nvarchar(200) NOT NULL
                        CONSTRAINT [DF_CarSaleContracts_WitnessTwoName] DEFAULT (N'');

                IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'CompanyIdNumber') IS NULL
                    ALTER TABLE [dbo].[PrintBrandingSettings] ADD [CompanyIdNumber] nvarchar(100) NOT NULL
                        CONSTRAINT [DF_CarPrintBranding_CompanyIdNumber] DEFAULT (N'');

                IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'CompanyIdIssuer') IS NULL
                    ALTER TABLE [dbo].[PrintBrandingSettings] ADD [CompanyIdIssuer] nvarchar(200) NOT NULL
                        CONSTRAINT [DF_CarPrintBranding_CompanyIdIssuer] DEFAULT (N'');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'CompanyIdIssuer') IS NOT NULL
                    ALTER TABLE [dbo].[PrintBrandingSettings] DROP COLUMN [CompanyIdIssuer];
                IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'CompanyIdNumber') IS NOT NULL
                    ALTER TABLE [dbo].[PrintBrandingSettings] DROP COLUMN [CompanyIdNumber];
                IF COL_LENGTH(N'dbo.CarSaleContracts', N'WitnessTwoName') IS NOT NULL
                    ALTER TABLE [dbo].[CarSaleContracts] DROP COLUMN [WitnessTwoName];
                IF COL_LENGTH(N'dbo.CarSaleContracts', N'WitnessOneName') IS NOT NULL
                    ALTER TABLE [dbo].[CarSaleContracts] DROP COLUMN [WitnessOneName];
                IF COL_LENGTH(N'dbo.CarSaleContracts', N'IsAgreedPrice') IS NOT NULL
                    ALTER TABLE [dbo].[CarSaleContracts] DROP COLUMN [IsAgreedPrice];
                """);
        }
    }
}
