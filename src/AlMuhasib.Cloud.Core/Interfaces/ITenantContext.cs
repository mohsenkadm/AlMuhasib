namespace AlMuhasib.Cloud.Core.Interfaces;

public interface ITenantContext
{
    int? TenantId { get; }
    int? TenantAccountId { get; }
    void SetTenant(int tenantId, int? accountId = null);
}
