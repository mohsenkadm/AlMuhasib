using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using AlMuhasib.Infrastructure.Data;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations;

/// <summary>
/// توسيع حقل باركود المنتج لوضع انفاركار ماكس (حتى 2000 حرف، فراغ مسموح).
/// يُزال الفهرس الفريد لأن مفتاح nvarchar(2000) يتجاوز حد 1700 بايت.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261101120000_WidenProductBarcodeInfarkarMax")]
public partial class WidenProductBarcodeInfarkarMax : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.Products', N'Barcode') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_Products_Barcode'
                      AND object_id = OBJECT_ID(N'dbo.Products'))
                BEGIN
                    DROP INDEX [IX_Products_Barcode] ON [dbo].[Products];
                END

                ALTER TABLE [dbo].[Products] ALTER COLUMN [Barcode] nvarchar(2000) NULL;
            END
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.Products', N'Barcode') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_Products_Barcode'
                      AND object_id = OBJECT_ID(N'dbo.Products'))
                BEGIN
                    DROP INDEX [IX_Products_Barcode] ON [dbo].[Products];
                END

                ALTER TABLE [dbo].[Products] ALTER COLUMN [Barcode] nvarchar(500) NULL;

                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_Products_Barcode'
                      AND object_id = OBJECT_ID(N'dbo.Products'))
                BEGIN
                    CREATE UNIQUE INDEX [IX_Products_Barcode]
                        ON [dbo].[Products] ([Barcode])
                        WHERE [Barcode] IS NOT NULL AND [IsDeleted] = 0;
                END
            END
            """);
    }
}
