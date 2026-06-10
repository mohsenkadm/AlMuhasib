using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMuhasib.Api.Controllers;

[ApiController]
[Route("api/admin/tenants")]
[Authorize(Policy = "Developer")]
public sealed class AdminTenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public AdminTenantsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Tenant>>> GetAll(CancellationToken ct) =>
        Ok(await _tenantService.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Tenant>> GetById(int id, CancellationToken ct)
    {
        var tenant = await _tenantService.GetByIdAsync(id, ct);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    public sealed class CreateTenantRequest
    {
        public string CompanyName { get; set; } = string.Empty;
        public bool IsMobileEnabled { get; set; } = true;
        public DateTime? LicenseExpiresAt { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime? AccountExpiresAt { get; set; }
    }

    [HttpPost]
    public async Task<ActionResult<Tenant>> Create([FromBody] CreateTenantRequest request, CancellationToken ct)
    {
        var tenant = await _tenantService.CreateAsync(request.CompanyName, request.IsMobileEnabled, request.LicenseExpiresAt, ct);
        if (!string.IsNullOrWhiteSpace(request.Username) && !string.IsNullOrWhiteSpace(request.Password))
            await _tenantService.CreateAccountAsync(tenant.Id, request.Username, request.Password, request.AccountExpiresAt, ct);

        return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, tenant);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Tenant model, CancellationToken ct)
    {
        if (id != model.Id) return BadRequest();
        await _tenantService.UpdateAsync(model, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/accounts")]
    public async Task<ActionResult<TenantAccount>> CreateAccount(int id, [FromBody] CreateAccountRequest request, CancellationToken ct)
    {
        var account = await _tenantService.CreateAccountAsync(id, request.Username, request.Password, request.ExpiresAt, ct);
        return Ok(account);
    }

    [HttpGet("{id:int}/accounts")]
    public async Task<ActionResult<List<TenantAccount>>> GetAccounts(int id, CancellationToken ct) =>
        Ok(await _tenantService.GetAccountsAsync(id, ct));

    [HttpPost("accounts/{accountId:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int accountId, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _tenantService.ResetPasswordAsync(accountId, request.NewPassword, ct);
        return NoContent();
    }

    [HttpPost("accounts/{accountId:int}/active")]
    public async Task<IActionResult> SetAccountActive(int accountId, [FromBody] SetActiveRequest request, CancellationToken ct)
    {
        await _tenantService.SetAccountActiveAsync(accountId, request.IsActive, ct);
        return NoContent();
    }

    [HttpPost("accounts/{accountId:int}/extend")]
    public async Task<IActionResult> ExtendAccount(int accountId, [FromBody] ExtendRequest request, CancellationToken ct)
    {
        await _tenantService.ExtendAccountAsync(accountId, request.ExpiresAt, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/extend-license")]
    public async Task<IActionResult> ExtendLicense(int id, [FromBody] ExtendRequest request, CancellationToken ct)
    {
        await _tenantService.ExtendLicenseAsync(id, request.ExpiresAt, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/mobile-enabled")]
    public async Task<IActionResult> SetMobileEnabled(int id, [FromBody] SetMobileEnabledRequest request, CancellationToken ct)
    {
        await _tenantService.SetMobileEnabledAsync(id, request.IsMobileEnabled, ct);
        return NoContent();
    }

    public sealed class CreateAccountRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
    }

    public sealed class ResetPasswordRequest { public string NewPassword { get; set; } = string.Empty; }
    public sealed class SetActiveRequest { public bool IsActive { get; set; } }
    public sealed class ExtendRequest { public DateTime? ExpiresAt { get; set; } }
    public sealed class SetMobileEnabledRequest { public bool IsMobileEnabled { get; set; } }
}
