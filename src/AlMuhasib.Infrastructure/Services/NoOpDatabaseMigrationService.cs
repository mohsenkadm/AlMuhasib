using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Infrastructure.Services;

public sealed class NoOpDatabaseMigrationService : IDatabaseMigrationService
{
    public Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

    public Task<IReadOnlyList<string>> ApplyPendingMigrationsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
}
