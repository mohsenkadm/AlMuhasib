using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudProductPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: previous failed runs may have applied some steps already
            // (e.g. PricingTypeId added, then IsSold duplicate failed).
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.InvoiceItems', 'PricingTypeId') IS NULL
                    ALTER TABLE [InvoiceItems] ADD [PricingTypeId] int NULL;

                IF OBJECT_ID(N'[dbo].[BusinessSettings]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [BusinessSettings] (
                        [Id] int NOT NULL IDENTITY,
                        [ProductPricingEnabled] bit NOT NULL,
                        [UpdateProductPriceOnPurchase] bit NOT NULL,
                        [TenantId] int NOT NULL,
                        [SyncId] uniqueidentifier NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(max) NOT NULL,
                        [UpdatedAt] datetime2 NULL,
                        [UpdatedBy] nvarchar(max) NULL,
                        [IsDeleted] bit NOT NULL,
                        [DeletedAt] datetime2 NULL,
                        [DeletedBy] nvarchar(max) NULL,
                        [RowVersion] rowversion NOT NULL,
                        CONSTRAINT [PK_BusinessSettings] PRIMARY KEY ([Id])
                    );
                END;

                IF OBJECT_ID(N'[dbo].[PricingTypes]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [PricingTypes] (
                        [Id] int NOT NULL IDENTITY,
                        [Name] nvarchar(200) NOT NULL,
                        [IsDefault] bit NOT NULL,
                        [IsActive] bit NOT NULL,
                        [TenantId] int NOT NULL,
                        [SyncId] uniqueidentifier NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(max) NOT NULL,
                        [UpdatedAt] datetime2 NULL,
                        [UpdatedBy] nvarchar(max) NULL,
                        [IsDeleted] bit NOT NULL,
                        [DeletedAt] datetime2 NULL,
                        [DeletedBy] nvarchar(max) NULL,
                        [RowVersion] rowversion NOT NULL,
                        CONSTRAINT [PK_PricingTypes] PRIMARY KEY ([Id])
                    );
                END;

                IF OBJECT_ID(N'[dbo].[ProductPrices]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [ProductPrices] (
                        [Id] int NOT NULL IDENTITY,
                        [ProductId] int NOT NULL,
                        [PricingTypeId] int NOT NULL,
                        [SalePrice] decimal(18,2) NOT NULL,
                        [PurchasePrice] decimal(18,2) NOT NULL,
                        [TenantId] int NOT NULL,
                        [SyncId] uniqueidentifier NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(max) NOT NULL,
                        [UpdatedAt] datetime2 NULL,
                        [UpdatedBy] nvarchar(max) NULL,
                        [IsDeleted] bit NOT NULL,
                        [DeletedAt] datetime2 NULL,
                        [DeletedBy] nvarchar(max) NULL,
                        [RowVersion] rowversion NOT NULL,
                        CONSTRAINT [PK_ProductPrices] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_ProductPrices_PricingTypes_PricingTypeId]
                            FOREIGN KEY ([PricingTypeId]) REFERENCES [PricingTypes] ([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_ProductPrices_Products_ProductId]
                            FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
                    );
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InvoiceItems_PricingTypeId' AND object_id = OBJECT_ID(N'[dbo].[InvoiceItems]'))
                    CREATE INDEX [IX_InvoiceItems_PricingTypeId] ON [InvoiceItems] ([PricingTypeId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BusinessSettings_TenantId_SyncId' AND object_id = OBJECT_ID(N'[dbo].[BusinessSettings]'))
                    CREATE UNIQUE INDEX [IX_BusinessSettings_TenantId_SyncId] ON [BusinessSettings] ([TenantId], [SyncId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PricingTypes_TenantId_Name' AND object_id = OBJECT_ID(N'[dbo].[PricingTypes]'))
                    CREATE INDEX [IX_PricingTypes_TenantId_Name] ON [PricingTypes] ([TenantId], [Name]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PricingTypes_TenantId_SyncId' AND object_id = OBJECT_ID(N'[dbo].[PricingTypes]'))
                    CREATE UNIQUE INDEX [IX_PricingTypes_TenantId_SyncId] ON [PricingTypes] ([TenantId], [SyncId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductPrices_PricingTypeId' AND object_id = OBJECT_ID(N'[dbo].[ProductPrices]'))
                    CREATE INDEX [IX_ProductPrices_PricingTypeId] ON [ProductPrices] ([PricingTypeId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductPrices_ProductId' AND object_id = OBJECT_ID(N'[dbo].[ProductPrices]'))
                    CREATE INDEX [IX_ProductPrices_ProductId] ON [ProductPrices] ([ProductId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductPrices_TenantId_ProductId_PricingTypeId' AND object_id = OBJECT_ID(N'[dbo].[ProductPrices]'))
                    CREATE UNIQUE INDEX [IX_ProductPrices_TenantId_ProductId_PricingTypeId]
                        ON [ProductPrices] ([TenantId], [ProductId], [PricingTypeId])
                        WHERE [IsDeleted] = 0;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductPrices_TenantId_SyncId' AND object_id = OBJECT_ID(N'[dbo].[ProductPrices]'))
                    CREATE UNIQUE INDEX [IX_ProductPrices_TenantId_SyncId] ON [ProductPrices] ([TenantId], [SyncId]);

                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = N'FK_InvoiceItems_PricingTypes_PricingTypeId'
                      AND parent_object_id = OBJECT_ID(N'[dbo].[InvoiceItems]')
                )
                BEGIN
                    ALTER TABLE [InvoiceItems] ADD CONSTRAINT [FK_InvoiceItems_PricingTypes_PricingTypeId]
                        FOREIGN KEY ([PricingTypeId]) REFERENCES [PricingTypes] ([Id]) ON DELETE SET NULL;
                END;

                INSERT INTO PricingTypes (Name, IsDefault, IsActive, TenantId, SyncId, CreatedAt, CreatedBy, IsDeleted)
                SELECT N'سعر مفرد', 1, 1, t.Id, 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001', '2026-07-01T00:00:00Z', N'System', 0
                FROM Tenants t
                WHERE NOT EXISTS (
                    SELECT 1 FROM PricingTypes pt
                    WHERE pt.TenantId = t.Id AND pt.IsDeleted = 0
                      AND (pt.IsDefault = 1 OR pt.SyncId = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001' OR pt.Name = N'سعر مفرد')
                );

                INSERT INTO BusinessSettings (ProductPricingEnabled, UpdateProductPriceOnPurchase, TenantId, SyncId, CreatedAt, CreatedBy, IsDeleted)
                SELECT 0, 0, t.Id, 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0002', '2026-07-01T00:00:00Z', N'System', 0
                FROM Tenants t
                WHERE NOT EXISTS (
                    SELECT 1 FROM BusinessSettings bs
                    WHERE bs.TenantId = t.Id AND bs.IsDeleted = 0
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = N'FK_InvoiceItems_PricingTypes_PricingTypeId'
                )
                    ALTER TABLE [InvoiceItems] DROP CONSTRAINT [FK_InvoiceItems_PricingTypes_PricingTypeId];

                IF OBJECT_ID(N'[dbo].[ProductPrices]', N'U') IS NOT NULL
                    DROP TABLE [ProductPrices];

                IF OBJECT_ID(N'[dbo].[BusinessSettings]', N'U') IS NOT NULL
                    DROP TABLE [BusinessSettings];

                IF OBJECT_ID(N'[dbo].[PricingTypes]', N'U') IS NOT NULL
                    DROP TABLE [PricingTypes];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InvoiceItems_PricingTypeId' AND object_id = OBJECT_ID(N'[dbo].[InvoiceItems]'))
                    DROP INDEX [IX_InvoiceItems_PricingTypeId] ON [InvoiceItems];

                IF COL_LENGTH('dbo.InvoiceItems', 'PricingTypeId') IS NOT NULL
                    ALTER TABLE [InvoiceItems] DROP COLUMN [PricingTypeId];
                """);
        }
    }
}
