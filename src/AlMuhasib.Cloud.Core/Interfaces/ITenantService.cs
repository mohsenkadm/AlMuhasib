using AlMuhasib.Cloud.Core.Entities;

namespace AlMuhasib.Cloud.Core.Interfaces;

public interface ITenantService
{
    Task<List<Tenant>> GetAllAsync(CancellationToken ct = default);
    Task<Tenant?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Tenant> CreateAsync(string companyName, bool isMobileEnabled, DateTime? licenseExpiresAt, int applicationSystemType = 0, CancellationToken ct = default);
    Task UpdateAsync(Tenant tenant, CancellationToken ct = default);
    Task DeleteAsync(int tenantId, CancellationToken ct = default);
    Task SetActiveAsync(int tenantId, bool isActive, CancellationToken ct = default);
    Task<TenantAccount> CreateAccountAsync(int tenantId, string username, string password, DateTime? expiresAt, CancellationToken ct = default);
    Task<List<TenantAccount>> GetAccountsAsync(int tenantId, CancellationToken ct = default);
    Task DeleteAccountAsync(int accountId, CancellationToken ct = default);
    Task ResetPasswordAsync(int accountId, string newPassword, CancellationToken ct = default);
    Task SetAccountActiveAsync(int accountId, bool isActive, CancellationToken ct = default);
    Task ExtendAccountAsync(int accountId, DateTime? expiresAt, CancellationToken ct = default);
    Task ExtendLicenseAsync(int tenantId, DateTime? licenseExpiresAt, CancellationToken ct = default);
    Task SetMobileEnabledAsync(int tenantId, bool enabled, CancellationToken ct = default);
}
