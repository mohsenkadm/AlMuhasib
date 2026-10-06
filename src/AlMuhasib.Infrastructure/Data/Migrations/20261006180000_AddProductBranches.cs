using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using AlMuhasib.Infrastructure.Data;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations;

/// <summary>Product ↔ Branch visibility links (ProductBranches) with backfill for existing products.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261006180000_AddProductBranches")]
public partial class AddProductBranches : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ProductBranches', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[ProductBranches] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [ProductId] int NOT NULL,
                    [BranchId] int NOT NULL,
                    [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_ProductBranches_CreatedAt] DEFAULT (SYSUTCDATETIME()),
                    CONSTRAINT [PK_ProductBranches] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_ProductBranches_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_ProductBranches_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]) ON DELETE NO ACTION
                );
                CREATE UNIQUE INDEX [IX_ProductBranches_ProductId_BranchId] ON [dbo].[ProductBranches]([ProductId], [BranchId]);
                CREATE INDEX [IX_ProductBranches_BranchId] ON [dbo].[ProductBranches]([BranchId]);
            END

            -- Backfill: every existing (non-deleted) product appears in every active branch.
            IF OBJECT_ID(N'dbo.ProductBranches', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL
            BEGIN
                INSERT INTO [dbo].[ProductBranches] ([ProductId], [BranchId], [CreatedAt])
                SELECT p.[Id], b.[Id], SYSUTCDATETIME()
                FROM [dbo].[Products] p
                CROSS JOIN [dbo].[Branches] b
                WHERE p.[IsDeleted] = 0
                  AND b.[IsDeleted] = 0
                  AND b.[IsActive] = 1
                  AND NOT EXISTS (
                      SELECT 1 FROM [dbo].[ProductBranches] pb
                      WHERE pb.[ProductId] = p.[Id] AND pb.[BranchId] = b.[Id]);
            END
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ProductBranches', N'U') IS NOT NULL
                DROP TABLE [dbo].[ProductBranches];
            """);
    }
}
