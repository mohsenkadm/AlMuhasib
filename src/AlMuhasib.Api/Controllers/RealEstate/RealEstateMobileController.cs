using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.RealEstate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.RealEstate;

[ApiController]
[Route("api/real-estate")]
[Authorize(Policy = "Tenant")]
public sealed class RealEstateMobileController : RealEstateApiControllerBase
{
    public RealEstateMobileController(ITenantContext tenantContext, CloudDbContext db) : base(db, tenantContext) { }

    [HttpGet("dashboard")]
    public async Task<ActionResult<RealEstateDashboardDto>> GetDashboard(CancellationToken ct)
    {
        if (await EnsureRealEstateTenantAsync(ct) is { } err) return err;

        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var contracts = await Db.RealEstateContracts.AsNoTracking()
            .Where(c => c.TenantId == TenantId && c.Status != RealEstateContractStatus.Cancelled)
            .ToListAsync(ct);

        var recent = contracts
            .OrderByDescending(c => c.ContractDate)
            .ThenByDescending(c => c.Id)
            .Take(10)
            .Select(RealEstateContractMapper.ToListItem)
            .ToList();

        return Ok(new RealEstateDashboardDto
        {
            TodayContracts = contracts.Count(c => c.ContractDate.Date == today),
            MonthContracts = contracts.Count(c => c.ContractDate.Date >= monthStart),
            TotalContracts = contracts.Count,
            UnpaidContracts = contracts.Count(c => c.RemainingAmount > 0),
            OverdueDebts = contracts.Count(c =>
                c.PaymentMode == RealEstatePaymentMode.Credit &&
                c.RemainingAmount > 0 &&
                c.DueDate.HasValue &&
                c.DueDate.Value.Date < today),
            TotalValue = contracts.Sum(c => c.TotalPrice),
            TotalReceived = contracts.Sum(c => c.AmountPaid),
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
                new NameAmountPoint { Name = "Partially Paid", Amount = contracts.Count(c => c.RemainingAmount > 0 && c.AmountPaid > 0) },
                new NameAmountPoint { Name = "Unpaid", Amount = contracts.Count(c => c.AmountPaid <= 0 && c.RemainingAmount > 0) }
            ],
            ByContractType =
            [
                new NameCountPoint { Name = "Sale", Count = contracts.Count(c => c.ContractType == RealEstateContractType.Sale) },
                new NameCountPoint { Name = "Purchase", Count = contracts.Count(c => c.ContractType == RealEstateContractType.Purchase) }
            ],
            ByPropertyType = contracts
                .GroupBy(c => c.PropertyType.ToString())
                .OrderByDescending(g => g.Count())
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .ToList(),
            RecentContracts = recent
        });
    }

    [HttpGet("debts")]
    public async Task<ActionResult<List<RealEstateDebtDto>>> GetDebts(
        [FromQuery] bool overdueOnly = false,
        CancellationToken ct = default)
    {
        if (await EnsureRealEstateTenantAsync(ct) is { } err) return err;

        var today = DateTime.Today;
        var contracts = await Db.RealEstateContracts.AsNoTracking()
            .Where(c =>
                c.TenantId == TenantId &&
                c.Status != RealEstateContractStatus.Cancelled &&
                c.PaymentMode == RealEstatePaymentMode.Credit &&
                c.RemainingAmount > 0 &&
                c.DebtorParty != RealEstateDebtorParty.None)
            .OrderBy(c => c.DueDate)
            .ToListAsync(ct);

        var items = contracts.Select(c =>
        {
            var isBuyer = c.DebtorParty == RealEstateDebtorParty.Buyer;
            var due = c.DueDate?.Date;
            var overdue = due.HasValue && due.Value < today;
            return new RealEstateDebtDto
            {
                SyncId = c.SyncId,
                ContractNumber = c.ContractNumber,
                ContractDate = c.ContractDate,
                DebtorName = isBuyer ? c.BuyerName : c.SellerName,
                DebtorPhone = isBuyer ? c.BuyerPhone : c.SellerPhone,
                DebtorParty = c.DebtorParty.ToString(),
                CounterpartyName = isBuyer ? c.SellerName : c.BuyerName,
                RemainingAmount = c.RemainingAmount,
                DueDate = c.DueDate,
                IsOverdue = overdue,
                DaysOverdue = overdue ? (today - due!.Value).Days : 0
            };
        });

        if (overdueOnly)
            items = items.Where(i => i.IsOverdue);

        return Ok(items.ToList());
    }
}

public sealed class RealEstateDashboardDto
{
    public int TodayContracts { get; set; }
    public int MonthContracts { get; set; }
    public int TotalContracts { get; set; }
    public int UnpaidContracts { get; set; }
    public int OverdueDebts { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal TotalRemaining { get; set; }
    public List<NameCountPoint> MonthlyContracts { get; set; } = [];
    public List<NameAmountPoint> PaymentStatusChart { get; set; } = [];
    public List<NameCountPoint> ByContractType { get; set; } = [];
    public List<NameCountPoint> ByPropertyType { get; set; } = [];
    public List<RealEstateContractListDto> RecentContracts { get; set; } = [];
}

public sealed class RealEstateDebtDto
{
    public Guid SyncId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime ContractDate { get; set; }
    public string DebtorName { get; set; } = string.Empty;
    public string DebtorPhone { get; set; } = string.Empty;
    public string DebtorParty { get; set; } = string.Empty;
    public string CounterpartyName { get; set; } = string.Empty;
    public decimal RemainingAmount { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysOverdue { get; set; }
}
