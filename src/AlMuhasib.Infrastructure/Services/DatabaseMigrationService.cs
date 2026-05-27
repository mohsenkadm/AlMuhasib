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

    if (pending.Count > 0)
      await db.Database.MigrateAsync(cancellationToken);

    return pending;
  }
}
