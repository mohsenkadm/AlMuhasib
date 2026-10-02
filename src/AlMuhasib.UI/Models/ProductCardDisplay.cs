using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

public partial class ProductPriceCardLine : ObservableObject
{
    public int ProductPriceId { get; init; }
    public int ProductId { get; init; }
    public int PricingTypeId { get; init; }
    public string PricingTypeName { get; init; } = string.Empty;
    public decimal SalePrice { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePriceUsd { get; set; }
    public decimal PurchasePriceUsd { get; set; }
    public bool ShowUsd { get; set; }

    public string SalePriceText => ShowUsd
        ? FormatDual(SalePrice, SalePriceUsd)
        : $"{SalePrice:N0}";

    public string PurchasePriceText => ShowUsd
        ? FormatDual(PurchasePrice, PurchasePriceUsd)
        : $"{PurchasePrice:N0}";

    public string SalePriceIqdText => $"{SalePrice:N0} {AccountingCurrencyHelper.IqdLabel}";
    public string PurchasePriceIqdText => $"{PurchasePrice:N0} {AccountingCurrencyHelper.IqdLabel}";
    public string SalePriceUsdText => $"{SalePriceUsd:N2} {AccountingCurrencyHelper.UsdLabel}";
    public string PurchasePriceUsdText => $"{PurchasePriceUsd:N2} {AccountingCurrencyHelper.UsdLabel}";

    private static string FormatDual(decimal iqd, decimal usd)
    {
        if (iqd > 0 && usd > 0)
            return $"{iqd:N0} د.ع · {usd:N2} $";
        if (usd > 0)
            return $"{usd:N2} $";
        return $"{iqd:N0} د.ع";
    }
}

public partial class ProductCardDisplay : ObservableObject
{
    public required Product Product { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ScientificName { get; init; }
    public string? UsageInstructions { get; init; }
    public string? Barcode { get; init; }
    public string? Description { get; init; }
    public string CategoryName { get; init; } = "—";
    public ObservableCollection<ProductPriceCardLine> Prices { get; } = [];
    public bool HasPrices => Prices.Count > 0;
    public bool HasScientificName => !string.IsNullOrWhiteSpace(ScientificName);
    public bool HasUsageInstructions => !string.IsNullOrWhiteSpace(UsageInstructions);
}
