using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using AlMuhasib.Infrastructure.Data;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations;

/// <summary>توسيع حقل باركود المنتج لتخزين عدة باركودات مفصولة بـ |.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261027180000_WidenProductBarcode")]
public partial class WidenProductBarcode : Migration
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

                ALTER TABLE [dbo].[Products] ALTER COLUMN [Barcode] nvarchar(50) NULL;

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
