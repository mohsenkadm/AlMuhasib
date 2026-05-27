namespace AlMuhasib.Core.Interfaces.Services;

public interface IDatabaseMigrationService
{
  Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default);

  Task<IReadOnlyList<string>> ApplyPendingMigrationsAsync(CancellationToken cancellationToken = default);
}
