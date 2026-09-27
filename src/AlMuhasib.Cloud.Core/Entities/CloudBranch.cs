namespace AlMuhasib.Cloud.Core.Entities;

/// <summary>فرع تنظيمي داخل مستأجر سحابي.</summary>
public class CloudBranch
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Guid SyncId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsMain { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    public const string MainBranchCode = "MAIN";

    public Tenant Tenant { get; set; } = null!;
    public ICollection<TenantAccountBranch> AccountBranches { get; set; } = [];
}

/// <summary>ربط حساب مستأجر ↔ فروع مسموحة.</summary>
public class TenantAccountBranch
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int TenantAccountId { get; set; }
    public int BranchId { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TenantAccount TenantAccount { get; set; } = null!;
    public CloudBranch Branch { get; set; } = null!;
}
