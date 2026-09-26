using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data.Car;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class CarDatabaseMigrationService : IDatabaseMigrationService
{
    private readonly IDbContextFactory<CarDbContext> _contextFactory;

    public CarDatabaseMigrationService(IDbContextFactory<CarDbContext> contextFactory)
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

        try
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("PendingModelChangesWarning", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("pending changes", StringComparison.OrdinalIgnoreCase))
        {
            // النموذج قد يختلف عن الـ snapshot في بيئات قديمة — نكمل بالإصلاح اليدوي.
            System.Diagnostics.Debug.WriteLine($"[CarMigrate] PendingModelChanges ignored: {ex.Message}");
        }

        await CarSchemaRepair.ApplyAsync(db, cancellationToken);
        if (!await CarSchemaRepair.IsSchemaReadyAsync(db, cancellationToken))
            throw new InvalidOperationException(CarSchemaRepair.StandaloneSchemaOutdatedMessage);

        return pending;
    }
}
