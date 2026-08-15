using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Helpers;

/// <summary>
/// تحميل أنواع التسعير وتطبيق نوع تسعير جماعي على بنود الفاتورة.
/// </summary>
public static class InvoiceBulkPricingHelper
{
    public static async Task LoadBulkPricingTypesAsync(
        IPricingTypeService? pricingTypeService,
        ObservableCollection<PricingType> target)
    {
        target.Clear();
        if (pricingTypeService is null)
            return;

        var types = await pricingTypeService.GetActiveAsync();
        foreach (var type in types.OrderByDescending(t => t.IsDefault).ThenBy(t => t.Name))
            target.Add(type);
    }

    public static List<ProductPricingOption> ToOptions(
        IEnumerable<ProductPrice> prices,
        bool usePurchasePrice)
    {
        return prices
            .Select(p => new ProductPricingOption
            {
                PricingTypeId = p.PricingTypeId,
                Name = p.PricingType?.Name ?? $"نوع {p.PricingTypeId}",
                Price = usePurchasePrice ? p.PurchasePrice : p.SalePrice,
                IsDefault = p.PricingType?.IsDefault == true
            })
            .ToList();
    }

    public static ProductPricingOption? ResolvePreferredOption(
        IReadOnlyList<ProductPricingOption> options,
        int? rowPricingTypeId,
        int? bulkPricingTypeId)
    {
        if (options.Count == 0)
            return null;

        if (bulkPricingTypeId is int bulkId)
        {
            var bulkMatch = options.FirstOrDefault(o => o.PricingTypeId == bulkId);
            if (bulkMatch is not null)
                return bulkMatch;
        }

        return options.FirstOrDefault(o => o.PricingTypeId == rowPricingTypeId)
               ?? options.FirstOrDefault(o => o.IsDefault)
               ?? options[0];
    }

    public static void ApplyPricingTypeToRows(
        IEnumerable<InvoiceItemRow> rows,
        int pricingTypeId)
    {
        foreach (var row in rows)
        {
            if (row.ProductId is null or <= 0)
                continue;

            var match = row.AvailablePricingOptions
                .FirstOrDefault(o => o.PricingTypeId == pricingTypeId);
            if (match is null)
                continue;

            row.SelectedPricingOption = match;
        }
    }
}
