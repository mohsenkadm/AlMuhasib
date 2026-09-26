using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class DatabaseMigrationService : IDatabaseMigrationService
{
  private readonly IDbContextFactory<AppDbContext> _contextFactory;

  public DatabaseMigrationService(IDbContextFactory<AppDbContext> contextFactory)
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
      // Always invoke MigrateAsync — it is idempotent and applies any newly discovered migrations.
      await db.Database.MigrateAsync(cancellationToken);
    }
    catch (InvalidOperationException ex) when (
        ex.Message.Contains("PendingModelChangesWarning", StringComparison.OrdinalIgnoreCase) ||
        ex.Message.Contains("pending changes", StringComparison.OrdinalIgnoreCase))
    {
      System.Diagnostics.Debug.WriteLine($"[AccountingMigrate] PendingModelChanges ignored: {ex.Message}");
    }

    await AccountingSchemaRepair.ApplyAsync(db, cancellationToken);
    if (!await AccountingSchemaRepair.IsVoucherSchemaReadyAsync(db, cancellationToken))
    {
      throw new InvalidOperationException(AccountingSchemaRepair.StandaloneSchemaOutdatedMessage);
    }

    return pending;
  }
}
