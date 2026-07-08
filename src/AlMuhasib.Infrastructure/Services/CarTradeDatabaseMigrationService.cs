using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data.CarTrade;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class CarTradeDatabaseMigrationService : IDatabaseMigrationService
{
    private readonly IDbContextFactory<CarTradeDbContext> _contextFactory;

    public CarTradeDatabaseMigrationService(IDbContextFactory<CarTradeDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
    }

    public async Task<IReadOnlyList<string>> ApplyPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count > 0)
        {
            await db.Database.MigrateAsync(cancellationToken);
            if (pending.Any(m => m.Contains("CarTradeSaleWorkflow", StringComparison.OrdinalIgnoreCase)))
                await CarTradeLegacyDataMigrator.MigrateAsync(db, cancellationToken);
        }
        return pending;
    }
}
