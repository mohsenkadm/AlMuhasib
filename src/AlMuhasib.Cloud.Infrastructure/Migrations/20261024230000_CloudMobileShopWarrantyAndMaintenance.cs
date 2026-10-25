using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations;

/// <summary>ضمان فواتير المبيعات + سجلات الصيانة لقالب محلات الموبايلات (سحابة).</summary>
public partial class CloudMobileShopWarrantyAndMaintenance : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.InvoiceItems', N'WarrantyEndDate') IS NULL
                ALTER TABLE [dbo].[InvoiceItems] ADD [WarrantyEndDate] datetime2 NULL;
            """);

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.MaintenanceRecords', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[MaintenanceRecords] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [TenantId] int NOT NULL,
                    [BranchId] int NOT NULL,
                    [SyncId] uniqueidentifier NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [CreatedBy] nvarchar(max) NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    [UpdatedBy] nvarchar(max) NULL,
                    [IsDeleted] bit NOT NULL CONSTRAINT [DF_CloudMaintenanceRecords_IsDeleted] DEFAULT (0),
                    [DeletedAt] datetime2 NULL,
                    [DeletedBy] nvarchar(max) NULL,
                    [RowVersion] rowversion NOT NULL,
                    [RecordNumber] nvarchar(50) NOT NULL,
                    [Date] datetime2 NOT NULL,
                    [CustomerId] int NOT NULL,
                    [ProductId] int NULL,
                    [ProductName] nvarchar(300) NOT NULL,
                    [SerialNumber] nvarchar(100) NOT NULL,
                    [PreviousSerialNumber] nvarchar(100) NULL,
                    [CostAmount] decimal(18,2) NOT NULL,
                    [Notes] nvarchar(1000) NULL,
                    [Status] int NOT NULL,
                    [WarrantyEndDate] datetime2 NULL,
                    [CashBoxId] int NULL,
                    [Currency] int NOT NULL CONSTRAINT [DF_CloudMaintenanceRecords_Currency] DEFAULT (0),
                    [FxRate] decimal(18,6) NOT NULL CONSTRAINT [DF_CloudMaintenanceRecords_FxRate] DEFAULT (1),
                    [VoucherId] int NULL,
                    CONSTRAINT [PK_MaintenanceRecords] PRIMARY KEY ([Id])
                );
                CREATE INDEX [IX_MaintenanceRecords_TenantId_Date] ON [dbo].[MaintenanceRecords]([TenantId], [Date]);
                CREATE INDEX [IX_MaintenanceRecords_TenantId_CustomerId] ON [dbo].[MaintenanceRecords]([TenantId], [CustomerId]);
                CREATE INDEX [IX_MaintenanceRecords_TenantId_Status] ON [dbo].[MaintenanceRecords]([TenantId], [Status]);
                CREATE UNIQUE INDEX [IX_MaintenanceRecords_TenantId_BranchId_RecordNumber]
                    ON [dbo].[MaintenanceRecords]([TenantId], [BranchId], [RecordNumber]);
            END
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.MaintenanceRecords', N'U') IS NOT NULL
                DROP TABLE [dbo].[MaintenanceRecords];
            """);
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.InvoiceItems', N'WarrantyEndDate') IS NOT NULL
                ALTER TABLE [dbo].[InvoiceItems] DROP COLUMN [WarrantyEndDate];
            """);
    }
}
