using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

/// <summary>حساب متوسط كلفة المنتج من المشتريات والرصيد الافتتاحي.</summary>
public static class ProductCostHelper
{
    public static decimal ComputeAverageUnitCost(
        IEnumerable<InvoiceItem> purchaseItems,
        decimal openingQuantity,
        decimal openingUnitCost)
    {
        var items = purchaseItems.ToList();
        var purchaseQty = items.Sum(i => i.Quantity);
        var purchaseCost = items.Sum(i => i.TotalPrice);
        var openingQty = Math.Max(0, openingQuantity);
        var totalQty = purchaseQty + openingQty;

        if (totalQty <= 0)
            return openingUnitCost;

        var openingCost = openingQty * openingUnitCost;
        return (purchaseCost + openingCost) / totalQty;
    }

    public static decimal ComputeAverageUnitCostForProduct(
        IEnumerable<InvoiceItem> purchaseItems,
        IEnumerable<WarehouseStock> stocks,
        int productId)
    {
        var productStocks = stocks.Where(s => s.ProductId == productId).ToList();
        var openingQty = productStocks.Sum(s => s.OpeningQuantity);
        var openingCost = productStocks.Sum(s => s.OpeningQuantity * s.UnitCost);
        var items = purchaseItems.ToList();
        var purchaseQty = items.Sum(i => i.Quantity);
        var purchaseCost = items.Sum(i => i.TotalPrice);
        var totalQty = purchaseQty + openingQty;

        if (totalQty <= 0)
            return openingQty > 0 ? openingCost / openingQty : 0;

        return (purchaseCost + openingCost) / totalQty;
    }

    public static decimal ComputeInventoryValue(
        IEnumerable<WarehouseStock> stocks,
        IReadOnlyDictionary<int, List<InvoiceItem>> purchasesByProduct)
    {
        decimal total = 0;
        foreach (var s in stocks.Where(ws => ws.Quantity > 0))
        {
            var purchases = purchasesByProduct.GetValueOrDefault(s.ProductId) ?? [];
            var openingQty = s.OpeningQuantity;
            var avg = ComputeAverageUnitCost(purchases, openingQty, s.UnitCost);
            total += Math.Round(s.Quantity * avg, 0);
        }

        return total;
    }

    public static async Task<decimal> GetProfitOpeningBalanceAsync(AppDbContext context, DateTime? asOf = null)
    {
        var q = context.CapitalEntries
            .Where(c => c.Type == CapitalEntryType.ProfitOpeningBalance);
        if (asOf.HasValue)
            q = q.Where(c => c.Date <= asOf.Value);
        return await q.SumAsync(c => (decimal?)c.Amount) ?? 0;
    }

    public static async Task<IReadOnlyDictionary<int, List<InvoiceItem>>> GetPurchaseItemsByProductAsync(
        AppDbContext context,
        IEnumerable<int>? productIds = null)
    {
        var query = context.InvoiceItems
            .Include(ii => ii.Invoice)
            .Where(ii => ii.ProductId != null && ii.Invoice!.InvoiceType == InvoiceType.Purchase);

        if (productIds is not null)
        {
            var ids = productIds.ToList();
            if (ids.Count == 0)
                return new Dictionary<int, List<InvoiceItem>>();
            query = query.Where(ii => ids.Contains(ii.ProductId!.Value));
        }

        var items = await query.ToListAsync();
        return items
            .GroupBy(ii => ii.ProductId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}
