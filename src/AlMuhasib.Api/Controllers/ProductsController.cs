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
    public async Task<ActionResult<List<CloudProduct>>> GetAll(CancellationToken ct)
    {
        var tenantId = int.Parse(User.FindFirst("tenant_id")!.Value);
        _tenantContext.SetTenant(tenantId);
        return Ok(await _db.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct));
    }

    [HttpGet("{syncId:guid}")]
    public async Task<ActionResult<CloudProduct>> GetBySyncId(Guid syncId, CancellationToken ct)
    {
        var tenantId = int.Parse(User.FindFirst("tenant_id")!.Value);
        var product = await _db.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.SyncId == syncId, ct);
        return product is null ? NotFound() : Ok(product);
    }
}
