using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.CarTrade;

public abstract class CarTradeApiControllerBase : ControllerBase
{
    protected CloudDbContext Db { get; }
    protected ITenantContext TenantContext { get; }

    protected CarTradeApiControllerBase(CloudDbContext db, ITenantContext tenantContext)
    {
        Db = db;
        TenantContext = tenantContext;
    }

    protected void EnsureTenant()
    {
        var tenantId = int.Parse(User.FindFirst("tenant_id")!.Value);
        TenantContext.SetTenant(tenantId);
    }

    protected int TenantId => TenantContext.TenantId!.Value;

    protected async Task<ActionResult?> EnsureCarTradeTenantAsync(CancellationToken ct)
    {
        EnsureTenant();
        var tenant = await Db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == TenantId, ct);
        if (tenant is null)
            return NotFound();
        if (tenant.ApplicationSystemType != (int)ApplicationSystemType.CarTrading)
            return BadRequest("Tenant is not configured for car trading.");
        return null;
    }
}
