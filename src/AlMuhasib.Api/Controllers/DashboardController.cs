using AlMuhasib.Cloud.Application.Abstractions;
using AlMuhasib.Cloud.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMuhasib.Api.Controllers;

[Route("api/dashboard")]
[Authorize(Policy = "Tenant")]
public sealed class DashboardController : TenantApiControllerBase
{
    private readonly ICloudDashboardService _dashboard;

    public DashboardController(
        ITenantContext tenantContext,
        ICloudMasterDataService masterData,
        ICloudDashboardService dashboard)
        : base(tenantContext, masterData)
    {
        _dashboard = dashboard;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        EnsureTenant();
        return Ok(await _dashboard.GetDashboardAsync(ct));
    }
}
