using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlMuhasib.Cloud.Infrastructure.Migrations
{
    /// <summary>
    /// Safe cloud Multi-Branch migration. Creates per-tenant Main branch, backfills BranchId,
    /// assigns TenantAccounts, then enforces FK/indexes. Non-accounting modules land on Main.
    /// </summary>
    public partial class AddMultiBranchCloud : Migration
    {
        private static readonly string[] BranchScopedTables =
        [
            "WarehouseTransfers", "WarehouseTransferItems", "WarehouseStocks", "Warehouses",
            "Vouchers", "Transfers", "Suppliers",
            "RestaurantTables", "RestaurantStockMovements", "RestaurantRecipes", "RestaurantRecipeLines",
            "RestaurantOrders", "RestaurantOrderPayments", "RestaurantOrderLines",
            "RestaurantMenuItems", "RestaurantMenuCategories", "RestaurantIngredientStocks", "RestaurantIngredients",
            "RealEstateParties", "RealEstateExpenseTypes", "RealEstateExpenses", "RealEstateContracts",
            "RealEstateContractPayments", "RealEstateContractClauses", "RealEstateClauseTemplates",
            "ProfitDistributions", "ProfitDistributionDetails",
            "Products", "ProductPrices", "ProductOffers", "PrintBrandingSettings", "PricingTypes",
            "Invoices", "InvoiceItems", "InvestorTransactions", "Investors", "Installments", "InstallmentPlans",
            "HotelVouchers", "HotelSettings", "HotelRoomTypes", "HotelRooms", "HotelReservations",
            "HotelReservationPayments", "HotelReservationCharges", "HotelRatePlanSeasons", "HotelRatePlans",
            "HotelHousekeepingTasks", "HotelGuests", "HotelFloors", "HotelExpenseTypes", "HotelExpenses", "HotelCashBoxes",
            "GoldWarehouseTransfers", "GoldWarehouses", "GoldVouchers", "GoldSuppliers", "GoldStockBalances",
            "GoldSettings", "GoldPayments", "GoldNotifications", "GoldMithqalPrices", "GoldKarats", "GoldItems",
            "GoldInvoices", "GoldInvoiceLines", "GoldFxRates", "GoldExpenseTypes", "GoldExpenses",
            "GoldCustomers", "GoldCashBoxes",
            "ExpenseTypes", "Expenses", "Customers", "CustomerAttachments", "Categories", "CashBoxes",
            "CarTradeTransactions", "CarTradePayments", "CarSaleContracts", "CarContractPayments",
            "CapitalEntries", "BusinessSettings", "BankAccounts", "ExchangeRates"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.Branches', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[Branches] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [TenantId] int NOT NULL,
                        [SyncId] uniqueidentifier NOT NULL,
                        [Name] nvarchar(200) NOT NULL,
                        [Code] nvarchar(50) NOT NULL,
                        [IsActive] bit NOT NULL CONSTRAINT [DF_CloudBranches_IsActive] DEFAULT (1),
                        [IsMain] bit NOT NULL CONSTRAINT [DF_CloudBranches_IsMain] DEFAULT (0),
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NULL,
                        [IsDeleted] bit NOT NULL CONSTRAINT [DF_CloudBranches_IsDeleted] DEFAULT (0),
                        CONSTRAINT [PK_Branches] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_Branches_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [dbo].[Tenants]([Id]) ON DELETE CASCADE
                    );
                    CREATE UNIQUE INDEX [IX_Branches_TenantId_Code] ON [dbo].[Branches]([TenantId], [Code]) WHERE [IsDeleted] = 0;
                    CREATE UNIQUE INDEX [IX_Branches_TenantId_SyncId] ON [dbo].[Branches]([TenantId], [SyncId]);
                END
                """);

            migrationBuilder.Sql("""
                INSERT INTO [dbo].[Branches] ([TenantId], [SyncId], [Name], [Code], [IsActive], [IsMain], [CreatedAt], [IsDeleted])
                SELECT t.[Id], NEWID(), N'الفرع الرئيسي', N'MAIN', 1, 1, SYSUTCDATETIME(), 0
                FROM [dbo].[Tenants] t
                WHERE NOT EXISTS (
                    SELECT 1 FROM [dbo].[Branches] b
                    WHERE b.[TenantId] = t.[Id] AND b.[Code] = N'MAIN' AND b.[IsDeleted] = 0);
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.TenantAccountBranches', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[TenantAccountBranches] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [TenantId] int NOT NULL,
                        [TenantAccountId] int NOT NULL,
                        [BranchId] int NOT NULL,
                        [IsDefault] bit NOT NULL CONSTRAINT [DF_TenantAccountBranches_IsDefault] DEFAULT (0),
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_TenantAccountBranches] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_TenantAccountBranches_TenantAccounts_TenantAccountId]
                            FOREIGN KEY ([TenantAccountId]) REFERENCES [dbo].[TenantAccounts]([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_TenantAccountBranches_Branches_BranchId]
                            FOREIGN KEY ([BranchId]) REFERENCES [dbo].[Branches]([Id]) ON DELETE NO ACTION
                    );
                    CREATE UNIQUE INDEX [IX_TenantAccountBranches_TenantAccountId_BranchId]
                        ON [dbo].[TenantAccountBranches]([TenantAccountId], [BranchId]);
                    CREATE INDEX [IX_TenantAccountBranches_TenantId_BranchId]
                        ON [dbo].[TenantAccountBranches]([TenantId], [BranchId]);
                END
                """);

            foreach (var table in BranchScopedTables)
            {
                migrationBuilder.Sql($"""
                    IF OBJECT_ID(N'dbo.{table}', N'U') IS NOT NULL
                       AND COL_LENGTH(N'dbo.{table}', N'BranchId') IS NULL
                        ALTER TABLE [dbo].[{table}] ADD [BranchId] int NULL;
                    """);
            }

            foreach (var table in BranchScopedTables)
            {
                migrationBuilder.Sql($"""
                    IF OBJECT_ID(N'dbo.{table}', N'U') IS NOT NULL
                       AND COL_LENGTH(N'dbo.{table}', N'BranchId') IS NOT NULL
                    BEGIN
                        UPDATE t SET t.[BranchId] = b.[Id]
                        FROM [dbo].[{table}] t
                        INNER JOIN [dbo].[Branches] b
                            ON b.[TenantId] = t.[TenantId] AND b.[IsMain] = 1 AND b.[IsDeleted] = 0
                        WHERE t.[BranchId] IS NULL OR t.[BranchId] = 0;
                    END
                    """);
            }

            migrationBuilder.Sql("""
                INSERT INTO [dbo].[TenantAccountBranches] ([TenantId], [TenantAccountId], [BranchId], [IsDefault], [CreatedAt])
                SELECT a.[TenantId], a.[Id], b.[Id], 1, SYSUTCDATETIME()
                FROM [dbo].[TenantAccounts] a
                INNER JOIN [dbo].[Branches] b ON b.[TenantId] = a.[TenantId] AND b.[IsMain] = 1 AND b.[IsDeleted] = 0
                WHERE NOT EXISTS (
                    SELECT 1 FROM [dbo].[TenantAccountBranches] x
                    WHERE x.[TenantAccountId] = a.[Id] AND x.[BranchId] = b.[Id]);
                """);

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
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{table}_TenantId_BranchId' AND object_id = OBJECT_ID(N'dbo.{table}'))
                        CREATE INDEX [IX_{table}_TenantId_BranchId] ON [dbo].[{table}]([TenantId], [BranchId]);
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in BranchScopedTables)
            {
                migrationBuilder.Sql($"""
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_{table}_Branches_BranchId')
                        ALTER TABLE [dbo].[{table}] DROP CONSTRAINT [FK_{table}_Branches_BranchId];
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_{table}_TenantId_BranchId' AND object_id = OBJECT_ID(N'dbo.{table}'))
                        DROP INDEX [IX_{table}_TenantId_BranchId] ON [dbo].[{table}];
                    IF COL_LENGTH(N'dbo.{table}', N'BranchId') IS NOT NULL
                        ALTER TABLE [dbo].[{table}] DROP COLUMN [BranchId];
                    """);
            }

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.TenantAccountBranches', N'U') IS NOT NULL
                    DROP TABLE [dbo].[TenantAccountBranches];
                IF OBJECT_ID(N'dbo.Branches', N'U') IS NOT NULL
                    DROP TABLE [dbo].[Branches];
                """);
        }
    }
}
