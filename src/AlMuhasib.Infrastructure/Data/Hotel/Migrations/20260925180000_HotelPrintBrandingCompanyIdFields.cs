using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Hotel.Migrations
{
    public partial class HotelPrintBrandingCompanyIdFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'CompanyIdNumber') IS NULL
                    ALTER TABLE [dbo].[PrintBrandingSettings] ADD [CompanyIdNumber] nvarchar(100) NOT NULL
                        CONSTRAINT [DF_HotelPrintBranding_CompanyIdNumber] DEFAULT (N'');
                IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'CompanyIdIssuer') IS NULL
                    ALTER TABLE [dbo].[PrintBrandingSettings] ADD [CompanyIdIssuer] nvarchar(200) NOT NULL
                        CONSTRAINT [DF_HotelPrintBranding_CompanyIdIssuer] DEFAULT (N'');
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'CompanyIdIssuer') IS NOT NULL
                    ALTER TABLE [dbo].[PrintBrandingSettings] DROP COLUMN [CompanyIdIssuer];
                IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'CompanyIdNumber') IS NOT NULL
                    ALTER TABLE [dbo].[PrintBrandingSettings] DROP COLUMN [CompanyIdNumber];
                """);
        }
    }
}
