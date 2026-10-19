using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using AlMuhasib.Infrastructure.Data;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations;

/// <summary>عمود إظهار المنتج في شاشة الكاشير (افتراضي: ظاهر).</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261019160000_AddProductShowInCashier")]
public partial class AddProductShowInCashier : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.Products', N'ShowInCashier') IS NULL
            BEGIN
                ALTER TABLE [dbo].[Products]
                ADD [ShowInCashier] bit NOT NULL
                    CONSTRAINT [DF_Products_ShowInCashier] DEFAULT (1);
            END
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.Products', N'ShowInCashier') IS NOT NULL
            BEGIN
                DECLARE @df sysname;
                SELECT @df = dc.name
                FROM sys.default_constraints dc
                INNER JOIN sys.columns c
                    ON c.default_object_id = dc.object_id
                WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Products')
                  AND c.name = N'ShowInCashier';
                IF @df IS NOT NULL
                    EXEC(N'ALTER TABLE [dbo].[Products] DROP CONSTRAINT [' + @df + N']');
                ALTER TABLE [dbo].[Products] DROP COLUMN [ShowInCashier];
            END
            """);
    }
}
