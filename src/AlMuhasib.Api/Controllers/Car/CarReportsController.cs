using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.Car;

[ApiController]
[Route("api/car/reports")]
[Authorize(Policy = "Tenant")]
public sealed class CarReportsController : CarApiControllerBase
{
    public CarReportsController(CloudDbContext db, ITenantContext tenantContext) : base(db, tenantContext) { }

    [HttpGet("contracts")]
    public async Task<ActionResult<List<CarContractListDto>>> GetContractsReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? status,
        CancellationToken ct = default)
    {
        if (await EnsureCarTenantAsync(ct) is { } err) return err;

        var query = Db.CarSaleContracts.AsNoTracking().Where(c => c.TenantId == TenantId);

        if (from.HasValue)
            query = query.Where(c => c.ContractDate >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(c => c.ContractDate <= to.Value.Date);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CarContractStatus>(status, true, out var statusEnum))
            query = query.Where(c => c.Status == statusEnum);

        var items = await query
            .OrderByDescending(c => c.ContractDate)
            .ThenByDescending(c => c.Id)
            .ToListAsync(ct);

        return Ok(items.Select(CarContractMapper.ToListItem).ToList());
    }
}
