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
    public string Name => Product.Name;
    public string? Barcode => Product.Barcode;
    public string? Description => Product.Description;
    public string CategoryName => Product.Category?.Name ?? "—";
    public ObservableCollection<ProductPriceCardLine> Prices { get; } = [];
    public bool HasPrices => Prices.Count > 0;
}
