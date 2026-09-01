using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

/// <summary>
/// Idempotent schema repairs for accounting DB after EF migrations.
/// Covers edge cases where migration history exists but columns are missing
/// (restored backup, partial deploy, branch server not migrated).
/// </summary>
public static class AccountingSchemaRepair
{
    public static async Task ApplyAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await TryExecAsync(db, """
            IF COL_LENGTH(N'dbo.Vouchers', N'SyncId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Vouchers] ADD [SyncId] uniqueidentifier NOT NULL
                    CONSTRAINT [DF_Vouchers_SyncId] DEFAULT (NEWID());
            END
            """, cancellationToken);

        await TryExecAsync(db, """
            IF COL_LENGTH(N'dbo.Vouchers', N'RowVersion') IS NULL
                ALTER TABLE [dbo].[Vouchers] ADD [RowVersion] rowversion NOT NULL;
            """, cancellationToken);

        await TryExecAsync(db, """
            UPDATE [dbo].[Vouchers]
            SET [SyncId] = NEWID()
            WHERE [SyncId] = '00000000-0000-0000-0000-000000000000';
            """, cancellationToken);

        await TryExecAsync(db, """
            IF COL_LENGTH(N'dbo.Vouchers', N'InvoiceId') IS NULL
                ALTER TABLE [dbo].[Vouchers] ADD [InvoiceId] int NULL;
            IF COL_LENGTH(N'dbo.Vouchers', N'InstallmentId') IS NULL
                ALTER TABLE [dbo].[Vouchers] ADD [InstallmentId] int NULL;
            IF COL_LENGTH(N'dbo.Vouchers', N'IsReconciled') IS NULL
                ALTER TABLE [dbo].[Vouchers] ADD [IsReconciled] bit NOT NULL
                    CONSTRAINT [DF_Vouchers_IsReconciled] DEFAULT (0);
            IF COL_LENGTH(N'dbo.Vouchers', N'ReconciledAt') IS NULL
                ALTER TABLE [dbo].[Vouchers] ADD [ReconciledAt] datetime2 NULL;
            IF COL_LENGTH(N'dbo.Vouchers', N'ReconciledBy') IS NULL
                ALTER TABLE [dbo].[Vouchers] ADD [ReconciledBy] nvarchar(100) NULL;
            """, cancellationToken);

        await TryExecAsync(db, """
            IF COL_LENGTH(N'dbo.BusinessSettings', N'PeriodLockEnabled') IS NULL
                ALTER TABLE [dbo].[BusinessSettings] ADD [PeriodLockEnabled] bit NOT NULL
                    CONSTRAINT [DF_BusinessSettings_PeriodLockEnabled] DEFAULT (0);
            IF COL_LENGTH(N'dbo.BusinessSettings', N'LockedThroughDate') IS NULL
                ALTER TABLE [dbo].[BusinessSettings] ADD [LockedThroughDate] datetime2 NULL;
            """, cancellationToken);

        await TryExecAsync(db, """
            IF COL_LENGTH(N'dbo.Invoices', N'PaidAmount') IS NULL
                ALTER TABLE [dbo].[Invoices] ADD [PaidAmount] decimal(18,2) NOT NULL
                    CONSTRAINT [DF_Invoices_PaidAmount] DEFAULT (0);
            IF COL_LENGTH(N'dbo.Invoices', N'RemainingAmount') IS NULL
                ALTER TABLE [dbo].[Invoices] ADD [RemainingAmount] decimal(18,2) NOT NULL
                    CONSTRAINT [DF_Invoices_RemainingAmount] DEFAULT (0);
            IF COL_LENGTH(N'dbo.Invoices', N'IsCreditPaid') IS NULL
                ALTER TABLE [dbo].[Invoices] ADD [IsCreditPaid] bit NOT NULL
                    CONSTRAINT [DF_Invoices_IsCreditPaid] DEFAULT (0);
            IF COL_LENGTH(N'dbo.Invoices', N'RelatedInvoiceId') IS NULL
                ALTER TABLE [dbo].[Invoices] ADD [RelatedInvoiceId] int NULL;
            """, cancellationToken);
    }

    public static async Task<bool> IsVoucherSchemaReadyAsync(
        AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await db.Vouchers.AsNoTracking()
                .Select(v => new { v.InvoiceId, v.InstallmentId, v.IsReconciled, v.SyncId })
                .Take(1)
                .ToListAsync(cancellationToken);

            _ = await db.BusinessSettings.AsNoTracking()
                .Select(s => new { s.PeriodLockEnabled, s.LockedThroughDate })
                .Take(1)
                .ToListAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AccountingSchemaRepair] Schema probe failed: {ex.Message}");
            return false;
        }
    }

    public static string BranchSchemaOutdatedMessage =>
        "قاعدة البيانات على الحاسبة الرئيسية تحتاج تحديث.\n\n" +
        "افتح البرنامج على الحاسبة الرئيسية مرة واحدة بعد آخر تحديث لتطبيق تحديثات قاعدة البيانات، " +
        "ثم أعد المحاولة من هذا الجهاز.";

    public static string StandaloneSchemaOutdatedMessage =>
        "تعذر التحقق من مخطط قاعدة البيانات المطلوب لحفظ السندات.\n\n" +
        "أعد تشغيل البرنامج بعد التحديث. إذا استمر الخطأ، خذ نسخة احتياطية وتواصل مع الدعم.";

    private static async Task TryExecAsync(AppDbContext db, string sql, CancellationToken cancellationToken)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AccountingSchemaRepair] SQL skipped/failed: {ex.Message}");
        }
    }
}
