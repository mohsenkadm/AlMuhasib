using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.Car;

public abstract class CarApiControllerBase : ControllerBase
{
    protected CloudDbContext Db { get; }
    protected ITenantContext TenantContext { get; }

    protected CarApiControllerBase(CloudDbContext db, ITenantContext tenantContext)
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

    protected async Task<ActionResult?> EnsureCarTenantAsync(CancellationToken ct)
    {
        EnsureTenant();
        var tenant = await Db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == TenantId, ct);
        if (tenant is null)
            return NotFound();
        if (tenant.ApplicationSystemType != (int)ApplicationSystemType.CarContracts)
            return BadRequest("Tenant is not configured for car contracts.");

        // Car has no multi-branch UX; ensure Main is bound so CloudBaseEntity.BranchId stamps succeed.
        if (TenantContext.BranchId is not > 0)
        {
            var mainId = await ResolveOrCreateMainBranchIdAsync(ct);
            if (mainId <= 0)
                return BadRequest("تعذر تهيئة الفرع الرئيسي للمستأجر.");
            TenantContext.SetBranch(mainId, [mainId], canViewAllBranches: true);
        }

        return null;
    }

    private async Task<int> ResolveOrCreateMainBranchIdAsync(CancellationToken ct)
    {
        Db.BypassBranchFilter = true;
        try
        {
            var main = await Db.Branches
                .FirstOrDefaultAsync(b => b.TenantId == TenantId && b.IsMain && !b.IsDeleted, ct);
            if (main is not null)
                return main.Id;

            main = new CloudBranch
            {
                TenantId = TenantId,
                Name = "الفرع الرئيسي",
                Code = CloudBranch.MainBranchCode,
                IsActive = true,
                IsMain = true,
                SyncId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };
            Db.Branches.Add(main);
            await Db.SaveChangesAsync(ct);
            return main.Id;
        }
        finally
        {
            Db.BypassBranchFilter = false;
        }
    }
}
