namespace AlMuhasib.Core.Interfaces.Services;

public sealed class SyncConnectionResult
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool IsLicensed { get; init; }
    public DateTime? LicenseExpiresAt { get; init; }
}

public sealed class SyncRunResult
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;
    public int AcceptedCount { get; init; }
    public int ConflictCount { get; init; }
}

public interface ISyncService
{
    Task<SyncConnectionResult> TestConnectionAsync(CancellationToken ct = default);
    Task<SyncRunResult> SyncNowAsync(CancellationToken ct = default);
    Task StartAutoSyncAsync(CancellationToken ct = default);
    void StopAutoSync();
}
