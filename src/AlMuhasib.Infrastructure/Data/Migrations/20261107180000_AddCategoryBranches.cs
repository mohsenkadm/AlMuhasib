using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using AlMuhasib.Infrastructure.Data;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations;

/// <summary>Category ↔ Branch visibility links (CategoryBranches) with backfill for existing categories.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261107180000_AddCategoryBranches")]
public partial class AddCategoryBranches : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.CategoryBranches', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[CategoryBranches] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [CategoryId] int NOT NULL,
                    [BranchId] int NOT NULL,
                    [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_CategoryBranches_CreatedAt] DEFAULT (SYSUTCDATETIME()),
                    CONSTRAINT [PK_CategoryBranches] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_CategoryBranches_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories]([Id]) ON DELETE CASCADE,
                    CONSTRAINT [FK_CategoryBranches_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]) ON DELETE NO ACTION
                );
                CREATE UNIQUE INDEX [IX_CategoryBranches_CategoryId_BranchId] ON [dbo].[CategoryBranches]([CategoryId], [BranchId]);
                CREATE INDEX [IX_CategoryBranches_BranchId] ON [dbo].[CategoryBranches]([BranchId]);
            END

            -- Backfill: only categories with ZERO links → all active branches (legacy).
            IF OBJECT_ID(N'dbo.CategoryBranches', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL
            BEGIN
                INSERT INTO [dbo].[CategoryBranches] ([CategoryId], [BranchId], [CreatedAt])
                SELECT c.[Id], b.[Id], SYSUTCDATETIME()
                FROM [dbo].[Categories] c
                CROSS JOIN [dbo].[Branches] b
                WHERE c.[IsDeleted] = 0
                  AND b.[IsDeleted] = 0
                  AND b.[IsActive] = 1
                  AND NOT EXISTS (
                      SELECT 1 FROM [dbo].[CategoryBranches] cb
                      WHERE cb.[CategoryId] = c.[Id]);
            END
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.CategoryBranches', N'U') IS NOT NULL
                DROP TABLE [dbo].[CategoryBranches];
            """);
    }
}
