using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Sync.Requests;
using AlMuhasib.Sync.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMuhasib.Api.Controllers;

[ApiController]
[Route("api/sync")]
[Authorize(Policy = "Tenant")]
public sealed class SyncController : ControllerBase
{
    private readonly ISyncEngine _syncEngine;
    private readonly ITenantContext _tenantContext;

    public SyncController(ISyncEngine syncEngine, ITenantContext tenantContext)
    {
        _syncEngine = syncEngine;
        _tenantContext = tenantContext;
    }

    [HttpPost("push")]
    public async Task<ActionResult<SyncPushResponse>> Push([FromBody] SyncPushRequest request, CancellationToken ct)
    {
        var tenantId = ResolveTenantId();
        _tenantContext.SetTenant(tenantId);
        return Ok(await _syncEngine.PushAsync(tenantId, request, ct));
    }

    [HttpPost("pull")]
    public async Task<ActionResult<SyncPullResponse>> Pull([FromBody] SyncPullRequest request, CancellationToken ct)
    {
        var tenantId = ResolveTenantId();
        _tenantContext.SetTenant(tenantId);
        return Ok(await _syncEngine.PullAsync(tenantId, request, ct));
    }

    [HttpGet("status")]
    public async Task<ActionResult<SyncStatusResponse>> Status(CancellationToken ct)
    {
        var tenantId = ResolveTenantId();
        return Ok(await _syncEngine.GetStatusAsync(tenantId, ct));
    }

    private int ResolveTenantId()
    {
        var tenantId = _tenantContext.TenantId
            ?? int.Parse(User.FindFirst("tenant_id")!.Value);
        if (tenantId <= 0)
            throw new InvalidOperationException("Invalid tenant_id claim.");
        return tenantId;
    }
}
