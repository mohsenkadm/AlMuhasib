using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Core.Interfaces.Services;

public interface ISqlServerInstanceDiscoveryService
{
    /// <summary>
    /// Discovers SQL Server and LocalDB instances installed on this machine.
    /// </summary>
    Task<IReadOnlyList<SqlServerInstanceInfo>> DiscoverLocalInstancesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests connectivity to a data source using Windows authentication.
    /// </summary>
    Task<NetworkConnectionTestResult> TestLocalConnectionAsync(string dataSource, CancellationToken cancellationToken = default);
}
