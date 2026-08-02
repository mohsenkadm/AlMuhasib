using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
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
    public string SalePriceText => $"{SalePrice:N0}";
    public string PurchasePriceText => $"{PurchasePrice:N0}";
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
