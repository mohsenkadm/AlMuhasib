using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations;

/// <summary>USD list prices on ProductPrices — independent from IQD sale/purchase prices.</summary>
public partial class AddProductPriceUsdColumns : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Idempotent: columns may already exist via AccountingSchemaRepair.
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.ProductPrices', N'SalePriceUsd') IS NULL
                ALTER TABLE [dbo].[ProductPrices] ADD [SalePriceUsd] decimal(18,2) NOT NULL
                    CONSTRAINT [DF_ProductPrices_SalePriceUsd] DEFAULT (0);
            IF COL_LENGTH(N'dbo.ProductPrices', N'PurchasePriceUsd') IS NULL
                ALTER TABLE [dbo].[ProductPrices] ADD [PurchasePriceUsd] decimal(18,2) NOT NULL
                    CONSTRAINT [DF_ProductPrices_PurchasePriceUsd] DEFAULT (0);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
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
