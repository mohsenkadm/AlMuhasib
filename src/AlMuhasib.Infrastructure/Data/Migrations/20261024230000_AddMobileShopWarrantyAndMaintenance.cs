using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations;

/// <summary>ضمان فواتير المبيعات + سجلات الصيانة لقالب محلات الموبايلات.</summary>
public partial class AddMobileShopWarrantyAndMaintenance : Migration
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
                    [SyncId] uniqueidentifier NOT NULL CONSTRAINT [DF_MaintenanceRecords_SyncId] DEFAULT (NEWID()),
                    [CreatedAt] datetime2 NOT NULL,
                    [CreatedBy] nvarchar(max) NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    [UpdatedBy] nvarchar(max) NULL,
                    [IsDeleted] bit NOT NULL CONSTRAINT [DF_MaintenanceRecords_IsDeleted] DEFAULT (0),
                    [DeletedAt] datetime2 NULL,
                    [DeletedBy] nvarchar(max) NULL,
                    [RowVersion] rowversion NOT NULL,
                    [BranchId] int NOT NULL,
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
                    [Currency] int NOT NULL CONSTRAINT [DF_MaintenanceRecords_Currency] DEFAULT (0),
                    [FxRate] decimal(18,6) NOT NULL CONSTRAINT [DF_MaintenanceRecords_FxRate] DEFAULT (1),
                    [VoucherId] int NULL,
                    CONSTRAINT [PK_MaintenanceRecords] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_MaintenanceRecords_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_MaintenanceRecords_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customers]([Id]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_MaintenanceRecords_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE SET NULL,
                    CONSTRAINT [FK_MaintenanceRecords_CashBoxes_CashBoxId] FOREIGN KEY ([CashBoxId]) REFERENCES [dbo].[CashBoxes]([Id]) ON DELETE SET NULL,
                    CONSTRAINT [FK_MaintenanceRecords_Vouchers_VoucherId] FOREIGN KEY ([VoucherId]) REFERENCES [dbo].[Vouchers]([Id]) ON DELETE SET NULL
                );
                CREATE UNIQUE INDEX [IX_MaintenanceRecords_BranchId_RecordNumber]
                    ON [dbo].[MaintenanceRecords]([BranchId], [RecordNumber]);
                CREATE INDEX [IX_MaintenanceRecords_Date] ON [dbo].[MaintenanceRecords]([Date]);
                CREATE INDEX [IX_MaintenanceRecords_CustomerId] ON [dbo].[MaintenanceRecords]([CustomerId]);
                CREATE INDEX [IX_MaintenanceRecords_SerialNumber] ON [dbo].[MaintenanceRecords]([SerialNumber]);
                CREATE INDEX [IX_MaintenanceRecords_Status] ON [dbo].[MaintenanceRecords]([Status]);
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
