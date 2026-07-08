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

        var buys = rows;
        var sold = rows.Where(r => r.IsSold).ToList();

        return new CarTradeReportData
        {
            Rows = rows.ToList(),
            BuyCount = buys.Count,
            SellCount = sold.Count,
            AvailableCount = rows.Count(r => !r.IsSold),
            SoldCount = sold.Count,
            TotalBuyValue = buys.Sum(r => r.PurchasePrice),
            TotalSellValue = sold.Sum(r => r.SalePrice),
            TotalPaid = rows.Sum(r => r.AmountPaid) + sold.Sum(r => r.SaleAmountPaid),
            TotalRemaining = rows.Sum(r => r.RemainingAmount),
            TotalSaleRemaining = sold.Sum(r => r.SaleRemainingAmount),
            MonthlyBuy = transactions
                .GroupBy(t => new DateTime(t.TransactionDate.Year, t.TransactionDate.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .ToList(),
            MonthlySell = transactions
                .Where(t => t.IsSold && t.SaleDate.HasValue)
                .GroupBy(t => new DateTime(t.SaleDate!.Value.Year, t.SaleDate.Value.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new NameCountPoint { Name = g.Key.ToString("yyyy/MM"), Count = g.Count() })
                .ToList(),
            CollectedVsRemaining =
            [
                new NameAmountPoint { Name = "ديون بائعين", Amount = rows.Sum(r => r.RemainingAmount) },
                new NameAmountPoint { Name = "ديون مشترين", Amount = sold.Sum(r => r.SaleRemainingAmount) }
            ],
            ByCarType = transactions
                .GroupBy(t => string.IsNullOrWhiteSpace(t.CarType) ? "غير محدد" : t.CarType)
                .OrderByDescending(g => g.Count())
                .Select(g => new NameCountPoint { Name = g.Key, Count = g.Count() })
                .ToList()
        };
    }
}
