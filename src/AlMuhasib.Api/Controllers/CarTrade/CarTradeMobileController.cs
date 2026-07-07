using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Car;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.CarTrade;

[ApiController]
[Route("api/car-trade")]
[Authorize(Policy = "Tenant")]
public sealed class CarTradeMobileController : CarTradeApiControllerBase
{
    public CarTradeMobileController(ITenantContext tenantContext, CloudDbContext db) : base(db, tenantContext) { }

    [HttpGet("dashboard")]
    public async Task<ActionResult<CarTradeDashboardDto>> GetDashboard(CancellationToken ct)
    {
        if (await EnsureCarTradeTenantAsync(ct) is { } err) return err;

        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var transactions = await Db.CarTradeTransactions.AsNoTracking()
            .Where(t => t.TenantId == TenantId && t.Status != CarTradeStatus.Cancelled)
            .ToListAsync(ct);

        var buys = transactions.Where(t => t.TradeType == CarTradeType.Buy).ToList();
        var sells = transactions.Where(t => t.TradeType == CarTradeType.Sell).ToList();

        var recent = transactions
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .Take(10)
            .Select(CarTradeMapper.ToListItem)
            .ToList();

        return Ok(new CarTradeDashboardDto
        {
            TodayTransactions = transactions.Count(t => t.TransactionDate.Date == today),
            MonthTransactions = transactions.Count(t => t.TransactionDate.Date >= monthStart),
            TotalTransactions = transactions.Count,
            UnpaidTransactions = transactions.Count(t => t.RemainingAmount > 0),
            BuyCount = buys.Count,
            SellCount = sells.Count,
            TotalBuyValue = buys.Sum(t => t.TotalAmount),
            TotalSellValue = sells.Sum(t => t.TotalAmount),
            TotalPaid = transactions.Sum(t => t.AmountPaid),
            TotalRemaining = transactions.Sum(t => t.RemainingAmount),
            MonthlyBuy = buys
                .GroupBy(t => new DateTime(t.TransactionDate.Year, t.TransactionDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .TakeLast(12)
                .ToList(),
            MonthlySell = sells
                .GroupBy(t => new DateTime(t.TransactionDate.Year, t.TransactionDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .TakeLast(12)
                .ToList(),
            PaymentStatusChart =
            [
                new NameAmountPoint { Name = "Fully Paid", Amount = transactions.Count(t => t.RemainingAmount <= 0) },
                new NameAmountPoint { Name = "Partially Paid", Amount = transactions.Count(t => t.RemainingAmount > 0 && t.AmountPaid > 0) },
                new NameAmountPoint { Name = "Unpaid", Amount = transactions.Count(t => t.AmountPaid <= 0 && t.RemainingAmount > 0) }
            ],
            TopCarTypes = transactions
                .GroupBy(t => string.IsNullOrWhiteSpace(t.CarType) ? "Unspecified" : t.CarType)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .ToList(),
            RecentTransactions = recent
        });
    }
}

public sealed class CarTradeDashboardDto
{
    public int TodayTransactions { get; set; }
    public int MonthTransactions { get; set; }
    public int TotalTransactions { get; set; }
    public int UnpaidTransactions { get; set; }
    public int BuyCount { get; set; }
    public int SellCount { get; set; }
    public decimal TotalBuyValue { get; set; }
    public decimal TotalSellValue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemaining { get; set; }
    public List<NameCountPoint> MonthlyBuy { get; set; } = [];
    public List<NameCountPoint> MonthlySell { get; set; } = [];
    public List<NameAmountPoint> PaymentStatusChart { get; set; } = [];
    public List<NameCountPoint> TopCarTypes { get; set; } = [];
    public List<CarTradeListDto> RecentTransactions { get; set; } = [];
}
