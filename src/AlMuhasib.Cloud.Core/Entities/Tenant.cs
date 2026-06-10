namespace AlMuhasib.Cloud.Core.Entities;

public class Tenant
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public bool IsMobileEnabled { get; set; }
    public DateTime? LicenseExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncAt { get; set; }

    public ICollection<TenantAccount> Accounts { get; set; } = [];
    public ICollection<DeviceSubscription> Devices { get; set; } = [];
}
