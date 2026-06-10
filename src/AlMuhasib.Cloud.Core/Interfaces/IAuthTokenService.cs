using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Sync.Responses;

namespace AlMuhasib.Cloud.Core.Interfaces;

public interface IAuthTokenService
{
    TenantLoginResponse CreateTenantTokens(TenantAccount account, Tenant tenant);
    string CreateDeveloperToken(DeveloperUser user);
    (int tenantId, int accountId)? ValidateTenantToken(string token);
    bool ValidateDeveloperToken(string token);
}
