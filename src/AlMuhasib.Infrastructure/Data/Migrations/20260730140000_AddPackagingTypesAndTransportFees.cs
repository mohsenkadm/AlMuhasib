using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPackagingTypesAndTransportFees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: safe if a previous failed update partially created objects.
            migrationBuilder.Sql("""
                IF COL_LENGTH('Invoices', 'TransportFeeAmount') IS NULL
                BEGIN
                    ALTER TABLE [Invoices] ADD [TransportFeeAmount] decimal(18,2) NOT NULL CONSTRAINT DF_Invoices_TransportFeeAmount DEFAULT (0);
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[PackagingTypes]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [PackagingTypes] (
                        [Id] int NOT NULL IDENTITY,
                        [Name] nvarchar(200) NOT NULL,
                        [IsDefault] bit NOT NULL,
                        [IsActive] bit NOT NULL,
                        [SyncId] uniqueidentifier NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(100) NOT NULL,
                        [UpdatedAt] datetime2 NULL,
                        [UpdatedBy] nvarchar(100) NULL,
                        [IsDeleted] bit NOT NULL,
                        [DeletedAt] datetime2 NULL,
                        [DeletedBy] nvarchar(100) NULL,
                        [RowVersion] rowversion NOT NULL,
                        CONSTRAINT [PK_PackagingTypes] PRIMARY KEY ([Id])
                    );
                    CREATE INDEX [IX_PackagingTypes_IsDeleted] ON [PackagingTypes] ([IsDeleted]);
                    CREATE INDEX [IX_PackagingTypes_Name] ON [PackagingTypes] ([Name]);
                    CREATE INDEX [IX_PackagingTypes_SyncId] ON [PackagingTypes] ([SyncId]);
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('ProductUnits', 'PackagingTypeId') IS NULL
                BEGIN
                    ALTER TABLE [ProductUnits] ADD [PackagingTypeId] int NULL;
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_ProductUnits_PackagingTypeId'
                      AND object_id = OBJECT_ID(N'[dbo].[ProductUnits]'))
                BEGIN
                    CREATE INDEX [IX_ProductUnits_PackagingTypeId] ON [ProductUnits] ([PackagingTypeId]);
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[FK_ProductUnits_PackagingTypes_PackagingTypeId]', N'F') IS NULL
                   AND COL_LENGTH('ProductUnits', 'PackagingTypeId') IS NOT NULL
                   AND OBJECT_ID(N'[dbo].[PackagingTypes]', N'U') IS NOT NULL
                BEGIN
                    ALTER TABLE [ProductUnits] WITH CHECK
                    ADD CONSTRAINT [FK_ProductUnits_PackagingTypes_PackagingTypeId]
                    FOREIGN KEY ([PackagingTypeId]) REFERENCES [PackagingTypes] ([Id]) ON DELETE SET NULL;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[FK_ProductUnits_PackagingTypes_PackagingTypeId]', N'F') IS NOT NULL
                    ALTER TABLE [ProductUnits] DROP CONSTRAINT [FK_ProductUnits_PackagingTypes_PackagingTypeId];
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_ProductUnits_PackagingTypeId'
                      AND object_id = OBJECT_ID(N'[dbo].[ProductUnits]'))
                    DROP INDEX [IX_ProductUnits_PackagingTypeId] ON [ProductUnits];
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('ProductUnits', 'PackagingTypeId') IS NOT NULL
                    ALTER TABLE [ProductUnits] DROP COLUMN [PackagingTypeId];
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[PackagingTypes]', N'U') IS NOT NULL
                    DROP TABLE [PackagingTypes];
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Invoices', 'TransportFeeAmount') IS NOT NULL
                BEGIN
                    DECLARE @df sysname;
                    SELECT @df = dc.name
                    FROM sys.default_constraints dc
                    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                    WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Invoices]')
                      AND c.name = N'TransportFeeAmount';
                    IF @df IS NOT NULL
                        EXEC(N'ALTER TABLE [Invoices] DROP CONSTRAINT [' + @df + N']');
                    ALTER TABLE [Invoices] DROP COLUMN [TransportFeeAmount];
                END
                """);
        }
    }
}
