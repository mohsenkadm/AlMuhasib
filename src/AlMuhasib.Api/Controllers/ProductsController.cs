using AlMuhasib.Cloud.Application.Models;
using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(Policy = "Tenant")]
public sealed class ProductsController : ControllerBase
{
    private readonly CloudDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ProductsController(CloudDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CloudProduct>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? categorySyncId,
        [FromQuery] string? barcode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        EnsureTenant();
        var tenantId = _tenantContext.TenantId!.Value;
        var query = _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.TenantId == tenantId);

        if (categorySyncId.HasValue)
            query = query.Where(p => p.Category.SyncId == categorySyncId.Value);
        if (!string.IsNullOrWhiteSpace(barcode))
            query = query.Where(p => p.Barcode == barcode.Trim());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.Like(p.Name, term) ||
                (p.Barcode != null && EF.Functions.Like(p.Barcode, term)));
        }

        query = query.OrderBy(p => p.Name);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return Ok(new PagedResult<CloudProduct>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{syncId:guid}")]
    public async Task<ActionResult<CloudProduct>> GetBySyncId(Guid syncId, CancellationToken ct)
    {
        EnsureTenant();
        var tenantId = _tenantContext.TenantId!.Value;
        var product = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.SyncId == syncId, ct);
        return product is null ? NotFound() : Ok(product);
    }

    private void EnsureTenant()
    {
        var tenantId = int.Parse(User.FindFirst("tenant_id")!.Value);
        _tenantContext.SetTenant(tenantId);
    }
}
