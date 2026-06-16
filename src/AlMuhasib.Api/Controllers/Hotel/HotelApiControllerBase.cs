using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.Hotel;

public abstract class HotelApiControllerBase : ControllerBase
{
    protected CloudDbContext Db { get; }
    protected ITenantContext TenantContext { get; }

    protected HotelApiControllerBase(CloudDbContext db, ITenantContext tenantContext)
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

    protected async Task<ActionResult?> EnsureHotelTenantAsync(CancellationToken ct)
    {
        EnsureTenant();
        var tenant = await Db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == TenantId, ct);
        if (tenant is null)
            return NotFound();
        if (tenant.ApplicationSystemType != (int)ApplicationSystemType.HotelManagement)
            return BadRequest("Tenant is not configured for hotel management.");
        return null;
    }
}
