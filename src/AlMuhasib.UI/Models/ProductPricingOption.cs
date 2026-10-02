using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

/// <summary>خيار تسعير لمنتج (نوع التسعير + السعر المعروض).</summary>
public partial class ProductPricingOption : ObservableObject
{
    public int PricingTypeId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal SalePrice { get; init; }
    public decimal SalePriceUsd { get; init; }
    public decimal PurchasePrice { get; init; }
    public decimal PurchasePriceUsd { get; init; }
    public bool IsDefault { get; init; }
    public AccountingCurrency Currency { get; init; } = AccountingCurrency.IQD;

    public string Display =>
        $"{Name} — {AccountingCurrencyHelper.Format(Price, Currency)}";
}
