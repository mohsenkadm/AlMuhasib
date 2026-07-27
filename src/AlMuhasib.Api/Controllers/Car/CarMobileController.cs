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
            TotalContracts = contracts.Count,
            UnpaidContracts = contracts.Count(c => c.RemainingAmount > 0),
            TotalCarValue = contracts.Sum(c => c.CarPrice),
            TotalReceived = contracts.Sum(c => c.AmountReceived),
            TotalRemaining = contracts.Sum(c => c.RemainingAmount),
            MonthlyContracts = contracts
                .GroupBy(c => new DateTime(c.ContractDate.Year, c.ContractDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .TakeLast(12)
                .ToList(),
            PaymentStatusChart =
            [
                new NameAmountPoint { Name = "Fully Paid", Amount = contracts.Count(c => c.RemainingAmount <= 0) },
                new NameAmountPoint { Name = "Partially Paid", Amount = contracts.Count(c => c.RemainingAmount > 0 && c.AmountReceived > 0) },
                new NameAmountPoint { Name = "Unpaid", Amount = contracts.Count(c => c.AmountReceived <= 0 && c.RemainingAmount > 0) }
            ],
            TopCarTypes = contracts
                .GroupBy(c => string.IsNullOrWhiteSpace(c.CarType) ? "Unspecified" : c.CarType)
                .OrderByDescending(g => g.Count())
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .Take(10)
                .ToList(),
            TopBuyers = contracts
                .GroupBy(c => string.IsNullOrWhiteSpace(c.BuyerName) ? "Unspecified" : c.BuyerName)
                .OrderByDescending(g => g.Count())
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .Take(8)
                .ToList(),
            RecentContracts = recent
        });
    }
}

public sealed class CarDashboardDto
{
    public int TodayContracts { get; set; }
    public int MonthContracts { get; set; }
    public int TotalContracts { get; set; }
    public int UnpaidContracts { get; set; }
    public decimal TotalCarValue { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal TotalRemaining { get; set; }
    public List<NameCountPoint> MonthlyContracts { get; set; } = [];
    public List<NameAmountPoint> PaymentStatusChart { get; set; } = [];
    public List<NameCountPoint> TopCarTypes { get; set; } = [];
    public List<NameCountPoint> TopBuyers { get; set; } = [];
    public List<CarContractListDto> RecentContracts { get; set; } = [];
}
