using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations;

/// <summary>
/// ProductPrices USD columns + CurrencyExchanges table for Desktop↔Cloud parity.
/// </summary>
public partial class AddProductPriceUsdAndCurrencyExchangesCloud : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.ProductPrices', N'SalePriceUsd') IS NULL
                ALTER TABLE [dbo].[ProductPrices] ADD [SalePriceUsd] decimal(18,2) NOT NULL
                    CONSTRAINT [DF_CloudProductPrices_SalePriceUsd] DEFAULT (0);
            IF COL_LENGTH(N'dbo.ProductPrices', N'PurchasePriceUsd') IS NULL
                ALTER TABLE [dbo].[ProductPrices] ADD [PurchasePriceUsd] decimal(18,2) NOT NULL
                    CONSTRAINT [DF_CloudProductPrices_PurchasePriceUsd] DEFAULT (0);
            """);

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.CurrencyExchanges', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[CurrencyExchanges] (
                    [Id] int NOT NULL IDENTITY,
                    [TenantId] int NOT NULL,
                    [BranchId] int NOT NULL,
                    [SyncId] uniqueidentifier NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [CreatedBy] nvarchar(max) NULL,
                    [UpdatedAt] datetime2 NULL,
                    [UpdatedBy] nvarchar(max) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedAt] datetime2 NULL,
                    [DeletedBy] nvarchar(max) NULL,
                    [RowVersion] rowversion NULL,
                    [FromCashBoxId] int NOT NULL,
                    [ToCashBoxId] int NOT NULL,
                    [FromCurrency] nvarchar(10) NOT NULL,
                    [ToCurrency] nvarchar(10) NOT NULL,
                    [FromAmount] decimal(18,4) NOT NULL,
                    [ToAmount] decimal(18,4) NOT NULL,
                    [FxRate] decimal(18,4) NOT NULL,
                    [Date] datetime2 NOT NULL,
                    [Notes] nvarchar(1000) NULL,
                    CONSTRAINT [PK_CurrencyExchanges] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_CurrencyExchanges_CashBoxes_From] FOREIGN KEY ([FromCashBoxId]) REFERENCES [dbo].[CashBoxes] ([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_CurrencyExchanges_CashBoxes_To] FOREIGN KEY ([ToCashBoxId]) REFERENCES [dbo].[CashBoxes] ([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX [IX_CurrencyExchanges_TenantId_SyncId] ON [dbo].[CurrencyExchanges] ([TenantId], [SyncId]);
                CREATE INDEX [IX_CurrencyExchanges_BranchId] ON [dbo].[CurrencyExchanges] ([BranchId]);
                CREATE INDEX [IX_CurrencyExchanges_Date] ON [dbo].[CurrencyExchanges] ([Date]);
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.CurrencyExchanges', N'U') IS NOT NULL
                DROP TABLE [dbo].[CurrencyExchanges];
            """);

        migrationBuilder.Sql("""
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
