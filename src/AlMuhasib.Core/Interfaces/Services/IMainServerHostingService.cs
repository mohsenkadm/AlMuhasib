using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IMainServerHostingService
{
    MainServerSettings Current { get; }
    void SaveSettings(MainServerSettings settings);
    string GeneratePairingCode();
    Task<MainServerSetupResult> ConfigureSqlExpressAsync(ApplicationSystemType systemType, CancellationToken cancellationToken = default);
    Task StartDiscoveryResponderAsync(ApplicationSystemType systemType, string databaseName, CancellationToken cancellationToken = default);
    Task StopDiscoveryResponderAsync();
    bool IsDiscoveryRunning { get; }
}

public class MainServerSetupResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? UpdatedConnectionString { get; init; }

    public static MainServerSetupResult Ok(string message, string? connectionString = null) =>
        new() { Success = true, Message = message, UpdatedConnectionString = connectionString };

    public static MainServerSetupResult Fail(string message) =>
        new() { Success = false, Message = message };
}
