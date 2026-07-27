using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data.RealEstate;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class RealEstateDatabaseMigrationService : IDatabaseMigrationService
{
    private readonly IDbContextFactory<RealEstateDbContext> _contextFactory;

    public RealEstateDatabaseMigrationService(IDbContextFactory<RealEstateDbContext> contextFactory)
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
            await db.Database.MigrateAsync(cancellationToken);
        return pending;
    }
}
