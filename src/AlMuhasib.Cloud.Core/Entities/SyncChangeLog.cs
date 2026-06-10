namespace AlMuhasib.Cloud.Core.Entities;

public class SyncChangeLog
{
    public long Id { get; set; }
    public int TenantId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid SyncId { get; set; }
    public string Direction { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? Details { get; set; }
}
