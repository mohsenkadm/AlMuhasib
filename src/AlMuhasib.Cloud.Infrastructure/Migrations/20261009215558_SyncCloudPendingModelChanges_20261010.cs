using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations;

/// <summary>
/// Syncs ProductPrices USD columns + CurrencyExchanges into the model snapshot.
/// Idempotent: the earlier hand-written SQL migration had no Designer/snapshot,
/// so EF raised PendingModelChangesWarning even when the DB already had the objects.
/// </summary>
public partial class SyncCloudPendingModelChanges_20261010 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.ProductPrices', N'SalePriceUsd') IS NULL
                ALTER TABLE [dbo].[ProductPrices] ADD [SalePriceUsd] decimal(18,2) NOT NULL
                    CONSTRAINT [DF_CloudProductPrices_SalePriceUsd] DEFAULT (0);
            IF COL_LENGTH(N'dbo.ProductPrices', N'PurchasePriceUsd') IS NULL
                ALTER TABLE [dbo].[ProductPrices] ADD [PurchasePriceUsd] decimal(18,2) NOT NULL
                    CONSTRAINT [DF_CloudProductPrices_PurchasePriceUsd] DEFAULT (0);

            IF OBJECT_ID(N'dbo.CurrencyExchanges', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[CurrencyExchanges] (
                    [Id] int NOT NULL IDENTITY,
                    [FromCashBoxId] int NOT NULL,
                    [ToCashBoxId] int NOT NULL,
                    [FromCurrency] nvarchar(10) NOT NULL,
                    [ToCurrency] nvarchar(10) NOT NULL,
                    [FromAmount] decimal(18,4) NOT NULL,
                    [ToAmount] decimal(18,4) NOT NULL,
                    [FxRate] decimal(18,4) NOT NULL,
                    [Date] datetime2 NOT NULL,
                    [Notes] nvarchar(1000) NULL,
                    [TenantId] int NOT NULL,
                    [BranchId] int NOT NULL,
                    [SyncId] uniqueidentifier NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [CreatedBy] nvarchar(max) NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    [UpdatedBy] nvarchar(max) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedAt] datetime2 NULL,
                    [DeletedBy] nvarchar(max) NULL,
                    [RowVersion] rowversion NOT NULL,
                    CONSTRAINT [PK_CurrencyExchanges] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_CurrencyExchanges_CashBoxes_FromCashBoxId] FOREIGN KEY ([FromCashBoxId]) REFERENCES [dbo].[CashBoxes] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_CurrencyExchanges_CashBoxes_ToCashBoxId] FOREIGN KEY ([ToCashBoxId]) REFERENCES [dbo].[CashBoxes] ([Id]) ON DELETE NO ACTION
                );
            END

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CurrencyExchanges_Date' AND object_id = OBJECT_ID(N'dbo.CurrencyExchanges'))
                CREATE INDEX [IX_CurrencyExchanges_Date] ON [dbo].[CurrencyExchanges] ([Date]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CurrencyExchanges_FromCashBoxId' AND object_id = OBJECT_ID(N'dbo.CurrencyExchanges'))
                CREATE INDEX [IX_CurrencyExchanges_FromCashBoxId] ON [dbo].[CurrencyExchanges] ([FromCashBoxId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CurrencyExchanges_ToCashBoxId' AND object_id = OBJECT_ID(N'dbo.CurrencyExchanges'))
                CREATE INDEX [IX_CurrencyExchanges_ToCashBoxId] ON [dbo].[CurrencyExchanges] ([ToCashBoxId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CurrencyExchanges_TenantId_BranchId' AND object_id = OBJECT_ID(N'dbo.CurrencyExchanges'))
                CREATE INDEX [IX_CurrencyExchanges_TenantId_BranchId] ON [dbo].[CurrencyExchanges] ([TenantId], [BranchId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CurrencyExchanges_TenantId_SyncId' AND object_id = OBJECT_ID(N'dbo.CurrencyExchanges'))
                CREATE UNIQUE INDEX [IX_CurrencyExchanges_TenantId_SyncId] ON [dbo].[CurrencyExchanges] ([TenantId], [SyncId]);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.CurrencyExchanges', N'U') IS NOT NULL
                DROP TABLE [dbo].[CurrencyExchanges];

            IF COL_LENGTH(N'dbo.ProductPrices', N'SalePriceUsd') IS NOT NULL
            BEGIN
                DECLARE @dfSale sysname =
                    (SELECT dc.name FROM sys.default_constraints dc
                     INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                     WHERE dc.parent_object_id = OBJECT_ID(N'dbo.ProductPrices') AND c.name = N'SalePriceUsd');
                IF @dfSale IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ProductPrices] DROP CONSTRAINT [' + @dfSale + N']');
                ALTER TABLE [dbo].[ProductPrices] DROP COLUMN [SalePriceUsd];
            END
            IF COL_LENGTH(N'dbo.ProductPrices', N'PurchasePriceUsd') IS NOT NULL
            BEGIN
                DECLARE @dfPurch sysname =
                    (SELECT dc.name FROM sys.default_constraints dc
                     INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                     WHERE dc.parent_object_id = OBJECT_ID(N'dbo.ProductPrices') AND c.name = N'PurchasePriceUsd');
                IF @dfPurch IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ProductPrices] DROP CONSTRAINT [' + @dfPurch + N']');
                ALTER TABLE [dbo].[ProductPrices] DROP COLUMN [PurchasePriceUsd];
            END
            """);
    }
}
