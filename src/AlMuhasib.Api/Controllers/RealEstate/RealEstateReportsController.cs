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
[Route("api/real-estate/reports")]
[Authorize(Policy = "Tenant")]
public sealed class RealEstateReportsController : RealEstateApiControllerBase
{
    public RealEstateReportsController(CloudDbContext db, ITenantContext tenantContext) : base(db, tenantContext) { }

    [HttpGet("contracts")]
    public async Task<ActionResult<RealEstateContractsReportDto>> GetContractsReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? status,
        [FromQuery] string? contractType,
        [FromQuery] string? propertyType,
        CancellationToken ct = default)
    {
        if (await EnsureRealEstateTenantAsync(ct) is { } err) return err;

        var query = Db.RealEstateContracts.AsNoTracking().Where(c => c.TenantId == TenantId);

        if (from.HasValue)
            query = query.Where(c => c.ContractDate >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(c => c.ContractDate <= to.Value.Date);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RealEstateContractStatus>(status, true, out var statusEnum))
            query = query.Where(c => c.Status == statusEnum);
        if (!string.IsNullOrWhiteSpace(contractType) && Enum.TryParse<RealEstateContractType>(contractType, true, out var typeEnum))
            query = query.Where(c => c.ContractType == typeEnum);
        if (!string.IsNullOrWhiteSpace(propertyType) && Enum.TryParse<RealEstatePropertyType>(propertyType, true, out var propEnum))
            query = query.Where(c => c.PropertyType == propEnum);

        var items = await query
            .OrderByDescending(c => c.ContractDate)
            .ThenByDescending(c => c.Id)
            .ToListAsync(ct);

        var rows = items.Select(RealEstateContractMapper.ToListItem).ToList();

        return Ok(new RealEstateContractsReportDto
        {
            Rows = rows,
            ContractCount = items.Count,
            TotalValue = items.Sum(c => c.TotalPrice),
            TotalReceived = items.Sum(c => c.AmountPaid),
            TotalRemaining = items.Sum(c => c.RemainingAmount),
            MonthlyContracts = items
                .GroupBy(c => new DateTime(c.ContractDate.Year, c.ContractDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .ToList(),
            CollectedVsRemaining =
            [
                new NameAmountPoint { Name = "Collected", Amount = items.Sum(c => c.AmountPaid) },
                new NameAmountPoint { Name = "Remaining", Amount = items.Sum(c => c.RemainingAmount) }
            ],
            ByPropertyType = items
                .GroupBy(c => c.PropertyType.ToString())
                .OrderByDescending(g => g.Count())
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .ToList(),
            ByContractType = items
                .GroupBy(c => c.ContractType.ToString())
                .OrderByDescending(g => g.Count())
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .ToList()
        });
    }

    [HttpGet("profit")]
    public async Task<ActionResult<RealEstateProfitReportDto>> GetProfitReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        if (await EnsureRealEstateTenantAsync(ct) is { } err) return err;

        var dateFrom = (from ?? DateTime.Today.AddMonths(-1)).Date;
        var dateTo = (to ?? DateTime.Today).Date;

        var contracts = await Db.RealEstateContracts.AsNoTracking()
            .Where(c =>
                c.TenantId == TenantId &&
                c.Status != RealEstateContractStatus.Cancelled &&
                c.ContractDate >= dateFrom &&
                c.ContractDate <= dateTo)
            .OrderByDescending(c => c.ContractDate)
            .ToListAsync(ct);

        var expenses = await Db.RealEstateExpenses.AsNoTracking()
            .Include(e => e.ExpenseType)
            .Include(e => e.RelatedContract)
            .Where(e =>
                e.TenantId == TenantId &&
                e.ExpenseDate >= dateFrom &&
                e.ExpenseDate <= dateTo)
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync(ct);

        var sales = contracts.Where(c => c.ContractType == RealEstateContractType.Sale).ToList();
        var purchases = contracts.Where(c => c.ContractType == RealEstateContractType.Purchase).ToList();

        var saleRevenue = sales.Sum(c => c.TotalPrice);
        var purchaseCost = purchases.Sum(c => c.TotalPrice);
        var gross = saleRevenue - purchaseCost;
        var totalExpenses = expenses.Sum(e => e.Amount);
        var net = gross - totalExpenses;
        var cashIn = sales.Sum(c => c.AmountPaid);
        var cashOutPurchases = purchases.Sum(c => c.AmountPaid);

        var monthKeys = contracts
            .Select(c => new DateTime(c.ContractDate.Year, c.ContractDate.Month, 1))
            .Concat(expenses.Select(e => new DateTime(e.ExpenseDate.Year, e.ExpenseDate.Month, 1)))
            .Distinct()
            .OrderBy(m => m)
            .ToList();

        return Ok(new RealEstateProfitReportDto
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            SaleContractsCount = sales.Count,
            PurchaseContractsCount = purchases.Count,
            ExpenseCount = expenses.Count,
            SaleRevenue = saleRevenue,
            PurchaseCost = purchaseCost,
            GrossProfit = gross,
            TotalExpenses = totalExpenses,
            NetProfit = net,
            ProfitMarginPercent = saleRevenue > 0 ? Math.Round(gross / saleRevenue * 100m, 2) : 0m,
            CashInFromSales = cashIn,
            CashOutOnPurchases = cashOutPurchases,
            CashExpenses = totalExpenses,
            NetCash = cashIn - cashOutPurchases - totalExpenses,
            SaleReceivables = sales.Sum(c => c.RemainingAmount),
            PurchasePayables = purchases.Sum(c => c.RemainingAmount),
            ExpensesByType = expenses
                .GroupBy(e => e.ExpenseType.Name)
                .OrderByDescending(g => g.Sum(x => x.Amount))
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(x => x.Amount) })
                .ToList(),
            MonthlySeries = monthKeys.Select(month =>
            {
                var monthEnd = month.AddMonths(1).AddDays(-1);
                var monthSales = sales.Where(c => c.ContractDate >= month && c.ContractDate <= monthEnd).Sum(c => c.TotalPrice);
                var monthPurchases = purchases.Where(c => c.ContractDate >= month && c.ContractDate <= monthEnd).Sum(c => c.TotalPrice);
                var monthExpenses = expenses.Where(e => e.ExpenseDate >= month && e.ExpenseDate <= monthEnd).Sum(e => e.Amount);
                var monthGross = monthSales - monthPurchases;
                return new RealEstateMonthlyProfitDto
                {
                    Period = month.ToString("yyyy/MM"),
                    SaleRevenue = monthSales,
                    PurchaseCost = monthPurchases,
                    Expenses = monthExpenses,
                    GrossProfit = monthGross,
                    NetProfit = monthGross - monthExpenses
                };
            }).ToList(),
            SaleRows = sales.Select(c => new RealEstateProfitContractDto
            {
                SyncId = c.SyncId,
                ContractNumber = c.ContractNumber,
                ContractDate = c.ContractDate,
                ContractType = "Sale",
                PartyName = c.BuyerName,
                PropertyLocation = c.PropertyLocation,
                TotalPrice = c.TotalPrice,
                AmountPaid = c.AmountPaid,
                RemainingAmount = c.RemainingAmount
            }).ToList(),
            PurchaseRows = purchases.Select(c => new RealEstateProfitContractDto
            {
                SyncId = c.SyncId,
                ContractNumber = c.ContractNumber,
                ContractDate = c.ContractDate,
                ContractType = "Purchase",
                PartyName = c.SellerName,
                PropertyLocation = c.PropertyLocation,
                TotalPrice = c.TotalPrice,
                AmountPaid = c.AmountPaid,
                RemainingAmount = c.RemainingAmount
            }).ToList(),
            ExpenseRows = expenses.Select(e => new RealEstateExpenseDto
            {
                SyncId = e.SyncId,
                ExpenseDate = e.ExpenseDate,
                Amount = e.Amount,
                Description = e.Description,
                Notes = e.Notes,
                ExpenseTypeSyncId = e.ExpenseType.SyncId,
                ExpenseTypeName = e.ExpenseType.Name,
                RelatedContractSyncId = e.RelatedContract?.SyncId,
                RelatedContractNumber = e.RelatedContract?.ContractNumber ?? string.Empty
            }).ToList()
        });
    }
}

public sealed class RealEstateContractsReportDto
{
    public List<RealEstateContractListDto> Rows { get; set; } = [];
    public int ContractCount { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal TotalRemaining { get; set; }
    public List<NameCountPoint> MonthlyContracts { get; set; } = [];
    public List<NameAmountPoint> CollectedVsRemaining { get; set; } = [];
    public List<NameCountPoint> ByPropertyType { get; set; } = [];
    public List<NameCountPoint> ByContractType { get; set; } = [];
}

public sealed class RealEstateProfitReportDto
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int SaleContractsCount { get; set; }
    public int PurchaseContractsCount { get; set; }
    public int ExpenseCount { get; set; }
    public decimal SaleRevenue { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ProfitMarginPercent { get; set; }
    public decimal CashInFromSales { get; set; }
    public decimal CashOutOnPurchases { get; set; }
    public decimal CashExpenses { get; set; }
    public decimal NetCash { get; set; }
    public decimal SaleReceivables { get; set; }
    public decimal PurchasePayables { get; set; }
    public List<NameAmountPoint> ExpensesByType { get; set; } = [];
    public List<RealEstateMonthlyProfitDto> MonthlySeries { get; set; } = [];
    public List<RealEstateProfitContractDto> SaleRows { get; set; } = [];
    public List<RealEstateProfitContractDto> PurchaseRows { get; set; } = [];
    public List<RealEstateExpenseDto> ExpenseRows { get; set; } = [];
}

public sealed class RealEstateMonthlyProfitDto
{
    public string Period { get; set; } = string.Empty;
    public decimal SaleRevenue { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal Expenses { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal NetProfit { get; set; }
}

public sealed class RealEstateProfitContractDto
{
    public Guid SyncId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime ContractDate { get; set; }
    public string ContractType { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public string PropertyLocation { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
}
