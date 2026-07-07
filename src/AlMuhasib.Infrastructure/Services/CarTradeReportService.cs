using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Car;
using AlMuhasib.Core.Models.CarTrade;
using AlMuhasib.Infrastructure.Data.CarTrade;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class CarTradeReportService : ICarTradeReportService
{
    private readonly IDbContextFactory<CarTradeDbContext> _contextFactory;
    private readonly ICarTradeService _tradeService;

    public CarTradeReportService(
        IDbContextFactory<CarTradeDbContext> contextFactory,
        ICarTradeService tradeService)
    {
        _contextFactory = contextFactory;
        _tradeService = tradeService;
    }

    public async Task<CarTradeReportData> GetReportAsync(CarTradeFilter filter, CancellationToken cancellationToken = default)
    {
        var rows = await _tradeService.GetAllForExportAsync(filter, cancellationToken);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var transactions = await context.CarTradeTransactions
            .Where(t => rows.Select(r => r.Id).Contains(t.Id))
            .ToListAsync(cancellationToken);

        var buys = rows.Where(r => r.TradeTypeValue == Core.Enums.CarTradeType.Buy).ToList();
        var sells = rows.Where(r => r.TradeTypeValue == Core.Enums.CarTradeType.Sell).ToList();

        return new CarTradeReportData
        {
            Rows = rows.ToList(),
            BuyCount = buys.Count,
            SellCount = sells.Count,
            TotalBuyValue = buys.Sum(r => r.TotalAmount),
            TotalSellValue = sells.Sum(r => r.TotalAmount),
            TotalPaid = rows.Sum(r => r.AmountPaid),
            TotalRemaining = rows.Sum(r => r.RemainingAmount),
            MonthlyBuy = transactions
                .Where(t => t.TradeType == Core.Enums.CarTradeType.Buy)
                .GroupBy(t => new DateTime(t.TransactionDate.Year, t.TransactionDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .ToList(),
            MonthlySell = transactions
                .Where(t => t.TradeType == Core.Enums.CarTradeType.Sell)
                .GroupBy(t => new DateTime(t.TransactionDate.Year, t.TransactionDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .ToList(),
            CollectedVsRemaining =
            [
                new NameAmountPoint { Name = "المحصّل", Amount = rows.Sum(r => r.AmountPaid) },
                new NameAmountPoint { Name = "المتبقي", Amount = rows.Sum(r => r.RemainingAmount) }
            ],
            ByCarType = transactions
                .GroupBy(t => string.IsNullOrWhiteSpace(t.CarType) ? "غير محدد" : t.CarType)
                .OrderByDescending(g => g.Count())
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .ToList()
        };
    }
}
