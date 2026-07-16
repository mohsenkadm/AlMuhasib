using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

public partial class ProductPriceEditRow : ObservableObject
{
    public event Action<ProductPriceEditRow>? Changed;

    public int Id { get; set; }

    public ObservableCollection<Product> AvailableProducts { get; set; } = [];
    public ObservableCollection<PricingType> AvailablePricingTypes { get; set; } = [];

    [ObservableProperty] private Product? _selectedProduct;
    [ObservableProperty] private PricingType? _selectedPricingType;
    [ObservableProperty] private decimal _salePrice;
    [ObservableProperty] private decimal _purchasePrice;

    public string ProductName => SelectedProduct?.Name ?? string.Empty;
    public string PricingTypeName => SelectedPricingType?.Name ?? string.Empty;

    partial void OnSelectedProductChanged(Product? value)
    {
        OnPropertyChanged(nameof(ProductName));
        Changed?.Invoke(this);
    }

    partial void OnSelectedPricingTypeChanged(PricingType? value)
    {
        OnPropertyChanged(nameof(PricingTypeName));
        Changed?.Invoke(this);
    }

    partial void OnSalePriceChanged(decimal value) => Changed?.Invoke(this);
    partial void OnPurchasePriceChanged(decimal value) => Changed?.Invoke(this);

    public ProductPrice ToEntity() => new()
    {
        Id = Id,
        ProductId = SelectedProduct?.Id ?? 0,
        PricingTypeId = SelectedPricingType?.Id ?? 0,
        SalePrice = SalePrice,
        PurchasePrice = PurchasePrice
    };

    public static ProductPriceEditRow FromEntity(
        ProductPrice entity,
        ObservableCollection<Product> products,
        ObservableCollection<PricingType> pricingTypes)
    {
        var row = new ProductPriceEditRow
        {
            Id = entity.Id,
            AvailableProducts = products,
            AvailablePricingTypes = pricingTypes,
            SalePrice = entity.SalePrice,
            PurchasePrice = entity.PurchasePrice,
            SelectedProduct = products.FirstOrDefault(p => p.Id == entity.ProductId) ?? entity.Product,
            SelectedPricingType = pricingTypes.FirstOrDefault(t => t.Id == entity.PricingTypeId) ?? entity.PricingType
        };
        return row;
    }
}
