using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Services;

namespace AlMuhasib.UI.Helpers;

/// <summary>
/// تكلفة الوحدة لبند فاتورة بنفس عملة المستند — بدون خلط دينار/دولار.
/// </summary>
public static class InvoiceDocumentCostResolver
{
    public static async Task<Dictionary<int, decimal>> ResolveProductCostsAsync(
        IUnitOfWork unitOfWork,
        IProductPriceService? productPriceService,
        bool pricingEnabled,
        IReadOnlyList<int> productIds,
        AccountingCurrency currency,
        IReadOnlyDictionary<int, int?>? pricingTypeByProduct = null)
    {
        var result = new Dictionary<int, decimal>();
        if (productIds.Count == 0)
            return result;

        IReadOnlyList<ProductPrice> allPrices = [];
        if (pricingEnabled && productPriceService is not null)
            allPrices = await productPriceService.GetByProductIdsAsync(productIds);

        var pricesByProduct = allPrices
            .GroupBy(p => p.ProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var allItems = (await unitOfWork.InvoiceItems.FindAsync(i =>
            i.ProductId != null && productIds.Contains(i.ProductId.Value))).ToList();
        var purchaseInvoiceIds = (await unitOfWork.Invoices.FindAsync(i =>
                i.InvoiceType == InvoiceType.Purchase && i.Currency == currency))
            .Select(i => i.Id)
            .ToHashSet();
        var purchaseItems = allItems
            .Where(i => i.ProductId is not null && purchaseInvoiceIds.Contains(i.InvoiceId))
            .ToList();

        // رصيد افتتاحي UnitCost بالدينار فقط — يُستخدم لمستندات الدينار فقط.
        List<WarehouseStock> stocks = [];
        if (currency == AccountingCurrency.IQD)
            stocks = (await unitOfWork.WarehouseStocks.FindAsync(s => productIds.Contains(s.ProductId))).ToList();

        foreach (var productId in productIds)
        {
            int? preferredTypeId = null;
            if (pricingTypeByProduct is not null)
                pricingTypeByProduct.TryGetValue(productId, out preferredTypeId);

            var catalogCost = ResolveCatalogPurchaseCost(
                pricesByProduct.GetValueOrDefault(productId),
                currency,
                preferredTypeId);
            if (catalogCost > 0)
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
                result[productId] = AccountingCurrencyHelper.NormalizeAmount(lastPurchase.UnitPrice, currency);
                continue;
            }

            var productPurchases = purchaseItems.Where(i => i.ProductId == productId).ToList();
            if (productPurchases.Count > 0 || currency == AccountingCurrency.IQD)
            {
                result[productId] = AccountingCurrencyHelper.NormalizeAmount(
                    ProductCostHelper.ComputeAverageUnitCostForProduct(productPurchases, stocks, productId),
                    currency);
                continue;
            }

            // فاتورة دولار بدون سعر شراء $ ولا مشتريات $ — لا تُقارن بتكلفة الدينار.
            result[productId] = 0m;
        }

        return result;
    }

    /// <summary>تكلفة شراء من كتالوج الأسعار بنفس عملة المستند ونوع التسعير إن وُجد.</summary>
    public static decimal ResolveCatalogPurchaseCost(
        IReadOnlyList<ProductPrice>? prices,
        AccountingCurrency currency,
        int? pricingTypeId = null)
    {
        if (prices is null || prices.Count == 0)
            return 0m;

        if (pricingTypeId is int typeId)
        {
            var matched = prices.FirstOrDefault(p => p.PricingTypeId == typeId);
            if (matched is not null)
            {
                var cost = ProductListPriceHelper.ResolveListPrice(matched, currency, isPurchase: true);
                if (cost > 0)
                    return cost;
            }
        }

        var preferred = prices.FirstOrDefault(p => p.PricingType?.IsDefault == true) ?? prices[0];
        var preferredCost = ProductListPriceHelper.ResolveListPrice(preferred, currency, isPurchase: true);
        if (preferredCost > 0)
            return preferredCost;

        foreach (var price in prices)
        {
            var cost = ProductListPriceHelper.ResolveListPrice(price, currency, isPurchase: true);
            if (cost > 0)
                return cost;
        }

        return 0m;
    }
}
