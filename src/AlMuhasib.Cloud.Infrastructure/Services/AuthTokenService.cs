using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Options;
using AlMuhasib.Sync.Responses;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AlMuhasib.Cloud.Infrastructure.Services;

public sealed class AuthTokenService : IAuthTokenService
{
    private readonly JwtOptions _options;

    public AuthTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public TenantLoginResponse CreateTenantTokens(TenantAccount account, Tenant tenant)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim("tenant_id", tenant.Id.ToString()),
            new Claim(ClaimTypes.Name, account.Username),
            new Claim(ClaimTypes.Role, "Tenant"),
            new Claim("system_type", tenant.ApplicationSystemType.ToString())
        };

        var token = CreateToken(claims, expiresAt);
        var refresh = GenerateRefreshToken();

        return new TenantLoginResponse
        {
            AccessToken = token,
            RefreshToken = refresh,
            AccessTokenExpiresAt = expiresAt,
            TenantId = tenant.Id,
            CompanyName = tenant.CompanyName,
            TenantName = tenant.CompanyName,
            ApplicationSystemType = tenant.ApplicationSystemType,
            IsMobileEnabled = tenant.IsMobileEnabled,
            LicenseExpiresAt = tenant.LicenseExpiresAt,
            AccountExpiresAt = account.ExpiresAt
        };
    }

    public string CreateDeveloperToken(DeveloperUser user)
    {
        var expiresAt = DateTime.UtcNow.AddHours(8);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, "Developer")
        };
        return CreateToken(claims, expiresAt);
    }

    public (int tenantId, int accountId)? ValidateTenantToken(string token)
    {
        var principal = ValidateToken(token);
        if (principal is null) return null;
        if (!principal.IsInRole("Tenant")) return null;

        var tenantClaim = principal.FindFirst("tenant_id")?.Value;
        var accountClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(tenantClaim, out var tenantId) || !int.TryParse(accountClaim, out var accountId))
            return null;

        return (tenantId, accountId);
    }

    public bool ValidateDeveloperToken(string token)
    {
        var principal = ValidateToken(token);
        return principal?.IsInRole("Developer") == true;
    }

    public string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private string CreateToken(Claim[] claims, DateTime expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private ClaimsPrincipal? ValidateToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            return handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _options.Issuer,
                ValidAudience = _options.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);
        }
        catch
        {
            return null;
        }
    }
}
