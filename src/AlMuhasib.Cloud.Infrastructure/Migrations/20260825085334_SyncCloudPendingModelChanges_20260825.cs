using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations
{
    /// <summary>
    /// Syncs the model snapshot for CloudProductOffer (CloudProductOffers shipped with an empty
    /// Designer stub). Also ensures ProductOffers has TenantId + unique (TenantId, SyncId)
    /// without failing when the table already exists from CloudProductOffers.
    /// </summary>
    public partial class SyncCloudPendingModelChanges_20260825 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[ProductOffers]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [ProductOffers] (
                        [Id] int NOT NULL IDENTITY,
                        [Name] nvarchar(max) NOT NULL,
                        [IsActive] bit NOT NULL,
                        [TriggerProductId] int NOT NULL,
                        [TriggerQuantity] decimal(18,2) NOT NULL,
                        [GiftProductId] int NOT NULL,
                        [GiftQuantity] decimal(18,2) NOT NULL,
                        [Notes] nvarchar(max) NULL,
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
                        CONSTRAINT [PK_ProductOffers] PRIMARY KEY ([Id])
                    );
                END

                IF OBJECT_ID(N'[dbo].[ProductOffers]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'dbo.ProductOffers', N'TenantId') IS NULL
                BEGIN
                    ALTER TABLE [ProductOffers]
                        ADD [TenantId] int NOT NULL
                            CONSTRAINT [DF_ProductOffers_TenantId] DEFAULT (0);
                END

                IF OBJECT_ID(N'[dbo].[ProductOffers]', N'U') IS NOT NULL
                   AND NOT EXISTS (
                        SELECT 1 FROM sys.indexes
                        WHERE name = N'IX_ProductOffers_TenantId_SyncId'
                          AND object_id = OBJECT_ID(N'[dbo].[ProductOffers]'))
                BEGIN
                    CREATE UNIQUE INDEX [IX_ProductOffers_TenantId_SyncId]
                        ON [ProductOffers] ([TenantId], [SyncId]);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = N'IX_ProductOffers_TenantId_SyncId'
                      AND object_id = OBJECT_ID(N'[dbo].[ProductOffers]'))
                    DROP INDEX [IX_ProductOffers_TenantId_SyncId] ON [ProductOffers];

                IF COL_LENGTH(N'dbo.ProductOffers', N'TenantId') IS NOT NULL
                BEGIN
                    DECLARE @df sysname =
                        (SELECT dc.name
                         FROM sys.default_constraints dc
                         INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                         WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[ProductOffers]')
                           AND c.name = N'TenantId');
                    IF @df IS NOT NULL
                        EXEC(N'ALTER TABLE [ProductOffers] DROP CONSTRAINT [' + @df + N']');
                    ALTER TABLE [ProductOffers] DROP COLUMN [TenantId];
                END
                """);
        }
    }
}
