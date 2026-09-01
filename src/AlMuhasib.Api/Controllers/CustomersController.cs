using AlMuhasib.Cloud.Application.Models;
using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Policy = "Tenant")]
public sealed class CustomersController : ControllerBase
{
    private readonly CloudDbContext _db;

    public CustomersController(CloudDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PagedResult<CloudCustomer>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var tenantId = int.Parse(User.FindFirst("tenant_id")!.Value);
        var query = _db.Customers.AsNoTracking().Where(c => c.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(c =>
                EF.Functions.Like(c.Name, term) ||
                (c.Phone != null && EF.Functions.Like(c.Phone, term)) ||
                (c.FileNumber != null && EF.Functions.Like(c.FileNumber, term)));
        }

        query = query.OrderBy(c => c.Name);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return Ok(new PagedResult<CloudCustomer>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }
}
