using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data.Customs;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class CustomsDatabaseMigrationService : IDatabaseMigrationService
{
    private readonly IDbContextFactory<CustomsDbContext> _contextFactory;

    public CustomsDatabaseMigrationService(IDbContextFactory<CustomsDbContext> contextFactory)
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
        await db.Database.MigrateAsync(cancellationToken);
        await ModulePrintBrandingSchemaRepair.EnsureCompanyIdColumnsAsync(db, cancellationToken);
        return pending;
    }
}
