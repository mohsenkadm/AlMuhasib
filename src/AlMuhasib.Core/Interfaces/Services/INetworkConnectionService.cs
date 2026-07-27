using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Core.Interfaces.Services;

public interface INetworkConnectionService
{
    NetworkConnectionProfile? Current { get; }
    bool IsBranchConfigured { get; }
    string BuildConnectionString(ApplicationSystemType systemType);
    string BuildConnectionString(NetworkConnectionProfile profile);
    Task<NetworkConnectionTestResult> TestConnectionAsync(NetworkConnectionProfile profile, string? plainPassword = null, CancellationToken cancellationToken = default);
    Task<NetworkConnectionTestResult> TestCurrentConnectionAsync(CancellationToken cancellationToken = default);
    void SaveBranchProfile(NetworkConnectionProfile profile);
    void ClearBranchProfile();
    NetworkConnectionProfile CreateProfileForSystem(ApplicationSystemType systemType, string databaseName);
}
