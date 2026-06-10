using AlMuhasib.Cloud.Core.Interfaces;

namespace AlMuhasib.Cloud.Infrastructure.Services;

public sealed class TenantContext : ITenantContext
{
    public int? TenantId { get; private set; }
    public int? TenantAccountId { get; private set; }

    public void SetTenant(int tenantId, int? accountId = null)
    {
        TenantId = tenantId;
        TenantAccountId = accountId;
    }
}
