using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using AlMuhasib.Infrastructure.Data;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations;

/// <summary>أنواع الإيرادات الأخرى وحركاتها النقدية.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261117120000_AddOtherIncomes")]
public partial class AddOtherIncomes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.OtherIncomeTypes', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[OtherIncomeTypes] (
                    [Id] int NOT NULL IDENTITY,
                    [Name] nvarchar(200) NOT NULL,
                    [IsActive] bit NOT NULL CONSTRAINT [DF_OtherIncomeTypes_IsActive] DEFAULT (1),
                    [BranchId] int NOT NULL,
                    [SyncId] uniqueidentifier NOT NULL CONSTRAINT [DF_OtherIncomeTypes_SyncId] DEFAULT (NEWID()),
                    [CreatedAt] datetime2 NOT NULL,
                    [CreatedBy] nvarchar(100) NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    [UpdatedBy] nvarchar(100) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedAt] datetime2 NULL,
                    [DeletedBy] nvarchar(100) NULL,
                    [RowVersion] rowversion NOT NULL,
                    CONSTRAINT [PK_OtherIncomeTypes] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_OtherIncomeTypes_Branches_BranchId]
                        FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]) ON DELETE NO ACTION
                );

                CREATE UNIQUE INDEX [IX_OtherIncomeTypes_Name]
                    ON [dbo].[OtherIncomeTypes]([Name])
                    WHERE [IsDeleted] = 0;
                CREATE INDEX [IX_OtherIncomeTypes_BranchId]
                    ON [dbo].[OtherIncomeTypes]([BranchId]);
                CREATE INDEX [IX_OtherIncomeTypes_IsDeleted]
                    ON [dbo].[OtherIncomeTypes]([IsDeleted]);
            END

            IF OBJECT_ID(N'dbo.OtherIncomes', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[OtherIncomes] (
                    [Id] int NOT NULL IDENTITY,
                    [IncomeNumber] nvarchar(30) NOT NULL,
                    [OtherIncomeTypeId] int NOT NULL,
                    [Currency] nvarchar(10) NOT NULL CONSTRAINT [DF_OtherIncomes_Currency] DEFAULT (N'IQD'),
                    [FxRate] decimal(18,4) NOT NULL,
                    [Amount] decimal(18,2) NOT NULL,
                    [Date] datetime2 NOT NULL,
                    [CashBoxId] int NOT NULL,
                    [Notes] nvarchar(1000) NULL,
                    [BranchId] int NOT NULL,
                    [SyncId] uniqueidentifier NOT NULL CONSTRAINT [DF_OtherIncomes_SyncId] DEFAULT (NEWID()),
                    [CreatedAt] datetime2 NOT NULL,
                    [CreatedBy] nvarchar(100) NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    [UpdatedBy] nvarchar(100) NULL,
                    [IsDeleted] bit NOT NULL,
                    [DeletedAt] datetime2 NULL,
                    [DeletedBy] nvarchar(100) NULL,
                    [RowVersion] rowversion NOT NULL,
                    CONSTRAINT [PK_OtherIncomes] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_OtherIncomes_OtherIncomeTypes_OtherIncomeTypeId]
                        FOREIGN KEY ([OtherIncomeTypeId]) REFERENCES [dbo].[OtherIncomeTypes]([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_OtherIncomes_CashBoxes_CashBoxId]
                        FOREIGN KEY ([CashBoxId]) REFERENCES [dbo].[CashBoxes]([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_OtherIncomes_Branches_BranchId]
                        FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]) ON DELETE NO ACTION
                );

                CREATE UNIQUE INDEX [IX_OtherIncomes_BranchId_IncomeNumber]
                    ON [dbo].[OtherIncomes]([BranchId], [IncomeNumber])
                    WHERE [IsDeleted] = 0;
                CREATE INDEX [IX_OtherIncomes_Date]
                    ON [dbo].[OtherIncomes]([Date]);
                CREATE INDEX [IX_OtherIncomes_OtherIncomeTypeId]
                    ON [dbo].[OtherIncomes]([OtherIncomeTypeId]);
                CREATE INDEX [IX_OtherIncomes_CashBoxId]
                    ON [dbo].[OtherIncomes]([CashBoxId]);
                CREATE INDEX [IX_OtherIncomes_BranchId]
                    ON [dbo].[OtherIncomes]([BranchId]);
                CREATE INDEX [IX_OtherIncomes_IsDeleted]
                    ON [dbo].[OtherIncomes]([IsDeleted]);
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.OtherIncomes', N'U') IS NOT NULL
                DROP TABLE [dbo].[OtherIncomes];
            IF OBJECT_ID(N'dbo.OtherIncomeTypes', N'U') IS NULL
                RETURN;
            DROP TABLE [dbo].[OtherIncomeTypes];
            """);
    }
}
