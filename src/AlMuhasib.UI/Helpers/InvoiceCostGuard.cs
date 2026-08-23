using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Services;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Helpers;

public sealed record BelowCostLine(string ItemName, decimal UnitPrice, decimal UnitCost, decimal DiscountPercent);

/// <summary>فحص البيع بأقل من التكلفة قبل حفظ الفاتورة.</summary>
public sealed class InvoiceCostGuard
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductPriceService? _productPriceService;
    private readonly bool _pricingEnabled;

    public InvoiceCostGuard(
        IUnitOfWork unitOfWork,
        IProductPriceService? productPriceService,
        bool pricingEnabled)
    {
        _unitOfWork = unitOfWork;
        _productPriceService = productPriceService;
        _pricingEnabled = pricingEnabled;
    }

    public async Task<IReadOnlyList<BelowCostLine>> FindBelowCostLinesAsync(
        IEnumerable<InvoiceItemRow> items,
        bool discountEnabled)
    {
        var rows = items
            .Where(i => i.ProductId is > 0 && !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity > 0 && !i.IsOfferGift)
            .ToList();
        if (rows.Count == 0)
            return [];

        var productIds = rows.Select(r => r.ProductId!.Value).Distinct().ToList();
        var costs = await ResolveCostsAsync(productIds);
        var result = new List<BelowCostLine>();

        foreach (var row in rows)
        {
            if (!costs.TryGetValue(row.ProductId!.Value, out var cost) || cost <= 0)
                continue;

            var baseQty = ProductDiscountHelper.ToBaseQuantity(row.Quantity, row.UnitConversionFactor);
            if (baseQty <= 0) continue;

            var gross = baseQty * row.UnitPrice;
            var discount = discountEnabled
                ? (row.DiscountPercent > 0
                    ? ProductDiscountHelper.CalculateDiscountFromPercent(gross, row.DiscountPercent)
                    : row.DiscountAmount)
                : 0m;
            var netUnitPrice = (gross - discount) / baseQty;

            if (netUnitPrice < cost)
            {
                result.Add(new BelowCostLine(
                    row.ItemName,
                    Math.Round(netUnitPrice, 0),
                    cost,
                    row.DiscountPercent));
            }
        }

        return result;
    }

    public static string FormatBelowCostMessage(IReadOnlyList<BelowCostLine> lines)
    {
        var body = string.Join("\n", lines.Select(l =>
            $"• {l.ItemName}: سعر البيع {l.UnitPrice:N0} د.ع < التكلفة {l.UnitCost:N0} د.ع"));
        return $"المواد التالية تُباع بأقل من التكلفة:\n{body}";
    }

    private async Task<Dictionary<int, decimal>> ResolveCostsAsync(IReadOnlyList<int> productIds)
    {
        var result = new Dictionary<int, decimal>();
        if (productIds.Count == 0)
            return result;

        var stocks = (await _unitOfWork.WarehouseStocks.FindAsync(s => productIds.Contains(s.ProductId))).ToList();
        var allItems = (await _unitOfWork.InvoiceItems.FindAsync(i =>
            i.ProductId != null && productIds.Contains(i.ProductId.Value))).ToList();
        var purchaseInvoiceIds = (await _unitOfWork.Invoices.FindAsync(i => i.InvoiceType == InvoiceType.Purchase))
            .Select(i => i.Id)
            .ToHashSet();
        var purchaseItems = allItems
            .Where(i => i.ProductId is not null && purchaseInvoiceIds.Contains(i.InvoiceId))
            .ToList();

        Dictionary<int, decimal>? catalogPurchase = null;
        if (_pricingEnabled && _productPriceService is not null)
        {
            var prices = await _productPriceService.GetByProductIdsAsync(productIds);
            catalogPurchase = prices
                .GroupBy(p => p.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var preferred = g.FirstOrDefault(p => p.PricingType?.IsDefault == true) ?? g.First();
                        return preferred.PurchasePrice;
                    });
        }

        foreach (var productId in productIds)
        {
            if (catalogPurchase is not null
                && catalogPurchase.TryGetValue(productId, out var catalogCost)
                && catalogCost > 0)
            {
                result[productId] = catalogCost;
                continue;
            }

            var lastPurchase = purchaseItems
                .Where(i => i.ProductId == productId && i.UnitPrice > 0)
                .OrderByDescending(i => i.Id)
                .FirstOrDefault();
            if (lastPurchase is not null)
            {
                result[productId] = lastPurchase.UnitPrice;
                continue;
            }

            result[productId] = Math.Round(
                ProductCostHelper.ComputeAverageUnitCostForProduct(purchaseItems, stocks, productId), 0);
        }

        return result;
    }
}
