using AlMuhasib.Sync.Dtos;
using AlMuhasib.Sync.Requests;

namespace AlMuhasib.Sync.Responses;

public sealed class SyncConflict
{
    public string EntityType { get; set; } = string.Empty;
    public Guid SyncId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public SyncDtoBase? ServerVersion { get; set; }
}

public sealed class SyncPushResponse
{
    public int AcceptedCount { get; set; }
    public int RejectedCount { get; set; }
    public List<SyncConflict> Conflicts { get; set; } = [];
    public DateTime ServerTime { get; set; }
}

public sealed class SyncPullResponse
{
    public SyncDataBundle Data { get; set; } = new();
    public string Cursor { get; set; } = string.Empty;
    public DateTime ServerTime { get; set; }
    public bool HasMore { get; set; }
}

public sealed class SyncStatusResponse
{
    public DateTime? LastSyncAt { get; set; }
    public int PendingPushCount { get; set; }
    public bool IsLicensed { get; set; }
    public string? LicenseMessage { get; set; }
}
