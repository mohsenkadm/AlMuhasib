using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldDashboardService : IGoldDashboardService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;
    private readonly IGoldSmartAlertService _alertService;
    private readonly IGoldPricingService _pricingService;

    public GoldDashboardService(
        IDbContextFactory<GoldDbContext> contextFactory,
        IGoldSmartAlertService alertService,
        IGoldPricingService pricingService)
    {
        _contextFactory = contextFactory;
        _alertService = alertService;
        _pricingService = pricingService;
    }

    public async Task<GoldDashboardData> GetDashboardAsync(
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var today = (asOfDate ?? DateTime.Today).Date;
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var todaySales = await context.GoldInvoices.AsNoTracking()
            .Where(i => i.InvoiceType == GoldInvoiceType.Sale &&
                        i.Status != GoldInvoiceStatus.Cancelled &&
                        i.InvoiceDate.Date == today)
            .ToListAsync(cancellationToken);

        var todayPurchases = await context.GoldInvoices.AsNoTracking()
            .Where(i => i.InvoiceType == GoldInvoiceType.Purchase &&
                        i.Status != GoldInvoiceStatus.Cancelled &&
                        i.InvoiceDate.Date == today)
            .ToListAsync(cancellationToken);

        var cashBoxes = await context.GoldCashBoxes.AsNoTracking()
            .Where(c => c.IsActive)
            .ToListAsync(cancellationToken);

        var stockRows = await GoldInventoryService.BuildStockRowsAsync(context, null, cancellationToken);

        var customersWithCredit = await context.GoldCustomers.AsNoTracking()
            .Where(c => c.CreditBalanceIqd > 0 || c.CreditBalanceUsd > 0)
            .ToListAsync(cancellationToken);

        var settings = await context.GoldSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == Core.Entities.Gold.GoldSettings.SingletonId, cancellationToken);
        var overdueDays = settings?.OverdueDaysThreshold ?? 30;
        var overdueCutoff = today.AddDays(-overdueDays);

        var overdueCount = await context.GoldInvoices.AsNoTracking()
            .Where(i => i.RemainingAmount > 0 &&
                        i.Status != GoldInvoiceStatus.Cancelled &&
                        i.InvoiceDate.Date <= overdueCutoff)
            .Select(i => i.CustomerId)
            .Distinct()
            .CountAsync(cancellationToken);

        var pricesUpdatedToday = await context.GoldMithqalPrices.AsNoTracking()
            .AnyAsync(p => p.PriceDate.Date == today, cancellationToken);

        var latestFx = await context.GoldFxRates.AsNoTracking()
            .OrderByDescending(r => r.RateDate)
            .ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var recent = await context.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Where(i => i.Status != GoldInvoiceStatus.Cancelled)
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Take(10)
            .ToListAsync(cancellationToken);

        var alerts = await _alertService.GetAlertsAsync(cancellationToken);
        var latestPrices = await _pricingService.GetLatestPricesAsync(cancellationToken);

        return new GoldDashboardData
        {
            TodaySalesIqd = todaySales.Sum(i => i.TotalAmountIqd),
            TodaySalesUsd = todaySales.Sum(i => i.TotalAmountUsd),
            TodayPurchasesIqd = todayPurchases.Sum(i => i.TotalAmountIqd),
            TodayPurchasesUsd = todayPurchases.Sum(i => i.TotalAmountUsd),
            CashBalanceIqd = cashBoxes.Where(c => c.Currency == GoldCurrency.IQD).Sum(c => c.Balance),
            CashBalanceUsd = cashBoxes.Where(c => c.Currency == GoldCurrency.USD).Sum(c => c.Balance),
            TotalStockGrams = stockRows.Sum(s => s.GramsOnHand),
            TotalStockValueIqd = stockRows.Sum(s => s.StockValue),
            OpenCreditCount = customersWithCredit.Count,
            OpenCreditIqd = customersWithCredit.Sum(c => c.CreditBalanceIqd),
            OpenCreditUsd = customersWithCredit.Sum(c => c.CreditBalanceUsd),
            OverdueCreditCount = overdueCount,
            LowStockKaratCount = stockRows.Count(s => s.IsLowStock),
            PricesUpdatedToday = pricesUpdatedToday,
            LatestUsdToIqd = latestFx?.UsdToIqd,
            StockByKarat = stockRows,
            RecentInvoices = recent.Select(GoldCurrencyHelper.ToListItem).ToList(),
            Alerts = alerts.Take(8).ToList(),
            LatestPrices = latestPrices.ToList()
        };
    }
}
