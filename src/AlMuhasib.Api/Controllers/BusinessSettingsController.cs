using AlMuhasib.Cloud.Application.Abstractions;
using AlMuhasib.Cloud.Application.Models;
using AlMuhasib.Cloud.Application.Models.Mobile;
using AlMuhasib.Cloud.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMuhasib.Api.Controllers;

[ApiController]
[Route("api/business-settings")]
[Authorize(Policy = "Tenant")]
public sealed class BusinessSettingsController : ControllerBase
{
    private readonly ICloudMasterDataService _masterData;
    private readonly ICloudMobileWriteService _mobileWrite;
    private readonly ITenantContext _tenantContext;

    public BusinessSettingsController(
        ICloudMasterDataService masterData,
        ICloudMobileWriteService mobileWrite,
        ITenantContext tenantContext)
    {
        _masterData = masterData;
        _mobileWrite = mobileWrite;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<BusinessSettingsDto>> Get(CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await _masterData.GetBusinessSettingsAsync(ct));
    }

    [HttpPut]
    public async Task<ActionResult<BusinessSettingsDto>> Update(
        [FromBody] UpdateBusinessSettingsRequest request, CancellationToken ct)
    {
        var tenantId = EnsureTenant();
        try
        {
            return Ok(await _mobileWrite.UpdateBusinessSettingsAsync(tenantId, request, Username, ct));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private int EnsureTenant()
    {
        var tenantId = int.Parse(User.FindFirst("tenant_id")!.Value);
        _tenantContext.SetTenant(tenantId);
        return tenantId;
    }

    private string Username => User.Identity?.Name ?? "api";
}
