using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldDashboardService : IGoldDashboardService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;
    private readonly IGoldPricingService _pricingService;

    public GoldDashboardService(
        IDbContextFactory<GoldDbContext> contextFactory,
        IGoldSmartAlertService alertService,
        IGoldPricingService pricingService)
    {
        _contextFactory = contextFactory;
        _ = alertService; // kept for DI compatibility; alerts loaded read-only below
        _pricingService = pricingService;
    }

    public async Task<GoldDashboardData> GetDashboardAsync(
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        var today = (asOfDate ?? DateTime.Today).Date;
        var thirtyDaysAgo = today.AddDays(-29);
        var tomorrow = today.AddDays(1);

        // Parallel independent queries (separate contexts) — major lag reduction.
        var todaySalesTask = SumTodayInvoicesAsync(GoldInvoiceType.Sale, today, tomorrow, cancellationToken);
        var todayPurchasesTask = SumTodayInvoicesAsync(GoldInvoiceType.Purchase, today, tomorrow, cancellationToken);
        var cashTask = LoadCashAsync(cancellationToken);
        var stockTask = LoadStockAsync(cancellationToken);
        var creditTask = LoadCreditAsync(cancellationToken);
        var settingsTask = LoadSettingsAsync(cancellationToken);
        var expensesTask = SumTodayExpensesAsync(today, tomorrow, cancellationToken);
        var fxTask = LoadLatestFxAsync(cancellationToken);
        var recentTask = LoadRecentInvoicesAsync(cancellationToken);
        var sales30Task = LoadSalesLast30DaysAsync(thirtyDaysAgo, tomorrow, cancellationToken);
        var pricesTask = _pricingService.GetLatestPricesAsync(cancellationToken);
        var alertsTask = LoadUnreadAlertsAsync(cancellationToken);

        await Task.WhenAll(
            todaySalesTask, todayPurchasesTask, cashTask, stockTask, creditTask,
            settingsTask, expensesTask, fxTask, recentTask, sales30Task, pricesTask, alertsTask);

        var (todaySalesIqd, todaySalesUsd) = todaySalesTask.Result;
        var (todayPurchasesIqd, todayPurchasesUsd) = todayPurchasesTask.Result;
        var cashBoxes = cashTask.Result;
        var stockRows = stockTask.Result;
        var (openCreditCount, openCreditIqd, openCreditUsd) = creditTask.Result;
        var settings = settingsTask.Result;
        var (todayExpensesIqd, todayExpensesUsd, hasExpenseToday) = expensesTask.Result;
        var latestFx = fxTask.Result;
        var recent = recentTask.Result;
        var salesLast30Days = sales30Task.Result;
        var latestPrices = pricesTask.Result;
        var alerts = alertsTask.Result;

        var overdueDays = settings?.OverdueDaysThreshold > 0 ? settings!.OverdueDaysThreshold : 30;
        var overdueCutoff = today.AddDays(-overdueDays);
        var overdueCount = await CountOverdueAsync(overdueCutoff, cancellationToken);

        var pricesUpdatedToday = await AnyPricesTodayAsync(today, tomorrow, cancellationToken);

        var lowStockRows = stockRows.Where(s => s.IsLowStock).ToList();

        return new GoldDashboardData
        {
            TodaySalesIqd = todaySalesIqd,
            TodaySalesUsd = todaySalesUsd,
            TodayPurchasesIqd = todayPurchasesIqd,
            TodayPurchasesUsd = todayPurchasesUsd,
            TodayExpensesIqd = todayExpensesIqd,
            TodayExpensesUsd = todayExpensesUsd,
            HasExpenseToday = hasExpenseToday,
            CashBalanceIqd = cashBoxes.Where(c => c.Currency == GoldCurrency.IQD).Sum(c => c.Balance),
            CashBalanceUsd = cashBoxes.Where(c => c.Currency == GoldCurrency.USD).Sum(c => c.Balance),
            TotalStockGrams = stockRows.Sum(s => s.GramsOnHand),
            TotalStockValueIqd = stockRows.Sum(s => s.StockValue),
            OpenCreditCount = openCreditCount,
            OpenCreditIqd = openCreditIqd,
            OpenCreditUsd = openCreditUsd,
            OverdueCreditCount = overdueCount,
            LowStockKaratCount = lowStockRows.Select(s => s.KaratValue).Distinct().Count(),
            LowWarehouseStockCount = lowStockRows.Select(s => s.WarehouseId).Distinct().Count(),
            PricesUpdatedToday = pricesUpdatedToday,
            LatestUsdToIqd = latestFx,
            SalesLast30Days = salesLast30Days,
            CashBoxes = cashBoxes,
            StockByKarat = stockRows,
            RecentInvoices = recent,
            Alerts = alerts.Take(8).ToList(),
            LatestPrices = latestPrices.ToList()
        };
    }

    private async Task<(decimal Iqd, decimal Usd)> SumTodayInvoicesAsync(
        GoldInvoiceType type, DateTime today, DateTime tomorrow, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var query = context.GoldInvoices.AsNoTracking()
            .Where(i => i.InvoiceType == type &&
                        i.Status != GoldInvoiceStatus.Cancelled &&
                        i.InvoiceDate >= today && i.InvoiceDate < tomorrow);
        var iqd = await query.SumAsync(i => (decimal?)i.TotalAmountIqd, ct) ?? 0;
        var usd = await query.SumAsync(i => (decimal?)i.TotalAmountUsd, ct) ?? 0;
        return (iqd, usd);
    }

    private async Task<List<GoldCashBoxSummary>> LoadCashAsync(CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.GoldCashBoxes.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new GoldCashBoxSummary
            {
                Id = c.Id,
                Name = c.Name,
                Balance = c.Balance,
                Currency = c.Currency
            })
            .ToListAsync(ct);
    }

    private async Task<List<GoldStockRow>> LoadStockAsync(CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await GoldInventoryService.BuildStockRowsAsync(context, null, ct);
    }

    private async Task<(int Count, decimal Iqd, decimal Usd)> LoadCreditAsync(CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var count = await context.GoldCustomers.AsNoTracking()
            .CountAsync(c => c.CreditBalanceIqd > 0 || c.CreditBalanceUsd > 0, ct);
        var iqd = await context.GoldCustomers.AsNoTracking()
            .Where(c => c.CreditBalanceIqd > 0)
            .SumAsync(c => (decimal?)c.CreditBalanceIqd, ct) ?? 0;
        var usd = await context.GoldCustomers.AsNoTracking()
            .Where(c => c.CreditBalanceUsd > 0)
            .SumAsync(c => (decimal?)c.CreditBalanceUsd, ct) ?? 0;
        return (count, iqd, usd);
    }

    private async Task<Core.Entities.Gold.GoldSettings?> LoadSettingsAsync(CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.GoldSettings.AsNoTracking().FirstOrDefaultAsync(ct);
    }

    private async Task<(decimal Iqd, decimal Usd, bool HasAny)> SumTodayExpensesAsync(
        DateTime today, DateTime tomorrow, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var iqd = await context.GoldExpenses.AsNoTracking()
            .Where(e => e.ExpenseDate >= today && e.ExpenseDate < tomorrow && e.Currency == GoldCurrency.IQD)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0;
        var usd = await context.GoldExpenses.AsNoTracking()
            .Where(e => e.ExpenseDate >= today && e.ExpenseDate < tomorrow && e.Currency == GoldCurrency.USD)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0;
        var hasAny = iqd > 0 || usd > 0 || await context.GoldExpenses.AsNoTracking()
            .AnyAsync(e => e.ExpenseDate >= today && e.ExpenseDate < tomorrow, ct);
        return (iqd, usd, hasAny);
    }

    private async Task<decimal?> LoadLatestFxAsync(CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.GoldFxRates.AsNoTracking()
            .OrderByDescending(r => r.RateDate)
            .ThenByDescending(r => r.Id)
            .Select(r => (decimal?)r.UsdToIqd)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<List<GoldInvoiceListItem>> LoadRecentInvoicesAsync(CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var recent = await context.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Where(i => i.Status != GoldInvoiceStatus.Cancelled)
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Take(10)
            .ToListAsync(ct);
        return recent.Select(GoldCurrencyHelper.ToListItem).ToList();
    }

    private async Task<List<DailySalesPoint>> LoadSalesLast30DaysAsync(
        DateTime from, DateTime tomorrow, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var salesRaw = await context.GoldInvoices.AsNoTracking()
            .Where(i => i.InvoiceType == GoldInvoiceType.Sale &&
                        i.Status != GoldInvoiceStatus.Cancelled &&
                        i.InvoiceDate >= from &&
                        i.InvoiceDate < tomorrow)
            .Select(i => new { i.InvoiceDate, i.TotalAmountIqd })
            .ToListAsync(ct);

        var salesByDay = salesRaw
            .GroupBy(i => i.InvoiceDate.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalAmountIqd));

        return Enumerable.Range(0, 30)
            .Select(offset =>
            {
                var d = from.AddDays(offset);
                return new DailySalesPoint
                {
                    Date = d,
                    Amount = salesByDay.TryGetValue(d, out var amount) ? amount : 0
                };
            })
            .ToList();
    }

    private async Task<List<GoldAlertItem>> LoadUnreadAlertsAsync(CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.GoldNotifications.AsNoTracking()
            .Where(n => !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(8)
            .Select(n => new GoldAlertItem
            {
                NotificationId = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                CreatedAt = n.CreatedAt,
                IsRead = n.IsRead,
                RelatedEntity = n.RelatedEntity,
                RelatedId = n.RelatedId
            })
            .ToListAsync(ct);
    }

    private async Task<int> CountOverdueAsync(DateTime overdueCutoff, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.GoldInvoices.AsNoTracking()
            .Where(i => i.RemainingAmount > 0 &&
                        i.Status != GoldInvoiceStatus.Cancelled &&
                        i.InvoiceDate <= overdueCutoff)
            .Select(i => i.CustomerId)
            .Distinct()
            .CountAsync(ct);
    }

    private async Task<bool> AnyPricesTodayAsync(DateTime today, DateTime tomorrow, CancellationToken ct)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.GoldMithqalPrices.AsNoTracking()
            .AnyAsync(p => p.PriceDate >= today && p.PriceDate < tomorrow, ct);
    }
}
