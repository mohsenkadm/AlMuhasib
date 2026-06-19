using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.Car;

[ApiController]
[Route("api/car")]
[Authorize(Policy = "Tenant")]
public sealed class CarMobileController : CarApiControllerBase
{
    public CarMobileController(ITenantContext tenantContext, CloudDbContext db) : base(db, tenantContext) { }

    [HttpGet("dashboard")]
    public async Task<ActionResult<CarDashboardDto>> GetDashboard(CancellationToken ct)
    {
        if (await EnsureCarTenantAsync(ct) is { } err) return err;

        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var contracts = await Db.CarSaleContracts.AsNoTracking()
            .Where(c => c.TenantId == TenantId && c.Status != CarContractStatus.Cancelled)
            .ToListAsync(ct);

        var recent = contracts
            .OrderByDescending(c => c.ContractDate)
            .ThenByDescending(c => c.Id)
            .Take(10)
            .Select(CarContractMapper.ToListItem)
            .ToList();

        return Ok(new CarDashboardDto
        {
            TodayContracts = contracts.Count(c => c.ContractDate.Date == today),
            MonthContracts = contracts.Count(c => c.ContractDate.Date >= monthStart),
            UnpaidContracts = contracts.Count(c => c.RemainingAmount > 0),
            TotalCarValue = contracts.Sum(c => c.CarPrice),
            TotalReceived = contracts.Sum(c => c.AmountReceived),
            TotalRemaining = contracts.Sum(c => c.RemainingAmount),
            RecentContracts = recent
        });
    }
}

public sealed class CarDashboardDto
{
    public int TodayContracts { get; set; }
    public int MonthContracts { get; set; }
    public int UnpaidContracts { get; set; }
    public decimal TotalCarValue { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal TotalRemaining { get; set; }
    public List<CarContractListDto> RecentContracts { get; set; } = [];
}
