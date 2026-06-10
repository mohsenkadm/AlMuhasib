using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Cloud.Infrastructure.Services;
using AlMuhasib.Sync;
using AlMuhasib.Sync.Requests;
using AlMuhasib.Sync.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly CloudDbContext _db;
    private readonly IAuthTokenService _tokenService;
    private readonly ILicenseValidator _licenseValidator;

    public AuthController(CloudDbContext db, IAuthTokenService tokenService, ILicenseValidator licenseValidator)
    {
        _db = db;
        _tokenService = tokenService;
        _licenseValidator = licenseValidator;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TenantLoginResponse>> Login([FromBody] TenantLoginRequest request, CancellationToken ct)
    {
        var account = await _db.TenantAccounts
            .Include(a => a.Tenant)
            .FirstOrDefaultAsync(a => a.Username == request.Username, ct);

        if (account is null || !BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
            return Unauthorized(new ApiErrorResponse { Code = SyncErrorCodes.InvalidCredentials, Message = "بيانات الدخول غير صحيحة" });

        var license = _licenseValidator.Validate(account.Tenant, account);
        if (!license.IsValid)
            return StatusCode(403, new ApiErrorResponse { Code = license.ErrorCode!, Message = license.Message! });

        var response = _tokenService.CreateTenantTokens(account, account.Tenant);
        account.RefreshToken = response.RefreshToken;
        account.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        await _db.SaveChangesAsync(ct);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<TenantLoginResponse>> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var account = await _db.TenantAccounts
            .Include(a => a.Tenant)
            .FirstOrDefaultAsync(a => a.RefreshToken == request.RefreshToken, ct);

        if (account is null || account.RefreshTokenExpiresAt < DateTime.UtcNow)
            return Unauthorized(new ApiErrorResponse { Code = SyncErrorCodes.InvalidCredentials, Message = "Refresh token غير صالح" });

        var license = _licenseValidator.Validate(account.Tenant, account);
        if (!license.IsValid)
            return StatusCode(403, new ApiErrorResponse { Code = license.ErrorCode!, Message = license.Message! });

        var response = _tokenService.CreateTenantTokens(account, account.Tenant);
        account.RefreshToken = response.RefreshToken;
        account.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        await _db.SaveChangesAsync(ct);
        return Ok(response);
    }

    [HttpGet("license-status")]
    [Authorize(Policy = "Tenant")]
    public async Task<ActionResult<LicenseStatusResponse>> LicenseStatus(CancellationToken ct)
    {
        var tenantId = int.Parse(User.FindFirst("tenant_id")!.Value);
        var accountId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var tenant = await _db.Tenants.AsNoTracking().FirstAsync(t => t.Id == tenantId, ct);
        var account = await _db.TenantAccounts.AsNoTracking().FirstAsync(a => a.Id == accountId, ct);
        var license = _licenseValidator.Validate(tenant, account);

        return Ok(new LicenseStatusResponse
        {
            IsActive = account.IsActive && tenant.IsActive,
            IsMobileEnabled = tenant.IsMobileEnabled,
            LicenseExpiresAt = tenant.LicenseExpiresAt,
            AccountExpiresAt = account.ExpiresAt,
            StatusCode = license.IsValid ? null : license.ErrorCode,
            Message = license.Message
        });
    }

    [HttpPost("developer/login")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> DeveloperLogin([FromBody] DeveloperLoginRequest request, CancellationToken ct)
    {
        var user = await _db.DeveloperUsers.FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new ApiErrorResponse { Code = SyncErrorCodes.InvalidCredentials, Message = "بيانات المطور غير صحيحة" });

        return Ok(new { accessToken = _tokenService.CreateDeveloperToken(user), username = user.Username });
    }
}
