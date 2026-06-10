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
    public async Task<ActionResult<List<CloudCustomer>>> GetAll(CancellationToken ct)
    {
        var tenantId = int.Parse(User.FindFirst("tenant_id")!.Value);
        return Ok(await _db.Customers.AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct));
    }
}
