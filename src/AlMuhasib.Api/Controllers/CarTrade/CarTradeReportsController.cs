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

        var query = Db.CarTradeTransactions.AsNoTracking().Where(t => t.TenantId == TenantId);

        if (from.HasValue)
            query = query.Where(t => t.TransactionDate >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(t => t.TransactionDate <= to.Value.Date);
        if (!string.IsNullOrWhiteSpace(tradeType) && Enum.TryParse<CarTradeType>(tradeType, true, out var tradeTypeEnum))
            query = query.Where(t => t.TradeType == tradeTypeEnum);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CarTradeStatus>(status, true, out var statusEnum))
            query = query.Where(t => t.Status == statusEnum);
        if (!string.IsNullOrWhiteSpace(paymentMode) && Enum.TryParse<CarTradePaymentMode>(paymentMode, true, out var paymentModeEnum))
            query = query.Where(t => t.PaymentMode == paymentModeEnum);
        if (unpaidOnly == true)
            query = query.Where(t => t.RemainingAmount > 0);

        var items = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.Id)
            .ToListAsync(ct);

        var rows = items.Select(CarTradeMapper.ToListItem).ToList();
        var buys = items.Where(t => t.TradeType == CarTradeType.Buy).ToList();
        var sells = items.Where(t => t.TradeType == CarTradeType.Sell).ToList();

        return Ok(new CarTradeTransactionsReportDto
        {
            Rows = rows,
            BuyCount = buys.Count,
            SellCount = sells.Count,
            TotalBuyValue = buys.Sum(t => t.TotalAmount),
            TotalSellValue = sells.Sum(t => t.TotalAmount),
            TotalPaid = rows.Sum(r => r.AmountPaid),
            TotalRemaining = rows.Sum(r => r.RemainingAmount),
            MonthlyBuy = buys
                .GroupBy(t => new DateTime(t.TransactionDate.Year, t.TransactionDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .ToList(),
            MonthlySell = sells
                .GroupBy(t => new DateTime(t.TransactionDate.Year, t.TransactionDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .ToList(),
            CollectedVsRemaining =
            [
                new NameAmountPoint { Name = "Collected", Amount = rows.Sum(r => r.AmountPaid) },
                new NameAmountPoint { Name = "Remaining", Amount = rows.Sum(r => r.RemainingAmount) }
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
        var query = Db.CarTradeTransactions.AsNoTracking()
            .Where(t => t.TenantId == TenantId && t.Status != CarTradeStatus.Cancelled && t.RemainingAmount > 0);

        if (from.HasValue)
            query = query.Where(t => t.TransactionDate >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(t => t.TransactionDate <= to.Value.Date);

        var transactions = await query.ToListAsync(ct);
        var rows = new List<CarTradePartyStatementRowDto>();

        foreach (var t in transactions)
        {
            if (t.TradeType == CarTradeType.Buy &&
                string.Equals(t.SellerName.Trim(), trimmedPartyName, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(partyPhone) ||
                 string.Equals(t.SellerPhone.Trim(), partyPhone.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                rows.Add(new CarTradePartyStatementRowDto
                {
                    TransactionDate = t.TransactionDate,
                    TransactionNumber = t.TransactionNumber,
                    TradeType = t.TradeType.ToString(),
                    CarName = t.CarName,
                    TotalAmount = t.TotalAmount,
                    AmountPaid = t.AmountPaid,
                    RemainingAmount = t.RemainingAmount,
                    PartyRole = "Seller"
                });
            }
            else if (t.TradeType == CarTradeType.Sell &&
                     string.Equals(t.BuyerName.Trim(), trimmedPartyName, StringComparison.OrdinalIgnoreCase) &&
                     (string.IsNullOrWhiteSpace(partyPhone) ||
                      string.Equals(t.BuyerPhone.Trim(), partyPhone.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                rows.Add(new CarTradePartyStatementRowDto
                {
                    TransactionDate = t.TransactionDate,
                    TransactionNumber = t.TransactionNumber,
                    TradeType = t.TradeType.ToString(),
                    CarName = t.CarName,
                    TotalAmount = t.TotalAmount,
                    AmountPaid = t.AmountPaid,
                    RemainingAmount = t.RemainingAmount,
                    PartyRole = "Buyer"
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
