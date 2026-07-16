using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.Infrastructure.Data.Hotel;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Hotel.Restaurant;

public sealed class RestaurantReportService : IRestaurantReportService
{
    private const string KitchenPurchaseExpenseType = "مشتريات مطعم";
    private readonly IDbContextFactory<HotelDbContext> _contextFactory;

    public RestaurantReportService(IDbContextFactory<HotelDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<RestaurantProfitSummary> GetProfitSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var end = to.Date.AddDays(1);
        var orders = await db.RestaurantOrders.AsNoTracking()
            .Where(o => o.OrderDate >= from.Date && o.OrderDate < end
                && (o.Status == RestaurantOrderStatus.Paid || o.Status == RestaurantOrderStatus.PostedToRoom))
            .ToListAsync(ct);

        var revenue = orders.Sum(o => o.TotalAmount);
        var cogs = orders.Sum(o => o.CogsAmount);
        var profit = revenue - cogs;
        var roomOrders = orders.Where(o => o.OrderType == RestaurantOrderType.RoomService).ToList();

        return new RestaurantProfitSummary
        {
            Revenue = revenue,
            Cogs = cogs,
            GrossProfit = profit,
            MarginPercent = revenue > 0 ? Math.Round(profit / revenue * 100, 1) : 0,
            OrderCount = orders.Count,
            AverageOrderValue = orders.Count > 0 ? Math.Round(revenue / orders.Count, 0) : 0,
            DiscountTotal = orders.Sum(o => o.DiscountAmount),
            RoomServiceRevenue = roomOrders.Sum(o => o.TotalAmount),
            RoomServiceOrderCount = roomOrders.Count
        };
    }

    public async Task<IReadOnlyList<RestaurantChannelSales>> GetSalesByChannelAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var end = to.Date.AddDays(1);
        var groups = await db.RestaurantOrders.AsNoTracking()
            .Where(o => o.OrderDate >= from.Date && o.OrderDate < end
                && (o.Status == RestaurantOrderStatus.Paid || o.Status == RestaurantOrderStatus.PostedToRoom))
            .GroupBy(o => o.OrderType)
            .Select(g => new { Type = g.Key, Revenue = g.Sum(o => o.TotalAmount), Count = g.Count() })
            .ToListAsync(ct);

        return groups.Select(g => new RestaurantChannelSales
        {
            OrderType = g.Type,
            Label = GetOrderTypeLabel(g.Type),
            Revenue = g.Revenue,
            OrderCount = g.Count
        }).OrderByDescending(x => x.Revenue).ToList();
    }

    public async Task<IReadOnlyList<RestaurantTopItem>> GetTopSellingItemsAsync(DateTime from, DateTime to, int limit = 10, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var end = to.Date.AddDays(1);
        var orderIds = await db.RestaurantOrders.AsNoTracking()
            .Where(o => o.OrderDate >= from.Date && o.OrderDate < end
                && (o.Status == RestaurantOrderStatus.Paid || o.Status == RestaurantOrderStatus.PostedToRoom))
            .Select(o => o.Id)
            .ToListAsync(ct);

        return await db.RestaurantOrderLines.AsNoTracking()
            .Where(l => orderIds.Contains(l.RestaurantOrderId))
            .GroupBy(l => l.ItemName)
            .Select(g => new RestaurantTopItem
            {
                ItemName = g.Key,
                QuantitySold = g.Sum(l => l.Quantity),
                Revenue = g.Sum(l => l.LineTotal)
            })
            .OrderByDescending(x => x.Revenue)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<RestaurantFinancialOverview> GetFinancialOverviewAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var profit = await GetProfitSummaryAsync(from, to, ct);
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var end = to.Date.AddDays(1);

        var kitchenPurchases = await db.HotelExpenses.AsNoTracking()
            .Where(e => e.ExpenseDate >= from.Date && e.ExpenseDate < end
                && e.ExpenseType.Name == KitchenPurchaseExpenseType)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0;

        return new RestaurantFinancialOverview
        {
            RestaurantRevenue = profit.Revenue,
            RestaurantCogs = profit.Cogs,
            RestaurantGrossProfit = profit.GrossProfit,
            KitchenPurchases = kitchenPurchases,
            HotelExpenses = kitchenPurchases,
            NetOperating = profit.GrossProfit - kitchenPurchases
        };
    }

    public async Task<IReadOnlyList<RestaurantDailySales>> GetDailySalesAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var end = to.Date.AddDays(1);
        var groups = await db.RestaurantOrders.AsNoTracking()
            .Where(o => o.OrderDate >= from.Date && o.OrderDate < end
                && (o.Status == RestaurantOrderStatus.Paid || o.Status == RestaurantOrderStatus.PostedToRoom))
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new RestaurantDailySales
            {
                Date = g.Key,
                Revenue = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        return groups;
    }

    public async Task<IReadOnlyList<RestaurantPaymentBreakdown>> GetPaymentBreakdownAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        var end = to.Date.AddDays(1);

        var paidOrderIds = await db.RestaurantOrders.AsNoTracking()
            .Where(o => o.OrderDate >= from.Date && o.OrderDate < end
                && (o.Status == RestaurantOrderStatus.Paid || o.Status == RestaurantOrderStatus.PostedToRoom))
            .Select(o => o.Id)
            .ToListAsync(ct);

        var groups = await db.RestaurantOrderPayments.AsNoTracking()
            .Where(p => paidOrderIds.Contains(p.RestaurantOrderId))
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new { Method = g.Key, Amount = g.Sum(p => p.Amount), Count = g.Count() })
            .ToListAsync(ct);

        return groups.Select(g => new RestaurantPaymentBreakdown
        {
            PaymentMethod = g.Method,
            Label = GetPaymentMethodLabel(g.Method),
            Amount = g.Amount,
            PaymentCount = g.Count
        }).OrderByDescending(x => x.Amount).ToList();
    }

    private static string GetOrderTypeLabel(RestaurantOrderType type) => type switch
    {
        RestaurantOrderType.DineIn => "صالة",
        RestaurantOrderType.Takeaway => "سفري",
        RestaurantOrderType.RoomService => "خدمة غرف",
        _ => type.ToString()
    };

    private static string GetPaymentMethodLabel(RestaurantPaymentMethod method) => method switch
    {
        RestaurantPaymentMethod.Cash => "نقداً",
        RestaurantPaymentMethod.Card => "بطاقة",
        RestaurantPaymentMethod.RoomCharge => "قيد على الغرفة",
        RestaurantPaymentMethod.Mixed => "مختلط",
        _ => method.ToString()
    };
}
