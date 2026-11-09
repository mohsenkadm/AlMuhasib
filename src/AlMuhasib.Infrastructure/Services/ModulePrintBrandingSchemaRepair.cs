using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

/// <summary>Shared idempotent repair for PrintBranding CompanyId* columns across module DBs.</summary>
public static class ModulePrintBrandingSchemaRepair
{
    public static Task EnsureCompanyIdColumnsAsync(DbContext db, CancellationToken cancellationToken = default) =>
        TryExecAsync(db, """
            IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'CompanyIdNumber') IS NULL
                ALTER TABLE [dbo].[PrintBrandingSettings] ADD [CompanyIdNumber] nvarchar(100) NOT NULL
                    CONSTRAINT [DF_ModulePrintBranding_CompanyIdNumber] DEFAULT (N'');
            IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'CompanyIdIssuer') IS NULL
                ALTER TABLE [dbo].[PrintBrandingSettings] ADD [CompanyIdIssuer] nvarchar(200) NOT NULL
                    CONSTRAINT [DF_ModulePrintBranding_CompanyIdIssuer] DEFAULT (N'');
            IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'ShowThermalLogo') IS NULL
                ALTER TABLE [dbo].[PrintBrandingSettings] ADD [ShowThermalLogo] bit NOT NULL
                    CONSTRAINT [DF_ModulePrintBranding_ShowThermalLogo] DEFAULT (1);
            IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'ThermalLogoData') IS NULL
                ALTER TABLE [dbo].[PrintBrandingSettings] ADD [ThermalLogoData] varbinary(max) NULL;
            IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'ThermalLogoContentType') IS NULL
                ALTER TABLE [dbo].[PrintBrandingSettings] ADD [ThermalLogoContentType] nvarchar(50) NULL;
            IF COL_LENGTH(N'dbo.PrintBrandingSettings', N'ThermalNotes') IS NULL
                ALTER TABLE [dbo].[PrintBrandingSettings] ADD [ThermalNotes] nvarchar(2000) NOT NULL
                    CONSTRAINT [DF_ModulePrintBranding_ThermalNotes] DEFAULT (N'');
            """, cancellationToken);

    private static async Task TryExecAsync(DbContext db, string sql, CancellationToken cancellationToken)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ModulePrintBrandingSchemaRepair] {ex.Message}");
        }
    }
}
