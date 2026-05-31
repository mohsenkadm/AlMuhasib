using AlMuhasib.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

public partial class PosCartLine : ObservableObject
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;

    [ObservableProperty]
    private decimal _quantity = 1m;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private decimal _lineTotal;

    partial void OnQuantityChanged(decimal value) => Recalc();
    partial void OnUnitPriceChanged(decimal value) => Recalc();

    private void Recalc() => LineTotal = Quantity * UnitPrice;

    public static PosCartLine FromProduct(Product product, decimal unitPrice, decimal quantity = 1m) =>
        new()
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = quantity,
            UnitPrice = unitPrice,
            LineTotal = quantity * unitPrice
        };
}
