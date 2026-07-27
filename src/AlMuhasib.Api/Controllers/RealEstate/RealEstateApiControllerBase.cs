using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.RealEstate;

public abstract class RealEstateApiControllerBase : ControllerBase
{
    protected CloudDbContext Db { get; }
    protected ITenantContext TenantContext { get; }

    protected RealEstateApiControllerBase(CloudDbContext db, ITenantContext tenantContext)
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

    protected async Task<ActionResult?> EnsureRealEstateTenantAsync(CancellationToken ct)
    {
        EnsureTenant();
        var tenant = await Db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == TenantId, ct);
        if (tenant is null)
            return NotFound();
        if (tenant.ApplicationSystemType != (int)ApplicationSystemType.RealEstateContracts)
            return BadRequest("Tenant is not configured for real estate contracts.");
        return null;
    }
}
