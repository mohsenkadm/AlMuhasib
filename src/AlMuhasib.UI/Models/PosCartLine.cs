using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

public partial class PosCartLine : ObservableObject
{
    public int ProductId { get; init; }
    public int? PricingTypeId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public Product? SourceProduct { get; init; }

    [ObservableProperty]
    private decimal _quantity = 1m;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private decimal _discountAmount;

    [ObservableProperty]
    private decimal _lineTotal;

    public bool ProductDiscountFeatureEnabled { get; set; }

    partial void OnQuantityChanged(decimal value)
    {
        RefreshProductDiscount();
        Recalc();
    }

    partial void OnUnitPriceChanged(decimal value)
    {
        RefreshProductDiscount();
        Recalc();
    }

    partial void OnDiscountAmountChanged(decimal value) => Recalc();

    public void RefreshProductDiscount()
    {
        if (!ProductDiscountFeatureEnabled || SourceProduct is null)
        {
            DiscountAmount = 0m;
            return;
        }

        DiscountAmount = ProductDiscountHelper.CalculateLineDiscount(SourceProduct, Quantity, UnitPrice);
    }

    private void Recalc() =>
        LineTotal = ProductDiscountHelper.CalculateLineTotal(Quantity, UnitPrice, DiscountAmount);

    public static PosCartLine FromProduct(
        Product product,
        decimal unitPrice,
        decimal quantity = 1m,
        int? pricingTypeId = null,
        bool productDiscountEnabled = false)
    {
        var line = new PosCartLine
        {
            ProductId = product.Id,
            PricingTypeId = pricingTypeId,
            ProductName = product.Name,
            SourceProduct = product,
            ProductDiscountFeatureEnabled = productDiscountEnabled,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
        line.RefreshProductDiscount();
        line.Recalc();
        return line;
    }
}
