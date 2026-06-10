namespace AlMuhasib.Cloud.Core.Entities;

public class DeviceSubscription
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? Platform { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public Tenant Tenant { get; set; } = null!;
}
