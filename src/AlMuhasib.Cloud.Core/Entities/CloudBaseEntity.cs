namespace AlMuhasib.Cloud.Core.Entities;

public abstract class CloudBaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Guid SyncId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = default!;
}

public interface ITenantEntity
{
    int TenantId { get; set; }
}
