using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.Models;

/// <summary>سطر اختيار كمية لقياس واحد داخل دايلوك الألبسة.</summary>
public partial class SizeQuantityChoice : ObservableObject
{
    public int ProductSizeId { get; init; }
    public string SizeName { get; init; } = string.Empty;

    [ObservableProperty]
    private decimal _availableStock;

    [ObservableProperty]
    private decimal _quantity;

    [RelayCommand]
    private void Increase() => Quantity += 1;

    [RelayCommand]
    private void Decrease()
    {
        if (Quantity > 0)
            Quantity -= 1;
    }
}

/// <summary>نتيجة تأكيد دايلوك القياس والكمية.</summary>
public sealed class SizeQuantitySelection
{
    public required Product Product { get; init; }
    public required IReadOnlyList<(int SizeId, string SizeName, decimal Quantity)> Lines { get; init; }
    public decimal UnitPrice { get; init; }
    public int? PricingTypeId { get; init; }
    public string? PricingTypeName { get; init; }
}

public partial class ProductSizeQuantityDialogViewModel : ObservableObject
{
    public ObservableCollection<SizeQuantityChoice> Sizes { get; } = [];

    [ObservableProperty]
    private string _productName = string.Empty;

    [ObservableProperty]
    private string _modeHint = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _showStock;

    public Product? Product { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int? PricingTypeId { get; private set; }
    public string? PricingTypeName { get; private set; }

    public decimal TotalQuantity => Sizes.Sum(s => s.Quantity);

    public void Load(
        Product product,
        IReadOnlyList<ProductSize> sizes,
        IReadOnlyDictionary<int, decimal>? stockBySizeId,
        bool showStock,
        string modeHint,
        decimal unitPrice = 0,
        int? pricingTypeId = null,
        string? pricingTypeName = null,
        IReadOnlyDictionary<int, decimal>? seedQuantities = null)
    {
        Product = product;
        ProductName = product.Name;
        ModeHint = modeHint;
        ShowStock = showStock;
        UnitPrice = unitPrice;
        PricingTypeId = pricingTypeId;
        PricingTypeName = pricingTypeName;
        ErrorMessage = string.Empty;
        Sizes.Clear();

        foreach (var size in sizes)
        {
            var choice = new SizeQuantityChoice
            {
                ProductSizeId = size.Id,
                SizeName = size.SizeName,
                AvailableStock = stockBySizeId?.GetValueOrDefault(size.Id) ?? 0,
                Quantity = seedQuantities?.GetValueOrDefault(size.Id) ?? 0
            };
            choice.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SizeQuantityChoice.Quantity))
                    OnPropertyChanged(nameof(TotalQuantity));
            };
            Sizes.Add(choice);
        }

        OnPropertyChanged(nameof(TotalQuantity));
    }

    public SizeQuantitySelection? TryBuildSelection()
    {
        ErrorMessage = string.Empty;
        if (Product is null)
        {
            ErrorMessage = "المنتج غير محدد";
            return null;
        }

        var lines = Sizes
            .Where(s => s.Quantity > 0)
            .Select(s => (s.ProductSizeId, s.SizeName, s.Quantity))
            .ToList();

        if (lines.Count == 0)
        {
            ErrorMessage = "أدخل كمية لقياس واحد على الأقل";
            return null;
        }

        return new SizeQuantitySelection
        {
            Product = Product,
            Lines = lines,
            UnitPrice = UnitPrice,
            PricingTypeId = PricingTypeId,
            PricingTypeName = PricingTypeName
        };
    }
}
