using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class LocalQueryService : ILocalQueryService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IReportService _reportService;

    public LocalQueryService(IDbContextFactory<AppDbContext> contextFactory, IReportService reportService)
    {
        _contextFactory = contextFactory;
        _reportService = reportService;
    }

    public IReadOnlyList<LocalQueryDefinition> GetAvailableQueries() =>
    [
        new() { Key = "overdue", Question = "من المتأخرون؟", Icon = "ClockAlert" },
        new() { Key = "profit_month", Question = "ما ربحي هذا الشهر؟", Icon = "ChartLine" },
        new() { Key = "low_stock", Question = "أقل 10 منتجات مخزوناً؟", Icon = "PackageVariant" },
        new() { Key = "due_today", Question = "أقساط مستحقة اليوم؟", Icon = "CalendarToday" }
    ];

    public async Task<LocalQueryResult> ExecuteAsync(string queryKey, CancellationToken cancellationToken = default)
    {
        return queryKey switch
        {
            "overdue" => await GetOverdueAsync(cancellationToken),
            "profit_month" => await GetProfitMonthAsync(cancellationToken),
            "low_stock" => await GetLowStockAsync(cancellationToken),
            "due_today" => await GetDueTodayAsync(cancellationToken),
            _ => new LocalQueryResult { Title = "—", Summary = "استعلام غير معروف" }
        };
    }

    private async Task<LocalQueryResult> GetOverdueAsync(CancellationToken ct)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var rows = await ctx.Installments.AsNoTracking()
            .Include(i => i.InstallmentPlan).ThenInclude(p => p!.Customer)
            .Where(i => i.Status == InstallmentStatus.Overdue && i.RemainingAmount > 0)
            .OrderByDescending(i => i.RemainingAmount)
            .Take(15)
            .ToListAsync(ct);

        return new LocalQueryResult
        {
            Title = "المتأخرون",
            Summary = $"{rows.Count} قسط متأخر",
            Lines = rows.Select(i =>
                $"{i.InstallmentPlan!.Customer!.Name} — {i.RemainingAmount:N0} د.ع — {i.DueDate:yyyy/MM/dd}").ToList()
        };
    }

    private async Task<LocalQueryResult> GetProfitMonthAsync(CancellationToken ct)
    {
        var from = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var to = DateTime.Today;
        var profit = await _reportService.GetProfitReportAsync(from, to);
        return new LocalQueryResult
        {
            Title = "ربح الشهر",
            Summary = $"صافي الربح: {profit.NetProfit:N0} د.ع",
            Lines =
            [
                $"إجمالي المبيعات: {profit.TotalSales:N0} د.ع",
                $"إجمالي المشتريات: {profit.TotalPurchases:N0} د.ع",
                $"المصروفات: {profit.TotalExpenses:N0} د.ع"
            ]
        };
    }

    private async Task<LocalQueryResult> GetLowStockAsync(CancellationToken ct)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var rows = await ctx.WarehouseStocks.AsNoTracking()
            .Include(ws => ws.Product)
            .GroupBy(ws => new { ws.ProductId, ws.Product!.Name })
            .Select(g => new { g.Key.Name, Qty = g.Sum(x => x.Quantity) })
            .Where(x => x.Qty >= 0)
            .OrderBy(x => x.Qty)
            .Take(10)
            .ToListAsync(ct);

        return new LocalQueryResult
        {
            Title = "أقل مخزوناً",
            Summary = $"{rows.Count} منتج",
            Lines = rows.Select(r => $"{r.Name} — {r.Qty:N0}").ToList()
        };
    }

    private async Task<LocalQueryResult> GetDueTodayAsync(CancellationToken ct)
    {
        var today = DateTime.Today;
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
        var rows = await ctx.Installments.AsNoTracking()
            .Include(i => i.InstallmentPlan).ThenInclude(p => p!.Customer)
            .Where(i => i.DueDate.Date == today && i.RemainingAmount > 0 && i.Status != InstallmentStatus.Paid)
            .ToListAsync(ct);

        return new LocalQueryResult
        {
            Title = "مستحق اليوم",
            Summary = $"{rows.Count} قسط — {rows.Sum(r => r.RemainingAmount):N0} د.ع",
            Lines = rows.Select(i =>
                $"{i.InstallmentPlan!.Customer!.Name} — {i.RemainingAmount:N0} د.ع").ToList()
        };
    }
}
