namespace AlMuhasib.Core.Entities;

/// <summary>حالة المزامنة لكل نوع كيان</summary>
public class SyncState
{
    public string EntityType { get; set; } = string.Empty;
    public DateTime? LastPulledAt { get; set; }
    public DateTime? LastPushedAt { get; set; }
    public string? ServerCursor { get; set; }
}
