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
                ALTER TABLE [dbo].[Products] ALTER COLUMN [Barcode] nvarchar(500) NULL;
            END
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.Products', N'Barcode') IS NOT NULL
            BEGIN
                ALTER TABLE [dbo].[Products] ALTER COLUMN [Barcode] nvarchar(50) NULL;
            END
            """);
    }
}
