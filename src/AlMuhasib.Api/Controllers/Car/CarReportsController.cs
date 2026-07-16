using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Car;
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
    public async Task<ActionResult<CarContractsReportDto>> GetContractsReport(
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

        var rows = items.Select(CarContractMapper.ToListItem).ToList();

        return Ok(new CarContractsReportDto
        {
            Rows = rows,
            ContractCount = items.Count,
            TotalCarValue = items.Sum(c => c.CarPrice),
            TotalReceived = items.Sum(c => c.AmountReceived),
            TotalRemaining = items.Sum(c => c.RemainingAmount),
            MonthlyContracts = items
                .GroupBy(c => new DateTime(c.ContractDate.Year, c.ContractDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .ToList(),
            CollectedVsRemaining =
            [
                new NameAmountPoint { Name = "Collected", Amount = items.Sum(c => c.AmountReceived) },
                new NameAmountPoint { Name = "Remaining", Amount = items.Sum(c => c.RemainingAmount) }
            ],
            ByCarType = items
                .GroupBy(c => string.IsNullOrWhiteSpace(c.CarType) ? "Unspecified" : c.CarType)
                .OrderByDescending(g => g.Count())
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .ToList()
        });
    }
}

public sealed class CarContractsReportDto
{
    public List<CarContractListDto> Rows { get; set; } = [];
    public int ContractCount { get; set; }
    public decimal TotalCarValue { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal TotalRemaining { get; set; }
    public List<NameCountPoint> MonthlyContracts { get; set; } = [];
    public List<NameAmountPoint> CollectedVsRemaining { get; set; } = [];
    public List<NameCountPoint> ByCarType { get; set; } = [];
}
