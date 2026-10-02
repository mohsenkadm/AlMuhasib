using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
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

    /// <summary>
    /// بنود المشتريات لمتوسط التكلفة بالدينار.
    /// مشتريات USD تُحوَّل عبر FxRate اللقطة على الفاتورة داخل COGS فقط —
    /// مبلغ الفاتورة الأصلي يبقى بالدولار.
    /// </summary>
    public static async Task<IReadOnlyDictionary<int, List<InvoiceItem>>> GetPurchaseItemsByProductAsync(
        AppDbContext context,
        IEnumerable<int>? productIds = null)
    {
        var query = context.InvoiceItems
            .Include(ii => ii.Invoice)
            .Where(ii => ii.ProductId != null
                         && (ii.Invoice.InvoiceType == InvoiceType.Purchase
                             || ii.Invoice.InvoiceType == InvoiceType.PurchaseReturn));

        if (productIds is not null)
        {
            var ids = productIds.ToList();
            if (ids.Count == 0)
                return new Dictionary<int, List<InvoiceItem>>();
            query = query.Where(ii => ids.Contains(ii.ProductId!.Value));
        }

        var items = await query.ToListAsync();
        // مرتجع المشتريات يُحسب بكمية وقيمة سالبة في متوسط التكلفة
        return items
            .GroupBy(ii => ii.ProductId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.Select(ToSignedPurchaseItemInIqd).ToList());
    }

    /// <summary>
    /// تكلفة البضاعة المباعة للفترة [fromInclusive, toExclusive) بمتوسط التكلفة.
    /// </summary>
    public static async Task<decimal> CalculateCogsAsync(
        AppDbContext context,
        DateTime? fromInclusive,
        DateTime? toExclusive)
    {
        var soldItemsQuery = context.InvoiceItems
            .Include(ii => ii.Invoice)
            .Where(ii => ii.ProductId != null
                         && ii.Invoice != null
                         && (ii.Invoice.InvoiceType == InvoiceType.Sale
                             || ii.Invoice.InvoiceType == InvoiceType.Installment
                             || ii.Invoice.InvoiceType == InvoiceType.SaleReturn));

        if (fromInclusive.HasValue)
            soldItemsQuery = soldItemsQuery.Where(ii => ii.Invoice!.Date >= fromInclusive.Value);
        if (toExclusive.HasValue)
            soldItemsQuery = soldItemsQuery.Where(ii => ii.Invoice!.Date < toExclusive.Value);

        var soldItems = await soldItemsQuery.ToListAsync();
        if (soldItems.Count == 0)
            return 0;

        var productIds = soldItems.Select(ii => ii.ProductId!.Value).Distinct().ToList();
        var stocks = await context.WarehouseStocks
            .Where(ws => productIds.Contains(ws.ProductId))
            .ToListAsync();

        var purchasesByProduct = await GetPurchaseItemsByProductAsync(context, productIds);
        if (toExclusive.HasValue)
        {
            purchasesByProduct = purchasesByProduct.ToDictionary(
                kv => kv.Key,
                kv => kv.Value
                    .Where(ii => ii.Invoice == null || ii.Invoice.Date < toExclusive.Value)
                    .ToList());
        }

        decimal cogs = 0;
        foreach (var sold in soldItems)
        {
            var productId = sold.ProductId!.Value;
            var productPurchases = purchasesByProduct.GetValueOrDefault(productId) ?? [];
            var avgCost = ComputeAverageUnitCostForProduct(productPurchases, stocks, productId);
            var qty = sold.Invoice!.InvoiceType == InvoiceType.SaleReturn
                ? -Math.Abs(sold.Quantity)
                : sold.Quantity;
            cogs += Math.Round(qty * avgCost, 0);
        }

        return cogs;
    }

    /// <summary>تحويل بند شراء إلى وحدة تكلفة بالدينار (للمتوسط فقط).</summary>
    public static InvoiceItem ToSignedPurchaseItemInIqd(InvoiceItem item)
    {
        var isReturn = item.Invoice?.InvoiceType == InvoiceType.PurchaseReturn;
        var isUsd = item.Invoice?.Currency == AccountingCurrency.USD;

        if (!isReturn && !isUsd)
            return item;

        var qty = isReturn ? -Math.Abs(item.Quantity) : item.Quantity;
        var total = isReturn ? -Math.Abs(item.TotalPrice) : item.TotalPrice;
        var unit = item.UnitPrice;

        if (isUsd)
        {
            var fx = item.Invoice!.FxRate;
            AccountingCurrencyRules.EnsureValidFxRate(AccountingCurrency.USD, fx, "تكلفة مخزون من فاتورة دولار");
            total = AccountingCurrencyHelper.RoundIqd(total * fx);
            unit = AccountingCurrencyHelper.RoundIqd(unit * fx);
        }

        return new InvoiceItem
        {
            ProductId = item.ProductId,
            Quantity = qty,
            TotalPrice = total,
            UnitPrice = unit,
            Invoice = item.Invoice
        };
    }
}
