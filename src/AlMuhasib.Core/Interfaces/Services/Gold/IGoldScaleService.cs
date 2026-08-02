namespace AlMuhasib.Core.Interfaces.Services.Gold;

public interface IGoldScaleService
{
    bool IsConnected { get; }
    string? ConnectedPort { get; }

    Task<IReadOnlyList<string>> GetAvailablePortsAsync(CancellationToken cancellationToken = default);
    Task ConnectAsync(string? comPort = null, int? baudRate = null, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<decimal> ReadWeightGramsAsync(CancellationToken cancellationToken = default);
    Task<bool> WaitForStableWeightAsync(
        decimal? thresholdGrams = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}
