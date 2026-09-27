using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Safe Multi-Branch migration:
    /// 1) Create Branches + Main
    /// 2) Add BranchId (nullable) to business tables
    /// 3) Backfill Main + link users
    /// 4) Make BranchId required (except AuditLogs)
    /// 5) FKs + branch-aware unique indexes
    /// </summary>
    public partial class AddMultiBranchAccounting : Migration
    {
        private static readonly string[] BranchScopedTables =
        [
            "WarehouseTransfers", "WarehouseTransferItems", "WarehouseStocks", "Warehouses",
            "Vouchers", "Transfers", "Suppliers",
            "SalesRepTargets", "SalesRepresentatives", "SalesRepCommissionRules",
            "SalesRepCommissionEntries", "SalesRepCollections",
            "ProfitDistributions", "ProfitDistributionDetails",
            "ProductUnits", "ProductSizeStocks", "ProductSizes", "ProductSerials",
            "Products", "ProductPrices", "ProductOffers", "ProductColors", "ProductBatches",
            "PrintBrandingSettings", "PricingTypes", "PackagingTypes",
            "LoyaltySettings", "LoyaltyPointTransactions",
            "Invoices", "InvoiceItems", "InvestorTransactions", "Investors",
            "Installments", "InstallmentPlans", "ExpenseTypes", "Expenses",
            "EntityCustomFieldSettings", "Employees", "Drivers",
            "Customers", "CustomerLoyaltyAccounts", "CustomerAttachments",
            "Categories", "CashBoxes", "CapitalEntries", "BusinessSettings", "BankAccounts",
            "ExchangeRates"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Branches table
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.Branches', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[Branches] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [Name] nvarchar(200) NOT NULL,
                        [Code] nvarchar(50) NOT NULL,
                        [IsActive] bit NOT NULL CONSTRAINT [DF_Branches_IsActive] DEFAULT (1),
                        [IsMain] bit NOT NULL CONSTRAINT [DF_Branches_IsMain] DEFAULT (0),
                        [SyncId] uniqueidentifier NOT NULL CONSTRAINT [DF_Branches_SyncId] DEFAULT (NEWID()),
                        [CreatedAt] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(100) NOT NULL CONSTRAINT [DF_Branches_CreatedBy] DEFAULT (N'System'),
                        [UpdatedAt] datetime2 NULL,
                        [UpdatedBy] nvarchar(100) NULL,
                        [IsDeleted] bit NOT NULL CONSTRAINT [DF_Branches_IsDeleted] DEFAULT (0),
                        [DeletedAt] datetime2 NULL,
                        [DeletedBy] nvarchar(100) NULL,
                        [RowVersion] rowversion NOT NULL,
                        CONSTRAINT [PK_Branches] PRIMARY KEY ([Id])
                    );
                    CREATE UNIQUE INDEX [IX_Branches_Code] ON [dbo].[Branches]([Code]) WHERE [IsDeleted] = 0;
                    CREATE INDEX [IX_Branches_IsActive] ON [dbo].[Branches]([IsActive]);
                    CREATE INDEX [IX_Branches_IsMain] ON [dbo].[Branches]([IsMain]);
                    CREATE INDEX [IX_Branches_IsDeleted] ON [dbo].[Branches]([IsDeleted]);
                    CREATE INDEX [IX_Branches_SyncId] ON [dbo].[Branches]([SyncId]);
                END
                """);

            // 2) Seed Main branch
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [dbo].[Branches] WHERE [Code] = N'MAIN' AND [IsDeleted] = 0)
                BEGIN
                    INSERT INTO [dbo].[Branches]
                        ([Name], [Code], [IsActive], [IsMain], [SyncId], [CreatedAt], [CreatedBy], [IsDeleted])
                    VALUES
                        (N'الفرع الرئيسي', N'MAIN', 1, 1, NEWID(), SYSUTCDATETIME(), N'System', 0);
                END
                ELSE
                BEGIN
                    UPDATE [dbo].[Branches] SET [IsMain] = 1, [IsActive] = 1
                    WHERE [Code] = N'MAIN' AND [IsDeleted] = 0;
                END
                """);

            // 3) UserBranches
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.UserBranches', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[UserBranches] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [UserId] int NOT NULL,
                        [BranchId] int NOT NULL,
                        [IsDefault] bit NOT NULL CONSTRAINT [DF_UserBranches_IsDefault] DEFAULT (0),
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_UserBranches] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_UserBranches_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_UserBranches_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]) ON DELETE NO ACTION
                    );
                    CREATE UNIQUE INDEX [IX_UserBranches_UserId_BranchId] ON [dbo].[UserBranches]([UserId], [BranchId]);
                    CREATE INDEX [IX_UserBranches_BranchId] ON [dbo].[UserBranches]([BranchId]);
                END
                """);

            // 4) Add nullable BranchId columns
            foreach (var table in BranchScopedTables)
            {
                migrationBuilder.Sql($"""
                    IF OBJECT_ID(N'dbo.{table}', N'U') IS NOT NULL
                       AND COL_LENGTH(N'dbo.{table}', N'BranchId') IS NULL
                        ALTER TABLE [dbo].[{table}] ADD [BranchId] int NULL;
                    """);
            }

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NOT NULL
                   AND COL_LENGTH(N'dbo.AuditLogs', N'BranchId') IS NULL
                    ALTER TABLE [dbo].[AuditLogs] ADD [BranchId] int NULL;
                IF COL_LENGTH(N'dbo.AuditLogs', N'IpAddress') IS NULL
                    ALTER TABLE [dbo].[AuditLogs] ADD [IpAddress] nvarchar(64) NULL;
                IF COL_LENGTH(N'dbo.AuditLogs', N'DeviceInfo') IS NULL
                    ALTER TABLE [dbo].[AuditLogs] ADD [DeviceInfo] nvarchar(200) NULL;
                """);

            // 5) Backfill all business rows → Main
            foreach (var table in BranchScopedTables)
            {
                migrationBuilder.Sql($"""
                    IF OBJECT_ID(N'dbo.{table}', N'U') IS NOT NULL
                       AND COL_LENGTH(N'dbo.{table}', N'BranchId') IS NOT NULL
                    BEGIN
                        UPDATE t SET t.[BranchId] = b.[Id]
                        FROM [dbo].[{table}] t
                        CROSS JOIN (SELECT TOP 1 [Id] FROM [dbo].[Branches] WHERE [IsMain] = 1 AND [IsDeleted] = 0) b
                        WHERE t.[BranchId] IS NULL OR t.[BranchId] = 0;
                    END
                    """);
            }

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.AuditLogs', N'BranchId') IS NOT NULL
                BEGIN
                    UPDATE t SET t.[BranchId] = b.[Id]
                    FROM [dbo].[AuditLogs] t
                    CROSS JOIN (SELECT TOP 1 [Id] FROM [dbo].[Branches] WHERE [IsMain] = 1 AND [IsDeleted] = 0) b
                    WHERE t.[BranchId] IS NULL OR t.[BranchId] = 0;
                END
                """);

            // 6) Link all users to Main
            migrationBuilder.Sql("""
                INSERT INTO [dbo].[UserBranches] ([UserId], [BranchId], [IsDefault], [CreatedAt])
                SELECT u.[Id], b.[Id], 1, SYSUTCDATETIME()
                FROM [dbo].[Users] u
                CROSS JOIN (SELECT TOP 1 [Id] FROM [dbo].[Branches] WHERE [IsMain] = 1 AND [IsDeleted] = 0) b
                WHERE u.[IsDeleted] = 0
                  AND NOT EXISTS (
                      SELECT 1 FROM [dbo].[UserBranches] ub
                      WHERE ub.[UserId] = u.[Id] AND ub.[BranchId] = b.[Id]);
                """);

            // 7) Make BranchId required on business tables
            foreach (var table in BranchScopedTables)
            {
                migrationBuilder.Sql($"""
                    IF OBJECT_ID(N'dbo.{table}', N'U') IS NOT NULL
                       AND COL_LENGTH(N'dbo.{table}', N'BranchId') IS NOT NULL
                    BEGIN
                        ALTER TABLE [dbo].[{table}] ALTER COLUMN [BranchId] int NOT NULL;
                    END
                    """);
            }

            // 8) FKs + indexes
            foreach (var table in BranchScopedTables)
            {
                migrationBuilder.Sql($"""
                    IF OBJECT_ID(N'dbo.{table}', N'U') IS NOT NULL
                       AND COL_LENGTH(N'dbo.{table}', N'BranchId') IS NOT NULL
                       AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_{table}_Branches_BranchId')
                    BEGIN
                        ALTER TABLE [dbo].[{table}] WITH CHECK
                        ADD CONSTRAINT [FK_{table}_Branches_BranchId]
                        FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]);
                    END
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{table}_BranchId' AND object_id = OBJECT_ID(N'dbo.{table}'))
                        CREATE INDEX [IX_{table}_BranchId] ON [dbo].[{table}]([BranchId]);
                    """);
            }

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.AuditLogs', N'BranchId') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AuditLogs_Branches_BranchId')
                BEGIN
                    ALTER TABLE [dbo].[AuditLogs] WITH CHECK
                    ADD CONSTRAINT [FK_AuditLogs_Branches_BranchId]
                    FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]);
                END
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_BranchId' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
                    CREATE INDEX [IX_AuditLogs_BranchId] ON [dbo].[AuditLogs]([BranchId]);
                """);

            // 9) Branch-aware document numbering indexes
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Invoices_InvoiceNumber' AND object_id = OBJECT_ID(N'dbo.Invoices'))
                    DROP INDEX [IX_Invoices_InvoiceNumber] ON [dbo].[Invoices];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Invoices_Date' AND object_id = OBJECT_ID(N'dbo.Invoices'))
                    DROP INDEX [IX_Invoices_Date] ON [dbo].[Invoices];
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Invoices_BranchId_InvoiceNumber' AND object_id = OBJECT_ID(N'dbo.Invoices'))
                    CREATE UNIQUE INDEX [IX_Invoices_BranchId_InvoiceNumber]
                    ON [dbo].[Invoices]([BranchId], [InvoiceNumber]) WHERE [IsDeleted] = 0;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Invoices_BranchId_Date' AND object_id = OBJECT_ID(N'dbo.Invoices'))
                    CREATE INDEX [IX_Invoices_BranchId_Date] ON [dbo].[Invoices]([BranchId], [Date]);

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Vouchers_VoucherNumber' AND object_id = OBJECT_ID(N'dbo.Vouchers'))
                    DROP INDEX [IX_Vouchers_VoucherNumber] ON [dbo].[Vouchers];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Vouchers_Date' AND object_id = OBJECT_ID(N'dbo.Vouchers'))
                    DROP INDEX [IX_Vouchers_Date] ON [dbo].[Vouchers];
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Vouchers_BranchId_VoucherNumber' AND object_id = OBJECT_ID(N'dbo.Vouchers'))
                    CREATE UNIQUE INDEX [IX_Vouchers_BranchId_VoucherNumber]
                    ON [dbo].[Vouchers]([BranchId], [VoucherNumber]) WHERE [IsDeleted] = 0;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Vouchers_BranchId_Date' AND object_id = OBJECT_ID(N'dbo.Vouchers'))
                    CREATE INDEX [IX_Vouchers_BranchId_Date] ON [dbo].[Vouchers]([BranchId], [Date]);
                """);

            // Safety: no business row without BranchId
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1 FROM [dbo].[Invoices] WHERE [BranchId] IS NULL OR [BranchId] = 0
                    UNION ALL SELECT 1 FROM [dbo].[Vouchers] WHERE [BranchId] IS NULL OR [BranchId] = 0
                    UNION ALL SELECT 1 FROM [dbo].[Products] WHERE [BranchId] IS NULL OR [BranchId] = 0
                    UNION ALL SELECT 1 FROM [dbo].[Customers] WHERE [BranchId] IS NULL OR [BranchId] = 0
                )
                    THROW 50001, N'Multi-Branch backfill failed: rows without BranchId remain.', 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Invoices_BranchId_InvoiceNumber' AND object_id = OBJECT_ID(N'dbo.Invoices'))
                    DROP INDEX [IX_Invoices_BranchId_InvoiceNumber] ON [dbo].[Invoices];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Invoices_BranchId_Date' AND object_id = OBJECT_ID(N'dbo.Invoices'))
                    DROP INDEX [IX_Invoices_BranchId_Date] ON [dbo].[Invoices];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Vouchers_BranchId_VoucherNumber' AND object_id = OBJECT_ID(N'dbo.Vouchers'))
                    DROP INDEX [IX_Vouchers_BranchId_VoucherNumber] ON [dbo].[Vouchers];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Vouchers_BranchId_Date' AND object_id = OBJECT_ID(N'dbo.Vouchers'))
                    DROP INDEX [IX_Vouchers_BranchId_Date] ON [dbo].[Vouchers];
                """);

            foreach (var table in BranchScopedTables)
            {
                migrationBuilder.Sql($"""
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_{table}_Branches_BranchId')
                        ALTER TABLE [dbo].[{table}] DROP CONSTRAINT [FK_{table}_Branches_BranchId];
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{table}_BranchId' AND object_id = OBJECT_ID(N'dbo.{table}'))
                        DROP INDEX [IX_{table}_BranchId] ON [dbo].[{table}];
                    IF COL_LENGTH(N'dbo.{table}', N'BranchId') IS NOT NULL
                        ALTER TABLE [dbo].[{table}] DROP COLUMN [BranchId];
                    """);
            }

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_AuditLogs_Branches_BranchId')
                    ALTER TABLE [dbo].[AuditLogs] DROP CONSTRAINT [FK_AuditLogs_Branches_BranchId];
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_BranchId' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
                    DROP INDEX [IX_AuditLogs_BranchId] ON [dbo].[AuditLogs];
                IF COL_LENGTH(N'dbo.AuditLogs', N'BranchId') IS NOT NULL
                    ALTER TABLE [dbo].[AuditLogs] DROP COLUMN [BranchId];
                IF COL_LENGTH(N'dbo.AuditLogs', N'IpAddress') IS NOT NULL
                    ALTER TABLE [dbo].[AuditLogs] DROP COLUMN [IpAddress];
                IF COL_LENGTH(N'dbo.AuditLogs', N'DeviceInfo') IS NOT NULL
                    ALTER TABLE [dbo].[AuditLogs] DROP COLUMN [DeviceInfo];

                IF OBJECT_ID(N'dbo.UserBranches', N'U') IS NOT NULL
                    DROP TABLE [dbo].[UserBranches];
                IF OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL
                    DROP TABLE [dbo].[Branches];
                """);
        }
    }
}
