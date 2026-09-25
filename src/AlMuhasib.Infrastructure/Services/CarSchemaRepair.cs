using AlMuhasib.Infrastructure.Data.Car;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

/// <summary>
/// Idempotent schema repairs for Car Contracts DB after EF migrations.
/// Covers cases where migration history is ahead of the real schema
/// (e.g. columns added to the model/snapshot without a desktop Up migration).
/// </summary>
public static class CarSchemaRepair
{
    public static async Task ApplyAsync(CarDbContext db, CancellationToken cancellationToken = default)
    {
        await TryExecAsync(db, """
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
            """, cancellationToken);

        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.CloudSyncSettings', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[CloudSyncSettings] (
                    [Id] int NOT NULL IDENTITY,
                    [ApiBaseUrl] nvarchar(max) NOT NULL,
                    [Username] nvarchar(max) NOT NULL,
                    [Password] nvarchar(max) NOT NULL,
                    [AutoSyncEnabled] bit NOT NULL,
                    [AutoSyncIntervalMinutes] int NOT NULL,
                    [LastSuccessfulSyncAt] datetime2 NULL,
                    [LastSyncError] nvarchar(max) NULL,
                    [AccessToken] nvarchar(max) NULL,
                    [RefreshToken] nvarchar(max) NULL,
                    [AccessTokenExpiresAt] datetime2 NULL,
                    CONSTRAINT [PK_CloudSyncSettings] PRIMARY KEY ([Id])
                );
                SET IDENTITY_INSERT [dbo].[CloudSyncSettings] ON;
                INSERT INTO [dbo].[CloudSyncSettings]
                    ([Id], [ApiBaseUrl], [Username], [Password], [AutoSyncEnabled], [AutoSyncIntervalMinutes],
                     [LastSuccessfulSyncAt], [LastSyncError], [AccessToken], [RefreshToken], [AccessTokenExpiresAt])
                VALUES (1, N'', N'', N'', 0, 15, NULL, NULL, NULL, NULL, NULL);
                SET IDENTITY_INSERT [dbo].[CloudSyncSettings] OFF;
            END
            """, cancellationToken);

        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.SyncStates', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[SyncStates] (
                    [EntityType] nvarchar(450) NOT NULL,
                    [LastPulledAt] datetime2 NULL,
                    [LastPushedAt] datetime2 NULL,
                    [ServerCursor] nvarchar(max) NULL,
                    CONSTRAINT [PK_SyncStates] PRIMARY KEY ([EntityType])
                );
            END
            """, cancellationToken);
    }

    public static async Task<bool> IsSchemaReadyAsync(
        CarDbContext db,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await db.CarSaleContracts.AsNoTracking()
                .Select(c => new { c.IsAgreedPrice, c.WitnessOneName, c.WitnessTwoName })
                .Take(1)
                .ToListAsync(cancellationToken);

            _ = await db.PrintBrandingSettings.AsNoTracking()
                .Select(p => new { p.CompanyIdNumber, p.CompanyIdIssuer })
                .Take(1)
                .ToListAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CarSchemaRepair] Schema probe failed: {ex.Message}");
            return false;
        }
    }

    public static string BranchSchemaOutdatedMessage =>
        "قاعدة بيانات عقود السيارات على الحاسبة الرئيسية تحتاج تحديث.\n\n" +
        "افتح برنامج عقود السيارات على الحاسبة الرئيسية مرة واحدة بعد آخر تحديث لتطبيق تحديثات قاعدة البيانات، " +
        "ثم أعد المحاولة من هذا الجهاز.";

    public static string StandaloneSchemaOutdatedMessage =>
        "تعذر التحقق من مخطط قاعدة بيانات عقود السيارات.\n\n" +
        "أعد تشغيل البرنامج بعد التحديث. إذا استمر الخطأ، خذ نسخة احتياطية وتواصل مع الدعم.";

    private static async Task TryExecAsync(CarDbContext db, string sql, CancellationToken cancellationToken)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CarSchemaRepair] SQL skipped/failed: {ex.Message}");
        }
    }
}
