namespace AlMuhasib.Core.Interfaces.Services;

public sealed class SyncConnectionResult
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool IsLicensed { get; init; }
    public DateTime? LicenseExpiresAt { get; init; }
}

public sealed class SyncProgressUpdate
{
    public int CurrentStep { get; init; }
    public int TotalSteps { get; init; }
    public string Message { get; init; } = string.Empty;

    public int Percent =>
        TotalSteps <= 0
            ? 0
            : Math.Clamp((int)Math.Round(CurrentStep * 100.0 / TotalSteps), 0, 100);

    public int RemainingSteps => Math.Max(0, TotalSteps - CurrentStep);
}

public sealed class SyncConflictInfo
{
    public string EntityType { get; init; } = string.Empty;
    public string EntityTypeArabic { get; init; } = string.Empty;
    public Guid SyncId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string ReasonArabic { get; init; } = string.Empty;

    public string DisplayLine =>
        $"• {EntityTypeArabic} | {ReasonArabic} | SyncId={SyncId:D}";
}

public sealed class SyncRunResult
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;
    public int AcceptedCount { get; init; }
    public int ConflictCount { get; init; }
    public IReadOnlyList<SyncConflictInfo> Conflicts { get; init; } = [];
    public string DiagnosticsText { get; init; } = string.Empty;
}

public interface ISyncService
{
    Task<SyncConnectionResult> TestConnectionAsync(CancellationToken ct = default);
    Task<SyncRunResult> SyncNowAsync(IProgress<SyncProgressUpdate>? progress = null, CancellationToken ct = default);
    Task StartAutoSyncAsync(CancellationToken ct = default);
    void StopAutoSync();
}
