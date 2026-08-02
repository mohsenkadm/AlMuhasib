using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

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
        try
        {
            if (!await db.Database.CanConnectAsync(cancellationToken))
                return ["EnsureCreated"];

            var hasSettings = await db.GoldSettings.AnyAsync(cancellationToken);
            return hasSettings ? [] : ["EnsureCreated"];
        }
        catch
        {
            return ["EnsureCreated"];
        }
    }

    public async Task<IReadOnlyList<string>> ApplyPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.EnsureCreatedAsync(cancellationToken);
        await SeedDefaultsAsync(db, cancellationToken);
        return ["EnsureCreated"];
    }

    private static async Task SeedDefaultsAsync(GoldDbContext db, CancellationToken ct)
    {
        if (!await db.GoldKarats.AnyAsync(ct))
        {
            db.GoldKarats.AddRange(
                new GoldKarat { KaratValue = 24, Name = "عيار 24", PurityFactor = 1.0m, DisplayOrder = 1, IsActive = true },
                new GoldKarat { KaratValue = 22, Name = "عيار 22", PurityFactor = 22m / 24m, DisplayOrder = 2, IsActive = true },
                new GoldKarat { KaratValue = 21, Name = "عيار 21", PurityFactor = 21m / 24m, DisplayOrder = 3, IsActive = true },
                new GoldKarat { KaratValue = 18, Name = "عيار 18", PurityFactor = 18m / 24m, DisplayOrder = 4, IsActive = true });
        }

        if (!await db.GoldSettings.AnyAsync(ct))
        {
            db.GoldSettings.Add(new GoldSettings
            {
                MithqalGrams = 5,
                ScaleBaudRate = 9600,
                ScaleStabilityThresholdGrams = 0.01m,
                AllowManualWeightEdit = true,
                LowStockAlertGrams = 10,
                OverdueDaysThreshold = 30,
                EnabledKaratsCsv = "24,22,21,18"
            });
        }

        if (!await db.GoldCashBoxes.AnyAsync(ct))
        {
            db.GoldCashBoxes.AddRange(
                new GoldCashBox
                {
                    Name = "صندوق الدينار",
                    Currency = GoldCurrency.IQD,
                    Balance = 0,
                    IsDefault = true,
                    IsActive = true
                },
                new GoldCashBox
                {
                    Name = "صندوق الدولار",
                    Currency = GoldCurrency.USD,
                    Balance = 0,
                    IsDefault = true,
                    IsActive = true
                });
        }

        await db.SaveChangesAsync(ct);
    }
}
