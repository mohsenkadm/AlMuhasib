using AlMuhasib.Core;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
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
        bool discountEnabled,
        AccountingCurrency currency = AccountingCurrency.IQD)
    {
        var rows = items
            .Where(i => i.ProductId is > 0 && !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity > 0 && !i.IsOfferGift)
            .ToList();
        if (rows.Count == 0)
            return [];

        var productIds = rows.Select(r => r.ProductId!.Value).Distinct().ToList();
        var pricingByProduct = rows
            .GroupBy(r => r.ProductId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(r => r.PricingTypeId).FirstOrDefault(id => id is > 0));

        var costs = await InvoiceDocumentCostResolver.ResolveProductCostsAsync(
            _unitOfWork,
            _productPriceService,
            _pricingEnabled,
            productIds,
            currency,
            pricingByProduct);

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
                    AccountingCurrencyHelper.NormalizeAmount(netUnitPrice, currency),
                    cost,
                    row.DiscountPercent));
            }
        }

        return result;
    }

    public static string FormatBelowCostMessage(
        IReadOnlyList<BelowCostLine> lines,
        AccountingCurrency currency = AccountingCurrency.IQD)
    {
        var body = string.Join("\n", lines.Select(l =>
            $"• {l.ItemName}: سعر البيع {AccountingCurrencyHelper.Format(l.UnitPrice, currency)} < التكلفة {AccountingCurrencyHelper.Format(l.UnitCost, currency)}"));
        return $"المواد التالية تُباع بأقل من التكلفة:\n{body}";
    }
}
