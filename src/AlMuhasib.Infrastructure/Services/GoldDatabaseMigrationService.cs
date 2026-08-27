using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data.Gold;
using AlMuhasib.Infrastructure.Services.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

/// <summary>
/// Gold Shop DB bootstrap. Uses <see cref="DatabaseFacade.EnsureCreatedAsync"/> because
/// there is no EF migrations history for GoldDbContext yet.
/// <para>
/// Important: EnsureCreated only creates a missing database/schema. It does NOT alter
/// existing tables when the model changes. Phase-2 upgrades therefore also run raw SQL
/// ALTER/CREATE statements when columns or tables are missing.
/// </para>
/// </summary>
public sealed class GoldDatabaseMigrationService : IDatabaseMigrationService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;

    public GoldDatabaseMigrationService(IDbContextFactory<GoldDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Database.CanConnectAsync(cancellationToken))
            return ["EnsureCreated"];

        try
        {
            // Probe Phase-2 readiness — throws if tables/columns are missing.
            _ = await db.GoldWarehouses.AsNoTracking().AnyAsync(cancellationToken);
            _ = await db.GoldSuppliers.AsNoTracking().AnyAsync(cancellationToken);
            var orphanStock = await db.GoldStockBalances.IgnoreQueryFilters()
                .AnyAsync(s => s.WarehouseId == 0, cancellationToken);
            if (orphanStock)
                return ["Phase2StockWarehouseBackfill"];

            var hasDefault = await db.GoldWarehouses.AnyAsync(w => w.IsDefault, cancellationToken);
            if (!hasDefault)
                return ["SeedPhase2Defaults"];

            _ = await db.GoldNotifications.AsNoTracking().AnyAsync(cancellationToken);

            var hasKarats = await db.GoldKarats.AnyAsync(cancellationToken);
            if (!hasKarats)
                return ["SeedPhase2Defaults"];

            var hasActiveKarats = await db.GoldKarats.AnyAsync(k => k.IsActive, cancellationToken);
            if (!hasActiveKarats)
                return ["SeedPhase2Defaults"];

            // EF materializes all mapped columns — missing schema upgrades surface here.
            _ = await db.GoldSettings.AsNoTracking()
                .Select(s => new { s.DefaultMakingChargeMode, s.IsConfigured })
                .FirstOrDefaultAsync(cancellationToken);

            return [];
        }
        catch
        {
            // Model/table mismatch — need EnsureCreated and/or raw SQL upgrades.
            return ["Phase2SchemaUpgrade"];
        }
    }

    public async Task<IReadOnlyList<string>> ApplyPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var applied = new List<string>();

        // EnsureCreated only works for brand-new databases (creates all tables from the model).
        // It will NOT upgrade an existing Phase-1 schema.
        var created = await db.Database.EnsureCreatedAsync(cancellationToken);
        if (created)
            applied.Add("EnsureCreated");

        await ApplyPhase2SchemaUpgradesAsync(db, cancellationToken);
        applied.Add("Phase2SchemaUpgrade");

        await ApplyMustFeatureSchemaUpgradesAsync(db, cancellationToken);
        applied.Add("MustFeatureSchemaUpgrade");

        await SeedPhase2DefaultsAsync(db, cancellationToken);
        applied.Add("SeedPhase2Defaults");

        return applied;
    }

    /// <summary>
    /// Best-effort raw SQL upgrades for databases that already existed before Phase 2.
    /// Failures are swallowed so a fresh EnsureCreated DB is unaffected.
    /// </summary>
    internal static async Task ApplyPhase2SchemaUpgradesAsync(GoldDbContext db, CancellationToken cancellationToken)
    {
        // New tables
        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldWarehouses', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[GoldWarehouses](
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [SyncId] UNIQUEIDENTIFIER NOT NULL,
                    [CreatedAt] DATETIME2 NOT NULL,
                    [CreatedBy] NVARCHAR(100) NOT NULL,
                    [UpdatedAt] DATETIME2 NULL,
                    [UpdatedBy] NVARCHAR(100) NULL,
                    [IsDeleted] BIT NOT NULL,
                    [DeletedAt] DATETIME2 NULL,
                    [DeletedBy] NVARCHAR(100) NULL,
                    [RowVersion] ROWVERSION NOT NULL,
                    [Name] NVARCHAR(200) NOT NULL,
                    [IsDefault] BIT NOT NULL,
                    [IsActive] BIT NOT NULL,
                    [Notes] NVARCHAR(2000) NOT NULL
                );
            END
            """, cancellationToken);

        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldSuppliers', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[GoldSuppliers](
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [SyncId] UNIQUEIDENTIFIER NOT NULL,
                    [CreatedAt] DATETIME2 NOT NULL,
                    [CreatedBy] NVARCHAR(100) NOT NULL,
                    [UpdatedAt] DATETIME2 NULL,
                    [UpdatedBy] NVARCHAR(100) NULL,
                    [IsDeleted] BIT NOT NULL,
                    [DeletedAt] DATETIME2 NULL,
                    [DeletedBy] NVARCHAR(100) NULL,
                    [RowVersion] ROWVERSION NOT NULL,
                    [Name] NVARCHAR(200) NOT NULL,
                    [Phone] NVARCHAR(50) NOT NULL,
                    [Address] NVARCHAR(500) NOT NULL,
                    [Notes] NVARCHAR(2000) NOT NULL,
                    [CreditBalanceIqd] DECIMAL(18,2) NOT NULL,
                    [CreditBalanceUsd] DECIMAL(18,2) NOT NULL,
                    [IsActive] BIT NOT NULL
                );
            END
            """, cancellationToken);

        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldExpenseTypes', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[GoldExpenseTypes](
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [SyncId] UNIQUEIDENTIFIER NOT NULL,
                    [CreatedAt] DATETIME2 NOT NULL,
                    [CreatedBy] NVARCHAR(100) NOT NULL,
                    [UpdatedAt] DATETIME2 NULL,
                    [UpdatedBy] NVARCHAR(100) NULL,
                    [IsDeleted] BIT NOT NULL,
                    [DeletedAt] DATETIME2 NULL,
                    [DeletedBy] NVARCHAR(100) NULL,
                    [RowVersion] ROWVERSION NOT NULL,
                    [Name] NVARCHAR(200) NOT NULL,
                    [IsActive] BIT NOT NULL
                );
            END
            """, cancellationToken);

        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldCategories', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[GoldCategories](
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [SyncId] UNIQUEIDENTIFIER NOT NULL,
                    [CreatedAt] DATETIME2 NOT NULL,
                    [CreatedBy] NVARCHAR(100) NOT NULL,
                    [UpdatedAt] DATETIME2 NULL,
                    [UpdatedBy] NVARCHAR(100) NULL,
                    [IsDeleted] BIT NOT NULL,
                    [DeletedAt] DATETIME2 NULL,
                    [DeletedBy] NVARCHAR(100) NULL,
                    [RowVersion] ROWVERSION NOT NULL,
                    [Name] NVARCHAR(200) NOT NULL,
                    [IsActive] BIT NOT NULL
                );
            END
            """, cancellationToken);

        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldExpenses', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[GoldExpenses](
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [SyncId] UNIQUEIDENTIFIER NOT NULL,
                    [CreatedAt] DATETIME2 NOT NULL,
                    [CreatedBy] NVARCHAR(100) NOT NULL,
                    [UpdatedAt] DATETIME2 NULL,
                    [UpdatedBy] NVARCHAR(100) NULL,
                    [IsDeleted] BIT NOT NULL,
                    [DeletedAt] DATETIME2 NULL,
                    [DeletedBy] NVARCHAR(100) NULL,
                    [RowVersion] ROWVERSION NOT NULL,
                    [ExpenseDate] DATETIME2 NOT NULL,
                    [ExpenseTypeId] INT NOT NULL,
                    [Amount] DECIMAL(18,2) NOT NULL,
                    [Currency] NVARCHAR(10) NOT NULL,
                    [CashBoxId] INT NOT NULL,
                    [Notes] NVARCHAR(2000) NOT NULL,
                    [WarehouseId] INT NULL
                );
            END
            """, cancellationToken);

        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldNotifications', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[GoldNotifications](
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [SyncId] UNIQUEIDENTIFIER NOT NULL,
                    [CreatedAt] DATETIME2 NOT NULL,
                    [CreatedBy] NVARCHAR(100) NOT NULL,
                    [UpdatedAt] DATETIME2 NULL,
                    [UpdatedBy] NVARCHAR(100) NULL,
                    [IsDeleted] BIT NOT NULL,
                    [DeletedAt] DATETIME2 NULL,
                    [DeletedBy] NVARCHAR(100) NULL,
                    [RowVersion] ROWVERSION NOT NULL,
                    [Type] NVARCHAR(40) NOT NULL,
                    [Title] NVARCHAR(200) NOT NULL,
                    [Message] NVARCHAR(2000) NOT NULL,
                    [IsRead] BIT NOT NULL,
                    [ReadAt] DATETIME2 NULL,
                    [RelatedEntity] NVARCHAR(100) NULL,
                    [RelatedId] INT NULL
                );
                CREATE INDEX [IX_GoldNotifications_IsRead] ON [dbo].[GoldNotifications]([IsRead]);
                CREATE INDEX [IX_GoldNotifications_Type] ON [dbo].[GoldNotifications]([Type]);
            END
            """, cancellationToken);

        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldWarehouseTransfers', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[GoldWarehouseTransfers](
                    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [SyncId] UNIQUEIDENTIFIER NOT NULL,
                    [CreatedAt] DATETIME2 NOT NULL,
                    [CreatedBy] NVARCHAR(100) NOT NULL,
                    [UpdatedAt] DATETIME2 NULL,
                    [UpdatedBy] NVARCHAR(100) NULL,
                    [IsDeleted] BIT NOT NULL,
                    [DeletedAt] DATETIME2 NULL,
                    [DeletedBy] NVARCHAR(100) NULL,
                    [RowVersion] ROWVERSION NOT NULL,
                    [TransferDate] DATETIME2 NOT NULL,
                    [FromWarehouseId] INT NOT NULL,
                    [ToWarehouseId] INT NOT NULL,
                    [KaratValue] INT NOT NULL,
                    [WeightGrams] DECIMAL(18,3) NOT NULL,
                    [Notes] NVARCHAR(2000) NOT NULL
                );
            END
            """, cancellationToken);

        // Existing table column upgrades
        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldStockBalances', N'U') IS NOT NULL
               AND COL_LENGTH('GoldStockBalances','WarehouseId') IS NULL
            BEGIN
                ALTER TABLE [dbo].[GoldStockBalances] ADD [WarehouseId] INT NOT NULL CONSTRAINT DF_GoldStockBalances_WarehouseId DEFAULT(0);
            END
            """, cancellationToken);

        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldInvoices', N'U') IS NOT NULL
               AND COL_LENGTH('GoldInvoices','SupplierId') IS NULL
                ALTER TABLE [dbo].[GoldInvoices] ADD [SupplierId] INT NULL;
            """, cancellationToken);

        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldInvoices', N'U') IS NOT NULL
               AND COL_LENGTH('GoldInvoices','WarehouseId') IS NULL
                ALTER TABLE [dbo].[GoldInvoices] ADD [WarehouseId] INT NULL;
            """, cancellationToken);

        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldInvoices', N'U') IS NOT NULL
               AND COL_LENGTH('GoldInvoices','IsExchange') IS NULL
                ALTER TABLE [dbo].[GoldInvoices] ADD [IsExchange] BIT NOT NULL CONSTRAINT DF_GoldInvoices_IsExchange DEFAULT(0);
            """, cancellationToken);

        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldInvoices', N'U') IS NOT NULL
               AND COL_LENGTH('GoldInvoices','ExchangeCashDifference') IS NULL
                ALTER TABLE [dbo].[GoldInvoices] ADD [ExchangeCashDifference] DECIMAL(18,2) NOT NULL CONSTRAINT DF_GoldInvoices_ExchangeCashDifference DEFAULT(0);
            """, cancellationToken);

        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldInvoiceLines', N'U') IS NOT NULL
               AND COL_LENGTH('GoldInvoiceLines','LineDirection') IS NULL
                ALTER TABLE [dbo].[GoldInvoiceLines] ADD [LineDirection] NVARCHAR(10) NOT NULL CONSTRAINT DF_GoldInvoiceLines_LineDirection DEFAULT(N'Out');
            """, cancellationToken);

        // Replace Phase-1 unique index on KaratValue with WarehouseId+KaratValue when possible
        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldStockBalances', N'U') IS NOT NULL
            BEGIN
                DECLARE @idx NVARCHAR(256);
                SELECT TOP 1 @idx = i.name
                FROM sys.indexes i
                INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                WHERE i.object_id = OBJECT_ID(N'dbo.GoldStockBalances')
                  AND i.is_unique = 1
                  AND c.name = 'KaratValue'
                  AND NOT EXISTS (
                      SELECT 1 FROM sys.index_columns ic2
                      INNER JOIN sys.columns c2 ON ic2.object_id = c2.object_id AND ic2.column_id = c2.column_id
                      WHERE ic2.object_id = i.object_id AND ic2.index_id = i.index_id AND c2.name = 'WarehouseId');
                IF @idx IS NOT NULL
                    EXEC(N'ALTER TABLE [dbo].[GoldStockBalances] DROP CONSTRAINT [' + @idx + N']');
                IF @idx IS NOT NULL
                    EXEC(N'DROP INDEX [' + @idx + N'] ON [dbo].[GoldStockBalances]');
            END
            """, cancellationToken);

        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldStockBalances', N'U') IS NOT NULL
               AND NOT EXISTS (
                   SELECT 1 FROM sys.indexes
                   WHERE object_id = OBJECT_ID(N'dbo.GoldStockBalances')
                     AND name = N'IX_GoldStockBalances_WarehouseId_KaratValue')
            BEGIN
                CREATE UNIQUE INDEX [IX_GoldStockBalances_WarehouseId_KaratValue]
                ON [dbo].[GoldStockBalances]([WarehouseId], [KaratValue]);
            END
            """, cancellationToken);
    }

    /// <summary>
    /// Must-features: making charge modes, customer gold credit grams, related invoice on returns.
    /// </summary>
    internal static async Task ApplyMustFeatureSchemaUpgradesAsync(GoldDbContext db, CancellationToken cancellationToken)
    {
        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldCustomers', N'U') IS NOT NULL
               AND COL_LENGTH('GoldCustomers','GoldCreditGrams') IS NULL
                ALTER TABLE [dbo].[GoldCustomers] ADD [GoldCreditGrams] DECIMAL(18,3) NOT NULL CONSTRAINT DF_GoldCustomers_GoldCreditGrams DEFAULT(0);
            """, cancellationToken);

        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldSettings', N'U') IS NOT NULL
               AND COL_LENGTH('GoldSettings','DefaultMakingChargeMode') IS NULL
                ALTER TABLE [dbo].[GoldSettings] ADD [DefaultMakingChargeMode] NVARCHAR(20) NOT NULL CONSTRAINT DF_GoldSettings_DefaultMakingChargeMode DEFAULT(N'Fixed');
            """, cancellationToken);

        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldInvoiceLines', N'U') IS NOT NULL
               AND COL_LENGTH('GoldInvoiceLines','MakingChargeMode') IS NULL
                ALTER TABLE [dbo].[GoldInvoiceLines] ADD [MakingChargeMode] NVARCHAR(20) NOT NULL CONSTRAINT DF_GoldInvoiceLines_MakingChargeMode DEFAULT(N'Fixed');
            """, cancellationToken);

        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldInvoiceLines', N'U') IS NOT NULL
               AND COL_LENGTH('GoldInvoiceLines','MakingChargeRate') IS NULL
                ALTER TABLE [dbo].[GoldInvoiceLines] ADD [MakingChargeRate] DECIMAL(18,4) NOT NULL CONSTRAINT DF_GoldInvoiceLines_MakingChargeRate DEFAULT(0);
            """, cancellationToken);

        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldInvoices', N'U') IS NOT NULL
               AND COL_LENGTH('GoldInvoices','RelatedInvoiceId') IS NULL
                ALTER TABLE [dbo].[GoldInvoices] ADD [RelatedInvoiceId] INT NULL;
            """, cancellationToken);

        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldSettings', N'U') IS NOT NULL
               AND COL_LENGTH('GoldSettings','IsConfigured') IS NULL
                ALTER TABLE [dbo].[GoldSettings] ADD [IsConfigured] BIT NOT NULL CONSTRAINT DF_GoldSettings_IsConfigured DEFAULT(0);
            """, cancellationToken);

        // Existing shops that already have operational data should skip the new first-run wizard.
        await TryExecAsync(db, """
            IF OBJECT_ID(N'dbo.GoldSettings', N'U') IS NOT NULL
               AND COL_LENGTH('GoldSettings','IsConfigured') IS NOT NULL
            BEGIN
                UPDATE s SET s.IsConfigured = 1
                FROM [dbo].[GoldSettings] s
                WHERE s.IsConfigured = 0
                  AND (
                        EXISTS (SELECT 1 FROM [dbo].[GoldInvoices])
                     OR EXISTS (SELECT 1 FROM [dbo].[GoldMithqalPrices])
                     OR EXISTS (SELECT 1 FROM [dbo].[GoldVouchers])
                     OR EXISTS (SELECT 1 FROM [dbo].[GoldStockBalances] WHERE [GramsOnHand] > 0)
                  );
            END
            """, cancellationToken);
    }

    /// <summary>
    /// Idempotent raw-SQL upgrades for existing Gold Shop databases.
    /// Safe to call from settings bootstrap when startup migration was skipped.
    /// </summary>
    internal static async Task EnsureSchemaCurrentAsync(GoldDbContext db, CancellationToken cancellationToken)
    {
        await ApplyPhase2SchemaUpgradesAsync(db, cancellationToken);
        await ApplyMustFeatureSchemaUpgradesAsync(db, cancellationToken);
    }

    internal static async Task SeedPhase2DefaultsAsync(GoldDbContext db, CancellationToken cancellationToken)
    {
        var defaultWarehouse = await db.GoldWarehouses
            .FirstOrDefaultAsync(w => w.IsDefault && !w.IsDeleted, cancellationToken);

        if (defaultWarehouse is null)
        {
            defaultWarehouse = await db.GoldWarehouses
                .FirstOrDefaultAsync(w => !w.IsDeleted, cancellationToken);

            if (defaultWarehouse is null)
            {
                defaultWarehouse = new GoldWarehouse
                {
                    Name = "المخزن الرئيسي",
                    IsDefault = true,
                    IsActive = true,
                    Notes = "مستودع افتراضي"
                };
                await db.GoldWarehouses.AddAsync(defaultWarehouse, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                defaultWarehouse.IsDefault = true;
                defaultWarehouse.IsActive = true;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        // Migrate Phase-1 stock rows that still have WarehouseId = 0 / unset to the default warehouse.
        var orphanBalances = await db.GoldStockBalances
            .IgnoreQueryFilters()
            .Where(s => s.WarehouseId == 0)
            .ToListAsync(cancellationToken);

        if (orphanBalances.Count > 0)
        {
            foreach (var balance in orphanBalances)
                balance.WarehouseId = defaultWarehouse.Id;

            await db.SaveChangesAsync(cancellationToken);
        }

        await GoldSettingsService.EnsureDefaultKaratsInternalAsync(db, cancellationToken);
        await GoldSettingsService.EnsureDefaultCashBoxesInternalAsync(db, cancellationToken);
    }

    private static async Task TryExecAsync(GoldDbContext db, string sql, CancellationToken cancellationToken)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
        catch
        {
            // Best-effort upgrade path; EnsureCreated / later seed covers fresh DBs.
        }
    }
}
