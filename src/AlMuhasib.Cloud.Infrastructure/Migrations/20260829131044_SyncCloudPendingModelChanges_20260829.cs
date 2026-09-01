using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations
{
    /// <summary>
    /// Adds gold-shop parity columns (vouchers supplier/opening/cash flags, invoice related id,
    /// making-charge mode/rate, customer gold credit grams, settings). Idempotent for DBs that
    /// may already have some columns from manual/sync updates.
    /// </summary>
    public partial class SyncCloudPendingModelChanges_20260829 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.GoldVouchers', N'AffectsCashBox') IS NULL
                    ALTER TABLE [GoldVouchers] ADD [AffectsCashBox] bit NOT NULL
                        CONSTRAINT [DF_GoldVouchers_AffectsCashBox] DEFAULT (1);

                IF COL_LENGTH(N'dbo.GoldVouchers', N'IsOpeningBalance') IS NULL
                    ALTER TABLE [GoldVouchers] ADD [IsOpeningBalance] bit NOT NULL
                        CONSTRAINT [DF_GoldVouchers_IsOpeningBalance] DEFAULT (0);

                IF COL_LENGTH(N'dbo.GoldVouchers', N'SupplierId') IS NULL
                    ALTER TABLE [GoldVouchers] ADD [SupplierId] int NULL;

                IF COL_LENGTH(N'dbo.GoldSettings', N'DefaultMakingChargeMode') IS NULL
                    ALTER TABLE [GoldSettings] ADD [DefaultMakingChargeMode] int NOT NULL
                        CONSTRAINT [DF_GoldSettings_DefaultMakingChargeMode] DEFAULT (0);

                IF COL_LENGTH(N'dbo.GoldSettings', N'IsConfigured') IS NULL
                    ALTER TABLE [GoldSettings] ADD [IsConfigured] bit NOT NULL
                        CONSTRAINT [DF_GoldSettings_IsConfigured] DEFAULT (0);

                IF COL_LENGTH(N'dbo.GoldInvoices', N'RelatedInvoiceId') IS NULL
                    ALTER TABLE [GoldInvoices] ADD [RelatedInvoiceId] int NULL;

                IF COL_LENGTH(N'dbo.GoldInvoiceLines', N'MakingChargeMode') IS NULL
                    ALTER TABLE [GoldInvoiceLines] ADD [MakingChargeMode] int NOT NULL
                        CONSTRAINT [DF_GoldInvoiceLines_MakingChargeMode] DEFAULT (0);

                IF COL_LENGTH(N'dbo.GoldInvoiceLines', N'MakingChargeRate') IS NULL
                    ALTER TABLE [GoldInvoiceLines] ADD [MakingChargeRate] decimal(18,2) NOT NULL
                        CONSTRAINT [DF_GoldInvoiceLines_MakingChargeRate] DEFAULT (0);

                IF COL_LENGTH(N'dbo.GoldCustomers', N'GoldCreditGrams') IS NULL
                    ALTER TABLE [GoldCustomers] ADD [GoldCreditGrams] decimal(18,2) NOT NULL
                        CONSTRAINT [DF_GoldCustomers_GoldCreditGrams] DEFAULT (0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.GoldVouchers', N'AffectsCashBox') IS NOT NULL
                BEGIN
                    DECLARE @df1 sysname = (SELECT dc.name FROM sys.default_constraints dc
                        INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.GoldVouchers') AND c.name = N'AffectsCashBox');
                    IF @df1 IS NOT NULL EXEC(N'ALTER TABLE [GoldVouchers] DROP CONSTRAINT [' + @df1 + N']');
                    ALTER TABLE [GoldVouchers] DROP COLUMN [AffectsCashBox];
                END

                IF COL_LENGTH(N'dbo.GoldVouchers', N'IsOpeningBalance') IS NOT NULL
                BEGIN
                    DECLARE @df2 sysname = (SELECT dc.name FROM sys.default_constraints dc
                        INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.GoldVouchers') AND c.name = N'IsOpeningBalance');
                    IF @df2 IS NOT NULL EXEC(N'ALTER TABLE [GoldVouchers] DROP CONSTRAINT [' + @df2 + N']');
                    ALTER TABLE [GoldVouchers] DROP COLUMN [IsOpeningBalance];
                END

                IF COL_LENGTH(N'dbo.GoldVouchers', N'SupplierId') IS NOT NULL
                    ALTER TABLE [GoldVouchers] DROP COLUMN [SupplierId];

                IF COL_LENGTH(N'dbo.GoldSettings', N'DefaultMakingChargeMode') IS NOT NULL
                BEGIN
                    DECLARE @df3 sysname = (SELECT dc.name FROM sys.default_constraints dc
                        INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.GoldSettings') AND c.name = N'DefaultMakingChargeMode');
                    IF @df3 IS NOT NULL EXEC(N'ALTER TABLE [GoldSettings] DROP CONSTRAINT [' + @df3 + N']');
                    ALTER TABLE [GoldSettings] DROP COLUMN [DefaultMakingChargeMode];
                END

                IF COL_LENGTH(N'dbo.GoldSettings', N'IsConfigured') IS NOT NULL
                BEGIN
                    DECLARE @df4 sysname = (SELECT dc.name FROM sys.default_constraints dc
                        INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.GoldSettings') AND c.name = N'IsConfigured');
                    IF @df4 IS NOT NULL EXEC(N'ALTER TABLE [GoldSettings] DROP CONSTRAINT [' + @df4 + N']');
                    ALTER TABLE [GoldSettings] DROP COLUMN [IsConfigured];
                END

                IF COL_LENGTH(N'dbo.GoldInvoices', N'RelatedInvoiceId') IS NOT NULL
                    ALTER TABLE [GoldInvoices] DROP COLUMN [RelatedInvoiceId];

                IF COL_LENGTH(N'dbo.GoldInvoiceLines', N'MakingChargeMode') IS NOT NULL
                BEGIN
                    DECLARE @df5 sysname = (SELECT dc.name FROM sys.default_constraints dc
                        INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.GoldInvoiceLines') AND c.name = N'MakingChargeMode');
                    IF @df5 IS NOT NULL EXEC(N'ALTER TABLE [GoldInvoiceLines] DROP CONSTRAINT [' + @df5 + N']');
                    ALTER TABLE [GoldInvoiceLines] DROP COLUMN [MakingChargeMode];
                END

                IF COL_LENGTH(N'dbo.GoldInvoiceLines', N'MakingChargeRate') IS NOT NULL
                BEGIN
                    DECLARE @df6 sysname = (SELECT dc.name FROM sys.default_constraints dc
                        INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.GoldInvoiceLines') AND c.name = N'MakingChargeRate');
                    IF @df6 IS NOT NULL EXEC(N'ALTER TABLE [GoldInvoiceLines] DROP CONSTRAINT [' + @df6 + N']');
                    ALTER TABLE [GoldInvoiceLines] DROP COLUMN [MakingChargeRate];
                END

                IF COL_LENGTH(N'dbo.GoldCustomers', N'GoldCreditGrams') IS NOT NULL
                BEGIN
                    DECLARE @df7 sysname = (SELECT dc.name FROM sys.default_constraints dc
                        INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.GoldCustomers') AND c.name = N'GoldCreditGrams');
                    IF @df7 IS NOT NULL EXEC(N'ALTER TABLE [GoldCustomers] DROP CONSTRAINT [' + @df7 + N']');
                    ALTER TABLE [GoldCustomers] DROP COLUMN [GoldCreditGrams];
                END
                """);
        }
    }
}
