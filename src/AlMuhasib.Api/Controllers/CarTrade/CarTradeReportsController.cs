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
[Route("api/car-trade/reports")]
[Authorize(Policy = "Tenant")]
public sealed class CarTradeReportsController : CarTradeApiControllerBase
{
    public CarTradeReportsController(CloudDbContext db, ITenantContext tenantContext) : base(db, tenantContext) { }

    [HttpGet("transactions")]
    public async Task<ActionResult<CarTradeTransactionsReportDto>> GetTransactionsReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? tradeType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? paymentMode = null,
        [FromQuery] bool? unpaidOnly = null,
        CancellationToken ct = default)
    {
        if (await EnsureCarTradeTenantAsync(ct) is { } err) return err;

        var query = Db.CarTradeTransactions.AsNoTracking()
            .Where(t => t.TenantId == TenantId && t.TradeType == CarTradeType.Buy);

        if (from.HasValue)
            query = query.Where(t => t.TransactionDate >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(t => t.TransactionDate <= to.Value.Date);
        if (unpaidOnly == true)
            query = query.Where(t => t.RemainingAmount > 0 || t.SaleRemainingAmount > 0);

        var items = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .ToListAsync(ct);

        var rows = items.Select(CarTradeMapper.ToListItem).ToList();
        var sold = items.Where(t => t.IsSold).ToList();

        return Ok(new CarTradeTransactionsReportDto
        {
            Rows = rows,
            BuyCount = items.Count,
            SellCount = sold.Count,
            TotalBuyValue = items.Sum(t => t.PurchasePrice),
            TotalSellValue = sold.Sum(t => t.SalePrice),
            TotalPaid = items.Sum(t => t.AmountPaid) + sold.Sum(t => t.SaleAmountPaid),
            TotalRemaining = items.Sum(t => t.RemainingAmount),
            MonthlyBuy = items
                .GroupBy(t => new DateTime(t.TransactionDate.Year, t.TransactionDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .ToList(),
            MonthlySell = sold
                .Where(t => t.SaleDate.HasValue)
                .GroupBy(t => new DateTime(t.SaleDate!.Value.Year, t.SaleDate.Value.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .ToList(),
            CollectedVsRemaining =
            [
                new NameAmountPoint { Name = "SellerDebt", Amount = items.Sum(t => t.RemainingAmount) },
                new NameAmountPoint { Name = "BuyerDebt", Amount = sold.Sum(t => t.SaleRemainingAmount) }
            ],
            ByCarType = items
                .GroupBy(t => string.IsNullOrWhiteSpace(t.CarType) ? "Unspecified" : t.CarType)
                .OrderByDescending(g => g.Count())
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .ToList()
        });
    }

    [HttpGet("party-statement")]
    public async Task<ActionResult<CarTradePartyStatementDto>> GetPartyStatement(
        [FromQuery] string partyName,
        [FromQuery] string? partyPhone = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        if (await EnsureCarTradeTenantAsync(ct) is { } err) return err;

        if (string.IsNullOrWhiteSpace(partyName))
            return BadRequest("Party name is required.");

        var trimmedPartyName = partyName.Trim();
        var transactions = await Db.CarTradeTransactions.AsNoTracking()
            .Where(t => t.TenantId == TenantId && t.Status != CarTradeStatus.Cancelled && t.TradeType == CarTradeType.Buy)
            .ToListAsync(ct);
        var rows = new List<CarTradePartyStatementRowDto>();

        foreach (var t in transactions)
        {
            if (t.RemainingAmount > 0 &&
                string.Equals(t.SellerName.Trim(), trimmedPartyName, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(partyPhone) ||
                 string.Equals(t.SellerPhone.Trim(), partyPhone.Trim(), StringComparison.OrdinalIgnoreCase)) &&
                (!from.HasValue || t.TransactionDate.Date >= from.Value.Date) &&
                (!to.HasValue || t.TransactionDate.Date <= to.Value.Date))
            {
                rows.Add(new CarTradePartyStatementRowDto
                {
                    TransactionDate = t.TransactionDate,
                    TransactionNumber = t.TransactionNumber,
                    TradeType = "Buy",
                    CarName = t.CarName,
                    TotalAmount = t.PurchasePrice,
                    AmountPaid = t.AmountPaid,
                    RemainingAmount = t.RemainingAmount,
                    PartyRole = "Seller",
                    DebtKind = "SellerDebt"
                });
            }

            if (t.IsSold && t.SaleRemainingAmount > 0 &&
                string.Equals(t.BuyerName.Trim(), trimmedPartyName, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(partyPhone) ||
                 string.Equals(t.BuyerPhone.Trim(), partyPhone.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                var saleDate = t.SaleDate ?? t.TransactionDate;
                if ((from.HasValue && saleDate.Date < from.Value.Date) ||
                    (to.HasValue && saleDate.Date > to.Value.Date))
                    continue;

                rows.Add(new CarTradePartyStatementRowDto
                {
                    TransactionDate = saleDate,
                    TransactionNumber = t.TransactionNumber,
                    TradeType = "Sell",
                    CarName = t.CarName,
                    TotalAmount = t.SalePrice,
                    AmountPaid = t.SaleAmountPaid,
                    RemainingAmount = t.SaleRemainingAmount,
                    PartyRole = "Buyer",
                    DebtKind = "BuyerDebt"
                });
            }
        }

        rows = rows.OrderBy(r => r.TransactionDate).ThenBy(r => r.TransactionNumber).ToList();

        var totalDebit = rows.Where(r => r.PartyRole == "Seller").Sum(r => r.RemainingAmount);
        var totalCredit = rows.Where(r => r.PartyRole == "Buyer").Sum(r => r.RemainingAmount);

        return Ok(new CarTradePartyStatementDto
        {
            PartyName = trimmedPartyName,
            PartyPhone = partyPhone ?? string.Empty,
            Rows = rows,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            Balance = totalCredit - totalDebit
        });
    }
}

public sealed class CarTradeTransactionsReportDto
{
    public List<CarTradeListDto> Rows { get; set; } = [];
    public int BuyCount { get; set; }
    public int SellCount { get; set; }
    public decimal TotalBuyValue { get; set; }
    public decimal TotalSellValue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemaining { get; set; }
    public List<NameCountPoint> MonthlyBuy { get; set; } = [];
    public List<NameCountPoint> MonthlySell { get; set; } = [];
    public List<NameAmountPoint> CollectedVsRemaining { get; set; } = [];
    public List<NameCountPoint> ByCarType { get; set; } = [];
}

public sealed class CarTradePartyStatementRowDto
{
    public DateTime TransactionDate { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public string TradeType { get; set; } = string.Empty;
    public string CarName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public string PartyRole { get; set; } = string.Empty;
    public string DebtKind { get; set; } = string.Empty;
}

public sealed class CarTradePartyStatementDto
{
    public string PartyName { get; set; } = string.Empty;
    public string PartyPhone { get; set; } = string.Empty;
    public List<CarTradePartyStatementRowDto> Rows { get; set; } = [];
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal Balance { get; set; }
}
