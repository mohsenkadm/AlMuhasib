using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using AlMuhasib.Cloud.Infrastructure.Data;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations;

/// <summary>حقول شعار وملاحظات إيصال الثيرمال الحراري (سحابة).</summary>
[DbContext(typeof(CloudDbContext))]
[Migration("20261109190000_AddCloudPrintBrandingThermalFields")]
public partial class AddCloudPrintBrandingThermalFields : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'ShowThermalLogo') IS NULL
                ALTER TABLE [dbo].[PrintBrandingSettings] ADD [ShowThermalLogo] bit NOT NULL
                    CONSTRAINT [DF_CloudPrintBranding_ShowThermalLogo] DEFAULT (1);
            IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'ThermalLogoData') IS NULL
                ALTER TABLE [dbo].[PrintBrandingSettings] ADD [ThermalLogoData] varbinary(max) NULL;
            IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'ThermalLogoContentType') IS NULL
                ALTER TABLE [dbo].[PrintBrandingSettings] ADD [ThermalLogoContentType] nvarchar(50) NULL;
            IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'ThermalNotes') IS NULL
                ALTER TABLE [dbo].[PrintBrandingSettings] ADD [ThermalNotes] nvarchar(2000) NOT NULL
                    CONSTRAINT [DF_CloudPrintBranding_ThermalNotes] DEFAULT (N'');
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'ThermalNotes') IS NOT NULL
            BEGIN
                DECLARE @dfNotes sysname;
                SELECT @dfNotes = dc.name
                FROM sys.default_constraints dc
                INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                WHERE dc.parent_object_id = OBJECT_ID(N'dbo.PrintBrandingSettings') AND c.name = N'ThermalNotes';
                IF @dfNotes IS NOT NULL
                    EXEC(N'ALTER TABLE [dbo].[PrintBrandingSettings] DROP CONSTRAINT [' + @dfNotes + N']');
                ALTER TABLE [dbo].[PrintBrandingSettings] DROP COLUMN [ThermalNotes];
            END
            IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'ThermalLogoContentType') IS NOT NULL
                ALTER TABLE [dbo].[PrintBrandingSettings] DROP COLUMN [ThermalLogoContentType];
            IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'ThermalLogoData') IS NOT NULL
                ALTER TABLE [dbo].[PrintBrandingSettings] DROP COLUMN [ThermalLogoData];
            IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'ShowThermalLogo') IS NOT NULL
            BEGIN
                DECLARE @dfLogo sysname;
                SELECT @dfLogo = dc.name
                FROM sys.default_constraints dc
                INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                WHERE dc.parent_object_id = OBJECT_ID(N'dbo.PrintBrandingSettings') AND c.name = N'ShowThermalLogo';
                IF @dfLogo IS NOT NULL
                    EXEC(N'ALTER TABLE [dbo].[PrintBrandingSettings] DROP CONSTRAINT [' + @dfLogo + N']');
                ALTER TABLE [dbo].[PrintBrandingSettings] DROP COLUMN [ShowThermalLogo];
            END
            """);
    }
}
